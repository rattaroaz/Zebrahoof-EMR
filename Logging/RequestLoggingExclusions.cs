using System;
using Microsoft.AspNetCore.Http;

namespace Zebrahoof_EMR.Logging;

/// <summary>
/// Blazor, SignalR, and static-asset traffic can dominate logs; this suppresses the completion
/// line for high-volume, low-signal request paths.
/// </summary>
public static class RequestLoggingExclusions
{
    public static bool IsExcluded(PathString path)
    {
        var s = path.Value;
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }

        if (s.StartsWith("/api", StringComparison.Ordinal))
        {
            return true;
        }

        if (s.StartsWith("/_framework", StringComparison.Ordinal) ||
            s.StartsWith("/_blazor", StringComparison.Ordinal) ||
            s.StartsWith("/_content", StringComparison.Ordinal) ||
            s.StartsWith("/_vs", StringComparison.Ordinal))
        {
            return true;
        }

        if (s.EndsWith(".map", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (s.StartsWith("/css", StringComparison.Ordinal) ||
            s.StartsWith("/js", StringComparison.Ordinal) ||
            s.StartsWith("/images", StringComparison.Ordinal) ||
            s.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (s.Equals("/health", StringComparison.Ordinal) ||
            s.Equals("/healthz", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
