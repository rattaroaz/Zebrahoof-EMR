using System.Net.Http;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class LocalAiEngineServiceTests
{
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
