using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class AuthSyncService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserMappingService _userMappingService;
    private readonly AuthStateService _authStateService;
    private readonly ILogger<AuthSyncService> _logger;

    public AuthSyncService(
        UserManager<ApplicationUser> userManager,
        UserMappingService userMappingService,
        AuthStateService authStateService,
        ILogger<AuthSyncService> logger)
    {
        _userManager = userManager;
        _userMappingService = userMappingService;
        _authStateService = authStateService;
        _logger = logger;
    }

    public async Task<bool> SyncUserAuthenticationAsync(string username)
    {
        try
        {
            var applicationUser = await _userManager.FindByNameAsync(username);
            if (applicationUser == null && username.Contains('@'))
            {
                applicationUser = await _userManager.FindByEmailAsync(username);
            }

            if (applicationUser == null || !applicationUser.IsActive)
            {
                return false;
            }

            var mappedUser = await _userMappingService.MapApplicationUserToUser(applicationUser);
            if (mappedUser == null)
            {
                return false;
            }

            _authStateService.Login(mappedUser);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auth sync failed for username {Username}", username);
            return false;
        }
    }

    public void LogoutUser()
    {
        _authStateService.Logout();
    }

    public async Task<bool> RefreshUserRoleAsync()
    {
        if (_authStateService.CurrentUser == null)
            return false;

        return await SyncUserAuthenticationAsync(_authStateService.CurrentUser.Username);
    }
}
