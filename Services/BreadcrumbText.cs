namespace Zebrahoof_EMR.Services;

public static class BreadcrumbText
{
    public sealed record Crumb(string Text, string? Href);

    public static IReadOnlyList<Crumb> Build(
        string path,
        Func<int, string?> patientName,
        Func<int, (int PatientId, string Name)?> encounterPatient)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var items = new List<Crumb> { new("Home", "/dashboard") };
        if (segments.Length == 0)
        {
            return items;
        }

        (int PatientId, string Name)? encounter = null;
        if (PatientRoute.TryParseEncounterId(path, out var encounterId))
        {
            encounter = encounterPatient(encounterId);
        }

        var built = "";
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            built += "/" + segment;
            var isLast = i == segments.Length - 1;
            var text = Label(segment, segments, i, patientName, encounter);
            items.Add(new Crumb(text, isLast ? null : built));
        }

        return items;
    }

    private static string Label(
        string segment,
        string[] segments,
        int index,
        Func<int, string?> patientName,
        (int PatientId, string Name)? encounter)
    {
        if (int.TryParse(segment, out var id))
        {
            if (index > 0 && segments[index - 1].Equals("patients", StringComparison.OrdinalIgnoreCase))
            {
                return patientName(id) ?? $"#{id}";
            }

            if (index > 0 && segments[index - 1].Equals("encounter", StringComparison.OrdinalIgnoreCase))
            {
                return encounter?.Name ?? "Encounter";
            }

            return segment;
        }

        return segment.ToLowerInvariant() switch
        {
            "dashboard" => "Dashboard",
            "patients" => "Patients",
            "schedule" => "Schedule",
            "tasks" => "Tasks",
            "orders" => "Orders",
            "inbox" => "Inbox",
            "patient-messages" => "Patient Messages",
            "admin" => "Administration",
            "settings" => "Settings",
            "templates" => "Templates",
            "users" => "Users",
            "profile" => "Profile",
            "preferences" => "Preferences",
            "encounter" => "Encounter",
            "chart" or "summary" => "Summary",
            _ => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(segment.Replace("-", " "))
        };
    }
}
