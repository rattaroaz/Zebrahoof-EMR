using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Zebrahoof_EMR.Services;

namespace Zebrahoof_EMR.Logging;

public static class EndpointAuditHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public static string? ResolveUserId(HttpContext? httpContext)
    {
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? httpContext.User.Identity?.Name;
    }

    public static Task AuditAsync(
        IAuditLogger audit,
        HttpContext? http,
        string action,
        string scope,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var json = metadata == null ? null : JsonSerializer.Serialize(metadata, JsonOptions);
        return audit.LogAsync(action, scope, json, ResolveUserId(http), cancellationToken);
    }
}
