using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class AuthStateService
{
    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;
    public string CurrentLocation { get; private set; } = "Main Clinic";
    
    public static List<string> AvailableLocations { get; } = new()
    {
        "Main Clinic",
        "South Branch", 
        "West Branch"
    };

    public event Action? OnAuthStateChanged;

    public void Login(User user)
    {
        CurrentUser = user;
        OnAuthStateChanged?.Invoke();
    }

    public void Logout()
    {
        CurrentUser = null;
        OnAuthStateChanged?.Invoke();
    }

    public void SwitchRole(UserRole role)
    {
        if (CurrentUser != null)
        {
            CurrentUser.Role = role;
            OnAuthStateChanged?.Invoke();
        }
    }

    public void SetLocation(string location)
    {
        if (AvailableLocations.Contains(location))
        {
            CurrentLocation = location;
            OnAuthStateChanged?.Invoke();
        }
    }

    // Mock users for different roles
    public static List<User> GetMockUsers() =>
    [
        new() { Id = 1, Username = "physician", FullName = "Dr. Sarah Smith", Email = "sarah.smith@clinic.com", Role = UserRole.Physician },
        new() { Id = 2, Username = "nurse", FullName = "Mike Jones, RN", Email = "mike.jones@clinic.com", Role = UserRole.Nurse },
        new() { Id = 3, Username = "admin", FullName = "Admin User", Email = "admin@clinic.com", Role = UserRole.Admin },
        new() { Id = 4, Username = "frontdesk", FullName = "Emily Chen", Email = "emily.chen@clinic.com", Role = UserRole.FrontDesk },
        new() { Id = 5, Username = "billing", FullName = "James Wilson", Email = "james.wilson@clinic.com", Role = UserRole.Billing }
    ];
}
