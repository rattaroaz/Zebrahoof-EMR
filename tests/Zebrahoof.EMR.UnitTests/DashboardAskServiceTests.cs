using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class DashboardAskServiceTests
{
    [Fact]
    public void TryParse_ReadsAnswerAndPatientHits()
    {
        var ok = DashboardAskService.TryParse(
            """{"answer":"Jane Smith, 14 days ago.","patients":[{"id":2,"name":"Jane Smith"}]}""",
            out var parsed,
            out var error);

        Assert.True(ok, error);
        Assert.Contains("Jane Smith", parsed!.Answer);
        Assert.Single(parsed.Patients);
        Assert.Equal(2, parsed.Patients[0].Id);
    }

    [Fact]
    public void TryParse_ReadsScheduleAction()
    {
        var ok = DashboardAskService.TryParse(
            """{"answer":"Booked.","patients":[{"id":4,"name":"Maria Garcia"}],"actions":[{"op":"add_appointment","patientId":4,"scheduledAt":"2026-09-08T15:00:00"}]}""",
            out var parsed,
            out var error);

        Assert.True(ok, error);
        Assert.Single(parsed!.Actions);
        Assert.Equal("add_appointment", parsed.Actions[0].Op);
        Assert.Equal(4, parsed.Actions[0].PatientId);
    }

    [Fact]
    public async Task ClinicRoster_IncludesPheochromocytomaVisitAboutTwoWeeksAgo()
    {
        var clinical = new MockClinicalDataService();
        var problems = await clinical.GetAllProblemsAsync();
        Assert.Contains(problems, p => p.PatientId == 2 && p.Name.Contains("Pheochromocytoma", StringComparison.OrdinalIgnoreCase));

        var encounters = await clinical.GetAllEncountersAsync();
        Assert.Contains(encounters, e =>
            e.PatientId == 2 &&
            e.Assessment != null &&
            e.Assessment.Contains("Pheochromocytoma", StringComparison.OrdinalIgnoreCase) &&
            (DateTime.Today - e.DateTime.Date).Days is >= 13 and <= 15);
    }
}
