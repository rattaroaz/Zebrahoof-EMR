using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace Zebrahoof_EMR.Services;

public sealed record LocalAiCatalogSnapshot(
    IReadOnlyList<LocalAiModelChoice> Models,
    IReadOnlyList<string> Families,
    DateTimeOffset? PulledAtUtc,
    bool FromLiveLibrary);

public sealed class LocalAiLibraryCacheFile
{
    public DateTimeOffset PulledAtUtc { get; set; }
    public string? SourceUrl { get; set; }
    public List<LocalAiModelChoice> Models { get; set; } = [];
}

/// <summary>
/// Live Ollama library catalog with a one-week disk cache. A pull is skipped
/// when one already succeeded inside the last seven days.
/// </summary>
public sealed class LocalAiLibraryCatalogService
{
    public const string HttpClientName = "OllamaLibrary";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly HttpClient _http;
    private readonly string _cachePath;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _snapshotLock = new();
    private LocalAiCatalogSnapshot _snapshot;

    public LocalAiLibraryCatalogService(
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment environment,
        ILogger<LocalAiLibraryCatalogService> logger)
        : this(
            httpClientFactory.CreateClient(HttpClientName),
            DefaultCachePath(environment.ContentRootPath),
            TimeProvider.System,
            logger)
    {
    }

    public LocalAiLibraryCatalogService(
        HttpClient httpClient,
        string cachePath,
        TimeProvider? timeProvider = null,
        ILogger? logger = null)
    {
        _http = httpClient;
        _cachePath = cachePath;
        _clock = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger.Instance;
        _snapshot = LoadCacheOrSeed();
    }

    private void ReplaceSnapshot(LocalAiCatalogSnapshot snapshot)
    {
        lock (_snapshotLock)
        {
            _snapshot = snapshot;
        }
    }

    public static string DefaultCachePath(string contentRoot) =>
        Path.Combine(contentRoot, "App_Data", "ai-engine", "library-catalog.json");

    public LocalAiCatalogSnapshot GetSnapshot()
    {
        lock (_snapshotLock)
        {
            return _snapshot;
        }
    }

    public IReadOnlyList<LocalAiModelChoice> Models => GetSnapshot().Models;

    public IReadOnlyList<string> Families => GetSnapshot().Families;

    public LocalAiModelChoice? Find(string? id) => LocalAiModels.Find(id, Models);

    /// <summary>
    /// Uses the cached library when a live pull happened within the last week.
    /// Otherwise fetches https://ollama.com/library and writes a new cache.
    /// </summary>
    public async Task<LocalAiCatalogSnapshot> EnsureFreshAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var current = GetSnapshot();
            if (LocalAiLibraryParser.IsCacheFresh(current.PulledAtUtc, _clock.GetUtcNow()))
            {
                return current;
            }

            await PullCoreAsync(cancellationToken);
            return GetSnapshot();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task PullCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.GetAsync(LocalAiLibraryParser.LibraryUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var listings = LocalAiLibraryParser.ParseLibraryHtml(html);
            if (listings.Count < 10)
            {
                _logger.LogWarning(
                    "Ollama library HTML parsed to {Count} models; keeping the previous catalog",
                    listings.Count);
                return;
            }

            var live = LocalAiLibraryParser.ToChoices(listings);
            var merged = LocalAiLibraryParser.MergeWithSeed(live);
            var pulledAt = _clock.GetUtcNow();
            WriteCache(new LocalAiLibraryCacheFile
            {
                PulledAtUtc = pulledAt,
                SourceUrl = LocalAiLibraryParser.LibraryUrl,
                Models = merged.ToList()
            });
            ReplaceSnapshot(new LocalAiCatalogSnapshot(
                merged,
                LocalAiLibraryParser.FamiliesOf(merged),
                pulledAt,
                FromLiveLibrary: true));
            _logger.LogInformation(
                "Pulled {Count} local AI models from the Ollama library",
                merged.Count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live Ollama library pull failed; using the cached or built-in catalog");
        }
    }

    private LocalAiCatalogSnapshot LoadCacheOrSeed()
    {
        var cached = TryReadCache();
        if (cached is { Models.Count: > 0 })
        {
            var models = LocalAiModels.OnlySupported(cached.Models);
            if (models.Count == 0)
            {
                return SeedSnapshot();
            }
            return new LocalAiCatalogSnapshot(
                models,
                LocalAiLibraryParser.FamiliesOf(models),
                cached.PulledAtUtc,
                FromLiveLibrary: true);
        }

        return SeedSnapshot();
    }

    private static LocalAiCatalogSnapshot SeedSnapshot() =>
        new(
            LocalAiModels.Catalog,
            LocalAiModels.Families,
            PulledAtUtc: null,
            FromLiveLibrary: false);

    private LocalAiLibraryCacheFile? TryReadCache()
    {
        try
        {
            if (!File.Exists(_cachePath))
            {
                return null;
            }

            var json = File.ReadAllText(_cachePath);
            return JsonSerializer.Deserialize<LocalAiLibraryCacheFile>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Could not read local AI library cache at {Path}", _cachePath);
            return null;
        }
    }

    private void WriteCache(LocalAiLibraryCacheFile file)
    {
        var directory = Path.GetDirectoryName(_cachePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(file, JsonOptions);
        var temp = _cachePath + ".tmp";
        File.WriteAllText(temp, json);
        File.Copy(temp, _cachePath, overwrite: true);
        TryDelete(temp);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }
}
