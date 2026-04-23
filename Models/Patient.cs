namespace Zebrahoof_EMR.Models;

public class Patient
{
    public int Id { get; set; }
    public string MRN { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public DateTime DateOfBirth { get; set; }
    public int Age => CalculateAge();
    public string Sex { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? PrimaryProvider { get; set; }
    public string? InsuranceName { get; set; }
    public string? InsuranceId { get; set; }
    public List<string> Allergies { get; set; } = new();
    public List<string> Alerts { get; set; } = new();
    public DateTime? LastVisit { get; set; }

    private int CalculateAge()
    {
        var today = DateTime.Today;
        var age = today.Year - DateOfBirth.Year;
        if (DateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}
