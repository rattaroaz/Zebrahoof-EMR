using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class MockAppointmentService
{
    private readonly List<Appointment> _appointments;
    private int _nextId = 100;

    public static readonly List<string> Providers =
    [
        "Dr. Sarah Smith",
        "Dr. Michael Chen",
        "Dr. Emily Wilson"
    ];

    public static readonly List<string> VisitTypes =
    [
        "Annual Exam",
        "Follow-up",
        "New Patient",
        "Sick Visit",
        "Diabetes Management",
        "Hypertension Check",
        "Lab Review",
        "Comprehensive Visit",
        "Well Child",
        "Procedure"
    ];

    public static readonly List<string> Locations =
    [
        "Room 1",
        "Room 2",
        "Room 3",
        "Procedure Room",
        "Telehealth"
    ];

    public MockAppointmentService()
    {
        _appointments = GenerateMockAppointments();
    }

    public Task<List<Appointment>> GetTodaysAppointmentsAsync() =>
        Task.FromResult(_appointments.Where(a => a.DateTime.Date == DateTime.Today).ToList());

    public Task<List<Appointment>> GetAppointmentsByDateAsync(DateTime date) =>
        Task.FromResult(_appointments.Where(a => a.DateTime.Date == date.Date).ToList());

    public Task<List<Appointment>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate) =>
        Task.FromResult(_appointments.Where(a => a.DateTime.Date >= startDate.Date && a.DateTime.Date <= endDate.Date).ToList());

    public Task<List<Appointment>> GetAppointmentsByProviderAsync(string provider) =>
        Task.FromResult(_appointments.Where(a => a.Provider == provider).ToList());

    public Task<List<Appointment>> GetAppointmentsByProviderAndDateAsync(string provider, DateTime date) =>
        Task.FromResult(_appointments.Where(a => a.Provider == provider && a.DateTime.Date == date.Date).ToList());

    public Task<List<Appointment>> GetAppointmentsByPatientAsync(int patientId) =>
        Task.FromResult(_appointments.Where(a => a.PatientId == patientId).ToList());

    public Task<Appointment?> GetAppointmentByIdAsync(int id) =>
        Task.FromResult(_appointments.FirstOrDefault(a => a.Id == id));

    public Task UpdateStatusAsync(int id, AppointmentStatus status)
    {
        var apt = _appointments.FirstOrDefault(a => a.Id == id);
        if (apt != null) apt.Status = status;
        return Task.CompletedTask;
    }

    public Task<Appointment> AddAppointmentAsync(Appointment appointment)
    {
        appointment.Id = _nextId++;
        _appointments.Add(appointment);
        return Task.FromResult(appointment);
    }

    public Task UpdateAppointmentAsync(Appointment appointment)
    {
        var existing = _appointments.FirstOrDefault(a => a.Id == appointment.Id);
        if (existing != null)
        {
            existing.PatientId = appointment.PatientId;
            existing.PatientName = appointment.PatientName;
            existing.DateTime = appointment.DateTime;
            existing.DurationMinutes = appointment.DurationMinutes;
            existing.VisitType = appointment.VisitType;
            existing.Provider = appointment.Provider;
            existing.Location = appointment.Location;
            existing.Status = appointment.Status;
            existing.Notes = appointment.Notes;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAppointmentAsync(int id)
    {
        var apt = _appointments.FirstOrDefault(a => a.Id == id);
        if (apt != null) _appointments.Remove(apt);
        return Task.CompletedTask;
    }

    public Task<List<Appointment>> GetWeekAppointmentsAsync(DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        return GetAppointmentsByDateRangeAsync(weekStart, weekEnd.AddDays(-1));
    }

    private static List<Appointment> GenerateMockAppointments()
    {
        var today = DateTime.Today;
        var appointments = new List<Appointment>
        {
            // Today's appointments
            new() { Id = 1, PatientId = 1, PatientName = "John Doe", DateTime = today.AddHours(8), DurationMinutes = 30, VisitType = "Follow-up", Provider = "Dr. Sarah Smith", Location = "Room 1", Status = AppointmentStatus.Completed },
            new() { Id = 2, PatientId = 2, PatientName = "Jane Smith", DateTime = today.AddHours(8).AddMinutes(30), DurationMinutes = 30, VisitType = "Annual Exam", Provider = "Dr. Michael Chen", Location = "Room 2", Status = AppointmentStatus.Completed },
            new() { Id = 3, PatientId = 3, PatientName = "Robert Johnson", DateTime = today.AddHours(9), DurationMinutes = 45, VisitType = "Diabetes Management", Provider = "Dr. Sarah Smith", Location = "Room 1", Status = AppointmentStatus.InProgress },
            new() { Id = 4, PatientId = 4, PatientName = "Maria Garcia", DateTime = today.AddHours(9).AddMinutes(30), DurationMinutes = 30, VisitType = "New Patient", Provider = "Dr. Emily Wilson", Location = "Room 3", Status = AppointmentStatus.CheckedIn },
            new() { Id = 5, PatientId = 5, PatientName = "William Brown", DateTime = today.AddHours(10), DurationMinutes = 20, VisitType = "Sick Visit", Provider = "Dr. Sarah Smith", Location = "Room 1", Status = AppointmentStatus.Scheduled },
            new() { Id = 6, PatientId = 1, PatientName = "John Doe", DateTime = today.AddHours(10).AddMinutes(30), DurationMinutes = 30, VisitType = "Lab Review", Provider = "Dr. Michael Chen", Location = "Room 2", Status = AppointmentStatus.Scheduled },
            new() { Id = 7, PatientId = 2, PatientName = "Jane Smith", DateTime = today.AddHours(11), DurationMinutes = 30, VisitType = "Follow-up", Provider = "Dr. Sarah Smith", Location = "Room 1", Status = AppointmentStatus.Scheduled },
            new() { Id = 8, PatientId = 3, PatientName = "Robert Johnson", DateTime = today.AddHours(13), DurationMinutes = 45, VisitType = "Comprehensive Visit", Provider = "Dr. Emily Wilson", Location = "Room 2", Status = AppointmentStatus.Scheduled },
            new() { Id = 9, PatientId = 4, PatientName = "Maria Garcia", DateTime = today.AddHours(14), DurationMinutes = 30, VisitType = "Follow-up", Provider = "Dr. Sarah Smith", Location = "Room 1", Status = AppointmentStatus.Scheduled },
            new() { Id = 10, PatientId = 5, PatientName = "William Brown", DateTime = today.AddHours(14).AddMinutes(30), DurationMinutes = 20, VisitType = "Well Child", Provider = "Dr. Michael Chen", Location = "Room 3", Status = AppointmentStatus.Scheduled },
            new() { Id = 11, PatientId = 1, PatientName = "John Doe", DateTime = today.AddHours(15), DurationMinutes = 30, VisitType = "Hypertension Check", Provider = "Dr. Emily Wilson", Location = "Room 2", Status = AppointmentStatus.Scheduled },
            
            // Tomorrow's appointments
            new() { Id = 12, PatientId = 2, PatientName = "Jane Smith", DateTime = today.AddDays(1).AddHours(9), DurationMinutes = 30, VisitType = "Follow-up", Provider = "Dr. Sarah Smith", Location = "Room 1", Status = AppointmentStatus.Scheduled },
            new() { Id = 13, PatientId = 3, PatientName = "Robert Johnson", DateTime = today.AddDays(1).AddHours(10), DurationMinutes = 45, VisitType = "Diabetes Management", Provider = "Dr. Michael Chen", Location = "Room 2", Status = AppointmentStatus.Scheduled },
            new() { Id = 14, PatientId = 4, PatientName = "Maria Garcia", DateTime = today.AddDays(1).AddHours(11), DurationMinutes = 30, VisitType = "Annual Exam", Provider = "Dr. Emily Wilson", Location = "Room 3", Status = AppointmentStatus.Scheduled },
            new() { Id = 15, PatientId = 5, PatientName = "William Brown", DateTime = today.AddDays(1).AddHours(14), DurationMinutes = 20, VisitType = "Sick Visit", Provider = "Dr. Sarah Smith", Location = "Room 1", Status = AppointmentStatus.Scheduled },
            
            // Day after tomorrow
            new() { Id = 16, PatientId = 1, PatientName = "John Doe", DateTime = today.AddDays(2).AddHours(8).AddMinutes(30), DurationMinutes = 30, VisitType = "Lab Review", Provider = "Dr. Sarah Smith", Location = "Room 1", Status = AppointmentStatus.Scheduled },
            new() { Id = 17, PatientId = 2, PatientName = "Jane Smith", DateTime = today.AddDays(2).AddHours(10), DurationMinutes = 30, VisitType = "Follow-up", Provider = "Dr. Michael Chen", Location = "Room 2", Status = AppointmentStatus.Scheduled },
            new() { Id = 18, PatientId = 3, PatientName = "Robert Johnson", DateTime = today.AddDays(2).AddHours(13), DurationMinutes = 45, VisitType = "Comprehensive Visit", Provider = "Dr. Emily Wilson", Location = "Room 3", Status = AppointmentStatus.Scheduled },
            
            // Later this week
            new() { Id = 19, PatientId = 4, PatientName = "Maria Garcia", DateTime = today.AddDays(3).AddHours(9), DurationMinutes = 30, VisitType = "New Patient", Provider = "Dr. Sarah Smith", Location = "Room 1", Status = AppointmentStatus.Scheduled },
            new() { Id = 20, PatientId = 5, PatientName = "William Brown", DateTime = today.AddDays(3).AddHours(11), DurationMinutes = 20, VisitType = "Well Child", Provider = "Dr. Michael Chen", Location = "Room 2", Status = AppointmentStatus.Scheduled },
            new() { Id = 21, PatientId = 1, PatientName = "John Doe", DateTime = today.AddDays(4).AddHours(10), DurationMinutes = 30, VisitType = "Follow-up", Provider = "Dr. Emily Wilson", Location = "Room 3", Status = AppointmentStatus.Scheduled },
            new() { Id = 22, PatientId = 2, PatientName = "Jane Smith", DateTime = today.AddDays(4).AddHours(14), DurationMinutes = 30, VisitType = "Annual Exam", Provider = "Dr. Sarah Smith", Location = "Room 1", Status = AppointmentStatus.Scheduled },
            
            // Next week preview
            new() { Id = 23, PatientId = 3, PatientName = "Robert Johnson", DateTime = today.AddDays(7).AddHours(9), DurationMinutes = 45, VisitType = "Diabetes Management", Provider = "Dr. Sarah Smith", Location = "Room 1", Status = AppointmentStatus.Scheduled },
            new() { Id = 24, PatientId = 4, PatientName = "Maria Garcia", DateTime = today.AddDays(7).AddHours(11), DurationMinutes = 30, VisitType = "Follow-up", Provider = "Dr. Michael Chen", Location = "Room 2", Status = AppointmentStatus.Scheduled }
        };
        return appointments;
    }
}
