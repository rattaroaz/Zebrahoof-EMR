namespace Zebrahoof_EMR.Models;

public class Appointment
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string VisitType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
}

public enum AppointmentStatus
{
    Scheduled,
    CheckedIn,
    InRoom,
    InProgress,
    Completed,
    NoShow,
    Cancelled
}
