using System.Text;

namespace Zebrahoof_EMR.Helpers;

public static class SessionCookieHelper
{
    public const string RefreshCookieName = "zebrahoof.refresh";
    public const string SessionIdCookieName = "zebrahoof.session.id";

    public static string Encode(Guid sessionId, string refreshToken)
    {
        var payload = $"{sessionId:N}:{refreshToken}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    public static bool TryDecode(string? cookieValue, out Guid sessionId, out string refreshToken)
    {
        sessionId = Guid.Empty;
        refreshToken = string.Empty;

        if (string.IsNullOrWhiteSpace(cookieValue))
        {
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(cookieValue);
            var payload = Encoding.UTF8.GetString(bytes);
            var parts = payload.Split(':', 2, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                return false;
            }

            if (!Guid.TryParseExact(parts[0], "N", out sessionId))
            {
                return false;
            }

            refreshToken = parts[1];
            return true;
        }
        catch
        {
            return false;
        }
    }
}
