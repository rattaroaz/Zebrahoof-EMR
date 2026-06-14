using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Zebrahoof_EMR.Logging;

/// <summary>
/// Helpers for diagnostic logs around large or sensitive payloads (e.g. HTTP bodies, model output).
/// Use <see cref="DescribeWithoutRawContent"/> for external responses — raw prefixes can still contain PHI or secrets.
/// </summary>
public static class SafeLogContent
{
    private const int MaxSafeApiMessageLength = 256;

    /// <summary>
    /// Summary for logs: length, short stable hash for correlating repeats, and optionally a short
    /// sanitizer-safe message extracted from common JSON error shapes — never raw body text.
    /// </summary>
    public static string DescribeWithoutRawContent(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "empty";
        }

        var len = value.Length;
        var hash = ShortContentHash(value);
        if (TryExtractSafeApiErrorSummary(value, out var apiMsg))
        {
            return $"length={len} sha256_12={hash} apiError={apiMsg}";
        }

        return $"length={len} sha256_12={hash}";
    }

    private static string ShortContentHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes.AsSpan(0, 6)); // 12 hex chars
    }

    private static bool TryExtractSafeApiErrorSummary(string json, out string summary)
    {
        summary = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var errEl))
            {
                if (errEl.ValueKind == JsonValueKind.Object &&
                    errEl.TryGetProperty("message", out var msgEl) &&
                    msgEl.ValueKind == JsonValueKind.String)
                {
                    summary = SanitizeOneLine(msgEl.GetString());
                    return summary.Length > 0;
                }

                if (errEl.ValueKind == JsonValueKind.String)
                {
                    summary = SanitizeOneLine(errEl.GetString());
                    return summary.Length > 0;
                }
            }

            if (root.TryGetProperty("message", out var messageEl) && messageEl.ValueKind == JsonValueKind.String)
            {
                summary = SanitizeOneLine(messageEl.GetString());
                return summary.Length > 0;
            }

            if (root.TryGetProperty("detail", out var detailEl) && detailEl.ValueKind == JsonValueKind.String)
            {
                summary = SanitizeOneLine(detailEl.GetString());
                return summary.Length > 0;
            }
        }
        catch (JsonException)
        {
            // Not JSON — omit apiError; caller still gets length + hash.
        }

        return false;
    }

    private static string SanitizeOneLine(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var collapsed = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (collapsed.Length > MaxSafeApiMessageLength)
        {
            collapsed = collapsed[..MaxSafeApiMessageLength] + "…";
        }

        return collapsed;
    }
}
