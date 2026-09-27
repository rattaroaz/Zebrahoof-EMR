using System.Globalization;
using System.Text;
using System.Text.Json;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public sealed record ClinicAskPatientRef(int Id, string Name);

public sealed record ClinicAskResult(
    string Answer,
    IReadOnlyList<ClinicAskPatientRef> Patients,
    bool WarnMix,
    IReadOnlyList<string> Applied);

/// <summary>
/// Dashboard ask: answers questions across the clinic record (who did I see, who has X).
/// </summary>
public sealed class DashboardAskService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly MockClinicalDataService _clinical;
    private readonly MockPatientService _patients;
    private readonly MockAppointmentService _appointments;
    private readonly PatientChartAskService _chartAsk;
    private readonly IClinicalAiService _ai;
    private readonly ILogger<DashboardAskService> _logger;

    public DashboardAskService(
        MockClinicalDataService clinical,
        MockPatientService patients,
        MockAppointmentService appointments,
        PatientChartAskService chartAsk,
        IClinicalAiService ai,
        ILogger<DashboardAskService> logger)
    {
        _clinical = clinical;
        _patients = patients;
        _appointments = appointments;
        _chartAsk = chartAsk;
        _ai = ai;
        _logger = logger;
    }

    public async Task<ClinicAskResult> AskAsync(
        string userMessage,
        IEnumerable<ChartAskTurn> history,
        CancellationToken cancellationToken = default)
    {
        var index = await BuildIndexAsync();
        var system = BuildSystemPrompt(index);
        var raw = await _ai.ChatAsync(system, history.Select(t => new ChatTurn(t.UserInput, t.AssistantResponse)), userMessage, cancellationToken);

        var roster = await _patients.GetAllPatientsAsync();
        if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
        {
            return new ClinicAskResult(raw ?? "No response from the local AI engine.", [], false, []);
        }

        if (!TryParse(raw, out var parsed, out var parseError) || parsed == null)
        {
            _logger.LogWarning("Dashboard ask response was not structured JSON: {Error}", parseError);
            return await FinishClinicAskAsync(raw.Trim(), userMessage, roster, [], parsedActions: []);
        }

        var hits = new List<ClinicAskPatientRef>();
        foreach (var item in parsed.Patients)
        {
            var match = roster.FirstOrDefault(p => p.Id == item.Id)
                        ?? roster.FirstOrDefault(p =>
                            !string.IsNullOrWhiteSpace(item.Name) &&
                            p.FullName.Contains(item.Name, StringComparison.OrdinalIgnoreCase));
            if (match != null && hits.All(h => h.Id != match.Id))
            {
                hits.Add(new ClinicAskPatientRef(match.Id, match.FullName));
            }
        }

        var answer = string.IsNullOrWhiteSpace(parsed.Answer) ? raw.Trim() : parsed.Answer.Trim();
        return await FinishClinicAskAsync(answer, userMessage, roster, hits, parsed.Actions);
    }

    public async Task<string> BuildIndexAsync()
    {
        var today = DateTime.Today;
        var patients = await _patients.GetAllPatientsAsync();
        var problems = await _clinical.GetAllProblemsAsync();
        var encounters = await _clinical.GetAllEncountersAsync();
        var meds = await _clinical.GetAllMedicationsAsync();
        var allergies = await _clinical.GetAllAllergiesAsync();
        var appointments = await _appointments.GetAppointmentsByDateRangeAsync(today.AddDays(-120), today.AddDays(14));

        var sb = new StringBuilder();
        sb.AppendLine($"TODAY: {today:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("PATIENTS (id|name|mrn|dob|age|sex|provider|lastVisit):");
        foreach (var p in patients.OrderBy(p => p.Id))
        {
            sb.AppendLine($"{p.Id}|{p.FullName}|{p.MRN}|{p.DateOfBirth:yyyy-MM-dd}|{p.Age}|{p.Sex}|{p.PrimaryProvider}|{p.LastVisit:yyyy-MM-dd}");
        }

        sb.AppendLine();
        sb.AppendLine("PROBLEMS (patientId|name|status|icd):");
        foreach (var problem in problems)
        {
            sb.AppendLine($"{problem.PatientId}|{problem.Name}|{problem.Status}|{problem.IcdCode}");
        }

        sb.AppendLine();
        sb.AppendLine("ENCOUNTERS (patientId|date|daysAgo|type|provider|complaint|assessment):");
        foreach (var encounter in encounters)
        {
            var days = (today - encounter.DateTime.Date).Days;
            sb.AppendLine($"{encounter.PatientId}|{encounter.DateTime:yyyy-MM-dd}|{days}|{encounter.VisitType}|{encounter.Provider}|{encounter.ChiefComplaint}|{encounter.Assessment}");
        }

        sb.AppendLine();
        sb.AppendLine("APPOINTMENTS last 120d / next 14d (patientId|name|date|daysAgo|type|provider|status):");
        foreach (var apt in appointments.OrderByDescending(a => a.DateTime))
        {
            var days = (today - apt.DateTime.Date).Days;
            sb.AppendLine($"{apt.PatientId}|{apt.PatientName}|{apt.DateTime:yyyy-MM-dd}|{days}|{apt.VisitType}|{apt.Provider}|{apt.Status}");
        }

        sb.AppendLine();
        sb.AppendLine("ACTIVE MEDS (patientId|name|dose):");
        foreach (var med in meds.Where(m => m.Status == MedicationStatus.Active))
        {
            sb.AppendLine($"{med.PatientId}|{med.Name}|{med.Dose} {med.Frequency}");
        }

        sb.AppendLine();
        sb.AppendLine("ALLERGIES (patientId|allergen|reaction):");
        foreach (var allergy in allergies.Where(a => a.Status == AllergyStatus.Active))
        {
            sb.AppendLine($"{allergy.PatientId}|{allergy.Allergen}|{allergy.Reaction}");
        }

        return sb.ToString();
    }

    private async Task<ClinicAskResult> FinishClinicAskAsync(
        string answer,
        string userMessage,
        IReadOnlyList<Patient> roster,
        List<ClinicAskPatientRef> hits,
        IEnumerable<ChartAskAction> parsedActions)
    {
        var applied = new List<string>();
        var failed = new List<string>();
        foreach (var action in parsedActions)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.Op))
            {
                continue;
            }

            var patient = ResolveActionPatient(action, roster)
                          ?? AskAppointmentActions.ResolveNamedPatient(userMessage, roster);
            if (patient == null)
            {
                failed.Add($"{action.Op} needs a named patient");
                continue;
            }

            if (hits.All(h => h.Id != patient.Id))
            {
                hits.Add(new ClinicAskPatientRef(patient.Id, patient.FullName));
            }

            await _chartAsk.ApplyActionsAsync(patient, [action], applied, failed);
            _logger.LogInformation(
                "Management ask applied {Op} for patient {PatientId}; applied={AppliedCount} failed={FailedCount}",
                action.Op,
                patient.Id,
                applied.Count,
                failed.Count);
        }

        if (!applied.Any(a => a.StartsWith("Scheduled ", StringComparison.Ordinal))
            && AskAppointmentActions.LooksLikeScheduleRequest(userMessage))
        {
            var named = AskAppointmentActions.ResolveNamedPatient(userMessage, roster);
            if (named == null)
            {
                failed.Add("schedule needs one named patient");
            }
            else
            {
                await _chartAsk.ApplyActionsAsync(named,
                [
                    new ChartAskAction
                    {
                        Op = "add_appointment",
                        PatientId = named.Id,
                        PatientName = named.FullName
                    }
                ], applied, failed);
                if (hits.All(h => h.Id != named.Id))
                {
                    hits.Add(new ClinicAskPatientRef(named.Id, named.FullName));
                }
            }
        }

        if (applied.Count > 0)
        {
            answer += "\n\nChanged:\n- " + string.Join("\n- ", applied);
        }

        if (failed.Count > 0)
        {
            answer += "\n\nCould not complete:\n- " + string.Join("\n- ", failed);
        }

        return new ClinicAskResult(
            answer,
            hits,
            PatientAskGuard.ShouldWarnInManagementMode(userMessage, roster),
            applied);
    }

    private static Patient? ResolveActionPatient(ChartAskAction action, IReadOnlyList<Patient> roster)
    {
        if (action.PatientId is int id && id > 0)
        {
            var byId = roster.FirstOrDefault(p => p.Id == id);
            if (byId != null)
            {
                return byId;
            }
        }

        foreach (var label in new[] { action.PatientName, action.Name })
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            var match = roster.FirstOrDefault(p => p.FullName.Equals(label.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    public static bool TryParse(string raw, out ClinicAskResponse? parsed, out string error)
    {
        parsed = null;
        error = string.Empty;
        try
        {
            parsed = JsonSerializer.Deserialize<ClinicAskResponse>(PatientChartAskService.ExtractJson(raw), JsonOptions);
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

    private static string BuildSystemPrompt(string index) =>
        $$"""
        You are the on-machine clinic assistant for Zebrahoof EMR in management mode (no patient chart is open).
        Today is {{DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}}.
        Answer generally. Do not assume a particular patient unless the user names one or clearly asks about a specific person.
        Answer from the clinic index. You may recall who was seen, who has a diagnosis, medications, allergies, and appointments.
        You can change the clinic schedule and, for a named patient, any chart item the signed-in user is allowed to change.
        If their role cannot do it, do not pretend you did — the system will refuse and you must tell them.
        When they ask to add, book, or schedule a named patient, emit add_appointment.
        For add_appointment include patientId or patientName, and scheduledAt (ISO datetime) when they give a time. Omit scheduledAt to use the next open clinic slot (08:00–17:00).
        Never say you scheduled someone unless you emit add_appointment. Do not invent patients.
        For relative time ("about 2 weeks ago", "last month"), use daysAgo and dates. Prefer the closest match and say if it is approximate.
        If nothing matches, say so.

        Respond with JSON only (no markdown fences):
        {
          "answer": "plain-language reply",
          "patients": [ { "id": 2, "name": "Jane Smith" } ],
          "actions": [ { "op": "add_appointment", "patientId": 4, "patientName": "Maria Garcia", "scheduledAt": "2026-09-08T15:00:00", "visitType": "Follow-up", "provider": "Dr. Sarah Smith" } ]
        }
        Put every patient you name in patients so the UI can open their chart.
        Chart actions must include patientId or patientName. Supported ops include add_appointment, cancel_appointment, add_problem, prescribe, order, add_note, update_demographics, and the other chart ops.

        CLINIC INDEX:
        {{index}}
        """;
}

public sealed class ClinicAskResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<ClinicAskPatientHint> Patients { get; set; } = [];
    public List<ChartAskAction> Actions { get; set; } = [];
}

public sealed class ClinicAskPatientHint
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
