using System.Net;
using System.Net.Http;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class LocalAiLibraryTests
{
    private const string LibraryHtml = """
        <li class="flex items-baseline border-b border-neutral-200 py-6">
          <a href="/library/qwen3.8" class="group w-full space-y-5">
            <div title="qwen3.8" class="flex flex-col">
              <h2><span>qwen3.8</span></h2>
              <p class="max-w-lg break-words text-neutral-800 text-md">Qwen3.8 delivers substantial gains across coding.</p>
            </div>
            <div class="flex flex-wrap space-x-2">
              <span class="inline-flex items-center rounded-md bg-indigo-50 px-2 py-0.5 text-xs font-medium text-indigo-600">thinking</span>
              <span class="inline-flex items-center rounded-md bg-[#ddf4ff] px-2 py-0.5 text-xs font-medium text-blue-600 sm:text-[13px]">27b</span>
            </div>
          </a>
        </li>
        <li class="flex items-baseline border-b border-neutral-200 py-6">
          <a href="/library/qwen2.5" class="group w-full space-y-5">
            <div title="qwen2.5">
              <p class="max-w-lg break-words">Qwen2.5 models are pretrained on Alibaba&#39;s latest large-scale dataset.</p>
            </div>
            <span class="inline-flex items-center rounded-md bg-[#ddf4ff] px-2 py-0.5 text-xs font-medium text-blue-600 sm:text-[13px]">0.5b</span>
            <span class="inline-flex items-center rounded-md bg-[#ddf4ff] px-2 py-0.5 text-xs font-medium text-blue-600 sm:text-[13px]">7b</span>
            <span class="inline-flex items-center rounded-md bg-[#ddf4ff] px-2 py-0.5 text-xs font-medium text-blue-600 sm:text-[13px]">32b</span>
          </a>
        </li>
        <li class="flex items-baseline border-b border-neutral-200 py-6">
          <a href="/library/nomic-embed-text" class="group w-full">
            <p class="max-w-lg break-words">Embedding model.</p>
          </a>
        </li>
        <li class="flex items-baseline border-b border-neutral-200 py-6">
          <a href="/library/kimi-k2" class="group w-full">
            <p class="max-w-lg break-words">Moonshot Kimi K2.</p>
          </a>
        </li>
        """;

    [Fact]
    public void ParseLibraryHtml_ReadsOfficialCardsAndSkipsEmbeddings()
    {
        var listings = LocalAiLibraryParser.ParseLibraryHtml(LibraryHtml);

        Assert.Contains(listings, l => l.Name == "qwen3.8" && l.SizeTags.Contains("27b") && l.Thinking);
        Assert.Contains(listings, l => l.Name == "qwen2.5" && l.SizeTags.Contains("7b"));
        Assert.Contains(listings, l => l.Name == "kimi-k2" && l.SizeTags.Count == 0);
        Assert.DoesNotContain(listings, l => l.Name.Contains("embed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Alibaba's", listings.First(l => l.Name == "qwen2.5").Description);
    }

    [Fact]
    public void ToChoices_BuildsQwen38Tag()
    {
        var listings = LocalAiLibraryParser.ParseLibraryHtml(LibraryHtml);
        var choices = LocalAiLibraryParser.ToChoices(listings);

        var qwen38 = Assert.Single(choices, m => m.Id == "qwen3.8:27b");
        Assert.Equal("Qwen", qwen38.Family);
        Assert.Equal("3.8 27B", qwen38.DisplayName);
        Assert.True(qwen38.Reasoning);
        Assert.InRange(qwen38.ParameterBillion, 26.9, 27.1);
        Assert.Contains(choices, m => m.Id == "qwen2.5:7b");
        Assert.DoesNotContain(choices, m => m.Id.Contains("kimi", StringComparison.OrdinalIgnoreCase));
        Assert.All(choices, m => Assert.True(LocalAiModels.IsSupportedFamily(m.Family)));
    }

    [Fact]
    public void MergeWithSeed_KeepsSeedHardwareEstimates()
    {
        var live = new[]
        {
            new LocalAiModelChoice("qwen2.5:7b", "Qwen", "2.5 7B", "live", 99, 99, 99, 99, 7),
            new LocalAiModelChoice("llama3.1:8b", "Llama", "3.1 8B", "unsupported", 4.9, 8, 12, 6, 8)
        };

        var merged = LocalAiLibraryParser.MergeWithSeed(live);

        var qwen = Assert.Single(merged, m => m.Id == "qwen2.5:7b");
        Assert.Equal(4.7, qwen.DownloadGb);
        Assert.Equal(8, qwen.MinRamGb);
        Assert.Contains(merged, m => m.Id == "qwen3.8:27b");
        Assert.DoesNotContain(merged, m => m.Id.StartsWith("llama", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(merged, m => !LocalAiModels.IsSupportedFamily(m.Family));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(6, true)]
    [InlineData(7, false)]
    [InlineData(10, false)]
    public void IsCacheFresh_UsesSevenDayWindow(int daysAgo, bool expected)
    {
        var now = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);
        var pulled = now.AddDays(-daysAgo);

        Assert.Equal(expected, LocalAiLibraryParser.IsCacheFresh(pulled, now));
    }

    [Fact]
    public void IsCacheFresh_FalseWhenNeverPulled()
    {
        Assert.False(LocalAiLibraryParser.IsCacheFresh(null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task EnsureFresh_SkipsNetworkWhenCacheIsUnderOneWeek()
    {
        var now = new DateTimeOffset(2026, 8, 28, 18, 0, 0, TimeSpan.Zero);
        var cacheDir = Path.Combine(Path.GetTempPath(), "zebrahoof-ai-lib-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cacheDir);
        var cachePath = Path.Combine(cacheDir, "library-catalog.json");
        File.WriteAllText(cachePath, """
            {"pulledAtUtc":"2026-08-25T18:00:00+00:00","sourceUrl":"https://ollama.com/library","models":[{"id":"qwen3.8:27b","family":"Qwen","displayName":"3.8 27B","description":"cached","downloadGb":18,"minRamGb":24,"recommendedRamGb":32,"minVramGb":16,"parameterBillion":27,"reasoning":true}]}
            """);
        var handler = new CountingHandler { Status = HttpStatusCode.OK, Body = RepeatLibraryHtml(12) };
        using var http = new HttpClient(handler);
        var clock = new FixedClock(now);
        var service = new LocalAiLibraryCatalogService(http, cachePath, clock);

        var snap = await service.EnsureFreshAsync();

        Assert.Equal(0, handler.Calls);
        Assert.True(snap.FromLiveLibrary);
        Assert.Contains(snap.Models, m => m.Id == "qwen3.8:27b");
        Assert.Equal(new DateTimeOffset(2026, 8, 25, 18, 0, 0, TimeSpan.Zero), snap.PulledAtUtc);
    }

    [Fact]
    public async Task EnsureFresh_PullsWhenCacheIsOlderThanOneWeek()
    {
        var now = new DateTimeOffset(2026, 8, 28, 18, 0, 0, TimeSpan.Zero);
        var cacheDir = Path.Combine(Path.GetTempPath(), "zebrahoof-ai-lib-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cacheDir);
        var cachePath = Path.Combine(cacheDir, "library-catalog.json");
        File.WriteAllText(cachePath, """
            {"pulledAtUtc":"2026-08-20T18:00:00+00:00","sourceUrl":"https://ollama.com/library","models":[{"id":"old:1b","family":"Qwen","displayName":"old","description":"stale","downloadGb":1,"minRamGb":2,"recommendedRamGb":3,"minVramGb":1,"parameterBillion":1,"reasoning":false}]}
            """);
        var handler = new CountingHandler { Status = HttpStatusCode.OK, Body = RepeatLibraryHtml(12) };
        using var http = new HttpClient(handler);
        var service = new LocalAiLibraryCatalogService(http, cachePath, new FixedClock(now));

        var snap = await service.EnsureFreshAsync();

        Assert.Equal(1, handler.Calls);
        Assert.True(snap.FromLiveLibrary);
        Assert.Contains(snap.Models, m => m.Id == "qwen3.8:27b");
        Assert.Equal(now, snap.PulledAtUtc);
        Assert.True(File.Exists(cachePath));
    }

    [Fact]
    public async Task EnsureFresh_KeepsSeedWhenPullFails()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), "zebrahoof-ai-lib-missing", Guid.NewGuid().ToString("N") + ".json");
        var handler = new CountingHandler { Status = HttpStatusCode.InternalServerError, Body = "nope" };
        using var http = new HttpClient(handler);
        var service = new LocalAiLibraryCatalogService(http, cachePath, new FixedClock(DateTimeOffset.UtcNow));

        var snap = await service.EnsureFreshAsync();

        Assert.False(snap.FromLiveLibrary);
        Assert.Null(snap.PulledAtUtc);
        Assert.Contains(snap.Models, m => m.Id == "qwen2.5:7b");
    }

    [Fact]
    public void CatalogService_DropsUnsupportedFamiliesFromCache()
    {
        var cacheDir = Path.Combine(Path.GetTempPath(), "zebrahoof-ai-lib-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cacheDir);
        var cachePath = Path.Combine(cacheDir, "library-catalog.json");
        File.WriteAllText(cachePath, """
            {"pulledAtUtc":"2026-08-28T18:00:00+00:00","sourceUrl":"https://ollama.com/library","models":[{"id":"llama3.1:8b","family":"Llama","displayName":"3.1 8B","description":"no","downloadGb":4.9,"minRamGb":8,"recommendedRamGb":12,"minVramGb":6,"parameterBillion":8,"reasoning":false},{"id":"qwen2.5:7b","family":"Qwen","displayName":"2.5 7B","description":"yes","downloadGb":4.7,"minRamGb":8,"recommendedRamGb":10,"minVramGb":6,"parameterBillion":7,"reasoning":false}]}
            """);
        using var http = new HttpClient(new CountingHandler());
        var service = new LocalAiLibraryCatalogService(http, cachePath, new FixedClock(new DateTimeOffset(2026, 8, 29, 12, 0, 0, TimeSpan.Zero)));

        var snap = service.GetSnapshot();
        Assert.Contains(snap.Models, m => m.Id == "qwen2.5:7b");
        Assert.DoesNotContain(snap.Models, m => m.Family == "Llama");
        Assert.All(snap.Families, f => Assert.True(LocalAiModels.IsSupportedFamily(f)));
    }

    [Fact]
    public void FamilyAndSizeHelpers()
    {
        Assert.Equal("Qwen", LocalAiLibraryParser.FamilyFromName("qwen3.8-flash-next"));
        Assert.Equal("DeepSeek", LocalAiLibraryParser.FamilyFromName("deepseek-r1"));
        Assert.True(LocalAiLibraryParser.TryParseParameterBillion("27b", out var p));
        Assert.Equal(27, p);
        Assert.True(LocalAiLibraryParser.TryParseParameterBillion("e2b", out var e2));
        Assert.Equal(2, e2);
        Assert.Equal("3.8 27B", LocalAiLibraryParser.BuildDisplayName("Qwen", "qwen3.8", "27b"));
        Assert.True(LocalAiLibraryParser.ShouldSkipModel("qwen3-embedding"));
        Assert.False(LocalAiLibraryParser.ShouldSkipModel("qwen3.8"));
    }

    private static string RepeatLibraryHtml(int officialCount)
    {
        var extra = string.Concat(Enumerable.Range(0, officialCount).Select(i =>
            $"""
            <li><a href="/library/extra-model-{i}"><p class="max-w-lg break-words">Extra {i}.</p>
            <span class="inline-flex items-center rounded-md bg-[#ddf4ff] px-2 py-0.5 text-xs font-medium text-blue-600">1b</span></a></li>
            """));
        return LibraryHtml + extra;
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Calls;
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string Body { get; set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(Status)
            {
                Content = new StringContent(Body)
            });
        }
    }

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTimeOffset _utc;

        public FixedClock(DateTimeOffset utc) => _utc = utc;

        public override DateTimeOffset GetUtcNow() => _utc;
    }
}
