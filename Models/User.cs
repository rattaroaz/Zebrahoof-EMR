namespace Zebrahoof_EMR.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum UserRole
{
    Admin,
    Physician,
    Nurse,
    MedicalAssistant,
    LabTechnician,
    FrontDesk,
    Billing,
    Patient
}
