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

    public static string? HeartRateMarker(int? beatsPerMinute) =>
        beatsPerMinute > 100 ? "High" : beatsPerMinute < 50 ? "Low" : null;

    public static string? HeartRateRangeError(int? beatsPerMinute) =>
        beatsPerMinute is null or >= 20 and <= 250 ? null : "Heart rate must be between 20 and 250 bpm.";

    public static string? RespiratoryMarker(int? breathsPerMinute) =>
        breathsPerMinute > 20 ? "High" : breathsPerMinute < 12 ? "Low" : null;

    public static string? RespiratoryRangeError(int? breathsPerMinute) =>
        breathsPerMinute is null or >= 4 and <= 60 ? null : "Respiratory rate must be between 4 and 60 breaths/min.";

    public static string? SystolicMarker(int? mmHg) =>
        mmHg >= 140 ? "High" : mmHg < 90 ? "Low" : null;

    public static string? SystolicRangeError(int? mmHg) =>
        mmHg is null or >= 60 and <= 250 ? null : "Systolic BP must be between 60 and 250 mmHg.";

    public static string? DiastolicMarker(int? mmHg) =>
        mmHg >= 90 ? "High" : mmHg < 60 ? "Low" : null;

    public static string? DiastolicRangeError(int? mmHg) =>
        mmHg is null or >= 30 and <= 150 ? null : "Diastolic BP must be between 30 and 150 mmHg.";

    public static string? OxygenMarker(int? percent) =>
        percent < 95 ? "Low" : null;

    public static string? OxygenRangeError(int? percent) =>
        percent is null or >= 50 and <= 100 ? null : "SpO2 must be between 50 and 100%.";

    public static string? TemperatureRangeError(decimal? fahrenheit) =>
        fahrenheit is null or >= 90 and <= 110 ? null : "Temperature must be between 90 and 110 °F.";

    public static string? WeightRangeError(decimal? pounds) =>
        pounds is null or > 0 and <= 1000 ? null : "Weight must be between 1 and 1000 lbs.";
}
