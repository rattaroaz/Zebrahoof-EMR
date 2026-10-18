using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Zebrahoof_EMR.Helpers;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class AuthSyncService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserMappingService _userMappingService;
    private readonly AuthStateService _authStateService;
    private readonly NoteDraftService _drafts;
    private readonly DraftAutosaveGate _autosave;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthSyncService> _logger;

    public AuthSyncService(
        UserManager<ApplicationUser> userManager,
        UserMappingService userMappingService,
        AuthStateService authStateService,
        NoteDraftService drafts,
        DraftAutosaveGate autosave,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthSyncService> logger)
    {
        _userManager = userManager;
        _userMappingService = userMappingService;
        _authStateService = authStateService;
        _drafts = drafts;
        _autosave = autosave;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Copies the circuit's authenticated principal into this circuit's auth state.
    /// Each circuit has its own <see cref="AuthStateService"/>, so one login cannot
    /// replace another session's user.
    /// </summary>
    public async Task BindCircuitAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(principal.Identity.Name))
        {
            if (_authStateService.IsAuthenticated)
            {
                _authStateService.Logout();
            }

            return;
        }

        var browserId = DraftBrowser.Read(_httpContextAccessor.HttpContext);
        _autosave.Capture(browserId, _drafts.CurrentEpoch(browserId));

        if (string.Equals(_authStateService.CurrentUser?.Username, principal.Identity.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await SyncUserAuthenticationAsync(principal.Identity.Name);
    }

    public async Task<bool> SyncUserAuthenticationAsync(string username)
    {
        ApplicationUser? applicationUser = null;
        try
        {
            applicationUser = await _userManager.FindByNameAsync(username);
            if (applicationUser == null && username.Contains('@', StringComparison.Ordinal))
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
            _logger.LogError(ex, "Auth sync failed for user id {UserId}", applicationUser?.Id ?? "(unknown)");
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
