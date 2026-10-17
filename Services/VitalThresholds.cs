namespace Zebrahoof_EMR.Services;

public static class VitalThresholds
{
    /// <summary>
    /// Flags fever at 100.0°F and above so 100.2°F is marked high, and hypothermia under 96.0°F.
    /// </summary>
    public static bool IsTemperatureAbnormal(decimal temperatureFahrenheit) =>
        temperatureFahrenheit >= 100.0m || temperatureFahrenheit < 96.0m;

    public static string? TemperatureMarker(decimal temperatureFahrenheit)
    {
        if (temperatureFahrenheit >= 100.0m)
        {
            return "High";
        }

        if (temperatureFahrenheit < 96.0m)
        {
            return "Low";
        }

        return null;
    }
}
