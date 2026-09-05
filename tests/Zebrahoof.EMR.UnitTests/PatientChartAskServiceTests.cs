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
        Assert.Contains(failed, f => f.Contains("unknown", StringComparison.OrdinalIgnoreCase));
    }

    private static (PatientChartAskService Service, MockClinicalDataService Clinical, Patient Patient) CreateService()
    {
        var clinical = new MockClinicalDataService();
        var patients = new MockPatientService(Substitute.For<IServiceScopeFactory>());
        var ai = Substitute.For<IClinicalAiService>();
        var service = new PatientChartAskService(clinical, patients, ai, NullLogger<PatientChartAskService>.Instance);
        var patient = new Patient { Id = 1, FirstName = "Test", LastName = "Patient", PrimaryProvider = "Dr. Smith" };
        return (service, clinical, patient);
    }
}
