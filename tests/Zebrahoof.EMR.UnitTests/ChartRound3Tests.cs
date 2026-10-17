using Microsoft.Extensions.DependencyInjection;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class ChartRound3Tests
{
    [Fact]
    public void AuthState_IsIsolatedPerScope()
    {
        var services = new ServiceCollection();
        services.AddScoped<AuthStateService>();
        using var provider = services.BuildServiceProvider();
        using var physicianScope = provider.CreateScope();
        using var schedulerScope = provider.CreateScope();
        var physician = physicianScope.ServiceProvider.GetRequiredService<AuthStateService>();
        var scheduler = schedulerScope.ServiceProvider.GetRequiredService<AuthStateService>();

        physician.Login(new User
        {
            Username = "physician",
            FullName = "Attending Physician",
            Role = UserRole.Physician
        });
        scheduler.Login(new User
        {
            Username = "Scheduler",
            FullName = "Scheduling Coordinator",
            Role = UserRole.FrontDesk
        });

        Assert.Equal("physician", physician.CurrentUser!.Username);
        Assert.Equal(UserRole.Physician, physician.CurrentUser.Role);
        Assert.Equal("Scheduler", scheduler.CurrentUser!.Username);
        Assert.Equal(UserRole.FrontDesk, scheduler.CurrentUser.Role);
        Assert.NotSame(physician, scheduler);
    }

    [Fact]
    public void NextClinicalId_IgnoresBenchmarkIds()
    {
        var next = MockClinicalDataService.NextClinicalId(new[] { 4, 5, 800158, 1_003_454, 700_000 });
        Assert.Equal(6, next);
    }

    [Fact]
    public async Task CreateNote_DoesNotReuseBenchmarkIds()
    {
        var clinical = new MockClinicalDataService();
        var notes = (List<ClinicalNote>)clinical.GetType()
            .GetField("_notes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(clinical)!;
        notes.Add(new ClinicalNote { Id = 800158, PatientId = 5, AuthorName = "Benchmark" });

        var created = await clinical.CreateNoteAsync(new ClinicalNote
        {
            PatientId = 1,
            AuthorName = "Attending Physician",
            ChiefComplaint = "Cough"
        });

        Assert.True(created.Id < MockClinicalDataService.BenchmarkIdFloor);
        Assert.NotEqual(800159, created.Id);
    }

    [Fact]
    public void VitalRanges_RejectImpossibleValues_AndFlagAbnormalOnes()
    {
        Assert.NotNull(VitalThresholds.HeartRateRangeError(900));
        Assert.NotNull(VitalThresholds.RespiratoryRangeError(-4));
        Assert.Null(VitalThresholds.HeartRateRangeError(118));
        Assert.Equal("High", VitalThresholds.HeartRateMarker(118));
        Assert.Equal("High", VitalThresholds.HeartRateMarker(900));
        Assert.Equal("Low", VitalThresholds.RespiratoryMarker(8));
        Assert.Null(VitalThresholds.SystolicRangeError(null));
    }

    [Fact]
    public async Task ActiveAllergyNames_ComeFromTheAllergyTable()
    {
        var clinical = new MockClinicalDataService();
        var before = await clinical.GetActiveAllergiesByPatientAsync(2);
        await clinical.AddAllergyAsync(new Allergy
        {
            PatientId = 2,
            Allergen = "Latex",
            Status = AllergyStatus.Active
        });

        var after = await clinical.GetActiveAllergiesByPatientAsync(2);
        Assert.DoesNotContain(before, allergy => allergy.Allergen == "Latex");
        Assert.Contains(after, allergy => allergy.Allergen == "Latex");
    }
}
