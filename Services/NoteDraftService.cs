using System.Collections.Concurrent;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Unsaved clinical-note text, kept per user and patient. Drafts expire after
/// 24 hours and are removed on sign-out.
/// </summary>
public sealed class NoteDraftService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, NoteDraft> _drafts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _epoch = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeProvider _time;

    public NoteDraftService()
        : this(TimeProvider.System)
    {
    }

    public NoteDraftService(TimeProvider time)
    {
        _time = time;
    }

    public long CurrentEpoch(string? user)
    {
        if (string.IsNullOrWhiteSpace(user))
        {
            return 0;
        }

        return _epoch.TryGetValue(user.Trim(), out var epoch) ? epoch : 0;
    }

    /// <summary>
    /// Explicit sign-out. Clears drafts and advances the epoch so a circuit that
    /// was already open cannot write the draft back.
    /// </summary>
    public void SignOut(string? user)
    {
        ClearUser(user);
        if (string.IsNullOrWhiteSpace(user))
        {
            return;
        }

        _epoch.AddOrUpdate(user.Trim(), 1, (_, current) => current + 1);
    }

    public bool TrySave(string? user, int patientId, NoteDraft draft, long epoch)
    {
        if (epoch != CurrentEpoch(user))
        {
            return false;
        }

        Save(user, patientId, draft);
        return true;
    }

    public void Save(string? user, int patientId, NoteDraft draft)
    {
        var key = Key(user, patientId);
        if (key == null)
        {
            return;
        }

        if (draft.IsEmpty)
        {
            _drafts.TryRemove(key, out _);
            return;
        }

        draft.SavedAt = _time.GetUtcNow();
        _drafts[key] = draft;
    }

    public int Sweep()
    {
        var now = _time.GetUtcNow();
        var removed = 0;
        foreach (var pair in _drafts)
        {
            if (now - pair.Value.SavedAt >= Lifetime && _drafts.TryRemove(pair.Key, out _))
            {
                removed++;
            }
        }

        return removed;
    }

    public NoteDraft? Get(string? user, int patientId)
    {
        var key = Key(user, patientId);
        if (key == null || !_drafts.TryGetValue(key, out var draft))
        {
            return null;
        }

        if (_time.GetUtcNow() - draft.SavedAt >= Lifetime)
        {
            _drafts.TryRemove(key, out _);
            return null;
        }

        return draft.Copy();
    }

    public void Clear(string? user, int patientId)
    {
        var key = Key(user, patientId);
        if (key != null)
        {
            _drafts.TryRemove(key, out _);
        }
    }

    public void ClearUser(string? user)
    {
        if (string.IsNullOrWhiteSpace(user))
        {
            return;
        }

        var prefix = user.Trim() + ":";
        foreach (var key in _drafts.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                _drafts.TryRemove(key, out _);
            }
        }
    }

    private static string? Key(string? user, int patientId)
    {
        if (string.IsNullOrWhiteSpace(user) || patientId <= 0)
        {
            return null;
        }

        return user.Trim() + ":" + patientId;
    }
}

public sealed class NoteDraft
{
    public string? ChiefComplaint { get; set; }
    public string? HistoryOfPresentIllness { get; set; }
    public string? ReviewOfSystems { get; set; }
    public string? PhysicalExam { get; set; }
    public string? Assessment { get; set; }
    public string? Plan { get; set; }
    public DateTimeOffset SavedAt { get; set; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(ChiefComplaint)
        && string.IsNullOrWhiteSpace(HistoryOfPresentIllness)
        && string.IsNullOrWhiteSpace(ReviewOfSystems)
        && string.IsNullOrWhiteSpace(PhysicalExam)
        && string.IsNullOrWhiteSpace(Assessment)
        && string.IsNullOrWhiteSpace(Plan);

    public NoteDraft Copy() => new()
    {
        ChiefComplaint = ChiefComplaint,
        HistoryOfPresentIllness = HistoryOfPresentIllness,
        ReviewOfSystems = ReviewOfSystems,
        PhysicalExam = PhysicalExam,
        Assessment = Assessment,
        Plan = Plan,
        SavedAt = SavedAt
    };
}
