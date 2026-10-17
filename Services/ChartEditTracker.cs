namespace Zebrahoof_EMR.Services;

/// <summary>Tracks in-progress chart edits on this circuit so a patient switch can warn before they are discarded.</summary>
public sealed class ChartEditTracker
{
    private int _depth;

    public bool HasUnsavedWork => _depth > 0;

    public int? PatientId { get; private set; }

    public string? PatientName { get; private set; }

    public event Action? Changed;

    public event Action? DiscardRequested;

    public IDisposable Push(int patientId, string? patientName = null)
    {
        _depth++;
        PatientId = patientId;
        if (!string.IsNullOrWhiteSpace(patientName))
        {
            PatientName = patientName;
        }

        Changed?.Invoke();
        return new Popper(this);
    }

    public void Discard()
    {
        _depth = 0;
        PatientId = null;
        PatientName = null;
        DiscardRequested?.Invoke();
        Changed?.Invoke();
    }

    private void Pop()
    {
        if (_depth > 0)
        {
            _depth--;
        }

        Changed?.Invoke();
    }

    private sealed class Popper : IDisposable
    {
        private ChartEditTracker? _owner;

        public Popper(ChartEditTracker owner) => _owner = owner;

        public void Dispose()
        {
            var owner = _owner;
            _owner = null;
            owner?.Pop();
        }
    }
}
