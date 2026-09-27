using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class PatientAskGuardTests
{
    private static readonly List<Patient> Roster =
    [
        new() { Id = 1, FirstName = "John", LastName = "Doe" },
        new() { Id = 2, FirstName = "Jane", LastName = "Smith" },
        new() { Id = 3, FirstName = "Robert", LastName = "Johnson" }
    ];

    [Theory]
    [InlineData("http://localhost:5222/patients/2", true)]
    [InlineData("http://localhost:5222/patients/2/chart", true)]
    [InlineData("http://localhost:5222/patients/2/notes/tree", true)]
    [InlineData("http://localhost:5222/patients", false)]
    [InlineData("http://localhost:5222/patients/", false)]
    [InlineData("http://localhost:5222/", false)]
    [InlineData("http://localhost:5222/schedule", false)]
    [InlineData("http://localhost:5222/inbox", false)]
    [InlineData("http://localhost:5222/encounter/12", false)]
    public void IsPatientMode_DetectsChartRoutes(string uri, bool expected)
    {
        Assert.Equal(expected, PatientAskGuard.IsPatientMode(uri));
    }

    [Fact]
    public void MentionsNamedPatient_IgnoresOpenChart()
    {
        Assert.False(PatientAskGuard.MentionsNamedPatient("What is John Doe on for blood pressure?", Roster, excludePatientId: 1));
        Assert.True(PatientAskGuard.MentionsNamedPatient("Compare this to Jane Smith", Roster, excludePatientId: 1));
    }

    [Fact]
    public void ShouldWarnInManagementMode_OnlyWhenQuestionNamesAPatient()
    {
        Assert.True(PatientAskGuard.ShouldWarnInManagementMode("How is Jane Smith doing?", Roster));
        Assert.False(PatientAskGuard.ShouldWarnInManagementMode("How many appointments are on the schedule today?", Roster));
        Assert.False(PatientAskGuard.ShouldWarnInManagementMode("Who has pheochromocytoma?", Roster));
        Assert.False(PatientAskGuard.ShouldWarnInManagementMode("Which patients are on metformin?", Roster));
    }

    [Fact]
    public void ShouldWarnInPatientMode_OnlyWhenQuestionNamesADifferentPatient()
    {
        Assert.False(PatientAskGuard.ShouldWarnInPatientMode("What are the active problems?", Roster, 1));
        Assert.False(PatientAskGuard.ShouldWarnInPatientMode("Who else has diabetes?", Roster, 1));
        Assert.True(PatientAskGuard.ShouldWarnInPatientMode("What about Jane Smith?", Roster, 1));
    }
}
