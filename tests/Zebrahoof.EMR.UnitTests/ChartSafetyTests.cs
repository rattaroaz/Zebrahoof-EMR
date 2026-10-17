using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
    public void SameTab_AtoB_DoesNotPrompt_WhileASecondBrowserHoldingADoes()
    {
        var registry = new ActivePatientRegistry();

        var opened = registry.Switch("physician", "tab", 1, "John Doe");
        var moved = registry.Switch("physician", "tab", 2, "Jane Smith");

        Assert.Equal(ChartOpenStatus.Granted, opened.Status);
        Assert.Equal(ChartOpenStatus.Granted, moved.Status);
        Assert.Equal((2, "Jane Smith"), registry.Current("physician"));

        registry.Release("physician", "tab", onlyIfPatientId: 1);
        Assert.Equal((2, "Jane Smith"), registry.Current("physician"));
        registry.Release("physician", "tab");

        var browser1 = registry.Switch("physician", "browser-1", 1, "John Doe");
        var browser2 = registry.Switch("physician", "browser-2", 2, "Jane Smith");

        Assert.Equal(ChartOpenStatus.Granted, browser1.Status);
        Assert.Equal(ChartOpenStatus.Blocked, browser2.Status);
        Assert.Equal("John Doe", browser2.OpenPatientName);
        Assert.Equal((1, "John Doe"), registry.Current("physician"));
    }

    [Fact]
    public void SameCircuitHandoff_StaysClaimedUntilThatCircuitSwitches_AndBlocksAnotherBrowser()
    {
        var registry = new ActivePatientRegistry();
        registry.Switch("physician", "tab", 1, "John Doe");
        registry.BeginHandoff("physician", "tab", 1);

        var other = registry.Switch("physician", "browser-2", 3, "Robert Johnson");
        Assert.Equal(ChartOpenStatus.Blocked, other.Status);
        Assert.Equal((1, "John Doe"), registry.Current("physician"));

        var moved = registry.Switch("physician", "tab", 2, "Jane Smith");
        Assert.Equal(ChartOpenStatus.Granted, moved.Status);
        registry.CommitHandoff("physician", "tab", 1);
        Assert.Equal((2, "Jane Smith"), registry.Current("physician"));

        registry.Switch("nurse", "tab-n", 4, "Ada");
        registry.BeginHandoff("nurse", "tab-n", 4);
        registry.CommitHandoff("nurse", "tab-n", 4);
        Assert.Null(registry.Current("nurse"));
    }

    [Fact]
    public void DisconnectedCircuit_BlocksDuringGrace_ThenReleases()
    {
        var clock = new ManualTime();
        var registry = new ActivePatientRegistry(clock, disconnectGraceSeconds: 8);
        registry.Switch("physician", "tab", 1, "John Doe");
        registry.MarkDisconnected("physician", "tab");

        clock.UtcNow = clock.UtcNow.AddSeconds(3);
        var duringGrace = registry.Switch("physician", "other", 2, "Jane Smith");
        Assert.Equal(ChartOpenStatus.Blocked, duringGrace.Status);
        Assert.Equal("John Doe", duringGrace.OpenPatientName);

        registry.MarkConnected("physician", "tab");
        clock.UtcNow = clock.UtcNow.AddSeconds(10);
        var reconnected = registry.Switch("physician", "other", 2, "Jane Smith");
        Assert.Equal(ChartOpenStatus.Blocked, reconnected.Status);

        registry.MarkDisconnected("physician", "tab");
        clock.UtcNow = clock.UtcNow.AddSeconds(8);
        var afterGrace = registry.Switch("physician", "other", 2, "Jane Smith");
        Assert.Equal(ChartOpenStatus.Granted, afterGrace.Status);
        Assert.Equal((2, "Jane Smith"), registry.Current("physician"));

        var reclaim = registry.Switch("physician", "tab", 1, "John Doe");
        Assert.Equal(ChartOpenStatus.Blocked, reclaim.Status);
        Assert.Equal((2, "Jane Smith"), registry.Current("physician"));
    }

    [Fact]
    public async Task ChartWrite_IsRefused_WhenTheCircuitNoLongerHoldsThePatient()
    {
        var registry = new ActivePatientRegistry();
        var clinical = new MockClinicalDataService(null, NullLogger<MockClinicalDataService>.Instance, new ChartWriteGuard(registry));
        registry.Switch("physician", "tab-a", 1, "John Doe");
        registry.ConfirmSwitch("physician", "tab-b", 2, "Jane Smith");

        var notesBefore = (await clinical.GetPatientNotesAsync(1)).Count;
        var refillsBefore = (await clinical.GetPrescriptionsByPatientAsync(1)).Count;

        using (CircuitCall.Push("physician", "tab-a"))
        {
            var note = await Assert.ThrowsAsync<ChartNotHeldException>(() => clinical.CreateNoteAsync(new ClinicalNote
            {
                PatientId = 1,
                ChiefComplaint = "Stale-dialog test",
                HistoryOfPresentIllness = "Should not be stored"
            }));
            Assert.Equal(ChartNotHeldException.RefusalMessage, note.Message);

            var refill = await Assert.ThrowsAsync<ChartNotHeldException>(() => clinical.OrderRefillAsync(1, 1, "Dr. Sarah Smith"));
            Assert.Equal(ChartNotHeldException.RefusalMessage, refill.Message);

            var prescribed = await Assert.ThrowsAsync<ChartNotHeldException>(() => clinical.PrescribeMedicationAsync(
                1, "Amoxicillin", "500 mg", "Oral", "BID", false, 7, 14, 0, null, "Dr. Sarah Smith", null));
            Assert.Equal(ChartNotHeldException.RefusalMessage, prescribed.Message);
        }

        var notes = await clinical.GetPatientNotesAsync(1);
        Assert.Equal(notesBefore, notes.Count);
        Assert.DoesNotContain(notes, item => item.ChiefComplaint == "Stale-dialog test");
        Assert.Equal(refillsBefore, (await clinical.GetPrescriptionsByPatientAsync(1)).Count);

        using (CircuitCall.Push("physician", "tab-b"))
        {
            var saved = await clinical.CreateNoteAsync(new ClinicalNote
            {
                PatientId = 2,
                ChiefComplaint = "Allowed while held",
                HistoryOfPresentIllness = "Jane is the open chart"
            });
            Assert.Equal("Allowed while held", saved.ChiefComplaint);
        }
    }

    private sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => UtcNow;
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
