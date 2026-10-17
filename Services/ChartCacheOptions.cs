namespace Zebrahoof_EMR.Services;

/// <summary>
/// Per-user chart cache limits. Capacity is how many opened patients one user keeps.
/// IdleMinutes drops a snapshot that has not been opened again. MaxSnapshots caps the process.
/// </summary>
public sealed class ChartCacheOptions
{
    public const string SectionName = "ChartCache";

    public int Capacity { get; set; } = 10;

    public int IdleMinutes { get; set; } = 30;

    public int MaxSnapshots { get; set; } = 200;
}
