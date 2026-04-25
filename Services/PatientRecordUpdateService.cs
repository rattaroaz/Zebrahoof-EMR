using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Logging;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Orchestrates the "Update Records" workflow:
///   1) Gather the current chart (problems, medications, allergies) and any uploaded
///      documents for a patient.
///   2) Ask Grok to reconcile them and return an updated chart as JSON.
///   3) Replace in-memory mock state so tabs refresh and persist the new chart +
///      documents to the local SQLite database.
/// </summary>
public class PatientRecordUpdateService
{
    private const int MaxDocumentChars = 18_000;
    private const int MaxTotalDocChars = 60_000;

    private readonly MockClinicalDataService _clinical;
    private readonly GrokApiService _grok;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PatientRecordUpdateService> _logger;

    public PatientRecordUpdateService(
        MockClinicalDataService clinical,
        GrokApiService grok,
        ApplicationDbContext db,
        ILogger<PatientRecordUpdateService> logger)
    {
        _clinical = clinical;
        _grok = grok;
        _db = db;
        _logger = logger;
    }

    public async Task<UpdateRecordsResult> UpdateAsync(Patient patient)
    {
        var problems = await _clinical.GetProblemsByPatientAsync(patient.Id);
        var medications = await _clinical.GetMedicationsByPatientAsync(patient.Id);
        var allergies = await _clinical.GetAllergiesByPatientAsync(patient.Id);

        var documents = await _db.Documents
            .Where(d => d.PatientId == patient.Id)
            .OrderByDescending(d => d.Date)
            .ToListAsync();

        var prompt = BuildPrompt(patient, problems, medications, allergies, documents);
        var rawResponse = await _grok.ProcessDocumentAsync(string.Empty, prompt);

        if (string.IsNullOrWhiteSpace(rawResponse) ||
            rawResponse.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
        {
            return UpdateRecordsResult.Failure(rawResponse ?? "No response from Grok.");
        }

        if (!TryParse(rawResponse, out var parsed, out var parseError))
        {
            _logger.LogWarning("Failed to parse Grok response: {Error}. Raw (truncated): {RawPrefix}", parseError,
                SafeLogContent.Truncate(rawResponse, SafeLogContent.DefaultMaxLength));
            return UpdateRecordsResult.Failure($"Could not parse Grok response: {parseError}");
        }

        var newProblems = parsed!.Problems.Select(p => p.ToDomain()).ToList();
        var newMedications = parsed.Medications.Select(m => m.ToDomain()).ToList();
        var newAllergies = parsed.Allergies.Select(a => a.ToDomain()).ToList();

        // Update the in-memory mock service so tabs reflect the new chart immediately.
        _clinical.ReplaceProblemsForPatient(patient.Id, newProblems);
        _clinical.ReplaceMedicationsForPatient(patient.Id, newMedications);
        _clinical.ReplaceAllergiesForPatient(patient.Id, newAllergies);

        // Persist the new chart to the local database.
        await PersistAsync(patient.Id, newProblems, newMedications, newAllergies);

        _clinical.NotifyPatientDataChanged(patient.Id);

        return UpdateRecordsResult.Success(
            problemsBefore: problems.Count,
            medicationsBefore: medications.Count,
            allergiesBefore: allergies.Count,
            problemsAfter: newProblems.Count,
            medicationsAfter: newMedications.Count,
            allergiesAfter: newAllergies.Count,
            documentsConsidered: documents.Count,
            summary: parsed.Summary);
    }

    private async Task PersistAsync(
        int patientId,
        List<Problem> problems,
        List<Medication> medications,
        List<Allergy> allergies)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var existingProblems = await _db.Problems.Where(p => p.PatientId == patientId).ToListAsync();
        _db.Problems.RemoveRange(existingProblems);

        var existingMeds = await _db.Medications.Where(m => m.PatientId == patientId).ToListAsync();
        _db.Medications.RemoveRange(existingMeds);

        var existingAllergies = await _db.Allergies.Where(a => a.PatientId == patientId).ToListAsync();
        _db.Allergies.RemoveRange(existingAllergies);

        foreach (var p in problems)
        {
            _db.Problems.Add(new Problem
            {
                PatientId = patientId,
                Name = p.Name,
                IcdCode = p.IcdCode,
                OnsetDate = p.OnsetDate,
                ResolvedDate = p.ResolvedDate,
                Status = p.Status,
                Severity = p.Severity,
                Notes = p.Notes
            });
        }
        foreach (var m in medications)
        {
            _db.Medications.Add(new Medication
            {
                PatientId = patientId,
                Name = m.Name,
                Dose = m.Dose,
                Route = m.Route,
                Frequency = m.Frequency,
                StartDate = m.StartDate,
                EndDate = m.EndDate,
                Prescriber = m.Prescriber,
                Status = m.Status,
                IsHighRisk = m.IsHighRisk,
                IsLongTerm = m.IsLongTerm,
                Instructions = m.Instructions,
                RefillsRemaining = m.RefillsRemaining,
                DaysSupply = m.DaysSupply,
                Pharmacy = m.Pharmacy
            });
        }
        foreach (var a in allergies)
        {
            _db.Allergies.Add(new Allergy
            {
                PatientId = patientId,
                Allergen = a.Allergen,
                Reaction = a.Reaction,
                Severity = a.Severity,
                Status = a.Status,
                OnsetDate = a.OnsetDate
            });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static string BuildPrompt(
        Patient patient,
        List<Problem> problems,
        List<Medication> medications,
        List<Allergy> allergies,
        List<Document> documents)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a clinical informatics assistant. Reconcile a patient chart with new evidence from uploaded documents and return an UPDATED chart.");
        sb.AppendLine();
        sb.AppendLine("RESPOND WITH JSON ONLY (no markdown fences, no commentary). Schema:");
        sb.AppendLine("{");
        sb.AppendLine("  \"summary\": string,                 // 1-3 sentence change summary for the clinician");
        sb.AppendLine("  \"problems\": [ { \"name\": string, \"icdCode\": string|null, \"onsetDate\": \"YYYY-MM-DD\"|null, \"resolvedDate\": \"YYYY-MM-DD\"|null, \"status\": \"Active|Resolved|Inactive\", \"severity\": string|null, \"notes\": string|null } ],");
        sb.AppendLine("  \"medications\": [ { \"name\": string, \"dose\": string, \"route\": string, \"frequency\": string, \"startDate\": \"YYYY-MM-DD\"|null, \"endDate\": \"YYYY-MM-DD\"|null, \"prescriber\": string|null, \"status\": \"Active|Discontinued|OnHold|Completed\", \"isHighRisk\": bool, \"isLongTerm\": bool, \"instructions\": string|null, \"refillsRemaining\": int|null, \"daysSupply\": int|null, \"pharmacy\": string|null } ],");
        sb.AppendLine("  \"allergies\": [ { \"allergen\": string, \"reaction\": string|null, \"severity\": \"Mild|Moderate|Severe|LifeThreatening\", \"status\": \"Active|Inactive|Resolved\", \"onsetDate\": \"YYYY-MM-DD\"|null } ]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- Return the COMPLETE chart, not a diff. Lists you return REPLACE the existing lists.");
        sb.AppendLine("- Preserve existing entries unless the documents clearly contradict, resolve, or update them.");
        sb.AppendLine("- Add entries only when the documents provide explicit evidence.");
        sb.AppendLine("- Never invent ICD codes; leave null if unknown.");
        sb.AppendLine("- Use ISO date strings; never include time components.");
        sb.AppendLine();
        sb.AppendLine($"PATIENT: {patient.FullName} | DOB {patient.DateOfBirth:yyyy-MM-dd} | Sex {patient.Sex} | MRN {patient.MRN}");
        sb.AppendLine();
        sb.AppendLine("CURRENT CHART (JSON):");
        sb.AppendLine(JsonSerializer.Serialize(new
        {
            problems = problems.Select(p => new { p.Name, p.IcdCode, p.OnsetDate, p.ResolvedDate, status = p.Status.ToString(), p.Severity, p.Notes }),
            medications = medications.Select(m => new { m.Name, m.Dose, m.Route, m.Frequency, m.StartDate, m.EndDate, m.Prescriber, status = m.Status.ToString(), m.IsHighRisk, m.IsLongTerm, m.Instructions, m.RefillsRemaining, m.DaysSupply, m.Pharmacy }),
            allergies = allergies.Select(a => new { a.Allergen, a.Reaction, severity = a.Severity.ToString(), status = a.Status.ToString(), a.OnsetDate })
        }, new JsonSerializerOptions { WriteIndented = false }));
        sb.AppendLine();

        sb.AppendLine($"UPLOADED DOCUMENTS ({documents.Count}):");
        if (documents.Count == 0)
        {
            sb.AppendLine("(none)");
        }
        else
        {
            var totalChars = 0;
            foreach (var doc in documents)
            {
                if (totalChars >= MaxTotalDocChars) break;

                var text = doc.ExtractedText ?? string.Empty;
                if (text.Length > MaxDocumentChars)
                {
                    text = text[..MaxDocumentChars] + "\n... [truncated]";
                }
                if (totalChars + text.Length > MaxTotalDocChars)
                {
                    text = text[..(MaxTotalDocChars - totalChars)] + "\n... [truncated]";
                }
                totalChars += text.Length;

                sb.AppendLine("---");
                sb.AppendLine($"DOCUMENT: {doc.FileName ?? doc.Name} | Category: {doc.Category} | Date: {doc.Date:yyyy-MM-dd}");
                sb.AppendLine(string.IsNullOrWhiteSpace(text) ? "(no extractable text)" : text);
            }
        }

        return sb.ToString();
    }

    private static bool TryParse(string raw, out GrokChartResponse? parsed, out string error)
    {
        parsed = null;
        error = string.Empty;
        try
        {
            var json = ExtractJson(raw);
            parsed = JsonSerializer.Deserialize<GrokChartResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
            });
            if (parsed == null)
            {
                error = "Empty JSON.";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string ExtractJson(string raw)
    {
        var trimmed = raw.Trim();
        // Strip ```json ... ``` fences if present.
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0)
            {
                trimmed = trimmed[(firstNewline + 1)..];
            }
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0)
            {
                trimmed = trimmed[..lastFence];
            }
            trimmed = trimmed.Trim();
        }
        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return trimmed[firstBrace..(lastBrace + 1)];
        }
        return trimmed;
    }

    private sealed class GrokChartResponse
    {
        public string Summary { get; set; } = string.Empty;
        public List<GrokProblem> Problems { get; set; } = new();
        public List<GrokMedication> Medications { get; set; } = new();
        public List<GrokAllergy> Allergies { get; set; } = new();
    }

    private sealed class GrokProblem
    {
        public string Name { get; set; } = string.Empty;
        public string? IcdCode { get; set; }
        public DateTime? OnsetDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public ProblemStatus Status { get; set; } = ProblemStatus.Active;
        public string? Severity { get; set; }
        public string? Notes { get; set; }

        public Problem ToDomain() => new()
        {
            Name = Name,
            IcdCode = IcdCode,
            OnsetDate = OnsetDate ?? DateTime.Today,
            ResolvedDate = ResolvedDate,
            Status = Status,
            Severity = Severity,
            Notes = Notes
        };
    }

    private sealed class GrokMedication
    {
        public string Name { get; set; } = string.Empty;
        public string? Dose { get; set; }
        public string? Route { get; set; }
        public string? Frequency { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Prescriber { get; set; }
        public MedicationStatus Status { get; set; } = MedicationStatus.Active;
        public bool IsHighRisk { get; set; }
        public bool IsLongTerm { get; set; } = true;
        public string? Instructions { get; set; }
        public int? RefillsRemaining { get; set; }
        public int? DaysSupply { get; set; }
        public string? Pharmacy { get; set; }

        public Medication ToDomain() => new()
        {
            Name = Name,
            Dose = Dose ?? string.Empty,
            Route = Route ?? string.Empty,
            Frequency = Frequency ?? string.Empty,
            StartDate = StartDate ?? DateTime.Today,
            EndDate = EndDate,
            Prescriber = Prescriber,
            Status = Status,
            IsHighRisk = IsHighRisk,
            IsLongTerm = IsLongTerm,
            Instructions = Instructions,
            RefillsRemaining = RefillsRemaining,
            DaysSupply = DaysSupply,
            Pharmacy = Pharmacy
        };
    }

    private sealed class GrokAllergy
    {
        public string Allergen { get; set; } = string.Empty;
        public string? Reaction { get; set; }
        public AllergySeverity Severity { get; set; } = AllergySeverity.Mild;
        public AllergyStatus Status { get; set; } = AllergyStatus.Active;
        public DateTime? OnsetDate { get; set; }

        public Allergy ToDomain() => new()
        {
            Allergen = Allergen,
            Reaction = Reaction,
            Severity = Severity,
            Status = Status,
            OnsetDate = OnsetDate
        };
    }
}

public sealed record UpdateRecordsResult(
    bool Succeeded,
    string? ErrorMessage,
    string Summary,
    int ProblemsBefore,
    int MedicationsBefore,
    int AllergiesBefore,
    int ProblemsAfter,
    int MedicationsAfter,
    int AllergiesAfter,
    int DocumentsConsidered)
{
    public static UpdateRecordsResult Failure(string error) =>
        new(false, error, string.Empty, 0, 0, 0, 0, 0, 0, 0);

    public static UpdateRecordsResult Success(
        int problemsBefore, int medicationsBefore, int allergiesBefore,
        int problemsAfter, int medicationsAfter, int allergiesAfter,
        int documentsConsidered, string summary) =>
        new(true, null, summary,
            problemsBefore, medicationsBefore, allergiesBefore,
            problemsAfter, medicationsAfter, allergiesAfter,
            documentsConsidered);
}
