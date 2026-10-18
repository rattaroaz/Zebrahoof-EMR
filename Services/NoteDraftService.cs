using System.Collections.Concurrent;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Unsaved clinical-note text, kept per user, browser, and patient. Drafts expire
/// after 24 hours. An explicit logout drops only that browser's drafts.
/// </summary>
public sealed class NoteDraftService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, NoteDraft> _drafts = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _epoch = new(StringComparer.Ordinal);
    private readonly TimeProvider _time;

    public NoteDraftService()
        : this(TimeProvider.System)
    {
    }

    public NoteDraftService(TimeProvider time)
    {
        _time = time;
    }

    public long CurrentEpoch(string? browserId)
    {
        if (!IsBrowser(browserId))
        {
            return 0;
        }

        return _epoch.TryGetValue(browserId!, out var epoch) ? epoch : 0;
    }

    /// <summary>
    /// Explicit logout of one browser. Other browsers of the same user keep their drafts.
    /// A circuit that already captured this epoch cannot write the draft back.
    /// </summary>
    public void Revoke(string? browserId)
    {
        if (!IsBrowser(browserId))
        {
            return;
        }

        ClearBrowser(browserId!);
        _epoch.AddOrUpdate(browserId!, 1, (_, current) => current + 1);
    }

    public bool TrySave(string? user, string? browserId, int patientId, NoteDraft draft, long epoch)
    {
        if (!IsBrowser(browserId) || epoch != CurrentEpoch(browserId))
        {
            return false;
        }

        Save(user, browserId, patientId, draft);
        return true;
    }

    public void Save(string? user, string? browserId, int patientId, NoteDraft draft)
    {
        var key = Key(user, browserId, patientId);
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

    public NoteDraft? Get(string? user, string? browserId, int patientId)
    {
        var key = Key(user, browserId, patientId);
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

    public void Clear(string? user, string? browserId, int patientId)
    {
        var key = Key(user, browserId, patientId);
        if (key != null)
        {
            _drafts.TryRemove(key, out _);
        }
    }

    private void ClearBrowser(string browserId)
    {
        var marker = "\u001f" + browserId + "\u001f";
        foreach (var key in _drafts.Keys)
        {
            if (key.Contains(marker, StringComparison.Ordinal))
            {
                _drafts.TryRemove(key, out _);
            }
        }
    }

    private static bool IsBrowser(string? browserId) =>
        !string.IsNullOrWhiteSpace(browserId) && browserId.IndexOf('\u001f') < 0;

    private static string? Key(string? user, string? browserId, int patientId)
    {
        if (string.IsNullOrWhiteSpace(user) || !IsBrowser(browserId) || patientId <= 0)
        {
            return null;
        }

        return user.Trim().ToUpperInvariant() + "\u001f" + browserId + "\u001f" + patientId;
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
