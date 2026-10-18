using System.Collections.Concurrent;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Unsaved clinical-note text, kept per user and patient across sign-out.
/// </summary>
public sealed class NoteDraftService
{
    private readonly ConcurrentDictionary<string, NoteDraft> _drafts = new(StringComparer.OrdinalIgnoreCase);

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

        _drafts[key] = draft;
    }

    public NoteDraft? Get(string? user, int patientId)
    {
        var key = Key(user, patientId);
        if (key == null)
        {
            return null;
        }

        return _drafts.TryGetValue(key, out var draft) ? draft.Copy() : null;
    }

    public void Clear(string? user, int patientId)
    {
        var key = Key(user, patientId);
        if (key != null)
        {
            _drafts.TryRemove(key, out _);
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
        Plan = Plan
    };
}
