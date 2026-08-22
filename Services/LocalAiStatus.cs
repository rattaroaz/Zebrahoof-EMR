namespace Zebrahoof_EMR.Services;

public enum LocalAiPhase
{
    Unknown = 0,
    NotInstalled,
    InstallingEngine,
    Starting,
    DownloadingModel,
    NeedsSetup,
    Ready,
    Error
}

public sealed record LocalAiStatus
{
    public LocalAiPhase Phase { get; init; } = LocalAiPhase.Unknown;
    public string Message { get; init; } = string.Empty;
    public string Model { get; init; } = LocalAiOptions.DefaultModel;
    public string? EngineVersion { get; init; }
    public string? EnginePath { get; init; }
    public string Host { get; init; } = LocalAiOptions.DefaultBaseUrl;
    public bool EngineInstalled { get; init; }
    public bool EngineRunning { get; init; }
    public bool ModelReady { get; init; }
    public IReadOnlyList<string> InstalledModels { get; init; } = Array.Empty<string>();
    public double? ProgressPercent { get; init; }
    public string? ProgressDetail { get; init; }
    public string? Error { get; init; }
    public bool CanCancel { get; init; }
    public LocalAiHardwareSnapshot? Hardware { get; init; }

    public bool IsBusy =>
        Phase is LocalAiPhase.InstallingEngine or LocalAiPhase.Starting or LocalAiPhase.DownloadingModel;

    public bool CanChat => Phase == LocalAiPhase.Ready && EngineRunning && ModelReady;
}
