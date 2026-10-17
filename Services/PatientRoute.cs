namespace Zebrahoof_EMR.Services;

/// <summary>
/// Reads the patient a URL is about. Chart identity comes from the route, never from a shared selected-patient field.
/// </summary>
public static class PatientRoute
{
    public static bool TryParsePatientId(string? pathOrUri, out int patientId) =>
        TryParseAfter(pathOrUri, "patients", out patientId);

    public static bool TryParseEncounterId(string? pathOrUri, out int encounterId) =>
        TryParseAfter(pathOrUri, "encounter", out encounterId);

    private static bool TryParseAfter(string? pathOrUri, string marker, out int id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(pathOrUri))
        {
            return false;
        }

        var path = pathOrUri;
        if (Uri.TryCreate(pathOrUri, UriKind.Absolute, out var uri))
        {
            path = uri.AbsolutePath;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i].Equals(marker, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(segments[i + 1], out id))
            {
                return true;
            }
        }

        return false;
    }
}
