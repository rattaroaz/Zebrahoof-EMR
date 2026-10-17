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
/// Same patient may be open in several circuits.
/// Moving to another patient on the same circuit is one locked switch.
/// A different circuit holding a patient must confirm before the chart changes.
/// </summary>
public sealed class ActivePatientRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Slot> _byUser = new(StringComparer.Ordinal);

    public event Action<ChartClosedNotice>? ChartClosed;

    public ChartOpenDecision TryOpen(string userId, string circuitId, int patientId, string patientName)
        => Switch(userId, circuitId, patientId, patientName);

    /// <summary>
    /// Claims <paramref name="patientId"/> for <paramref name="circuitId"/>.
    /// When that circuit is the only holder, replacing the open patient happens inside this call.
    /// When a different circuit holds a patient, the slot is left unchanged and the caller must prompt.
    /// </summary>
    public ChartOpenDecision Switch(string userId, string circuitId, int patientId, string patientName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(circuitId);

        lock (_gate)
        {
            var held = _byUser.TryGetValue(userId, out var slot)
                && (slot!.Circuits.Count > 0 || slot.HandoffCircuitId != null);
            if (!held)
            {
                var created = new Slot(patientId, patientName);
                created.Circuits[circuitId] = false;
                _byUser[userId] = created;
                return Granted(patientId, patientName);
            }

            var thisCircuitHolds = slot!.Circuits.ContainsKey(circuitId)
                || string.Equals(slot.HandoffCircuitId, circuitId, StringComparison.Ordinal);
            var otherCircuitHolds = slot.Circuits.Keys.Any(id => !string.Equals(id, circuitId, StringComparison.Ordinal))
                || (slot.HandoffCircuitId != null && !string.Equals(slot.HandoffCircuitId, circuitId, StringComparison.Ordinal));

            if (slot.PatientId == patientId)
            {
                ClearHandoff(slot, circuitId);
                slot.Circuits.TryAdd(circuitId, false);
                if (!string.IsNullOrWhiteSpace(patientName))
                {
                    slot.PatientName = patientName;
                }

                return Granted(slot.PatientId, slot.PatientName);
            }

            if (thisCircuitHolds && !otherCircuitHolds)
            {
                slot.PatientId = patientId;
                slot.PatientName = patientName;
                slot.HandoffCircuitId = null;
                slot.HandoffPatientId = null;
                slot.Circuits.Clear();
                slot.Circuits[circuitId] = false;
                return Granted(patientId, patientName);
            }

            return new ChartOpenDecision(
                ChartOpenStatus.Blocked,
                slot.PatientId,
                slot.PatientName,
                slot.Circuits.Values.Any(unsaved => unsaved));
        }
    }

    /// <summary>
    /// Drops this circuit's hold without opening a gap another circuit can take, until <see cref="CommitHandoff"/>
    /// or until the same circuit <see cref="Switch"/>es to the next patient.
    /// </summary>
    public void BeginHandoff(string userId, string circuitId, int patientId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(circuitId))
        {
            return;
        }

        lock (_gate)
        {
            if (!_byUser.TryGetValue(userId, out var slot) || slot.PatientId != patientId)
            {
                return;
            }

            if (!slot.Circuits.Remove(circuitId))
            {
                return;
            }

            if (slot.Circuits.Count == 0)
            {
                slot.HandoffCircuitId = circuitId;
                slot.HandoffPatientId = patientId;
            }
        }
    }

    public void CommitHandoff(string userId, string circuitId, int patientId)
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

            if (slot.HandoffCircuitId == circuitId
                && slot.HandoffPatientId == patientId
                && slot.Circuits.Count == 0)
            {
                _byUser.Remove(userId);
            }
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

    public void Release(string userId, string circuitId, int? onlyIfPatientId = null)
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

            if (onlyIfPatientId is int patientId && slot.PatientId != patientId)
            {
                return;
            }

            slot.Circuits.Remove(circuitId);
            if (string.Equals(slot.HandoffCircuitId, circuitId, StringComparison.Ordinal))
            {
                slot.HandoffCircuitId = null;
                slot.HandoffPatientId = null;
            }

            if (slot.Circuits.Count == 0 && slot.HandoffCircuitId == null)
            {
                _byUser.Remove(userId);
            }
        }
    }

    private static ChartOpenDecision Granted(int patientId, string patientName)
        => new(ChartOpenStatus.Granted, patientId, patientName, false);

    private static void ClearHandoff(Slot slot, string circuitId)
    {
        if (string.Equals(slot.HandoffCircuitId, circuitId, StringComparison.Ordinal))
        {
            slot.HandoffCircuitId = null;
            slot.HandoffPatientId = null;
        }
    }

    public (int PatientId, string PatientName)? Current(string userId)
    {
        lock (_gate)
        {
            if (!_byUser.TryGetValue(userId, out var slot)
                || (slot.Circuits.Count == 0 && slot.HandoffCircuitId == null))
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
        public string? HandoffCircuitId { get; set; }
        public int? HandoffPatientId { get; set; }
    }
}
