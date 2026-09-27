using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public static class PatientAskGuard
{
    public const string MixWarning =
        "Warning: Mixing patient information can be confusing and cause problems. Keep this conversation in one context when you can.";

    /// <summary>
    /// Patient chart routes: /patients/12 or /patients/12/chart. The list /patients is management mode.
    /// </summary>
    public static bool IsPatientMode(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri) || !Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        var parts = parsed.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
               && parts[0].Equals("patients", StringComparison.OrdinalIgnoreCase)
               && int.TryParse(parts[1], out _);
    }

    public static bool MentionsNamedPatient(
        string? text,
        IEnumerable<Patient> roster,
        int? excludePatientId = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (var patient in roster)
        {
            if (excludePatientId is int current && patient.Id == current)
            {
                continue;
            }

            if (text.Contains(patient.FullName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (patient.FirstName.Length >= 2
                && patient.LastName.Length >= 2
                && text.Contains(patient.FirstName, StringComparison.OrdinalIgnoreCase)
                && text.Contains(patient.LastName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ShouldWarnInManagementMode(string question, IEnumerable<Patient> roster) =>
        MentionsNamedPatient(question, roster);

    public static bool ShouldWarnInPatientMode(string question, IEnumerable<Patient> roster, int currentPatientId) =>
        MentionsNamedPatient(question, roster, currentPatientId);
}
