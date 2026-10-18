using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Zebrahoof_EMR.Services;

public static class ChartUser
{
    public static async Task<string?> ResolveAsync(AuthenticationStateProvider auth)
    {
        var state = await auth.GetAuthenticationStateAsync();
        return Resolve(state.User);
    }

    public static string? Resolve(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.Identity?.Name;
    }
}
