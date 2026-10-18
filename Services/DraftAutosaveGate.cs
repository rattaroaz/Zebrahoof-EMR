namespace Zebrahoof_EMR.Services;

/// <summary>
/// One circuit's draft-save generation. Captured when the circuit signs in,
/// for that browser only. An explicit sign-out of this browser stops the timer
/// and rejects saves from this generation. Another device is unaffected.
/// </summary>
public sealed class DraftAutosaveGate
{
    public bool Enabled { get; private set; } = true;

    public string BrowserId { get; private set; } = string.Empty;

    public long Epoch { get; private set; }

    public bool EpochCaptured { get; private set; }

    public void Capture(string? browserId, long epoch)
    {
        if (EpochCaptured || string.IsNullOrWhiteSpace(browserId))
        {
            return;
        }

        BrowserId = browserId;
        Epoch = epoch;
        EpochCaptured = true;
    }

    public void Stop() => Enabled = false;
}
