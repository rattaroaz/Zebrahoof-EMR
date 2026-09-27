using System.Net.Http;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class LocalAiEngineServiceTests
{
    [Fact]
    public void KnownCanChat_IsUnknownUntilProbed_AndReadyOnlyWhenEngineAndModelAreUp()
    {
        var unknown = new LocalAiStatus();
        Assert.Null(unknown.KnownCanChat);
        Assert.False(unknown.CanChat);

        var ready = new LocalAiStatus
        {
            Phase = LocalAiPhase.Ready,
            EngineInstalled = true,
            EngineRunning = true,
            ModelReady = true,
            Model = "qwen2.5:7b"
        };
        Assert.True(ready.KnownCanChat);

        var installedNotRunning = new LocalAiStatus
        {
            Phase = LocalAiPhase.NeedsSetup,
            EngineInstalled = true,
            EngineRunning = false,
            ModelReady = false
        };
        Assert.False(installedNotRunning.KnownCanChat);
        Assert.Contains("not running", installedNotRunning.NotReadyMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComputePhase_ReadyWhenInstalledRunningAndModelPresent()
    {
        var phase = LocalAiEnginePaths.ComputePhase(
            engineInstalled: true,
            engineRunning: true,
            modelReady: true,
            isBusyInstalling: false,
            isBusyStarting: false,
            isBusyPulling: false,
            hasError: false);

        Assert.Equal(LocalAiPhase.Ready, phase);
    }

    [Fact]
    public void ComputePhase_NotInstalledWhenMissing()
    {
        var phase = LocalAiEnginePaths.ComputePhase(
            engineInstalled: false,
            engineRunning: false,
            modelReady: false,
            isBusyInstalling: false,
            isBusyStarting: false,
            isBusyPulling: false,
            hasError: false);

        Assert.Equal(LocalAiPhase.NotInstalled, phase);
    }

    [Fact]
    public void ComputePhase_NeedsSetupWhenInstalledButNotReady()
    {
        var phase = LocalAiEnginePaths.ComputePhase(
            engineInstalled: true,
            engineRunning: true,
            modelReady: false,
            isBusyInstalling: false,
            isBusyStarting: false,
            isBusyPulling: false,
            hasError: false);

        Assert.Equal(LocalAiPhase.NeedsSetup, phase);
    }

    [Fact]
    public void ComputePhase_BusyFlagsWin()
    {
        Assert.Equal(LocalAiPhase.InstallingEngine, LocalAiEnginePaths.ComputePhase(
            true, false, false, isBusyInstalling: true, false, false, false));
        Assert.Equal(LocalAiPhase.Starting, LocalAiEnginePaths.ComputePhase(
            true, false, false, false, isBusyStarting: true, false, false));
        Assert.Equal(LocalAiPhase.DownloadingModel, LocalAiEnginePaths.ComputePhase(
            true, true, false, false, false, isBusyPulling: true, false));
    }

    [Fact]
    public void ParseModelTags_ReadsNames()
    {
        const string json = """{"models":[{"name":"qwen2.5:7b"},{"name":"qwen2.5:3b"}]}""";

        var names = LocalAiEngineService.ParseModelTags(json);

        Assert.Equal(new[] { "qwen2.5:7b", "qwen2.5:3b" }, names);
    }

    [Theory]
    [InlineData("qwen2.5:7b", "qwen2.5:7b", true)]
    [InlineData("qwen2.5:7b", "qwen2.5:3b", false)]
    [InlineData("qwen2.5:7b-instruct", "qwen2.5:7b", true)]
    public void ModelIsPresent_MatchesExactOrPrefix(string installed, string requested, bool expected)
    {
        Assert.Equal(expected, LocalAiEngineService.ModelIsPresent(new[] { installed }, requested));
    }

    [Fact]
    public void GetDefaultEngineDirectory_IsUnderAppData()
    {
        var dir = LocalAiEnginePaths.GetDefaultEngineDirectory(@"C:\app");
        Assert.Equal(Path.Combine(@"C:\app", "App_Data", "ai-engine", "ollama"), dir);
    }

    [Fact]
    public void ListInstalledModelTags_ReadsOllamaManifests()
    {
        var root = Path.Combine(Path.GetTempPath(), "zh-models-" + Guid.NewGuid().ToString("N"));
        var manifests = Path.Combine(root, "manifests", "registry.ollama.ai", "library");
        Directory.CreateDirectory(Path.Combine(manifests, "qwen2.5"));
        Directory.CreateDirectory(Path.Combine(manifests, "qwen3"));
        File.WriteAllText(Path.Combine(manifests, "qwen2.5", "7b"), "{}");
        File.WriteAllText(Path.Combine(manifests, "qwen3", "8b"), "{}");

        var tags = LocalAiEnginePaths.ListInstalledModelTags(root);

        Assert.Contains("qwen2.5:7b", tags);
        Assert.Contains("qwen3:8b", tags);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void ListInstalledModelTags_EmptyWhenMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "zh-models-missing-" + Guid.NewGuid().ToString("N"));
        Assert.Empty(LocalAiEnginePaths.ListInstalledModelTags(root));
    }

    [Theory]
    [InlineData("registry.ollama.ai/library/qwen2.5/7b", "qwen2.5:7b")]
    [InlineData("registry.ollama.ai/library/qwen3.8/27b", "qwen3.8:27b")]
    [InlineData("registry.ollama.ai/lmstudio/foo/bar", "lmstudio/foo:bar")]
    public void TryParseManifestTag_MapsOllamaLayout(string relative, string expected)
    {
        var root = Path.Combine(@"C:\models", "manifests");
        var file = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(LocalAiEnginePaths.TryParseManifestTag(root, file, out var tag));
        Assert.Equal(expected, tag);
    }

    [Fact]
    public void FormatSavedModelsSummary_Truncates()
    {
        var tags = new[] { "a", "b", "c", "d", "e", "f", "g" };
        Assert.Equal("a, b, c, and 4 more", LocalAiEnginePaths.FormatSavedModelsSummary(tags, show: 3));
        Assert.Equal("a, b", LocalAiEnginePaths.FormatSavedModelsSummary(["a", "b"]));
        Assert.Equal(string.Empty, LocalAiEnginePaths.FormatSavedModelsSummary(Array.Empty<string>()));
    }

    [Fact]
    public void PreferredModel_RoundTripsOnDisk()
    {
        var root = Path.Combine(Path.GetTempPath(), "zh-pref-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Assert.Null(LocalAiEnginePaths.TryReadPreferredModel(root));

        LocalAiEnginePaths.WritePreferredModel(root, "  qwen3:8b  ");
        Assert.Equal("qwen3:8b", LocalAiEnginePaths.TryReadPreferredModel(root));
        Assert.True(File.Exists(LocalAiEnginePaths.GetPreferredModelPath(root)));

        LocalAiEnginePaths.WritePreferredModel(root, "deepseek-r1:7b");
        Assert.Equal("deepseek-r1:7b", LocalAiEnginePaths.TryReadPreferredModel(root));
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void MergeTags_UnionsAndDedupes()
    {
        var merged = LocalAiEnginePaths.MergeTags(["qwen2.5:7b"], ["qwen2.5:7b", "qwen3:8b"], null);
        Assert.Equal(2, merged.Count);
        Assert.Contains("qwen2.5:7b", merged);
        Assert.Contains("qwen3:8b", merged);
    }

    [Fact]
    public void FormatBytes_UsesReadableUnits()
    {
        Assert.Equal("512 B", LocalAiEngineService.FormatBytes(512));
        Assert.Equal("1.0 KB", LocalAiEngineService.FormatBytes(1024));
        Assert.Equal("1.0 MB", LocalAiEngineService.FormatBytes(1024 * 1024));
    }

    [Fact]
    public void IsUnreachableEngine_TreatsConnectionRefusedAsDownNotFailed()
    {
        Assert.True(LocalAiEngineService.IsUnreachableEngine(
            new HttpRequestException("No connection could be made because the target machine actively refused it. (127.0.0.1:11434)")));
        Assert.True(LocalAiEngineService.IsUnreachableEngine(new TaskCanceledException("probe timed out")));
        Assert.False(LocalAiEngineService.IsUnreachableEngine(new InvalidOperationException("bad archive")));
    }

    [Fact]
    public void DownloadUrl_UsesOfficialLatestRelease()
    {
        var url = LocalAiEnginePaths.GetArchiveDownloadUrl();
        Assert.StartsWith("https://github.com/ollama/ollama/releases/latest/download/", url);
        Assert.Contains("ollama-", url);
    }
}
