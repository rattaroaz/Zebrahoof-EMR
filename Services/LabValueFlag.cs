namespace Zebrahoof_EMR.Services;

public static class LabValueFlag
{
    /// <summary>H when the value is at or above the high bound, L when it is at or below the low bound.</summary>
    public static string Marker(string? value, string? referenceRange)
    {
        if (!decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var number)
            && !decimal.TryParse(value, out number))
        {
            return "H";
        }

        var range = referenceRange?.Trim();
        if (string.IsNullOrEmpty(range))
        {
            return "H";
        }

        if (range.StartsWith('<') && TryDecimal(range[1..], out var lessThan) && number >= lessThan)
        {
            return "H";
        }

        if (range.StartsWith('>') && TryDecimal(range[1..], out var greaterThan) && number <= greaterThan)
        {
            return "L";
        }

        var parts = range.Split(['-', '–'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && TryDecimal(parts[0], out var low) && TryDecimal(parts[1], out var high))
        {
            if (number > high)
            {
                return "H";
            }

            if (number < low)
            {
                return "L";
            }
        }

        return "H";
    }

    private static bool TryDecimal(string text, out decimal number) =>
        decimal.TryParse(text.Trim(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out number)
        || decimal.TryParse(text.Trim(), out number);
}
