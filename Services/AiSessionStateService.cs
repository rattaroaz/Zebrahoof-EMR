namespace Zebrahoof_EMR.Services;

/// <summary>
/// Scoped (per-circuit) state for the Update Records workflow.
/// Persists across navigations in the same browser session.
/// </summary>
public class AiSessionStateService
{
    private readonly HashSet<int> _recordsUpdated = new();
    private readonly Dictionary<int, int> _lastKnownDocumentCount = new();

    public bool HaveRecordsBeenUpdated(int patientId) => _recordsUpdated.Contains(patientId);

    public void MarkRecordsUpdated(int patientId) => _recordsUpdated.Add(patientId);

    public void ResetRecordsUpdated(int patientId) => _recordsUpdated.Remove(patientId);

    /// <summary>
    /// Records the known document count for a patient and returns true if the count
    /// has grown since the last check (indicating new documents were uploaded).
    /// </summary>
    public bool UpdateDocumentCount(int patientId, int currentCount)
    {
        var previous = _lastKnownDocumentCount.TryGetValue(patientId, out var c) ? c : 0;
        _lastKnownDocumentCount[patientId] = currentCount;
        return currentCount > previous;
    }
}
