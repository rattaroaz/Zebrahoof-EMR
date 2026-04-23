namespace Zebrahoof_EMR.Services;

/// <summary>
/// Scoped (per-circuit) state tracking which Grok actions have been performed for
/// each patient in the current session. State persists across navigations within
/// the same browser session and resets when the app is reloaded.
/// </summary>
public class GrokSessionStateService
{
    private readonly HashSet<int> _documentsSentToGrok = new();
    private readonly HashSet<int> _recordsUpdated = new();
    private readonly Dictionary<int, int> _lastKnownDocumentCount = new();

    public bool HaveDocumentsBeenSent(int patientId) => _documentsSentToGrok.Contains(patientId);

    public void MarkDocumentsSent(int patientId) => _documentsSentToGrok.Add(patientId);

    public void ResetDocumentsSent(int patientId) => _documentsSentToGrok.Remove(patientId);

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
