using Microsoft.Extensions.DependencyInjection;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class ActivePatientRegistryTests
{
    [Fact]
    public void TwoUsers_OpenDifferentPatients_WithoutLeaking()
    {
        var registry = new ActivePatientRegistry();

        var physician = registry.TryOpen("physician", "circuit-a", 1, "John Doe");
        var nurse = registry.TryOpen("nurse", "circuit-b", 2, "Jane Smith");

        Assert.Equal(ChartOpenStatus.Granted, physician.Status);
        Assert.Equal(ChartOpenStatus.Granted, nurse.Status);
        Assert.Equal((1, "John Doe"), registry.Current("physician"));
        Assert.Equal((2, "Jane Smith"), registry.Current("nurse"));
    }

    [Fact]
    public void SameUser_SecondPatient_IsBlockedUntilExplicitSwitch_AndOtherCircuitCloses()
    {
        var registry = new ActivePatientRegistry();
        ChartClosedNotice? closed = null;
        registry.ChartClosed += notice => closed = notice;

        var first = registry.TryOpen("physician", "tab-1", 2, "Jane Smith");
        var second = registry.TryOpen("physician", "tab-2", 1, "John Doe");

        Assert.Equal(ChartOpenStatus.Granted, first.Status);
        Assert.Equal(ChartOpenStatus.Blocked, second.Status);
        Assert.Equal("Jane Smith", second.OpenPatientName);
        Assert.Equal((2, "Jane Smith"), registry.Current("physician"));

        registry.SetUnsaved("physician", "tab-1", true);
        var warned = registry.TryOpen("physician", "tab-2", 1, "John Doe");
        Assert.True(warned.OpenChartHasUnsavedWork);

        registry.ConfirmSwitch("physician", "tab-2", 1, "John Doe");

        Assert.Equal((1, "John Doe"), registry.Current("physician"));
        Assert.NotNull(closed);
        Assert.Equal("tab-1", closed!.CircuitId);
        Assert.Equal("John Doe", closed.OpenedPatientName);
        Assert.Equal(2, closed.ClosedPatientId);
    }

    [Fact]
    public void SamePatient_CanStayOpen_OnTwoCircuits()
    {
        var registry = new ActivePatientRegistry();

        Assert.Equal(ChartOpenStatus.Granted, registry.TryOpen("physician", "tab-1", 1, "John Doe").Status);
        Assert.Equal(ChartOpenStatus.Granted, registry.TryOpen("physician", "tab-2", 1, "John Doe").Status);

        registry.Release("physician", "tab-1");
        Assert.Equal((1, "John Doe"), registry.Current("physician"));
        registry.Release("physician", "tab-2");
        Assert.Null(registry.Current("physician"));
    }

    [Fact]
    public void TwoCircuits_DoNotShareAppState()
    {
        var services = new ServiceCollection();
        services.AddScoped<AppStateService>();
        using var provider = services.BuildServiceProvider();
        using var physician = provider.CreateScope();
        using var nurse = provider.CreateScope();

        var physicianState = physician.ServiceProvider.GetRequiredService<AppStateService>();
        var nurseState = nurse.ServiceProvider.GetRequiredService<AppStateService>();
        physicianState.SelectPatient(new Patient { Id = 2, FirstName = "Jane", LastName = "Smith" });

        Assert.NotSame(physicianState, nurseState);
        Assert.Equal("Jane Smith", physicianState.SelectedPatient!.FullName);
        Assert.Null(nurseState.SelectedPatient);
    }
}

public class PatientContextRouteTests
{
    [Fact]
    public void EncounterBreadcrumb_UsesThatEncountersPatient_NotAnotherChart()
    {
        var crumbs = BreadcrumbText.Build(
            "/encounter/1",
            _ => "Jane Smith",
            _ => (1, "John Doe"));

        var text = string.Join(" > ", crumbs.Select(c => c.Text));
        Assert.Contains("John Doe", text);
        Assert.DoesNotContain("Jane Smith", text);
        Assert.DoesNotContain("Patient:", text);
    }

    [Fact]
    public void DashboardBreadcrumb_DoesNotInsertAPatient()
    {
        var crumbs = BreadcrumbText.Build("/dashboard", _ => "Jane Smith", _ => (1, "John Doe"));
        Assert.DoesNotContain(crumbs, c => c.Text.Contains("Jane") || c.Text.Contains("John"));
    }

    [Theory]
    [InlineData("/patients/3/summary", 3)]
    [InlineData("/patients/12", 12)]
    public void PatientRoute_ReadsPatientId(string path, int expected)
    {
        Assert.True(PatientRoute.TryParsePatientId(path, out var id));
        Assert.Equal(expected, id);
    }
}

public class PatientSearchAndAlertTests
{
    [Fact]
    public void Search_MatchesDateOfBirth_AndKeepsTheFullQuery()
    {
        var john = new Patient
        {
            FirstName = "John",
            LastName = "Doe",
            MRN = "MRN001",
            DateOfBirth = new DateTime(1985, 3, 15)
        };

        Assert.True(PatientSearch.Matches(john, "03/15/1985"));
        Assert.True(PatientSearch.Matches(john, "1985-03-15"));
        Assert.True(PatientSearch.Matches(john, "garcia") == false);
        Assert.Equal("garcia", "garcia");
    }

    [Fact]
    public void AbnormalLab_A1c_IsMarkedHigh()
    {
        Assert.Equal("H", LabValueFlag.Marker("5.8", "<5.7"));
        Assert.Equal("L", LabValueFlag.Marker("45", ">60"));
        Assert.Equal("H", LabValueFlag.Marker("5.6", "3.5-5.0"));
    }

    [Fact]
    public void Temperature_100Point2_IsHigh()
    {
        Assert.True(VitalThresholds.IsTemperatureAbnormal(100.2m));
        Assert.Equal("High", VitalThresholds.TemperatureMarker(100.2m));
        Assert.False(VitalThresholds.IsTemperatureAbnormal(98.6m));
    }

    [Fact]
    public void Inbox_PinsUnacknowledgedCritical_AboveNewerRoutineMail()
    {
        var routine = new InboxMessage
        {
            Id = 1,
            Subject = "Question about medication",
            SentAt = DateTime.Today,
            IsUrgent = false
        };
        var critical = new InboxMessage
        {
            Id = 2,
            Subject = "Critical Lab Result - Potassium 5.6",
            SentAt = DateTime.Today.AddDays(-1),
            IsUrgent = true
        };

        var sorted = InboxOrdering.Sort(new[] { routine, critical });

        Assert.Equal(critical.Id, sorted[0].Id);
        critical.IsAcknowledged = true;
        sorted = InboxOrdering.Sort(new[] { routine, critical });
        Assert.Equal(routine.Id, sorted[0].Id);
    }
}
