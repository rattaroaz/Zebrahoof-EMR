using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Per-user LRU of charts the user has actually opened. Peeking, sweeping, and invalidating do not fetch.
/// This type does not write audit records; the chart gate audits a real open.
/// </summary>
public sealed class PatientChartCache
{
    private readonly IPatientChartLoader _loader;
    private readonly TimeProvider _time;
    private readonly int _capacity;
    private readonly int _maxSnapshots;
    private readonly TimeSpan _idle;
    private readonly object _gate = new();
    private readonly Dictionary<string, LinkedList<int>> _order = new(StringComparer.Ordinal);
    private readonly Dictionary<(string UserId, int PatientId), Entry> _entries = new();
    private int _epoch;

    public PatientChartCache(IPatientChartLoader loader, IOptions<ChartCacheOptions> options, TimeProvider time)
    {
        _loader = loader;
        _time = time;
        var value = options.Value;
        _capacity = Math.Max(1, value.Capacity);
        _maxSnapshots = Math.Max(1, value.MaxSnapshots);
        _idle = TimeSpan.FromMinutes(Math.Max(1, value.IdleMinutes));
    }

    /// <summary>Returns a cached chart without loading and without changing LRU order.</summary>
    public bool TryPeek(string userId, int patientId, out PatientChartSnapshot? snapshot)
    {
        snapshot = null;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        lock (_gate)
        {
            if (!_entries.TryGetValue((userId, patientId), out var entry) || IsExpired(entry))
            {
                return false;
            }

            snapshot = entry.Snapshot;
            return true;
        }
    }

    public IReadOnlyList<int> PatientIdsOldestFirst(string userId)
    {
        lock (_gate)
        {
            if (!_order.TryGetValue(userId, out var list))
            {
                return Array.Empty<int>();
            }

            var ids = new List<int>();
            for (var node = list.Last; node != null; node = node.Previous)
            {
                ids.Add(node.Value);
            }

            return ids;
        }
    }

    public async Task<ChartCacheOpen> OpenAsync(string userId, int patientId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (TryTakeFresh(userId, patientId, out var cached))
        {
            return new ChartCacheOpen(cached, ChartCacheSource.Cache, 0);
        }

        var epoch = CurrentEpoch();
        var started = Stopwatch.GetTimestamp();
        var loaded = await _loader.LoadAsync(patientId, cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (loaded == null)
        {
            throw new InvalidOperationException($"Patient {patientId} was not found.");
        }

        if (CurrentEpoch() == epoch)
        {
            Store(userId, patientId, loaded);
        }

        return new ChartCacheOpen(loaded, ChartCacheSource.Loader, elapsed);
    }

    /// <summary>
    /// Reloads a chart that is already cached. Does nothing when the user has not opened that patient.
    /// Returns the new snapshot only when the chart data changed.
    /// </summary>
    public async Task<PatientChartSnapshot?> RefreshAsync(string userId, int patientId, CancellationToken cancellationToken = default)
    {
        int epoch;
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(userId) || !_entries.ContainsKey((userId, patientId)) || IsExpired(_entries[(userId, patientId)]))
            {
                return null;
            }

            epoch = _epoch;
        }

        var loaded = await _loader.LoadAsync(patientId, cancellationToken);
        if (loaded == null)
        {
            Remove(userId, patientId);
            return null;
        }

        lock (_gate)
        {
            if (_epoch != epoch || !_entries.TryGetValue((userId, patientId), out var entry))
            {
                return null;
            }

            if (string.Equals(entry.Snapshot.Fingerprint, loaded.Fingerprint, StringComparison.Ordinal))
            {
                return null;
            }

            entry.Snapshot = loaded;
            return loaded;
        }
    }

    public void InvalidatePatient(int patientId)
    {
        lock (_gate)
        {
            _epoch++;
            var keys = _entries.Keys.Where(key => key.PatientId == patientId).ToList();
            foreach (var key in keys)
            {
                RemoveLocked(key.UserId, key.PatientId);
            }
        }
    }

    public void ClearUser(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        lock (_gate)
        {
            if (!_order.TryGetValue(userId, out var list))
            {
                return;
            }

            _epoch++;

            foreach (var patientId in list.ToList())
            {
                _entries.Remove((userId, patientId));
            }

            _order.Remove(userId);
        }
    }

    public void Sweep()
    {
        lock (_gate)
        {
            var expired = _entries.Where(pair => IsExpired(pair.Value)).Select(pair => pair.Key).ToList();
            foreach (var key in expired)
            {
                RemoveLocked(key.UserId, key.PatientId);
            }
        }
    }

    private bool TryTakeFresh(string userId, int patientId, out PatientChartSnapshot snapshot)
    {
        lock (_gate)
        {
            if (_entries.TryGetValue((userId, patientId), out var entry) && !IsExpired(entry))
            {
                entry.LastAccess = _time.GetUtcNow();
                TouchLocked(userId, patientId);
                snapshot = entry.Snapshot;
                return true;
            }
        }

        snapshot = null!;
        return false;
    }

    private void Store(string userId, int patientId, PatientChartSnapshot snapshot)
    {
        lock (_gate)
        {
            var now = _time.GetUtcNow();
            if (_entries.TryGetValue((userId, patientId), out var existing))
            {
                existing.Snapshot = snapshot;
                existing.LastAccess = now;
                TouchLocked(userId, patientId);
            }
            else
            {
                _entries[(userId, patientId)] = new Entry { Snapshot = snapshot, LastAccess = now };
                TouchLocked(userId, patientId);
            }

            TrimUserLocked(userId);
            TrimProcessLocked(userId, patientId);
        }
    }

    private void TouchLocked(string userId, int patientId)
    {
        if (!_order.TryGetValue(userId, out var list))
        {
            list = new LinkedList<int>();
            _order[userId] = list;
        }

        var node = list.Find(patientId);
        if (node != null)
        {
            list.Remove(node);
        }

        list.AddFirst(patientId);
    }

    private void TrimUserLocked(string userId)
    {
        if (!_order.TryGetValue(userId, out var list))
        {
            return;
        }

        while (list.Count > _capacity && list.Last is { } oldest)
        {
            RemoveLocked(userId, oldest.Value);
        }
    }

    private void TrimProcessLocked(string protectedUserId, int protectedPatientId)
    {
        while (_entries.Count > _maxSnapshots)
        {
            (string UserId, int PatientId)? oldestKey = null;
            var oldestAccess = DateTimeOffset.MaxValue;
            foreach (var pair in _entries)
            {
                if (pair.Key == (protectedUserId, protectedPatientId))
                {
                    continue;
                }

                if (pair.Value.LastAccess < oldestAccess)
                {
                    oldestAccess = pair.Value.LastAccess;
                    oldestKey = pair.Key;
                }
            }

            if (oldestKey is not { } key)
            {
                return;
            }

            RemoveLocked(key.UserId, key.PatientId);
        }
    }

    private int CurrentEpoch()
    {
        lock (_gate)
        {
            return _epoch;
        }
    }

    private void Remove(string userId, int patientId)
    {
        lock (_gate)
        {
            RemoveLocked(userId, patientId);
        }
    }

    private void RemoveLocked(string userId, int patientId)
    {
        _entries.Remove((userId, patientId));
        if (_order.TryGetValue(userId, out var list))
        {
            var node = list.Find(patientId);
            if (node != null)
            {
                list.Remove(node);
            }

            if (list.Count == 0)
            {
                _order.Remove(userId);
            }
        }
    }

    private bool IsExpired(Entry entry) => _time.GetUtcNow() - entry.LastAccess >= _idle;

    private sealed class Entry
    {
        public PatientChartSnapshot Snapshot { get; set; } = null!;
        public DateTimeOffset LastAccess { get; set; }
    }
}
