namespace Zebrahoof_EMR.Services;

/// <summary>
/// One circuit's draft-save generation. Captured when the circuit signs in.
/// An explicit sign-out stops the timer and rejects saves from this generation.
/// </summary>
public sealed class DraftAutosaveGate
{
    public bool Enabled { get; private set; } = true;

    public long Epoch { get; private set; }

    public bool EpochCaptured { get; private set; }

    public void Capture(long epoch)
    {
        if (EpochCaptured)
        {
            return;
        }

        Epoch = epoch;
        EpochCaptured = true;
    }

    public void Stop() => Enabled = false;
}
