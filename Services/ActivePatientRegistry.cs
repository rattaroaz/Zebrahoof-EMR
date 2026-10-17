namespace Zebrahoof_EMR.Services;

public enum ChartOpenStatus
{
    Granted,
    Blocked
}

/// <summary>Why a chart could not be shown. The caller must ask before replacing the open patient.</summary>
public sealed record ChartOpenDecision(
    ChartOpenStatus Status,
    int? OpenPatientId,
    string? OpenPatientName,
    bool OpenChartHasUnsavedWork);

public sealed record ChartClosedNotice(
    string UserId,
    string CircuitId,
    int ClosedPatientId,
    string ClosedPatientName,
    int OpenedPatientId,
    string OpenedPatientName);

/// <summary>
/// One open patient per signed-in user, across tabs, browsers, and devices.
/// Same patient may be open in several circuits. A different patient is refused until the user confirms a switch.
/// </summary>
public sealed class ActivePatientRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Slot> _byUser = new(StringComparer.Ordinal);

    public event Action<ChartClosedNotice>? ChartClosed;

    public ChartOpenDecision TryOpen(string userId, string circuitId, int patientId, string patientName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(circuitId);

        lock (_gate)
        {
            if (!_byUser.TryGetValue(userId, out var slot) || slot.Circuits.Count == 0)
            {
                slot = new Slot(patientId, patientName);
                slot.Circuits[circuitId] = false;
                _byUser[userId] = slot;
                return new ChartOpenDecision(ChartOpenStatus.Granted, patientId, patientName, false);
            }

            if (slot.PatientId == patientId)
            {
                slot.Circuits.TryAdd(circuitId, false);
                if (!string.IsNullOrWhiteSpace(patientName))
                {
                    slot.PatientName = patientName;
                }

                return new ChartOpenDecision(ChartOpenStatus.Granted, patientId, slot.PatientName, false);
            }

            return new ChartOpenDecision(
                ChartOpenStatus.Blocked,
                slot.PatientId,
                slot.PatientName,
                slot.Circuits.Values.Any(unsaved => unsaved));
        }
    }

    /// <summary>
    /// Replaces the user's open patient. Every other circuit that was showing the previous chart is notified to close it.
    /// </summary>
    public void ConfirmSwitch(string userId, string circuitId, int patientId, string patientName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(circuitId);

        List<string> closedCircuits;
        int closedPatientId;
        string closedPatientName;

        lock (_gate)
        {
            if (!_byUser.TryGetValue(userId, out var slot) || slot.Circuits.Count == 0)
            {
                var created = new Slot(patientId, patientName);
                created.Circuits[circuitId] = false;
                _byUser[userId] = created;
                return;
            }

            if (slot.PatientId == patientId)
            {
                slot.Circuits[circuitId] = false;
                if (!string.IsNullOrWhiteSpace(patientName))
                {
                    slot.PatientName = patientName;
                }

                return;
            }

            closedPatientId = slot.PatientId;
            closedPatientName = slot.PatientName;
            closedCircuits = slot.Circuits.Keys.Where(id => !string.Equals(id, circuitId, StringComparison.Ordinal)).ToList();
            slot.PatientId = patientId;
            slot.PatientName = patientName;
            slot.Circuits.Clear();
            slot.Circuits[circuitId] = false;
        }

        foreach (var id in closedCircuits)
        {
            ChartClosed?.Invoke(new ChartClosedNotice(
                userId,
                id,
                closedPatientId,
                closedPatientName,
                patientId,
                patientName));
        }
    }

    public void SetUnsaved(string userId, string circuitId, bool unsaved)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(circuitId))
        {
            return;
        }

        lock (_gate)
        {
            if (_byUser.TryGetValue(userId, out var slot) && slot.Circuits.ContainsKey(circuitId))
            {
                slot.Circuits[circuitId] = unsaved;
            }
        }
    }

    public void Release(string userId, string circuitId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(circuitId))
        {
            return;
        }

        lock (_gate)
        {
            if (!_byUser.TryGetValue(userId, out var slot))
            {
                return;
            }

            slot.Circuits.Remove(circuitId);
            if (slot.Circuits.Count == 0)
            {
                _byUser.Remove(userId);
            }
        }
    }

    public (int PatientId, string PatientName)? Current(string userId)
    {
        lock (_gate)
        {
            if (!_byUser.TryGetValue(userId, out var slot) || slot.Circuits.Count == 0)
            {
                return null;
            }

            return (slot.PatientId, slot.PatientName);
        }
    }

    private sealed class Slot(int patientId, string patientName)
    {
        public int PatientId { get; set; } = patientId;
        public string PatientName { get; set; } = patientName;
        public Dictionary<string, bool> Circuits { get; } = new(StringComparer.Ordinal);
    }
}
