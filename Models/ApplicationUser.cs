using Microsoft.AspNetCore.Identity;

namespace Zebrahoof_EMR.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Department { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? MfaSecret { get; set; }
    public string? PasswordSalt { get; set; }
}
