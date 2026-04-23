using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class MockTaskService
{
    private readonly List<ClinicalTask> _tasks;
    private int _nextId = 100;

    public static readonly List<string> Assignees =
    [
        "Dr. Sarah Smith",
        "Dr. Michael Chen",
        "Dr. Emily Wilson",
        "Mike Jones, RN",
        "Emily Chen",
        "Lisa Brown, MA"
    ];

    public MockTaskService()
    {
        _tasks = GenerateMockTasks();
    }

    public Task<List<ClinicalTask>> GetAllTasksAsync() =>
        Task.FromResult(_tasks.OrderByDescending(t => t.CreatedAt).ToList());

    public Task<List<ClinicalTask>> GetPendingTasksAsync() =>
        Task.FromResult(_tasks
            .Where(t => t.Status == ClinicalTaskStatus.Pending || t.Status == ClinicalTaskStatus.InProgress)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToList());

    public Task<List<ClinicalTask>> GetCompletedTasksAsync() =>
        Task.FromResult(_tasks
            .Where(t => t.Status == ClinicalTaskStatus.Completed)
            .OrderByDescending(t => t.CompletedAt)
            .ToList());

    public Task<List<ClinicalTask>> GetTasksByAssigneeAsync(string assignee) =>
        Task.FromResult(_tasks.Where(t => t.AssignedTo == assignee).OrderByDescending(t => t.CreatedAt).ToList());

    public Task<List<ClinicalTask>> GetTasksByPatientAsync(int patientId) =>
        Task.FromResult(_tasks.Where(t => t.PatientId == patientId).OrderByDescending(t => t.CreatedAt).ToList());

    public Task<List<ClinicalTask>> GetTasksByTypeAsync(ClinicalTaskType type) =>
        Task.FromResult(_tasks.Where(t => t.Type == type).OrderByDescending(t => t.CreatedAt).ToList());

    public Task<List<ClinicalTask>> GetOverdueTasksAsync() =>
        Task.FromResult(_tasks
            .Where(t => t.DueDate.HasValue && 
                        t.DueDate.Value.Date < DateTime.Today && 
                        t.Status != ClinicalTaskStatus.Completed &&
                        t.Status != ClinicalTaskStatus.Cancelled)
            .OrderBy(t => t.DueDate)
            .ToList());

    public Task<List<ClinicalTask>> GetTasksDueTodayAsync() =>
        Task.FromResult(_tasks
            .Where(t => t.DueDate.HasValue && 
                        t.DueDate.Value.Date == DateTime.Today &&
                        t.Status != ClinicalTaskStatus.Completed &&
                        t.Status != ClinicalTaskStatus.Cancelled)
            .OrderByDescending(t => t.Priority)
            .ToList());

    public Task<ClinicalTask?> GetTaskByIdAsync(int id) =>
        Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id));

    public Task<ClinicalTask> AddTaskAsync(ClinicalTask task)
    {
        task.Id = _nextId++;
        task.CreatedAt = DateTime.Now;
        _tasks.Add(task);
        return Task.FromResult(task);
    }

    public Task UpdateTaskAsync(ClinicalTask task)
    {
        var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
        if (existing != null)
        {
            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.Type = task.Type;
            existing.Priority = task.Priority;
            existing.Status = task.Status;
            existing.PatientId = task.PatientId;
            existing.PatientName = task.PatientName;
            existing.AssignedTo = task.AssignedTo;
            existing.DueDate = task.DueDate;
            if (task.Status == ClinicalTaskStatus.Completed && !existing.CompletedAt.HasValue)
            {
                existing.CompletedAt = DateTime.Now;
            }
        }
        return Task.CompletedTask;
    }

    public Task CompleteTaskAsync(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.Status = ClinicalTaskStatus.Completed;
            task.CompletedAt = DateTime.Now;
        }
        return Task.CompletedTask;
    }

    public Task UpdateStatusAsync(int id, ClinicalTaskStatus status)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.Status = status;
            if (status == ClinicalTaskStatus.Completed)
            {
                task.CompletedAt = DateTime.Now;
            }
        }
        return Task.CompletedTask;
    }

    public Task DeleteTaskAsync(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task != null) _tasks.Remove(task);
        return Task.CompletedTask;
    }

    public Task<int> GetPendingCountAsync() =>
        Task.FromResult(_tasks.Count(t => t.Status == ClinicalTaskStatus.Pending || t.Status == ClinicalTaskStatus.InProgress));

    public Task<int> GetOverdueCountAsync() =>
        Task.FromResult(_tasks.Count(t => t.DueDate.HasValue && 
                                          t.DueDate.Value.Date < DateTime.Today && 
                                          t.Status != ClinicalTaskStatus.Completed &&
                                          t.Status != ClinicalTaskStatus.Cancelled));

    private static List<ClinicalTask> GenerateMockTasks()
    {
        var now = DateTime.Now;
        return
        [
            // Urgent/High Priority Tasks
            new() { Id = 1, Title = "Sign Progress Note", Description = "Sign note for John Doe visit on " + DateTime.Today.AddDays(-1).ToShortDateString(), Type = ClinicalTaskType.SignNote, Priority = ClinicalTaskPriority.High, Status = ClinicalTaskStatus.Pending, PatientId = 1, PatientName = "John Doe", AssignedTo = "Dr. Sarah Smith", CreatedBy = "System", CreatedAt = now.AddHours(-2), DueDate = DateTime.Today },
            new() { Id = 2, Title = "Review Lab Results - STAT", Description = "Critical potassium level needs review", Type = ClinicalTaskType.ReviewResults, Priority = ClinicalTaskPriority.Urgent, Status = ClinicalTaskStatus.Pending, PatientId = 3, PatientName = "Robert Johnson", AssignedTo = "Dr. Sarah Smith", CreatedBy = "Lab System", CreatedAt = now.AddMinutes(-30), DueDate = DateTime.Today },
            new() { Id = 3, Title = "Referral Review", Description = "Cardiology referral needs authorization", Type = ClinicalTaskType.Referral, Priority = ClinicalTaskPriority.Urgent, Status = ClinicalTaskStatus.InProgress, PatientId = 1, PatientName = "John Doe", AssignedTo = "Emily Chen", CreatedBy = "Dr. Sarah Smith", CreatedAt = now.AddHours(-24), DueDate = DateTime.Today },
            
            // Normal Priority Tasks
            new() { Id = 4, Title = "Review Lab Results", Description = "CBC and CMP results ready for review", Type = ClinicalTaskType.ReviewResults, Priority = ClinicalTaskPriority.Normal, Status = ClinicalTaskStatus.Pending, PatientId = 3, PatientName = "Robert Johnson", AssignedTo = "Dr. Sarah Smith", CreatedBy = "Lab System", CreatedAt = now.AddHours(-5), DueDate = DateTime.Today },
            new() { Id = 5, Title = "Medication Renewal", Description = "Metformin 500mg renewal request", Type = ClinicalTaskType.MedicationRenewal, Priority = ClinicalTaskPriority.Normal, Status = ClinicalTaskStatus.Pending, PatientId = 3, PatientName = "Robert Johnson", AssignedTo = "Dr. Sarah Smith", CreatedBy = "Pharmacy", CreatedAt = now.AddDays(-1), DueDate = DateTime.Today.AddDays(1) },
            new() { Id = 6, Title = "Sign Referral Letter", Description = "Orthopedic referral for Jane Smith", Type = ClinicalTaskType.SignNote, Priority = ClinicalTaskPriority.Normal, Status = ClinicalTaskStatus.Pending, PatientId = 2, PatientName = "Jane Smith", AssignedTo = "Dr. Michael Chen", CreatedBy = "Emily Chen", CreatedAt = now.AddHours(-8), DueDate = DateTime.Today.AddDays(2) },
            new() { Id = 7, Title = "Review Imaging Results", Description = "Chest X-ray results available", Type = ClinicalTaskType.ReviewResults, Priority = ClinicalTaskPriority.Normal, Status = ClinicalTaskStatus.Pending, PatientId = 4, PatientName = "Maria Garcia", AssignedTo = "Dr. Emily Wilson", CreatedBy = "Radiology", CreatedAt = now.AddHours(-3), DueDate = DateTime.Today.AddDays(1) },
            
            // Low Priority Tasks  
            new() { Id = 8, Title = "Phone Call", Description = "Follow up call regarding test results", Type = ClinicalTaskType.PhoneCall, Priority = ClinicalTaskPriority.Low, Status = ClinicalTaskStatus.Pending, PatientId = 2, PatientName = "Jane Smith", AssignedTo = "Mike Jones, RN", CreatedBy = "Dr. Sarah Smith", CreatedAt = now.AddDays(-2), DueDate = DateTime.Today },
            new() { Id = 9, Title = "Patient Education", Description = "Diabetes education materials to send", Type = ClinicalTaskType.Other, Priority = ClinicalTaskPriority.Low, Status = ClinicalTaskStatus.Pending, PatientId = 3, PatientName = "Robert Johnson", AssignedTo = "Lisa Brown, MA", CreatedBy = "Dr. Sarah Smith", CreatedAt = now.AddDays(-1), DueDate = DateTime.Today.AddDays(3) },
            new() { Id = 10, Title = "Update Care Plan", Description = "Annual care plan update needed", Type = ClinicalTaskType.Other, Priority = ClinicalTaskPriority.Low, Status = ClinicalTaskStatus.Pending, PatientId = 5, PatientName = "William Brown", AssignedTo = "Dr. Sarah Smith", CreatedBy = "System", CreatedAt = now.AddDays(-3), DueDate = DateTime.Today.AddDays(7) },
            
            // Overdue Tasks
            new() { Id = 11, Title = "Sign Discharge Summary", Description = "Hospital discharge summary pending signature", Type = ClinicalTaskType.SignNote, Priority = ClinicalTaskPriority.High, Status = ClinicalTaskStatus.Pending, PatientId = 1, PatientName = "John Doe", AssignedTo = "Dr. Sarah Smith", CreatedBy = "Hospital System", CreatedAt = now.AddDays(-3), DueDate = DateTime.Today.AddDays(-1) },
            new() { Id = 12, Title = "Medication Prior Auth", Description = "Prior authorization for Humira", Type = ClinicalTaskType.MedicationRenewal, Priority = ClinicalTaskPriority.Normal, Status = ClinicalTaskStatus.InProgress, PatientId = 4, PatientName = "Maria Garcia", AssignedTo = "Emily Chen", CreatedBy = "Pharmacy", CreatedAt = now.AddDays(-5), DueDate = DateTime.Today.AddDays(-2) },
            
            // Completed Tasks (recent)
            new() { Id = 13, Title = "Review Lab Results", Description = "Annual labs reviewed", Type = ClinicalTaskType.ReviewResults, Priority = ClinicalTaskPriority.Normal, Status = ClinicalTaskStatus.Completed, PatientId = 2, PatientName = "Jane Smith", AssignedTo = "Dr. Sarah Smith", CreatedBy = "Lab System", CreatedAt = now.AddDays(-2), DueDate = DateTime.Today.AddDays(-1), CompletedAt = now.AddDays(-1) },
            new() { Id = 14, Title = "Phone Call Completed", Description = "Discussed medication changes", Type = ClinicalTaskType.PhoneCall, Priority = ClinicalTaskPriority.Normal, Status = ClinicalTaskStatus.Completed, PatientId = 5, PatientName = "William Brown", AssignedTo = "Mike Jones, RN", CreatedBy = "Dr. Michael Chen", CreatedAt = now.AddDays(-3), DueDate = DateTime.Today.AddDays(-2), CompletedAt = now.AddHours(-4) },
            new() { Id = 15, Title = "Sign Progress Note", Description = "Follow-up visit note signed", Type = ClinicalTaskType.SignNote, Priority = ClinicalTaskPriority.High, Status = ClinicalTaskStatus.Completed, PatientId = 4, PatientName = "Maria Garcia", AssignedTo = "Dr. Emily Wilson", CreatedBy = "System", CreatedAt = now.AddDays(-1), DueDate = DateTime.Today, CompletedAt = now.AddHours(-2) }
        ];
    }
}
