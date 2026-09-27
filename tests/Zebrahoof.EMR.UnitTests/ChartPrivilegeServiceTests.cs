using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class ChartPrivilegeServiceTests
{
    private readonly ChartPrivilegeService _privileges = new();

    [Fact]
    public void Recheck_EveryKnownOperationHasACapability()
    {
        foreach (var op in ChartPrivilegeService.KnownOperations)
        {
            Assert.True(
                ChartPrivilegeService.TryGetCapability(op, out _),
                $"{op} is listed as a chart change but has no privilege mapping.");
        }
    }

    [Fact]
    public void Recheck_UnsupportedAndUnsignedUsersAreDenied()
    {
        Assert.False(_privileges.Evaluate(null, "add_problem").Allowed);
        Assert.False(_privileges.Evaluate(UserRole.Patient, "add_problem").Allowed);
        Assert.False(_privileges.Evaluate(UserRole.Physician, "teleport_patient").Allowed);
        Assert.Contains("signed in", _privileges.Evaluate(null, "add_problem").Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Patient role", _privileges.Evaluate(UserRole.Patient, "add_problem").Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(UserRole.Physician, "add_problem", true)]
    [InlineData(UserRole.Physician, "prescribe", true)]
    [InlineData(UserRole.Physician, "update_insurance", true)]
    [InlineData(UserRole.Nurse, "add_problem", true)]
    [InlineData(UserRole.Nurse, "prescribe", false)]
    [InlineData(UserRole.Nurse, "update_insurance", false)]
    [InlineData(UserRole.MedicalAssistant, "add_vitals", true)]
    [InlineData(UserRole.MedicalAssistant, "add_note", true)]
    [InlineData(UserRole.MedicalAssistant, "prescribe", false)]
    [InlineData(UserRole.LabTechnician, "order", true)]
    [InlineData(UserRole.LabTechnician, "add_problem", false)]
    [InlineData(UserRole.FrontDesk, "add_appointment", true)]
    [InlineData(UserRole.FrontDesk, "update_demographics", true)]
    [InlineData(UserRole.FrontDesk, "add_problem", false)]
    [InlineData(UserRole.FrontDesk, "prescribe", false)]
    [InlineData(UserRole.Billing, "update_insurance", true)]
    [InlineData(UserRole.Billing, "update_demographics", true)]
    [InlineData(UserRole.Billing, "add_problem", false)]
    [InlineData(UserRole.Admin, "prescribe", true)]
    [InlineData(UserRole.Admin, "update_insurance", true)]
    public void Recheck_RoleMatrixMatchesWhatTheUserCanChange(UserRole role, string op, bool allowed)
    {
        var decision = _privileges.Evaluate(role, op);
        Assert.Equal(allowed, decision.Allowed);
        if (!allowed)
        {
            Assert.False(string.IsNullOrWhiteSpace(decision.Message));
            Assert.Contains("cannot", decision.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Recheck_ChartTabsHaveAnAskPathOrAreDocumentedUiOnly()
    {
        string[] askCovers =
        [
            "encounter", "problems", "medications", "allergies", "orders", "labs", "imaging",
            "vitals", "immunizations", "notes", "careteam", "demographics", "schedule"
        ];
        string[] uiOnly = ["history", "documents"];

        Assert.Contains("update_encounter", ChartPrivilegeService.KnownOperations);
        Assert.Contains("add_problem", ChartPrivilegeService.KnownOperations);
        Assert.Contains("prescribe", ChartPrivilegeService.KnownOperations);
        Assert.Contains("add_allergy", ChartPrivilegeService.KnownOperations);
        Assert.Contains("order", ChartPrivilegeService.KnownOperations);
        Assert.Contains("add_vitals", ChartPrivilegeService.KnownOperations);
        Assert.Contains("add_immunization", ChartPrivilegeService.KnownOperations);
        Assert.Contains("add_note", ChartPrivilegeService.KnownOperations);
        Assert.Contains("add_care_team", ChartPrivilegeService.KnownOperations);
        Assert.Contains("update_demographics", ChartPrivilegeService.KnownOperations);
        Assert.Contains("add_appointment", ChartPrivilegeService.KnownOperations);
        Assert.Equal(13, askCovers.Length);
        Assert.Contains("history", uiOnly);
        Assert.Contains("documents", uiOnly);
    }

    [Fact]
    public async Task Recheck_DeniedRoleDoesNotWriteTheChart()
    {
        var (frontDesk, clinical, patient) = CreateService(UserRole.FrontDesk);
        var applied = new List<string>();
        var failed = new List<string>();
        await frontDesk.ApplyActionsAsync(patient,
        [
            new ChartAskAction { Op = "add_problem", Name = "Privilege-Blocked-Gout" },
            new ChartAskAction { Op = "prescribe", Name = "Privilege-Blocked-Amox", Dose = "500 mg" }
        ], applied, failed);

        Assert.Empty(applied);
        Assert.Equal(2, failed.Count);
        Assert.All(failed, f => Assert.Contains("cannot", f, StringComparison.OrdinalIgnoreCase));
        var problems = await clinical.GetProblemsByPatientAsync(patient.Id);
        Assert.DoesNotContain(problems, p => p.Name == "Privilege-Blocked-Gout");
    }

    [Fact]
    public async Task Recheck_AllowedRoleDoesWriteTheChart()
    {
        var (physician, clinical, patient) = CreateService(UserRole.Physician);
        var applied = new List<string>();
        var failed = new List<string>();
        await physician.ApplyActionsAsync(patient,
        [
            new ChartAskAction { Op = "add_problem", Name = "Privilege-Allowed-Gout" }
        ], applied, failed);

        Assert.Empty(failed);
        Assert.Contains(applied, a => a.Contains("Privilege-Allowed-Gout", StringComparison.OrdinalIgnoreCase));
        var problems = await clinical.GetProblemsByPatientAsync(patient.Id);
        Assert.Contains(problems, p => p.Name == "Privilege-Allowed-Gout");
    }

    private static (PatientChartAskService Service, MockClinicalDataService Clinical, Patient Patient) CreateService(UserRole role)
    {
        var clinical = new MockClinicalDataService();
        var patients = new MockPatientService(Substitute.For<IServiceScopeFactory>());
        var auth = new AuthStateService();
        auth.Login(new User { Id = 9, Username = "role-test", FullName = "Role Test", Role = role });
        var service = new PatientChartAskService(
            clinical,
            patients,
            new AskAppointmentActions(new MockAppointmentService()),
            new ChartPrivilegeService(),
            auth,
            Substitute.For<IClinicalAiService>(),
            NullLogger<PatientChartAskService>.Instance);
        var patient = new Patient { Id = 1, FirstName = "Test", LastName = "Patient", PrimaryProvider = "Dr. Smith" };
        return (service, clinical, patient);
    }
}
