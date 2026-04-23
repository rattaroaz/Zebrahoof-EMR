using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Zebrahoof_EMR.Helpers;

public static class DeviceFingerprintHelper
{
    public static string ComputeFingerprint(HttpContext context)
    {
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        var ip = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()
                 ?? context.Connection.RemoteIpAddress?.ToString()
                 ?? "unknown";
        var clientHint = context.Request.Headers["X-Device-Id"].ToString();

        var raw = $"{userAgent}|{ip}|{clientHint}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(bytes);
    }
}
