namespace Zebrahoof_EMR.Services;

/// <summary>
/// Sliding idle decision for the auth cookie. The absolute expiry stays on the cookie;
/// only the last-activity stamp is rewritten, and only after <see cref="RenewAfter"/>.
/// </summary>
public static class AuthCookieIdle
{
    public const string LastActivityKey = ".zebrahoof_last_activity";

    /// <summary>Avoid reissuing the cookie on every request while still sliding well inside the idle window.</summary>
    public static readonly TimeSpan RenewAfter = TimeSpan.FromMinutes(1);

    public static AuthCookieIdleDecision Evaluate(
        IDictionary<string, string?> items,
        DateTimeOffset now,
        TimeSpan idleWindow)
    {
        if (!items.TryGetValue(LastActivityKey, out var ticksText) || !long.TryParse(ticksText, out var ticks))
        {
            items[LastActivityKey] = now.Ticks.ToString();
            return AuthCookieIdleDecision.Renew;
        }

        var lastActivity = new DateTimeOffset(ticks, TimeSpan.Zero);
        if (now - lastActivity > idleWindow)
        {
            return AuthCookieIdleDecision.Expired;
        }

        if (now - lastActivity >= RenewAfter)
        {
            items[LastActivityKey] = now.Ticks.ToString();
            return AuthCookieIdleDecision.Renew;
        }

        return AuthCookieIdleDecision.Keep;
    }

    /// <summary>
    /// Circuit and hub traffic stay up on an idle tab. Counting those requests would slide the window forever.
    /// </summary>
    public static bool CountsAsActivity(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return !path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase);
    }
}

public enum AuthCookieIdleDecision
{
    Keep,
    Renew,
    Expired
}
