using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class AskAppointmentActionsTests
{
    private static readonly List<Patient> Roster =
    [
        new() { Id = 1, FirstName = "John", LastName = "Doe", PrimaryProvider = "Dr. Sarah Smith" },
        new() { Id = 4, FirstName = "Maria", LastName = "Garcia", PrimaryProvider = "Dr. Sarah Smith" }
    ];

    [Theory]
    [InlineData("add Maria Garcia to the schedule", true)]
    [InlineData("book maria garcia tomorrow at 3pm", true)]
    [InlineData("schedule an appointment for Maria Garcia", true)]
    [InlineData("what's on the schedule today", false)]
    [InlineData("who has an appointment with Dr. Smith", false)]
    [InlineData("list tomorrow's appointments", false)]
    public void LooksLikeScheduleRequest_DetectsBookingCommands(string question, bool expected)
    {
        Assert.Equal(expected, AskAppointmentActions.LooksLikeScheduleRequest(question));
    }

    [Fact]
    public void ResolveNamedPatient_RequiresOneSpecificName()
    {
        Assert.Equal(4, AskAppointmentActions.ResolveNamedPatient("add Maria Garcia to the schedule", Roster)?.Id);
        Assert.Null(AskAppointmentActions.ResolveNamedPatient("who is on metformin", Roster));
    }

    [Fact]
    public async Task ScheduleAsync_WritesAppointmentForNamedPatient()
    {
        var appointments = new MockAppointmentService();
        var actions = new AskAppointmentActions(appointments);
        var when = DateTime.Today.AddDays(5).AddHours(15);

        var summary = await actions.ScheduleAsync(new ChartAskAction
        {
            Op = "add_appointment",
            PatientName = "Maria Garcia",
            ScheduledAt = when.ToString("yyyy-MM-ddTHH:mm:ss"),
            VisitType = "Follow-up"
        }, Roster);

        Assert.Contains("Maria Garcia", summary, StringComparison.OrdinalIgnoreCase);
        var created = await appointments.GetAppointmentsByPatientAsync(4);
        Assert.Contains(created, a => a.DateTime == when && a.VisitType == "Follow-up" && a.Status == AppointmentStatus.Scheduled);
    }

    [Fact]
    public async Task ScheduleAsync_UsesNextOpenSlotWhenTimeOmitted()
    {
        var appointments = new MockAppointmentService();
        var actions = new AskAppointmentActions(appointments);

        var summary = await actions.ScheduleAsync(new ChartAskAction
        {
            Op = "add_appointment",
            PatientId = 4
        }, Roster);

        Assert.StartsWith("Scheduled Maria Garcia", summary);
        var created = (await appointments.GetAppointmentsByPatientAsync(4))
            .OrderByDescending(a => a.Id)
            .First();
        Assert.True(created.Id >= 100);
        Assert.InRange(created.DateTime.TimeOfDay, TimeSpan.FromHours(8), TimeSpan.FromHours(17));
    }
}
