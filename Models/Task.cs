namespace Zebrahoof_EMR.Models;

public class ClinicalTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ClinicalTaskType Type { get; set; }
    public ClinicalTaskPriority Priority { get; set; }
    public ClinicalTaskStatus Status { get; set; }
    public int? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum ClinicalTaskType
{
    SignNote,
    ReviewResults,
    MedicationRenewal,
    Referral,
    PhoneCall,
    Other
}

public enum ClinicalTaskPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum ClinicalTaskStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}
