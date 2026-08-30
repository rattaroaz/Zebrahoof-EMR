using System.Text.Json;
using MudBlazor;

namespace Zebrahoof_EMR.Services;

public sealed record ChartSummaryWindowDef(
    string Key,
    string Label,
    string Icon,
    bool HasTab);

/// <summary>
/// Summary-page windows map 1:1 to chart tabs (plus Risk Scores).
/// Layout is the ordered list of visible window keys in a non-overlapping grid.
/// </summary>
public static class ChartSummaryWindows
{
    public const string StorageKey = "zebrahoof-chart-summary-windows";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static readonly ChartSummaryWindowDef[] Catalog =
    [
        new("problems", "Problems", Icons.Material.Filled.LocalHospital, true),
        new("medications", "Medications", Icons.Material.Filled.Medication, true),
        new("allergies", "Allergies", Icons.Material.Filled.Warning, true),
        new("labs", "Labs", Icons.Material.Filled.Science, true),
        new("vitals", "Vitals", Icons.Material.Filled.MonitorHeart, true),
        new("encounter", "Encounter", Icons.Material.Filled.MeetingRoom, true),
        new("risk", "Risk Scores", Icons.Material.Filled.Assessment, false),
        new("orders", "Orders", Icons.Material.Filled.Assignment, true),
        new("imaging", "Imaging", Icons.Material.Filled.Image, true),
        new("history", "History", Icons.Material.Filled.History, true),
        new("immunizations", "Immunizations", Icons.Material.Filled.Vaccines, true),
        new("documents", "Documents", Icons.Material.Filled.Description, true),
        new("notes", "Notes", Icons.Material.Filled.Note, true),
        new("careteam", "Care Team", Icons.Material.Filled.Groups, true),
        new("demographics", "Demographics", Icons.Material.Filled.Person, true)
    ];

    public static readonly string[] DefaultVisible =
    [
        "problems", "medications", "allergies", "labs", "vitals", "encounter", "risk"
    ];

    public static ChartSummaryWindowDef? Find(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? null
            : Catalog.FirstOrDefault(w => string.Equals(w.Key, key, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<string> ResolveVisible(IEnumerable<string>? saved)
    {
        if (saved == null)
        {
            return DefaultVisible.ToArray();
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var key in saved)
        {
            var def = Find(key);
            if (def != null && seen.Add(def.Key))
            {
                result.Add(def.Key);
            }
        }

        return result;
    }

    public static IReadOnlyList<ChartSummaryWindowDef> Hidden(IEnumerable<string> visible)
    {
        var shown = new HashSet<string>(visible, StringComparer.OrdinalIgnoreCase);
        return Catalog.Where(w => !shown.Contains(w.Key)).ToArray();
    }

    public static List<string> Add(IReadOnlyList<string> visible, string key)
    {
        var list = visible.ToList();
        var def = Find(key);
        if (def == null || list.Any(k => string.Equals(k, def.Key, StringComparison.OrdinalIgnoreCase)))
        {
            return list;
        }

        list.Add(def.Key);
        return list;
    }

    public static List<string> Remove(IReadOnlyList<string> visible, string key) =>
        visible.Where(k => !string.Equals(k, key, StringComparison.OrdinalIgnoreCase)).ToList();

    public static List<string> Move(IReadOnlyList<string> visible, string dragKey, string targetKey)
    {
        var list = visible.ToList();
        var from = list.FindIndex(k => string.Equals(k, dragKey, StringComparison.OrdinalIgnoreCase));
        var to = list.FindIndex(k => string.Equals(k, targetKey, StringComparison.OrdinalIgnoreCase));
        if (from < 0 || to < 0 || from == to)
        {
            return list;
        }

        var item = list[from];
        list.RemoveAt(from);
        if (from < to)
        {
            to--;
        }

        list.Insert(to, item);
        return list;
    }

    public static List<string> ParseLayout(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return DefaultVisible.ToList();
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var keys = ReadKeys(doc.RootElement);
            return ResolveVisible(keys).ToList();
        }
        catch (JsonException)
        {
            return DefaultVisible.ToList();
        }
    }

    public static string SerializeLayout(IEnumerable<string> visible) =>
        JsonSerializer.Serialize(visible.ToList(), JsonOptions);

    private static IEnumerable<string> ReadKeys(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return KeysFromArray(root);
        }

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("windows", out var windows))
        {
            return KeysFromArray(windows);
        }

        return Array.Empty<string>();
    }

    private static IEnumerable<string> KeysFromArray(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var key = item.GetString();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    yield return key;
                }
            }
            else if (item.ValueKind == JsonValueKind.Object &&
                     item.TryGetProperty("key", out var keyEl) &&
                     keyEl.ValueKind == JsonValueKind.String)
            {
                var key = keyEl.GetString();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    yield return key;
                }
            }
        }
    }
}
