namespace Zebrahoof_EMR.Services;

public static class ClinicTime
{
    public static DateTime ToClinic(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;

    public static string Format(DateTime value, string format) =>
        ToClinic(value).ToString(format);
}
