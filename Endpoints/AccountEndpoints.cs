using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Zebrahoof_EMR.Helpers;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof_EMR.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/login", HandleLogin);
        endpoints.MapPost("/account/logout", HandleLogout);
        endpoints.MapGet("/account/logout", HandleLogout);
        endpoints.MapPost("/account/refresh", HandleRefresh);
    }

    private static async Task<IResult> HandleLogin(
        HttpContext context,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        SessionService sessionService,
        AuthSyncService authSyncService)
    {
        var form = await context.Request.ReadFormAsync();
        var rawInput = form["Username"].ToString().Trim();
        var password = form["Password"].ToString();
        var rememberMe = form.TryGetValue("RememberMe", out var rememberValue) &&
                         string.Equals(rememberValue, "on", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(rememberValue, "true", StringComparison.OrdinalIgnoreCase);
        var returnUrl = NormalizeReturnUrl(form["ReturnUrl"].ToString());

        ApplicationUser? user = null;
        if (!string.IsNullOrWhiteSpace(rawInput))
        {
            user = await userManager.FindByNameAsync(rawInput);
            if (user == null && rawInput.Contains('@', StringComparison.Ordinal))
            {
                user = await userManager.FindByEmailAsync(rawInput);
            }
        }

        if (user == null)
        {
            return Results.Redirect($"/login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        if (!user.IsActive)
        {
            return Results.Redirect($"/login?error=inactive&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var result = await signInManager.PasswordSignInAsync(user.UserName!, password, rememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var roles = await userManager.GetRolesAsync(user);
            var idleWindow = ResolveIdleWindow(roles);
            await IssueSessionAsync(context, user, sessionService, idleWindow);
            
            // Sync user authentication state with actual roles
            await authSyncService.SyncUserAuthenticationAsync(user.UserName!);
            
            return Results.Redirect(returnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            return Results.Redirect($"/mfa-challenge?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        if (result.IsLockedOut)
        {
            return Results.Redirect($"/login?error=locked&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        return Results.Redirect($"/login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    private static async Task<IResult> HandleLogout(
        HttpContext context,
        SignInManager<ApplicationUser> signInManager,
        SessionService sessionService,
        AuthSyncService authSyncService)
    {
        await signInManager.SignOutAsync();
        await RevokeSessionAsync(context, sessionService, "logout");
        ClearSessionCookies(context);
        
        // Clear auth state
        authSyncService.LogoutUser();
        
        return Results.Redirect("/login?logout=success");
    }

    private static async Task<IResult> HandleRefresh(HttpContext context, SessionService sessionService)
    {
        if (!TryReadSessionCookies(context, out var sessionId, out var refreshToken))
        {
            ClearSessionCookies(context);
            return Results.Unauthorized();
        }

        var session = await sessionService.ValidateRefreshTokenAsync(sessionId, refreshToken);
        if (session == null || await sessionService.IsIdleExpiredAsync(sessionId))
        {
            ClearSessionCookies(context);
            return Results.Unauthorized();
        }

        var newToken = await sessionService.RotateRefreshTokenAsync(sessionId);
        if (newToken == null)
        {
            ClearSessionCookies(context);
            return Results.Unauthorized();
        }

        WriteSessionCookies(context, sessionId, newToken);
        return Results.Ok();
    }

    private static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (!Uri.TryCreate(returnUrl, UriKind.Relative, out var uri) || returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return "/";
        }

        return uri.OriginalString;
    }

    private static async Task IssueSessionAsync(HttpContext context, ApplicationUser user, SessionService sessionService, TimeSpan idleWindow)
    {
        var fingerprint = DeviceFingerprintHelper.ComputeFingerprint(context);
        var deviceName = context.Request.Headers["X-Device-Name"].ToString();
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var session = await sessionService.CreateSessionAsync(
            user.Id,
            fingerprint,
            string.IsNullOrWhiteSpace(deviceName) ? context.Request.Headers.UserAgent.ToString() : deviceName,
            ip,
            idleWindow,
            TimeSpan.FromHours(12));

        WriteSessionCookies(context, session.Id, session.RefreshToken!);
        context.Response.Cookies.Append(SessionCookieHelper.SessionIdCookieName, session.Id.ToString(), BuildCookieOptions(httpOnly: false));
    }

    private static async Task RevokeSessionAsync(HttpContext context, SessionService sessionService, string reason)
    {
        if (!TryReadSessionCookies(context, out var sessionId, out _))
        {
            return;
        }

        await sessionService.RevokeSessionAsync(sessionId, reason);
    }

    private static bool TryReadSessionCookies(HttpContext context, out Guid sessionId, out string refreshToken)
    {
        sessionId = Guid.Empty;
        refreshToken = string.Empty;

        var raw = context.Request.Cookies[SessionCookieHelper.RefreshCookieName];
        if (!SessionCookieHelper.TryDecode(raw, out sessionId, out refreshToken))
        {
            return false;
        }

        return true;
    }

    private static void WriteSessionCookies(HttpContext context, Guid sessionId, string refreshToken)
    {
        context.Response.Cookies.Append(SessionCookieHelper.RefreshCookieName,
            SessionCookieHelper.Encode(sessionId, refreshToken),
            BuildCookieOptions(httpOnly: true));
    }

    private static void ClearSessionCookies(HttpContext context)
    {
        context.Response.Cookies.Delete(SessionCookieHelper.RefreshCookieName);
        context.Response.Cookies.Delete(SessionCookieHelper.SessionIdCookieName);
    }

    private static CookieOptions BuildCookieOptions(bool httpOnly) => new()
    {
        HttpOnly = httpOnly,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        MaxAge = TimeSpan.FromHours(12),
        Path = "/"
    };

    private static TimeSpan ResolveIdleWindow(IList<string> roles)
    {
        var adminRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Admin",
            "System Administrator"
        };

        return roles.Any(role => adminRoles.Contains(role))
            ? TimeSpan.FromMinutes(30)
            : TimeSpan.FromMinutes(15);
    }
}
