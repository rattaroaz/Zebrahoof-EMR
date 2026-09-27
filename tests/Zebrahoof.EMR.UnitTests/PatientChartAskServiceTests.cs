using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class PatientChartAskServiceTests
{
    [Fact]
    public void TryParse_ReadsAnswerAndActionsFromFencedJson()
    {
        var ok = PatientChartAskService.TryParse(
            """
            ```json
            {"answer":"Done.","actions":[{"op":"add_problem","name":"Gout"}]}
            ```
            """,
            out var parsed,
            out var error);

        Assert.True(ok, error);
        Assert.Equal("Done.", parsed!.Answer);
        Assert.Single(parsed.Actions);
        Assert.Equal("add_problem", parsed.Actions[0].Op);
        Assert.Equal("Gout", parsed.Actions[0].Name);
    }

    [Fact]
    public async Task ApplyActions_AddsProblemAllergyAndOrdersLab()
    {
        var (service, clinical, patient) = CreateService();

        var applied = new List<string>();
        var failed = new List<string>();
        await service.ApplyActionsAsync(patient,
        [
            new ChartAskAction { Op = "add_problem", Name = "Ask-Service-Gout" },
            new ChartAskAction { Op = "add_allergy", Allergen = "Ask-Service-Sulfa", Reaction = "Hives", Severity = "Moderate" },
            new ChartAskAction { Op = "order", Name = "Complete Blood Count (CBC)" }
        ], applied, failed);

        Assert.Empty(failed);
        Assert.Contains(applied, a => a.Contains("Ask-Service-Gout", StringComparison.OrdinalIgnoreCase));
        var problems = await clinical.GetProblemsByPatientAsync(patient.Id);
        Assert.Contains(problems, p => p.Name == "Ask-Service-Gout");
        var allergies = await clinical.GetAllergiesByPatientAsync(patient.Id);
        Assert.Contains(allergies, a => a.Allergen == "Ask-Service-Sulfa");
        var orders = await clinical.GetSimulatedOrdersByPatientAsync(patient.Id);
        Assert.Contains(orders, o => o.DisplayName.Contains("CBC", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApplyActions_DiscontinuesMedicationByName()
    {
        var (service, clinical, patient) = CreateService();
        await clinical.AddMedicationToListAsync(patient.Id, "Ask-Service-Lisinopril", "10 mg", "PO", "daily", "Dr. Test", null, null);

        var applied = new List<string>();
        var failed = new List<string>();
        await service.ApplyActionsAsync(patient,
        [
            new ChartAskAction { Op = "discontinue_medication", Name = "Ask-Service-Lisinopril", Reason = "cough" }
        ], applied, failed);

        Assert.Empty(failed);
        var meds = await clinical.GetMedicationsByPatientAsync(patient.Id);
        Assert.Contains(meds, m => m.Name == "Ask-Service-Lisinopril" && m.Status == MedicationStatus.Discontinued);
    }

    [Fact]
    public async Task ApplyActions_UnknownOpIsReportedNotThrown()
    {
        var (service, _, patient) = CreateService();
        var applied = new List<string>();
        var failed = new List<string>();
        await service.ApplyActionsAsync(patient,
        [
            new ChartAskAction { Op = "teleport_patient", Name = "nowhere" }
        ], applied, failed);

        Assert.Empty(applied);
        Assert.Contains(failed, f =>
            f.Contains("not a supported", StringComparison.OrdinalIgnoreCase)
            || f.Contains("unknown", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApplyActions_FrontDeskCannotPrescribe()
    {
        var clinical = new MockClinicalDataService();
        var patients = new MockPatientService(Substitute.For<IServiceScopeFactory>());
        var service = new PatientChartAskService(
            clinical,
            patients,
            new AskAppointmentActions(new MockAppointmentService()),
            new ChartPrivilegeService(),
            SignedIn(UserRole.FrontDesk),
            Substitute.For<IClinicalAiService>(),
            NullLogger<PatientChartAskService>.Instance);
        var patient = new Patient { Id = 1, FirstName = "Test", LastName = "Patient" };

        var applied = new List<string>();
        var failed = new List<string>();
        await service.ApplyActionsAsync(patient,
        [
            new ChartAskAction { Op = "prescribe", Name = "Amoxicillin", Dose = "500 mg" }
        ], applied, failed);

        Assert.Empty(applied);
        Assert.Contains(failed, f => f.Contains("Front Desk", StringComparison.OrdinalIgnoreCase));
        var meds = await clinical.GetMedicationsByPatientAsync(patient.Id);
        Assert.DoesNotContain(meds, m => m.Name.Contains("Amoxicillin", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApplyActions_SchedulesAppointmentForOpenChart()
    {
        var appointments = new MockAppointmentService();
        var clinical = new MockClinicalDataService();
        var patients = new MockPatientService(Substitute.For<IServiceScopeFactory>());
        var service = new PatientChartAskService(
            clinical,
            patients,
            new AskAppointmentActions(appointments),
            new ChartPrivilegeService(),
            SignedIn(UserRole.Physician),
            Substitute.For<IClinicalAiService>(),
            NullLogger<PatientChartAskService>.Instance);
        var patient = new Patient { Id = 4, FirstName = "Maria", LastName = "Garcia", PrimaryProvider = "Dr. Sarah Smith" };
        var when = DateTime.Today.AddDays(6).AddHours(10);

        var applied = new List<string>();
        var failed = new List<string>();
        await service.ApplyActionsAsync(patient,
        [
            new ChartAskAction
            {
                Op = "add_appointment",
                PatientId = 4,
                PatientName = "Maria Garcia",
                ScheduledAt = when.ToString("yyyy-MM-ddTHH:mm:ss")
            }
        ], applied, failed);

        Assert.Empty(failed);
        Assert.Contains(applied, a => a.Contains("Maria Garcia", StringComparison.OrdinalIgnoreCase));
        var created = await appointments.GetAppointmentsByPatientAsync(4);
        Assert.Contains(created, a => a.DateTime == when);
    }

    private static (PatientChartAskService Service, MockClinicalDataService Clinical, Patient Patient) CreateService()
    {
        var clinical = new MockClinicalDataService();
        var patients = new MockPatientService(Substitute.For<IServiceScopeFactory>());
        var schedule = new AskAppointmentActions(new MockAppointmentService());
        var ai = Substitute.For<IClinicalAiService>();
        var service = new PatientChartAskService(
            clinical,
            patients,
            schedule,
            new ChartPrivilegeService(),
            SignedIn(UserRole.Physician),
            ai,
            NullLogger<PatientChartAskService>.Instance);
        var patient = new Patient { Id = 1, FirstName = "Test", LastName = "Patient", PrimaryProvider = "Dr. Smith" };
        return (service, clinical, patient);
    }

    private static AuthStateService SignedIn(UserRole role)
    {
        var auth = new AuthStateService();
        auth.Login(new User { Id = 1, Username = "tester", FullName = "Test User", Role = role });
        return auth;
    }
}
