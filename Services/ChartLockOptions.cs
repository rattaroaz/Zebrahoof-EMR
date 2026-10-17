namespace Zebrahoof_EMR.Services;

public sealed class ChartLockOptions
{
    public const string Section = "ChartLock";

    /// <summary>How long a dropped browser keeps the chart before another browser may open one. About 5–10 seconds.</summary>
    public int DisconnectGraceSeconds { get; set; } = 8;
}
