using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Zebrahoof_EMR.Logging;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Downloads, extracts, starts, and provisions a local Ollama engine plus a Qwen
/// model. All inference stays on loopback. This service is a singleton so install
/// progress is shared across circuits.
/// </summary>
public sealed class LocalAiEngineService : IDisposable
{
    public const string HttpClientName = "LocalAiInstaller";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly IOptionsMonitor<LocalAiOptions> _options;
    private readonly ILogger<LocalAiEngineService> _logger;
    private readonly SemaphoreSlim _opLock = new(1, 1);
    private readonly object _statusLock = new();
    private readonly HttpClient _probeClient;

    private LocalAiStatus _status;
    private Process? _serveProcess;
    private readonly ConcurrentQueue<string> _serveOutput = new();
    private string? _preferredModel;
    private CancellationTokenSource? _opCts;
    private bool _busyInstalling;
    private bool _busyStarting;
    private bool _busyPulling;
    private bool _disposed;

    public LocalAiEngineService(
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment environment,
        IOptionsMonitor<LocalAiOptions> options,
        ILogger<LocalAiEngineService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _environment = environment;
        _options = options;
        _logger = logger;
        _probeClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        _status = new LocalAiStatus
        {
            Phase = LocalAiPhase.Unknown,
            Message = "Checking local AI engine…",
            Model = options.CurrentValue.Model,
            Host = NormalizeHost(options.CurrentValue.BaseUrl)
        };
    }

    public event Action? StatusChanged;

    public string EffectiveModel =>
        !string.IsNullOrWhiteSpace(_preferredModel)
            ? _preferredModel!
            : (string.IsNullOrWhiteSpace(_options.CurrentValue.Model)
                ? LocalAiOptions.DefaultModel
                : _options.CurrentValue.Model.Trim());

    public LocalAiStatus GetSnapshot()
    {
        lock (_statusLock)
        {
            return _status;
        }
    }

    public async Task<LocalAiStatus> RefreshStatusAsync(CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        var model = EffectiveModel;
        var host = NormalizeHost(options.BaseUrl);
        var engineDir = ResolveEngineDirectory(options);
        var exe = LocalAiEnginePaths.FindOllamaExecutable(engineDir);
        var installed = !string.IsNullOrEmpty(exe);
        var running = false;
        var modelReady = false;
        string? version = null;
        IReadOnlyList<string> models = Array.Empty<string>();
        string? error = null;

        if (installed)
        {
            try
            {
                version = await ReadVersionAsync(exe!, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                _logger.LogDebug(ex, "Could not read ollama version from {Path}", exe);
            }
        }

        try
        {
            var tags = await ProbeTagsAsync(host, cancellationToken);
            if (tags != null)
            {
                running = true;
                models = tags;
                modelReady = ModelIsPresent(tags, model);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            running = false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _logger.LogWarning(ex, "Failed to probe local AI engine at {Host}", host);
        }

        string message;
        if (_busyInstalling)
        {
            message = GetSnapshot().Message;
        }
        else if (_busyStarting)
        {
            message = "Starting local AI engine…";
        }
        else if (_busyPulling)
        {
            message = GetSnapshot().Message;
        }
        else if (installed && running && modelReady)
        {
            message = $"Ready — {model} on this machine. Clinical data is not sent to the cloud.";
        }
        else if (installed && running && !modelReady)
        {
            message = $"Engine is running, but {model} is not downloaded yet.";
        }
        else if (installed && !running)
        {
            message = "Engine is installed but not running.";
        }
        else
        {
            message = "Local AI is not installed. Download Ollama and a Qwen model to enable chart chat and document analysis on this machine.";
        }

        var phase = LocalAiEnginePaths.ComputePhase(
            installed,
            running,
            modelReady,
            _busyInstalling,
            _busyStarting,
            _busyPulling,
            error != null);

        // Preserve in-flight progress while busy.
        var current = GetSnapshot();
        Publish(new LocalAiStatus
        {
            Phase = phase,
            Message = _busyInstalling || _busyPulling || _busyStarting ? current.Message : message,
            Model = model,
            EngineVersion = version,
            EnginePath = exe,
            Host = host,
            EngineInstalled = installed,
            EngineRunning = running,
            ModelReady = modelReady,
            InstalledModels = models,
            ProgressPercent = current.IsBusy ? current.ProgressPercent : null,
            ProgressDetail = current.IsBusy ? current.ProgressDetail : null,
            Error = _busyInstalling || _busyPulling ? current.Error : error,
            CanCancel = (_busyInstalling || _busyPulling) && _opCts is { IsCancellationRequested: false },
            Hardware = current.Hardware ?? SafeProbeHardware()
        });

        return GetSnapshot();
    }

    /// <summary>
    /// Downloads the engine if needed, starts it, and pulls the configured Qwen model.
    /// </summary>
    public async Task InstallAndPrepareAsync(string? modelId = null, CancellationToken cancellationToken = default)
    {
        await RunExclusiveAsync(async ct =>
        {
            if (!string.IsNullOrWhiteSpace(modelId))
            {
                _preferredModel = modelId.Trim();
            }

            await InstallEngineCoreAsync(ct);
            await StartEngineCoreAsync(ct);
            await PullModelCoreAsync(EffectiveModel, ct);
        }, cancellationToken);
    }

    public Task InstallEngineAsync(CancellationToken cancellationToken = default) =>
        RunExclusiveAsync(InstallEngineCoreAsync, cancellationToken);

    public Task StartEngineAsync(CancellationToken cancellationToken = default) =>
        RunExclusiveAsync(StartEngineCoreAsync, cancellationToken);

    public Task PullModelAsync(string? modelId = null, CancellationToken cancellationToken = default) =>
        RunExclusiveAsync(ct =>
        {
            if (!string.IsNullOrWhiteSpace(modelId))
            {
                _preferredModel = modelId.Trim();
            }

            return PullModelCoreAsync(EffectiveModel, ct);
        }, cancellationToken);

    /// <summary>Stops an in-flight engine or model download.</summary>
    public bool CancelCurrent()
    {
        var cts = _opCts;
        if (cts == null)
        {
            return false;
        }

        try
        {
            cts.Cancel();
            _logger.LogInformation("User cancelled the local AI download");
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Used on app startup: if an engine is already present, start it. Never downloads.
    /// </summary>
    public async Task TryStartIfInstalledAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await RefreshStatusAsync(cancellationToken);
            if (!status.EngineInstalled || status.EngineRunning)
            {
                return;
            }

            await StartEngineAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // App shutting down.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto-start of local AI engine failed");
        }
    }

    public void SetPreferredModel(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return;
        }

        _preferredModel = modelId.Trim();
        _ = RefreshStatusAsync();
    }

    private async Task InstallEngineCoreAsync(CancellationToken cancellationToken)
    {
        _busyInstalling = true;
        var options = _options.CurrentValue;
        var engineDir = ResolveEngineDirectory(options);
        Directory.CreateDirectory(engineDir);

        var existing = LocalAiEnginePaths.FindOllamaExecutable(engineDir);
        if (!string.IsNullOrEmpty(existing))
        {
            _logger.LogInformation("Local AI engine already present at {Path}", existing);
            _busyInstalling = false;
            await RefreshStatusAsync(cancellationToken);
            return;
        }

        PublishBusy(
            LocalAiPhase.InstallingEngine,
            "Downloading the local AI engine (Ollama)…",
            progress: 0);

        var downloadsDir = LocalAiEnginePaths.GetDownloadsDirectory(_environment.ContentRootPath);
        Directory.CreateDirectory(downloadsDir);

        string archivePath;
        try
        {
            archivePath = await DownloadEngineArchiveAsync(downloadsDir, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _busyInstalling = false;
            TryDeleteFile(Path.Combine(downloadsDir, LocalAiEnginePaths.GetArchiveFileName()));
            await PublishStoppedAsync("Engine download stopped.");
            throw;
        }
        catch (Exception ex)
        {
            _busyInstalling = false;
            PublishError("Could not download the local AI engine.", ex.Message);
            throw;
        }

        PublishBusy(LocalAiPhase.InstallingEngine, "Extracting the local AI engine…", progress: 85);

        try
        {
            ExtractArchive(archivePath, engineDir);
        }
        catch (Exception ex)
        {
            _busyInstalling = false;
            PublishError("Downloaded the engine but failed to extract it.", ex.Message);
            throw;
        }

        var exe = LocalAiEnginePaths.FindOllamaExecutable(engineDir);
        if (string.IsNullOrEmpty(exe))
        {
            _busyInstalling = false;
            PublishError(
                "The engine archive extracted but ollama was not found.",
                "Expected ollama.exe (Windows) or ollama (Linux) inside the archive.");
            throw new InvalidOperationException("ollama executable missing after extract.");
        }

        try
        {
            File.Delete(archivePath);
        }
        catch (IOException)
        {
            // Leave the archive; not fatal.
        }

        _logger.LogInformation("Installed local AI engine at {Path}", exe);
        _busyInstalling = false;
        await RefreshStatusAsync(cancellationToken);
    }

    private async Task StartEngineCoreAsync(CancellationToken cancellationToken)
    {
        _busyStarting = true;
        PublishBusy(LocalAiPhase.Starting, "Starting the local AI engine…");

        try
        {
            var options = _options.CurrentValue;
            var host = NormalizeHost(options.BaseUrl);
            if (await ProbeTagsAsync(host, cancellationToken) != null)
            {
                _logger.LogInformation("Local AI engine already listening at {Host}", host);
                return;
            }

            var engineDir = ResolveEngineDirectory(options);
            var exe = LocalAiEnginePaths.FindOllamaExecutable(engineDir);
            if (string.IsNullOrEmpty(exe))
            {
                throw new InvalidOperationException(
                    "Local AI engine is not installed. Install it from Settings before starting.");
            }

            StartServeProcess(exe, engineDir);
            await WaitUntilRunningAsync(host, TimeSpan.FromSeconds(45), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await PublishStoppedAsync("Engine start stopped.");
            throw;
        }
        catch (Exception ex)
        {
            PublishError("Could not start the local AI engine.", ex.Message);
            throw;
        }
        finally
        {
            _busyStarting = false;
            if (!cancellationToken.IsCancellationRequested)
            {
                await RefreshStatusAsync(cancellationToken);
            }
        }
    }

    private async Task PullModelCoreAsync(string model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException("No local AI model is configured.");
        }

        _busyPulling = true;
        PublishBusy(LocalAiPhase.DownloadingModel, $"Downloading {model}…", progress: 0);

        try
        {
            var host = NormalizeHost(_options.CurrentValue.BaseUrl);
            if (await ProbeTagsAsync(host, cancellationToken) == null)
            {
                await StartEngineCoreAsync(cancellationToken);
            }

            var client = _httpClientFactory.CreateClient(HttpClientName);
            client.Timeout = TimeSpan.FromHours(2);
            client.BaseAddress ??= new Uri(host.TrimEnd('/') + "/");

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(host.TrimEnd('/') + "/"), "api/pull"))
            {
                Content = JsonContent.Create(new { name = model, stream = true })
            };

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                ApplyPullProgress(model, line);
            }

            var tags = await ProbeTagsAsync(host, cancellationToken) ?? Array.Empty<string>();
            if (!ModelIsPresent(tags, model))
            {
                throw new InvalidOperationException(
                    $"The engine finished the download request but {model} is not listed. Check disk space and try again.");
            }
        }
        catch (OperationCanceledException)
        {
            _busyPulling = false;
            await TryDeletePartialModelAsync(model);
            await PublishStoppedAsync($"Download of {model} stopped.");
            throw;
        }
        catch (Exception ex)
        {
            PublishError($"Could not download {model}.", ex.Message);
            throw;
        }
        finally
        {
            _busyPulling = false;
            if (!cancellationToken.IsCancellationRequested)
            {
                await RefreshStatusAsync(cancellationToken);
            }
        }
    }

    private void ApplyPullProgress(string model, string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var errorEl))
            {
                throw new InvalidOperationException(errorEl.GetString() ?? "Model download failed.");
            }

            var status = root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : "downloading";
            double? percent = null;
            string? detail = status;

            if (root.TryGetProperty("total", out var totalEl) &&
                root.TryGetProperty("completed", out var completedEl) &&
                totalEl.TryGetInt64(out var total) &&
                completedEl.TryGetInt64(out var completed) &&
                total > 0)
            {
                percent = Math.Round(100.0 * completed / total, 1);
                detail = $"{status} — {FormatBytes(completed)} / {FormatBytes(total)}";
            }

            PublishBusy(LocalAiPhase.DownloadingModel, $"Downloading {model}…", percent, detail);
        }
        catch (JsonException)
        {
            // Skip malformed progress lines.
        }
    }

    private async Task<string> DownloadEngineArchiveAsync(string downloadsDir, CancellationToken cancellationToken)
    {
        var fileName = LocalAiEnginePaths.GetArchiveFileName();
        var destination = Path.Combine(downloadsDir, fileName);
        var url = LocalAiEnginePaths.GetArchiveDownloadUrl();
        var client = _httpClientFactory.CreateClient(HttpClientName);

        _logger.LogInformation("Downloading local AI engine from {Url}", url);

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(
            destination,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 82_000,
            useAsync: true);

        var buffer = new byte[82_000];
        long copied = 0;
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            copied += read;
            if (total is > 0)
            {
                var percent = Math.Min(80, Math.Round(80.0 * copied / total.Value, 1));
                PublishBusy(
                    LocalAiPhase.InstallingEngine,
                    "Downloading the local AI engine (Ollama)…",
                    percent,
                    $"{FormatBytes(copied)} / {FormatBytes(total.Value)}");
            }
            else
            {
                PublishBusy(
                    LocalAiPhase.InstallingEngine,
                    "Downloading the local AI engine (Ollama)…",
                    progressDetail: FormatBytes(copied));
            }
        }

        return destination;
    }

    private static void ExtractArchive(string archivePath, string engineDir)
    {
        if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(archivePath, engineDir, overwriteFiles: true);
            return;
        }

        if (archivePath.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase) ||
            archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
        {
            var psi = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xzf \"{archivePath}\" -C \"{engineDir}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start tar to extract the engine archive.");
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("tar failed to extract the local AI engine archive.");
            }

            return;
        }

        throw new InvalidOperationException("Unsupported engine archive format.");
    }

    private void StartServeProcess(string exePath, string engineDir)
    {
        if (_serveProcess is { HasExited: false })
        {
            return;
        }

        var modelsDir = LocalAiEnginePaths.GetModelsDirectory(_environment.ContentRootPath);
        Directory.CreateDirectory(modelsDir);

        var host = new Uri(NormalizeHost(_options.CurrentValue.BaseUrl));
        var listen = $"{host.Host}:{host.Port}";

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = "serve",
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? engineDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var libDir = Path.Combine(Path.GetDirectoryName(exePath) ?? engineDir, "lib", "ollama");
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        if (Directory.Exists(libDir))
        {
            psi.Environment["PATH"] = libDir + Path.PathSeparator + path;
        }

        psi.Environment["OLLAMA_HOST"] = listen;
        psi.Environment["OLLAMA_MODELS"] = modelsDir;

        var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start ollama serve.");
        }

        process.EnableRaisingEvents = true;
        process.OutputDataReceived += (_, e) => CaptureServeLine(e.Data, stderr: false);
        process.ErrorDataReceived += (_, e) => CaptureServeLine(e.Data, stderr: true);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _serveProcess = process;
        _logger.LogInformation("Started local AI engine process {Pid} on {Listen}", process.Id, listen);
    }

    private async Task WaitUntilRunningAsync(string host, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_serveProcess is { HasExited: true })
            {
                throw new InvalidOperationException(
                    "The local AI engine process exited immediately. " + DescribeServeOutput());
            }

            if (await ProbeTagsAsync(host, cancellationToken) != null)
            {
                return;
            }

            await Task.Delay(750, cancellationToken);
        }

        throw new TimeoutException(
            "The local AI engine did not become ready in time. " + DescribeServeOutput());
    }

    private void CaptureServeLine(string? line, bool stderr)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        while (_serveOutput.Count > 80 && _serveOutput.TryDequeue(out _))
        {
            // keep a short ring of startup output for error messages
        }

        _serveOutput.Enqueue(line);
        if (stderr)
        {
            _logger.LogInformation("ollama serve: {Line}", line);
        }
        else
        {
            _logger.LogDebug("ollama serve: {Line}", line);
        }
    }

    private string DescribeServeOutput()
    {
        if (_serveOutput.IsEmpty)
        {
            return "No output was captured from ollama serve.";
        }

        var sb = new StringBuilder();
        foreach (var line in _serveOutput)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(line);
            if (sb.Length > 400)
            {
                break;
            }
        }

        return sb.ToString();
    }

    private async Task<IReadOnlyList<string>?> ProbeTagsAsync(string host, CancellationToken cancellationToken)
    {
        try
        {
            var url = new Uri(new Uri(host.TrimEnd('/') + "/"), "api/tags");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            using var response = await _probeClient.GetAsync(url, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cts.Token);
            return ParseModelTags(body);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsUnreachableEngine(ex))
        {
            // Nothing is listening yet — that is normal before `ollama serve` starts.
            return null;
        }
    }

    /// <summary>
    /// Connection refused / probe timeout while the engine is down is expected,
    /// not a start failure.
    /// </summary>
    internal static bool IsUnreachableEngine(Exception ex)
    {
        if (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return true;
        }

        if (ex is OperationCanceledException)
        {
            return true;
        }

        return ex.InnerException != null && IsUnreachableEngine(ex.InnerException);
    }

    public static IReadOnlyList<string> ParseModelTags(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var names = new List<string>();
        foreach (var model in models.EnumerateArray())
        {
            if (model.TryGetProperty("name", out var nameEl))
            {
                var name = nameEl.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    public static bool ModelIsPresent(IEnumerable<string> installed, string requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return false;
        }

        var wanted = requested.Trim();
        foreach (var name in installed)
        {
            if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // qwen2.5:7b matches qwen2.5:7b-... variants and qwen2.5:latest if requested is qwen2.5
            if (name.StartsWith(wanted + "-", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith(wanted + ":", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<string?> ReadVersionAsync(string exePath, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = "--version",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var process = Process.Start(psi);
        if (process == null)
        {
            return null;
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var line = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(line) ? null : line;
    }

    private async Task RunExclusiveAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        if (!await _opLock.WaitAsync(0, cancellationToken))
        {
            throw new InvalidOperationException("A local AI install or download is already running.");
        }

        var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _opCts = linked;
        try
        {
            await work(linked.Token);
        }
        finally
        {
            _opCts = null;
            linked.Dispose();
            _opLock.Release();
        }
    }

    private async Task PublishStoppedAsync(string message)
    {
        _busyInstalling = false;
        _busyPulling = false;
        _busyStarting = false;
        var status = await RefreshStatusAsync();
        Publish(status with
        {
            Message = message,
            Error = null,
            CanCancel = false,
            ProgressPercent = null,
            ProgressDetail = null
        });
        _logger.LogInformation("{Message}", message);
    }

    private async Task TryDeletePartialModelAsync(string model)
    {
        try
        {
            var host = NormalizeHost(_options.CurrentValue.BaseUrl);
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(new Uri(host.TrimEnd('/') + "/"), "api/delete"))
            {
                Content = JsonContent.Create(new { name = model })
            };
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await client.SendAsync(request, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not clean up partial model {Model} after cancel", model);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Leave the partial file.
        }
    }

    public LocalAiHardwareSnapshot ProbeHardware() => SafeProbeHardware();

    private LocalAiHardwareSnapshot SafeProbeHardware()
    {
        try
        {
            return LocalAiHardwareProbe.Probe(_environment.ContentRootPath);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Hardware probe failed");
            return new LocalAiHardwareSnapshot
            {
                TotalRamGb = 8,
                AvailableRamGb = 4,
                CpuCores = Math.Max(1, Environment.ProcessorCount),
                FreeDiskGb = 0,
                DiskRoot = _environment.ContentRootPath
            };
        }
    }

    private string ResolveEngineDirectory(LocalAiOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.EngineDirectory))
        {
            return Path.GetFullPath(options.EngineDirectory);
        }

        return LocalAiEnginePaths.GetDefaultEngineDirectory(_environment.ContentRootPath);
    }

    private static string NormalizeHost(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return LocalAiOptions.DefaultBaseUrl;
        }

        var trimmed = baseUrl.Trim().TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            uri.Host is not ("127.0.0.1" or "localhost" or "::1"))
        {
            // Force loopback so clinical data cannot be pointed at a remote host via config typo.
            return LocalAiOptions.DefaultBaseUrl;
        }

        return trimmed;
    }

    private void PublishBusy(LocalAiPhase phase, string message, double? progress = null, string? progressDetail = null)
    {
        var current = GetSnapshot();
        Publish(current with
        {
            Phase = phase,
            Message = message,
            ProgressPercent = progress,
            ProgressDetail = progressDetail,
            Error = null,
            CanCancel = phase is LocalAiPhase.InstallingEngine or LocalAiPhase.DownloadingModel
        });
    }

    private void PublishError(string message, string? detail)
    {
        var current = GetSnapshot();
        _logger.LogError("Local AI engine error: {Message} {Detail}", message, detail);
        Publish(current with
        {
            Phase = LocalAiPhase.Error,
            Message = message,
            Error = detail,
            ProgressPercent = null,
            ProgressDetail = null
        });
    }

    private void Publish(LocalAiStatus status)
    {
        lock (_statusLock)
        {
            _status = status;
        }

        try
        {
            StatusChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Local AI status listener threw");
        }
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        double value = bytes;
        string[] units = ["KB", "MB", "GB", "TB"];
        foreach (var unit in units)
        {
            value /= 1024.0;
            if (value < 1024)
            {
                return $"{value:0.0} {unit}";
            }
        }

        return $"{value:0.0} PB";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _probeClient.Dispose();
        _opLock.Dispose();
        // Leave ollama serve running so the next app start is faster.
        _serveProcess?.Dispose();
    }
}
