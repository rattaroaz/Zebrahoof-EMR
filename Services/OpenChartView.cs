namespace Zebrahoof_EMR.Services;

/// <summary>The chart the current circuit is showing. Filled only after a real open.</summary>
public sealed class OpenChartView
{
    public PatientChartSnapshot? Snapshot { get; private set; }
    public string Source { get; private set; } = "";
    public double LoadMilliseconds { get; private set; }
    public bool Updating { get; private set; }
    public bool JustUpdated { get; private set; }

    public event Action? Changed;

    public void Publish(ChartCacheOpen open, bool justUpdated = false)
    {
        Snapshot = open.Snapshot;
        Source = open.Source;
        LoadMilliseconds = open.LoadMilliseconds;
        Updating = false;
        JustUpdated = justUpdated;
        Changed?.Invoke();
    }

    public void SetUpdating(bool updating)
    {
        if (Updating == updating)
        {
            return;
        }

        Updating = updating;
        if (updating)
        {
            JustUpdated = false;
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        if (Snapshot == null && !Updating && !JustUpdated)
        {
            return;
        }

        Snapshot = null;
        Source = "";
        LoadMilliseconds = 0;
        Updating = false;
        JustUpdated = false;
        Changed?.Invoke();
    }
}
