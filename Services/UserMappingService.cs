using Microsoft.AspNetCore.Identity;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class UserMappingService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserMappingService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<User?> MapApplicationUserToUser(ApplicationUser applicationUser)
    {
        if (applicationUser == null)
            return null;

        var roles = await _userManager.GetRolesAsync(applicationUser);
        var primaryRole = DeterminePrimaryRole(roles);

        return new User
        {
            Id = int.TryParse(applicationUser.Id, out var id) ? id : 0,
            Username = applicationUser.UserName ?? string.Empty,
            FullName = GetDisplayName(applicationUser),
            Email = applicationUser.Email ?? string.Empty,
            Role = primaryRole,
            IsActive = applicationUser.IsActive
        };
    }

    private static UserRole DeterminePrimaryRole(IList<string> roles)
    {
        if (roles == null || roles.Count == 0)
            return UserRole.Physician; // Default fallback

        // Priority order for role determination
        var rolePriority = new Dictionary<string, UserRole>(StringComparer.OrdinalIgnoreCase)
        {
            // Admin roles
            { "Admin", UserRole.Admin },
            { "Administrator", UserRole.Admin },
            { "System Administrator", UserRole.Admin },
            { "Manager", UserRole.Admin },
            { "Supervisor", UserRole.Admin },
            { "Director", UserRole.Admin },
            
            // Clinical roles
            { "Physician", UserRole.Physician },
            { "Doctor", UserRole.Physician },
            { "Nurse", UserRole.Nurse },
            { "RN", UserRole.Nurse },
            
            // Medical Assistant roles
            { "MA", UserRole.MedicalAssistant },
            { "Medical Assistant", UserRole.MedicalAssistant },
            
            // Lab roles
            { "Lab", UserRole.LabTechnician },
            { "Lab Technician", UserRole.LabTechnician },
            { "Lab Tech", UserRole.LabTechnician },
            { "Technician", UserRole.LabTechnician },
            
            // Front desk/operations roles
            { "FrontDesk", UserRole.FrontDesk },
            { "Front Desk", UserRole.FrontDesk },
            { "Receptionist", UserRole.FrontDesk },
            { "Scheduler", UserRole.FrontDesk },
            
            // Billing roles
            { "Billing", UserRole.Billing },
            { "Biller", UserRole.Billing },
            
            // Patient role
            { "Patient", UserRole.Patient }
        };

        // Find the first matching role based on priority
        foreach (var role in roles)
        {
            if (rolePriority.TryGetValue(role, out var mappedRole))
                return mappedRole;
        }

        // If no specific role matches, try to parse the role name directly
        if (roles.Count > 0 && Enum.TryParse<UserRole>(roles[0], true, out var parsedRole))
            return parsedRole;

        // Additional fallback logic for common role patterns
        if (roles.Count > 0)
        {
            var firstRole = roles[0].ToLowerInvariant();
            if (firstRole.Contains("admin") || firstRole.Contains("manager") || firstRole.Contains("supervisor") || firstRole.Contains("director"))
                return UserRole.Admin;
            
            if (firstRole.Contains("physician") || firstRole.Contains("doctor") || firstRole.Contains("dr"))
                return UserRole.Physician;
            
            if (firstRole.Contains("nurse") || firstRole.Contains("rn"))
                return UserRole.Nurse;
            
            if (firstRole.Contains("ma") || firstRole.Contains("medical assistant"))
                return UserRole.MedicalAssistant;
            
            if (firstRole.Contains("lab") || firstRole.Contains("technician") || firstRole.Contains("tech"))
                return UserRole.LabTechnician;
            
            if (firstRole.Contains("reception") || firstRole.Contains("schedule") || firstRole.Contains("front"))
                return UserRole.FrontDesk;
            
            if (firstRole.Contains("bill"))
                return UserRole.Billing;
            
            if (firstRole.Contains("patient"))
                return UserRole.Patient;
        }

        // Default fallback
        return UserRole.Physician;
    }

    private static string GetDisplayName(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            return user.DisplayName;

        if (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName))
            return $"{user.FirstName} {user.LastName}";

        return user.UserName ?? "Unknown User";
    }
}
