namespace Zebrahoof_EMR.Logging;

/// <summary>
/// Truncates potentially large or sensitive strings (e.g. API bodies, model output) for diagnostic logs.
/// </summary>
public static class SafeLogContent
{
    public const int DefaultMaxLength = 512;
    public const int ShortMaxLength = 200;

    public static string Truncate(string? value, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "… [truncated]";
    }
}
