using Microsoft.AspNetCore.Http;

namespace Zebrahoof_EMR.Logging;

/// <summary>
/// Paths that must not be rewritten by the 404 status-code handler.
/// Re-entering /not-found (or API/circuit routes) can recurse until the process dies.
/// </summary>
public static class StatusCodePagePaths
{
    public static bool ShouldLeaveNotFoundAsIs(PathString path)
    {
        var s = path.Value;
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }

        if (s.Equals("/not-found", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return s.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
               || s.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)
               || s.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
               || s.StartsWith("/_content", StringComparison.OrdinalIgnoreCase)
               || s.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase)
               || s.StartsWith("/health", StringComparison.OrdinalIgnoreCase);
    }
}
