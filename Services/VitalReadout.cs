using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

/// <summary>Text for vital-sign cells. Blank measures render as an em dash, with a real space before a High/Low flag.</summary>
public static class VitalReadout
{
    public const string Blank = "\u2014";

    public readonly record struct KnownMeasure(string Value, string? Flag, DateTime RecordedAt);

    public static string BloodPressure(int? systolic, int? diastolic)
    {
        if (systolic is null && diastolic is null)
        {
            return Blank;
        }

        return $"{Format(systolic)}/{Format(diastolic)}";
    }

    public static string? BloodPressureFlag(int? systolic, int? diastolic)
    {
        var systolicFlag = VitalThresholds.SystolicMarker(systolic);
        var diastolicFlag = VitalThresholds.DiastolicMarker(diastolic);
        if (systolicFlag == "High" || diastolicFlag == "High")
        {
            return "High";
        }

        if (systolicFlag == "Low" || diastolicFlag == "Low")
        {
            return "Low";
        }

        return null;
    }

    public static string Format(int? value) => value?.ToString() ?? Blank;

    public static string Format(decimal? value) => value?.ToString() ?? Blank;

    public static string WithFlag(string value, string? flag) =>
        string.IsNullOrEmpty(flag) ? value : $"{value}\u00A0{flag}";

    public static KnownMeasure? Last(
        IEnumerable<VitalSigns> vitals,
        Func<VitalSigns, bool> hasValue,
        Func<VitalSigns, string> value,
        Func<VitalSigns, string?> flag)
    {
        foreach (var vital in vitals.OrderByDescending(v => v.RecordedAt))
        {
            if (!hasValue(vital))
            {
                continue;
            }

            return new KnownMeasure(value(vital), flag(vital), vital.RecordedAt);
        }

        return null;
    }
}
