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

    public void SetLocation(string location)
    {
        if (AvailableLocations.Contains(location))
        {
            CurrentLocation = location;
            OnAuthStateChanged?.Invoke();
        }
    }
}
