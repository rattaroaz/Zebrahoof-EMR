using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public static class PatientSearch
{
    public static bool Matches(Patient patient, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        var query = term.Trim();
        if (patient.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || patient.MRN.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (patient.Phone?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || (patient.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return true;
        }

        return MatchesDateOfBirth(patient.DateOfBirth, query);
    }

    public static bool MatchesDateOfBirth(DateTime dateOfBirth, string query)
    {
        if (DateTime.TryParse(query, out var parsed) && parsed.Date == dateOfBirth.Date)
        {
            return true;
        }

        var formats = new[]
        {
            dateOfBirth.ToString("MM/dd/yyyy"),
            dateOfBirth.ToString("M/d/yyyy"),
            dateOfBirth.ToString("yyyy-MM-dd"),
            dateOfBirth.ToString("MM-dd-yyyy"),
            dateOfBirth.ToString("MMddyyyy"),
            dateOfBirth.ToString("yyyyMMdd")
        };

        return formats.Any(format => format.Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}
