namespace Zebrahoof_EMR.Helpers;

/// <summary>
/// Identifies one browser, not one user and not one auth session.
/// Idle expiry and a closed browser keep the cookie, so drafts can be restored.
/// An explicit logout deletes it. Another device has its own cookie.
/// </summary>
public static class DraftBrowser
{
    public const string CookieName = "zebrahoof.draft-browser";
    public const string ItemKey = "zebrahoof.draft-browser.id";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public static string? Read(HttpContext? context)
    {
        if (context == null)
        {
            return null;
        }

        if (context.Items.ContainsKey(ItemKey))
        {
            return context.Items[ItemKey] is string staged && IsId(staged) ? staged : null;
        }

        if (context.Request.Cookies.TryGetValue(CookieName, out var cookie) && IsId(cookie))
        {
            return cookie;
        }

        return null;
    }

    public static string Ensure(HttpContext context)
    {
        var current = Read(context);
        if (current != null)
        {
            context.Items[ItemKey] = current;
            return current;
        }

        var created = Guid.NewGuid().ToString("N");
        context.Items[ItemKey] = created;
        context.Response.Cookies.Append(CookieName, created, BuildOptions(context));
        return created;
    }

    public static void Delete(HttpContext context)
    {
        context.Items[ItemKey] = string.Empty;
        context.Response.Cookies.Delete(CookieName, BuildOptions(context));
    }

    public static bool IsId(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 64
        && value.IndexOf('\u001f') < 0;

    private static CookieOptions BuildOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        MaxAge = Lifetime,
        Path = "/",
        IsEssential = true
    };
}
