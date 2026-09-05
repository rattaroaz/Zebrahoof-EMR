using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public sealed record ChartAskTurn(string UserInput, string AssistantResponse);

public sealed record ChartAskResult(
    string Answer,
    IReadOnlyList<string> Applied,
    IReadOnlyList<string> Failed);

/// <summary>
/// Chart-level ask/order: the local AI can answer questions and apply structured
/// mutations to this patient's record.
/// </summary>
public sealed class PatientChartAskService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    private readonly MockClinicalDataService _clinical;
    private readonly MockPatientService _patients;
    private readonly IClinicalAiService _ai;
    private readonly ILogger<PatientChartAskService> _logger;

    public PatientChartAskService(
        MockClinicalDataService clinical,
        MockPatientService patients,
        IClinicalAiService ai,
        ILogger<PatientChartAskService> logger)
    {
        _clinical = clinical;
        _patients = patients;
        _ai = ai;
        _logger = logger;
    }

    public async Task<ChartAskResult> AskAsync(
        Patient patient,
        string userMessage,
        IEnumerable<ChartAskTurn> history,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await BuildSnapshotAsync(patient);
        var system = BuildSystemPrompt(patient, snapshot);
        var raw = await _ai.ChatAsync(system, history.Select(t => new ChatTurn(t.UserInput, t.AssistantResponse)), userMessage, cancellationToken);

        if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
        {
            return new ChartAskResult(raw ?? "No response from the local AI engine.", [], []);
        }

        if (!TryParse(raw, out var parsed, out var parseError) || parsed == null)
        {
            _logger.LogWarning("Chart ask response was not structured JSON: {Error}", parseError);
            return new ChartAskResult(raw.Trim(), [], []);
        }

        var applied = new List<string>();
        var failed = new List<string>();
        await ApplyActionsAsync(patient, parsed.Actions, applied, failed);

        var answer = string.IsNullOrWhiteSpace(parsed.Answer) ? raw.Trim() : parsed.Answer.Trim();
        if (applied.Count > 0)
        {
            answer += "\n\nChanged on this chart:\n- " + string.Join("\n- ", applied);
        }

        if (failed.Count > 0)
        {
            answer += "\n\nCould not complete:\n- " + string.Join("\n- ", failed);
        }

        return new ChartAskResult(answer, applied, failed);
    }

    public async Task ApplyActionsAsync(
        Patient patient,
        IEnumerable<ChartAskAction> actions,
        List<string> applied,
        List<string> failed)
    {
        var changed = false;
        foreach (var action in actions)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.Op))
            {
                continue;
            }

            try
            {
                var summary = await ApplyOneAsync(patient, action);
                if (summary.StartsWith("skip:", StringComparison.OrdinalIgnoreCase))
                {
                    failed.Add(summary[5..].Trim());
                }
                else
                {
                    applied.Add(summary);
                    changed = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Chart ask action {Op} failed for patient {PatientId}", action.Op, patient.Id);
                failed.Add($"{action.Op}: {ex.Message}");
            }
        }

        if (changed)
        {
            _clinical.NotifyPatientDataChanged(patient.Id);
        }
    }

    public static bool TryParse(string raw, out ChartAskResponse? parsed, out string error)
    {
        parsed = null;
        error = string.Empty;
        try
        {
            parsed = JsonSerializer.Deserialize<ChartAskResponse>(ExtractJson(raw), JsonOptions);
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

    private async Task<string> ApplyOneAsync(Patient patient, ChartAskAction action)
    {
        var op = action.Op.Trim().ToLowerInvariant().Replace('-', '_');
        return op switch
        {
            "add_problem" => await AddProblemAsync(patient.Id, action),
            "update_problem" or "resolve_problem" => await UpdateProblemAsync(patient.Id, action, resolve: op == "resolve_problem"),
            "remove_problem" => await RemoveProblemAsync(patient.Id, action),
            "add_medication" => await AddMedicationAsync(patient.Id, action),
            "update_medication" => await UpdateMedicationAsync(patient.Id, action),
            "discontinue_medication" => await DiscontinueMedicationAsync(patient.Id, action),
            "prescribe" => await PrescribeAsync(patient.Id, action),
            "refill" => await RefillAsync(patient.Id, action),
            "add_allergy" => await AddAllergyAsync(patient.Id, action),
            "remove_allergy" or "resolve_allergy" => await RemoveAllergyAsync(patient.Id, action),
            "add_vitals" => await AddVitalsAsync(patient.Id, action),
            "add_note" => await AddNoteAsync(patient.Id, action),
            "add_immunization" => await AddImmunizationAsync(patient.Id, action),
            "order" or "order_lab" or "order_imaging" => await OrderAsync(patient, action),
            "add_care_team" => await AddCareTeamAsync(patient.Id, action),
            "create_task" => await CreateTaskAsync(patient, action),
            "send_message" => await SendMessageAsync(patient, action),
            "update_demographics" => await UpdateDemographicsAsync(patient, action),
            "update_encounter" => await UpdateEncounterAsync(patient.Id, action),
            _ => $"skip: unknown action '{action.Op}'"
        };
    }

    private async Task<string> AddProblemAsync(int patientId, ChartAskAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Name))
        {
            return "skip: add_problem needs a name";
        }

        await _clinical.AddProblemAsync(new Problem
        {
            PatientId = patientId,
            Name = action.Name.Trim(),
            IcdCode = action.IcdCode,
            OnsetDate = ParseDate(action.OnsetDate) ?? DateTime.Today,
            Status = ParseEnum(action.Status, ProblemStatus.Active),
            Severity = action.Severity,
            Notes = action.Notes
        });
        return $"Added problem {action.Name}";
    }

    private async Task<string> UpdateProblemAsync(int patientId, ChartAskAction action, bool resolve)
    {
        if (string.IsNullOrWhiteSpace(action.Name))
        {
            return "skip: problem name required";
        }

        var updated = await _clinical.UpdateProblemAsync(patientId, action.Name, problem =>
        {
            if (resolve || string.Equals(action.Status, "Resolved", StringComparison.OrdinalIgnoreCase))
            {
                problem.Status = ProblemStatus.Resolved;
                problem.ResolvedDate = DateTime.Today;
            }
            else if (!string.IsNullOrWhiteSpace(action.Status))
            {
                problem.Status = ParseEnum(action.Status, problem.Status);
            }

            if (!string.IsNullOrWhiteSpace(action.IcdCode)) problem.IcdCode = action.IcdCode;
            if (!string.IsNullOrWhiteSpace(action.Severity)) problem.Severity = action.Severity;
            if (!string.IsNullOrWhiteSpace(action.Notes)) problem.Notes = action.Notes;
        });

        return updated == null
            ? $"skip: no problem matching '{action.Name}'"
            : resolve ? $"Resolved problem {updated.Name}" : $"Updated problem {updated.Name}";
    }

    private async Task<string> RemoveProblemAsync(int patientId, ChartAskAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Name))
        {
            return "skip: problem name required";
        }

        var removed = await _clinical.RemoveProblemAsync(patientId, action.Name);
        return removed ? $"Removed problem {action.Name}" : $"skip: no problem matching '{action.Name}'";
    }

    private async Task<string> AddMedicationAsync(int patientId, ChartAskAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Name))
        {
            return "skip: add_medication needs a name";
        }

        await _clinical.AddMedicationToListAsync(
            patientId,
            action.Name.Trim(),
            action.Dose ?? string.Empty,
            action.Route ?? "PO",
            action.Frequency ?? string.Empty,
            action.Prescriber,
            action.Pharmacy,
            action.Instructions,
            action.IsHighRisk ?? false);
        return $"Added medication {action.Name}";
    }

    private async Task<string> UpdateMedicationAsync(int patientId, ChartAskAction action)
    {
        var med = await FindMedicationAsync(patientId, action.Name);
        if (med == null)
        {
            return $"skip: no medication matching '{action.Name}'";
        }

        await _clinical.UpdateMedicationAsync(
            med.Id,
            dose: action.Dose,
            route: action.Route,
            frequency: action.Frequency,
            prescriber: action.Prescriber,
            pharmacy: action.Pharmacy,
            instructions: action.Instructions,
            daysSupply: action.DaysSupply);
        return $"Updated medication {med.Name}";
    }

    private async Task<string> DiscontinueMedicationAsync(int patientId, ChartAskAction action)
    {
        var med = await FindMedicationAsync(patientId, action.Name);
        if (med == null)
        {
            return $"skip: no medication matching '{action.Name}'";
        }

        await _clinical.DiscontinueMedicationAsync(med.Id, action.Reason ?? action.Notes);
        return $"Discontinued {med.Name}";
    }

    private async Task<string> PrescribeAsync(int patientId, ChartAskAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Name))
        {
            return "skip: prescribe needs a medication name";
        }

        await _clinical.PrescribeMedicationAsync(
            patientId,
            action.Name.Trim(),
            action.Dose ?? string.Empty,
            action.Route ?? "PO",
            action.Frequency ?? string.Empty,
            action.IsLongTerm ?? true,
            action.DaysSupply,
            action.Quantity,
            action.Refills,
            action.Instructions,
            action.Prescriber,
            action.Pharmacy,
            action.IsHighRisk ?? false);
        return $"Prescribed {action.Name}";
    }

    private async Task<string> RefillAsync(int patientId, ChartAskAction action)
    {
        var med = await FindMedicationAsync(patientId, action.Name);
        if (med == null)
        {
            return $"skip: no medication matching '{action.Name}'";
        }

        await _clinical.OrderRefillAsync(patientId, med.Id, action.Prescriber, action.DaysSupply, action.Quantity);
        return $"Refilled {med.Name}";
    }

    private async Task<string> AddAllergyAsync(int patientId, ChartAskAction action)
    {
        var allergen = action.Allergen ?? action.Name;
        if (string.IsNullOrWhiteSpace(allergen))
        {
            return "skip: add_allergy needs an allergen";
        }

        await _clinical.AddAllergyAsync(new Allergy
        {
            PatientId = patientId,
            Allergen = allergen.Trim(),
            Reaction = action.Reaction,
            Severity = ParseEnum(action.Severity, AllergySeverity.Moderate),
            Status = AllergyStatus.Active,
            OnsetDate = ParseDate(action.OnsetDate) ?? DateTime.Today
        });
        return $"Added allergy {allergen}";
    }

    private async Task<string> RemoveAllergyAsync(int patientId, ChartAskAction action)
    {
        var allergen = action.Allergen ?? action.Name;
        if (string.IsNullOrWhiteSpace(allergen))
        {
            return "skip: allergy name required";
        }

        var removed = await _clinical.RemoveAllergyAsync(patientId, allergen);
        return removed ? $"Removed allergy {allergen}" : $"skip: no allergy matching '{allergen}'";
    }

    private async Task<string> AddVitalsAsync(int patientId, ChartAskAction action)
    {
        await _clinical.AddVitalsAsync(new VitalSigns
        {
            PatientId = patientId,
            RecordedAt = DateTime.Now,
            RecordedBy = action.RecordedBy ?? "Local AI",
            Temperature = action.Temperature,
            SystolicBP = action.SystolicBp,
            DiastolicBP = action.DiastolicBp,
            HeartRate = action.HeartRate,
            RespiratoryRate = action.RespiratoryRate,
            OxygenSaturation = action.OxygenSaturation,
            Weight = action.Weight,
            Height = action.Height,
            BMI = action.Bmi
        });
        return "Recorded vitals";
    }

    private async Task<string> AddNoteAsync(int patientId, ChartAskAction action)
    {
        var encounters = await _clinical.GetEncountersByPatientAsync(patientId);
        var encounterId = encounters.FirstOrDefault()?.Id ?? 0;
        await _clinical.CreateNoteAsync(new ClinicalNote
        {
            PatientId = patientId,
            EncounterId = encounterId,
            AuthorName = action.Author ?? "Local AI",
            Status = NoteStatus.InProgress,
            ChiefComplaint = action.ChiefComplaint,
            HistoryOfPresentIllness = action.History,
            Assessment = action.Assessment,
            Plan = action.Plan ?? action.Notes
        });
        return "Added clinical note";
    }

    private async Task<string> AddImmunizationAsync(int patientId, ChartAskAction action)
    {
        var name = action.VaccineName ?? action.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            return "skip: immunization needs a vaccine name";
        }

        await _clinical.AddImmunizationAsync(new Immunization
        {
            PatientId = patientId,
            VaccineName = name.Trim(),
            AdministeredBy = action.AdministeredBy ?? action.Author ?? "Staff",
            Status = ImmunizationStatus.Completed,
            Notes = action.Notes,
            Route = action.Route,
            Site = action.Site
        });
        return $"Recorded immunization {name}";
    }

    private async Task<string> OrderAsync(Patient patient, ChartAskAction action)
    {
        var name = action.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            return "skip: order needs a name";
        }

        var catalog = _clinical.GetSimulatedOrderCatalog();
        var spec = catalog.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                   ?? catalog.FirstOrDefault(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        spec ??= new SimulatedOrderSpec
        {
            Type = GuessOrderType(action.Op, action.Category),
            Name = name.Trim(),
            Category = action.Category ?? "Ordered",
            Destination = GuessOrderType(action.Op, action.Category) is OrderType.Imaging or OrderType.Ekg ? "Imaging" : "Labs",
            IncludedTests = [name.Trim()]
        };

        var provider = action.Prescriber ?? patient.PrimaryProvider ?? "Ordering clinician";
        await _clinical.SignSimulatedCartAsync(patient.Id, patient.FullName, provider,
        [
            new OrderCartItem
            {
                OrderData = spec,
                Priority = string.IsNullOrWhiteSpace(action.Priority) ? "Routine" : action.Priority,
                Details = action.Indication ?? action.Notes ?? action.Details
            }
        ]);
        return $"Ordered {spec.Name}";
    }

    private async Task<string> AddCareTeamAsync(int patientId, ChartAskAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Name))
        {
            return "skip: care team member needs a name";
        }

        await _clinical.AddCareTeamMemberAsync(new CareTeamMember
        {
            PatientId = patientId,
            Name = action.Name.Trim(),
            Role = action.Role ?? "Consultant",
            Specialty = action.Specialty,
            Phone = action.Phone,
            Organization = action.Organization,
            IsPrimary = action.IsPrimary ?? false
        });
        return $"Added care team member {action.Name}";
    }

    private async Task<string> CreateTaskAsync(Patient patient, ChartAskAction action)
    {
        var title = action.Title ?? action.Name;
        if (string.IsNullOrWhiteSpace(title))
        {
            return "skip: task needs a title";
        }

        await _clinical.CreateTaskAsync(new ClinicalTask
        {
            Title = title.Trim(),
            Description = action.Notes ?? action.Details ?? string.Empty,
            Type = ClinicalTaskType.Other,
            Priority = ClinicalTaskPriority.Normal,
            PatientId = patient.Id,
            PatientName = patient.FullName,
            AssignedTo = action.AssignedTo ?? "Care team",
            CreatedBy = "Local AI",
            DueDate = ParseDate(action.DueDate)
        });
        return $"Created task {title}";
    }

    private async Task<string> SendMessageAsync(Patient patient, ChartAskAction action)
    {
        var subject = action.Subject ?? action.Title ?? "Chart message";
        await _clinical.SendMessageAsync(new InboxMessage
        {
            PatientId = patient.Id,
            PatientName = patient.FullName,
            Subject = subject,
            Body = action.Body ?? action.Notes ?? action.Details ?? string.Empty,
            FromName = action.FromName ?? "Local AI",
            FromRole = action.FromRole ?? "Clinical assistant",
            ToName = action.ToName ?? patient.PrimaryProvider ?? "Care team",
            Category = MessageCategory.ClinicalQuestion
        });
        return $"Sent inbox message '{subject}'";
    }

    private async Task<string> UpdateDemographicsAsync(Patient patient, ChartAskAction action)
    {
        var updated = await _patients.UpdatePatientAsync(patient.Id, current =>
        {
            if (!string.IsNullOrWhiteSpace(action.FirstName)) current.FirstName = action.FirstName;
            if (!string.IsNullOrWhiteSpace(action.LastName)) current.LastName = action.LastName;
            if (!string.IsNullOrWhiteSpace(action.Phone)) current.Phone = action.Phone;
            if (!string.IsNullOrWhiteSpace(action.Email)) current.Email = action.Email;
            if (!string.IsNullOrWhiteSpace(action.Address)) current.Address = action.Address;
            if (!string.IsNullOrWhiteSpace(action.City)) current.City = action.City;
            if (!string.IsNullOrWhiteSpace(action.State)) current.State = action.State;
            if (!string.IsNullOrWhiteSpace(action.ZipCode)) current.ZipCode = action.ZipCode;
            if (!string.IsNullOrWhiteSpace(action.PrimaryProvider)) current.PrimaryProvider = action.PrimaryProvider;
            if (!string.IsNullOrWhiteSpace(action.InsuranceName)) current.InsuranceName = action.InsuranceName;
        });

        if (updated == null)
        {
            return "skip: patient not found";
        }

        patient.FirstName = updated.FirstName;
        patient.LastName = updated.LastName;
        patient.Phone = updated.Phone;
        patient.Email = updated.Email;
        patient.Address = updated.Address;
        patient.City = updated.City;
        patient.State = updated.State;
        patient.ZipCode = updated.ZipCode;
        patient.PrimaryProvider = updated.PrimaryProvider;
        patient.InsuranceName = updated.InsuranceName;
        return "Updated demographics";
    }

    private async Task<string> UpdateEncounterAsync(int patientId, ChartAskAction action)
    {
        var encounter = await _clinical.UpdateLatestEncounterAsync(patientId, current =>
        {
            if (!string.IsNullOrWhiteSpace(action.ChiefComplaint)) current.ChiefComplaint = action.ChiefComplaint;
            if (!string.IsNullOrWhiteSpace(action.Assessment)) current.Assessment = action.Assessment;
            if (!string.IsNullOrWhiteSpace(action.Plan)) current.Plan = action.Plan;
        });
        return encounter == null ? "skip: no encounter to update" : "Updated latest encounter";
    }

    private async Task<Medication?> FindMedicationAsync(int patientId, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var meds = await _clinical.GetMedicationsByPatientAsync(patientId);
        return meds.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase))
               ?? meds.FirstOrDefault(m => m.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> BuildSnapshotAsync(Patient patient)
    {
        var problems = await _clinical.GetProblemsByPatientAsync(patient.Id);
        var meds = await _clinical.GetMedicationsByPatientAsync(patient.Id);
        var allergies = await _clinical.GetAllergiesByPatientAsync(patient.Id);
        var vitals = await _clinical.GetLatestVitalsByPatientAsync(patient.Id);
        var labs = await _clinical.GetRecentLabResultsByPatientAsync(patient.Id, 8);
        var imaging = await _clinical.GetImagingByPatientAsync(patient.Id);
        var orders = await _clinical.GetSimulatedOrdersByPatientAsync(patient.Id);
        var notes = await _clinical.GetPatientNotesAsync(patient.Id);
        var immunizations = await _clinical.GetImmunizationsByPatientAsync(patient.Id);
        var encounters = await _clinical.GetEncountersByPatientAsync(patient.Id);

        return JsonSerializer.Serialize(new
        {
            demographics = new
            {
                patient.FullName,
                patient.MRN,
                dob = patient.DateOfBirth.ToString("yyyy-MM-dd"),
                patient.Age,
                patient.Sex,
                patient.Phone,
                patient.Email,
                patient.Address,
                patient.City,
                patient.State,
                patient.ZipCode,
                patient.PrimaryProvider
            },
            problems = problems.Select(p => new { p.Name, p.IcdCode, status = p.Status.ToString(), p.Severity, p.Notes }),
            medications = meds.Select(m => new { m.Name, m.Dose, m.Route, m.Frequency, status = m.Status.ToString(), m.Instructions }),
            allergies = allergies.Select(a => new { a.Allergen, a.Reaction, severity = a.Severity.ToString() }),
            latestVitals = vitals == null ? null : new { vitals.RecordedAt, vitals.SystolicBP, vitals.DiastolicBP, vitals.HeartRate, vitals.Temperature, vitals.OxygenSaturation },
            recentLabs = labs.Select(l => new { l.TestName, l.Value, l.Units, l.IsAbnormal }),
            imaging = imaging.Take(5).Select(i => new { i.Description, i.Impression, i.StudyDate }),
            orders = orders.Take(8).Select(o => new { o.DisplayName, o.Status, o.ResultSummary }),
            immunizations = immunizations.Select(i => new { i.VaccineName, i.AdministeredDate, status = i.Status.ToString() }),
            latestEncounter = encounters.Select(e => new { e.VisitType, e.ChiefComplaint, e.Assessment, e.Plan, e.DateTime }).FirstOrDefault(),
            latestNote = notes.Select(n => new { n.Assessment, n.Plan, n.ChiefComplaint }).FirstOrDefault()
        }, new JsonSerializerOptions { WriteIndented = false });
    }

    private static string BuildSystemPrompt(Patient patient, string snapshot) =>
        $$"""
        You are the on-machine clinical assistant for Zebrahoof EMR. The clinician is looking at {{patient.FullName}}'s chart.
        You may answer questions AND change this patient's record when they ask or order something.

        Respond with JSON only (no markdown fences). Schema:
        {
          "answer": "plain-language reply for the clinician",
          "actions": [ { "op": "add_problem", "...fields" } ]
        }

        When they only ask a question, return actions: [].
        When they ask you to change, order, add, remove, prescribe, refill, document, or message, emit the matching actions.
        Use only these ops: add_problem, update_problem, resolve_problem, remove_problem, add_medication, update_medication, discontinue_medication, prescribe, refill, add_allergy, remove_allergy, add_vitals, add_note, add_immunization, order, add_care_team, create_task, send_message, update_demographics, update_encounter.
        Match existing chart items by name. Do not invent ICD codes. Use ISO dates (YYYY-MM-DD) when you include dates.
        For orders, put the study or panel in "name" (e.g. CBC, BMP, chest x-ray, EKG).

        CURRENT CHART:
        {{snapshot}}
        """;

    private static OrderType GuessOrderType(string op, string? category)
    {
        var hay = $"{op} {category}".ToLowerInvariant();
        if (hay.Contains("ekg") || hay.Contains("ecg")) return OrderType.Ekg;
        if (hay.Contains("imag") || hay.Contains("xray") || hay.Contains("x-ray") || hay.Contains("ct") || hay.Contains("mri") || hay.Contains("echo"))
        {
            return OrderType.Imaging;
        }

        return OrderType.Lab;
    }

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date) ? date.Date : null;

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;

    internal static string ExtractJson(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0) trimmed = trimmed[(firstNewline + 1)..];
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0) trimmed = trimmed[..lastFence];
            trimmed = trimmed.Trim();
        }

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        return firstBrace >= 0 && lastBrace > firstBrace
            ? trimmed[firstBrace..(lastBrace + 1)]
            : trimmed;
    }
}

public sealed class ChartAskResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<ChartAskAction> Actions { get; set; } = [];
}

public sealed class ChartAskAction
{
    public string Op { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? IcdCode { get; set; }
    public string? Status { get; set; }
    public string? Severity { get; set; }
    public string? Notes { get; set; }
    public string? OnsetDate { get; set; }
    public string? Dose { get; set; }
    public string? Route { get; set; }
    public string? Frequency { get; set; }
    public string? Prescriber { get; set; }
    public string? Pharmacy { get; set; }
    public string? Instructions { get; set; }
    public string? Reason { get; set; }
    public bool? IsLongTerm { get; set; }
    public bool? IsHighRisk { get; set; }
    public int? DaysSupply { get; set; }
    public int? Quantity { get; set; }
    public int? Refills { get; set; }
    public string? Allergen { get; set; }
    public string? Reaction { get; set; }
    public decimal? Temperature { get; set; }
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public int? HeartRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? OxygenSaturation { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public decimal? Bmi { get; set; }
    public string? RecordedBy { get; set; }
    public string? Author { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? History { get; set; }
    public string? Assessment { get; set; }
    public string? Plan { get; set; }
    public string? VaccineName { get; set; }
    public string? AdministeredBy { get; set; }
    public string? Site { get; set; }
    public string? Priority { get; set; }
    public string? Indication { get; set; }
    public string? Details { get; set; }
    public string? Category { get; set; }
    public string? Role { get; set; }
    public string? Specialty { get; set; }
    public string? Phone { get; set; }
    public string? Organization { get; set; }
    public bool? IsPrimary { get; set; }
    public string? Title { get; set; }
    public string? AssignedTo { get; set; }
    public string? DueDate { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? FromName { get; set; }
    public string? FromRole { get; set; }
    public string? ToName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? PrimaryProvider { get; set; }
    public string? InsuranceName { get; set; }
}
