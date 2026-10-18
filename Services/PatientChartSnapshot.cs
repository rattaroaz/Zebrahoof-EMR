using System.Text;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Chart data captured when a user actually opens a patient. Nothing here is filled by prefetch.
/// </summary>
public sealed class PatientChartSnapshot
{
    public int PatientId { get; init; }
    public Patient Patient { get; init; } = null!;
    public IReadOnlyList<Problem> Problems { get; init; } = Array.Empty<Problem>();
    public IReadOnlyList<Medication> Medications { get; init; } = Array.Empty<Medication>();
    public IReadOnlyList<Allergy> Allergies { get; init; } = Array.Empty<Allergy>();
    public IReadOnlyList<LabResult> Labs { get; init; } = Array.Empty<LabResult>();
    public IReadOnlyList<VitalSigns> Vitals { get; init; } = Array.Empty<VitalSigns>();
    public IReadOnlyList<ClinicalNote> Notes { get; init; } = Array.Empty<ClinicalNote>();
    public IReadOnlyList<Encounter> Encounters { get; init; } = Array.Empty<Encounter>();
    public IReadOnlyList<RiskScoreEntry> RiskScores { get; init; } = Array.Empty<RiskScoreEntry>();
    public IReadOnlyList<SimulatedOrderRecord> Orders { get; init; } = Array.Empty<SimulatedOrderRecord>();
    public IReadOnlyList<ImagingStudy> Imaging { get; init; } = Array.Empty<ImagingStudy>();
    public IReadOnlyList<Immunization> Immunizations { get; init; } = Array.Empty<Immunization>();
    public IReadOnlyList<CareTeamMember> CareTeam { get; init; } = Array.Empty<CareTeamMember>();
    public IReadOnlyList<Document> Documents { get; init; } = Array.Empty<Document>();
    public string Fingerprint { get; init; } = "";
    public DateTimeOffset LoadedAt { get; init; }

    public static string BuildFingerprint(
        Patient patient,
        IReadOnlyList<Problem> problems,
        IReadOnlyList<Medication> medications,
        IReadOnlyList<Allergy> allergies,
        IReadOnlyList<LabResult> labs,
        IReadOnlyList<VitalSigns> vitals,
        IReadOnlyList<ClinicalNote> notes,
        IReadOnlyList<Encounter> encounters,
        IReadOnlyList<Document>? documents = null)
    {
        var text = new StringBuilder();
        text.Append(patient.Id).Append('|')
            .Append(patient.FirstName).Append('|')
            .Append(patient.LastName).Append('|')
            .Append(patient.MRN).Append('|')
            .Append(string.Join(',', patient.Allergies)).Append('|')
            .Append(string.Join(',', patient.Alerts)).Append('|');
        Append(text, problems.Select(p => $"{p.Id}:{p.Status}:{p.Name}"));
        Append(text, medications.Select(m => $"{m.Id}:{m.Status}:{m.Name}:{m.Dose}"));
        Append(text, allergies.Select(a => $"{a.Id}:{a.Status}:{a.Allergen}:{a.Severity}"));
        Append(text, labs.Select(l => $"{l.Id}:{l.Status}:{l.TestName}:{l.Value}:{l.IsAbnormal}"));
        Append(text, vitals.Select(v => $"{v.Id}:{v.RecordedAt.Ticks}:{v.Temperature}:{v.SystolicBP}"));
        Append(text, notes.Select(n => $"{n.Id}:{n.CreatedAt.Ticks}:{n.Status}:{n.ChiefComplaint}"));
        Append(text, encounters.Select(e => $"{e.Id}:{e.Status}:{e.DateTime.Ticks}"));
        if (documents != null)
        {
            Append(text, documents.Select(d => $"{d.Id}:{d.Name}:{d.Category}:{d.Date.Ticks}"));
        }

        return text.ToString();
    }

    private static void Append(StringBuilder text, IEnumerable<string> parts)
    {
        foreach (var part in parts)
        {
            text.Append(part).Append(';');
        }

        text.Append('|');
    }
}

public static class ChartCacheSource
{
    public const string Cache = "cache";
    public const string Loader = "loader";
}

public sealed record ChartCacheOpen(PatientChartSnapshot Snapshot, string Source, double LoadMilliseconds);
