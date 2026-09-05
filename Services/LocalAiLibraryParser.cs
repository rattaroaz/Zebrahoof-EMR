using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Zebrahoof_EMR.Services;

public sealed record LocalAiLibraryListing(
    string Name,
    string Description,
    IReadOnlyList<string> SizeTags,
    bool Thinking);

public static class LocalAiLibraryParser
{
    public const int CacheTtlDays = 7;
    public const string LibraryUrl = "https://ollama.com/library";

    private static readonly Regex LibraryHref = new(
        @"href=""/library/(?<name>[^""/?#:]+)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Description = new(
        @"max-w-lg[^>]*>(?<desc>[\s\S]*?)</p>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SizeChip = new(
        @"bg-\[#ddf4ff\][^>]*>(?<size>[^<]+)</span>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ThinkingChip = new(
        @">\s*thinking\s*<",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SizeTag = new(
        @"^\s*(?<num>\d+(?:\.\d+)?)(?<unit>[bt])\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] FamilyOrder = LocalAiModels.SupportedFamilies;

    public static bool IsCacheFresh(DateTimeOffset? pulledAtUtc, DateTimeOffset nowUtc, int ttlDays = CacheTtlDays)
    {
        if (pulledAtUtc is null)
        {
            return false;
        }

        return nowUtc - pulledAtUtc.Value < TimeSpan.FromDays(ttlDays);
    }

    public static IReadOnlyList<LocalAiLibraryListing> ParseLibraryHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var listings = new List<LocalAiLibraryListing>();

        foreach (Match match in LibraryHref.Matches(html))
        {
            var name = match.Groups["name"].Value.Trim();
            if (name.Length == 0 || !seen.Add(name) || ShouldSkipModel(name))
            {
                continue;
            }

            var windowStart = match.Index;
            var windowLength = Math.Min(2400, html.Length - windowStart);
            var card = html.Substring(windowStart, windowLength);

            var descMatch = Description.Match(card);
            var description = descMatch.Success
                ? WebUtility.HtmlDecode(descMatch.Groups["desc"].Value).Trim()
                : string.Empty;

            var sizes = new List<string>();
            var sizeSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match chip in SizeChip.Matches(card))
            {
                var raw = chip.Groups["size"].Value.Trim();
                if (TryNormalizeSizeTag(raw, out var size) && sizeSeen.Add(size))
                {
                    sizes.Add(size);
                }
            }

            listings.Add(new LocalAiLibraryListing(
                name,
                description,
                sizes,
                ThinkingChip.IsMatch(card) || LooksReasoning(name)));
        }

        return listings;
    }

    public static IReadOnlyList<LocalAiModelChoice> ToChoices(IEnumerable<LocalAiLibraryListing> listings)
    {
        var choices = new List<LocalAiModelChoice>();
        foreach (var listing in listings)
        {
            var family = FamilyFromName(listing.Name);
            if (!LocalAiModels.IsSupportedFamily(family))
            {
                continue;
            }

            var description = string.IsNullOrWhiteSpace(listing.Description)
                ? "Official Ollama library model."
                : listing.Description;

            if (listing.SizeTags.Count == 0)
            {
                choices.Add(BuildChoice(listing.Name, family, listing.Name, description, listing.Thinking, null));
                continue;
            }

            foreach (var size in listing.SizeTags)
            {
                var id = listing.Name + ":" + size;
                choices.Add(BuildChoice(id, family, listing.Name, description, listing.Thinking, size));
            }
        }

        return choices;
    }

    public static IReadOnlyList<LocalAiModelChoice> MergeWithSeed(
        IEnumerable<LocalAiModelChoice> live,
        IEnumerable<LocalAiModelChoice>? seed = null)
    {
        var seedList = LocalAiModels.OnlySupported(seed ?? LocalAiModels.Catalog).ToList();
        var byId = new Dictionary<string, LocalAiModelChoice>(StringComparer.OrdinalIgnoreCase);

        foreach (var model in LocalAiModels.OnlySupported(live))
        {
            byId[model.Id] = model;
        }

        foreach (var model in seedList)
        {
            if (byId.TryGetValue(model.Id, out var existing))
            {
                byId[model.Id] = existing with
                {
                    DownloadGb = model.DownloadGb,
                    MinRamGb = model.MinRamGb,
                    RecommendedRamGb = model.RecommendedRamGb,
                    MinVramGb = model.MinVramGb,
                    ParameterBillion = model.ParameterBillion,
                    DisplayName = model.DisplayName,
                    Reasoning = existing.Reasoning || model.Reasoning
                };
            }
            else
            {
                byId[model.Id] = model;
            }
        }

        return byId.Values
            .OrderBy(m => FamilyRank(m.Family))
            .ThenBy(m => m.Family, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.ParameterBillion)
            .ThenBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string[] FamiliesOf(IEnumerable<LocalAiModelChoice> catalog) =>
        catalog.Select(m => m.Family).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static bool ShouldSkipModel(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        return name.Contains("embed", StringComparison.OrdinalIgnoreCase)
               || name.Contains("minilm", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryNormalizeSizeTag(string raw, out string size)
    {
        size = string.Empty;
        if (string.Equals(raw.Trim(), "e2b", StringComparison.OrdinalIgnoreCase))
        {
            size = "e2b";
            return true;
        }

        var match = SizeTag.Match(raw);
        if (!match.Success)
        {
            return false;
        }

        var num = match.Groups["num"].Value;
        var unit = match.Groups["unit"].Value.ToLowerInvariant();
        size = num + unit;
        return true;
    }

    public static bool TryParseParameterBillion(string? sizeOrName, out double parameterBillion)
    {
        parameterBillion = 0;
        if (string.IsNullOrWhiteSpace(sizeOrName))
        {
            return false;
        }

        if (string.Equals(sizeOrName.Trim(), "e2b", StringComparison.OrdinalIgnoreCase))
        {
            parameterBillion = 2;
            return true;
        }

        var match = SizeTag.Match(sizeOrName);
        if (!match.Success)
        {
            return false;
        }

        if (!double.TryParse(match.Groups["num"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        parameterBillion = match.Groups["unit"].Value.Equals("t", StringComparison.OrdinalIgnoreCase)
            ? value * 1000
            : value;
        return parameterBillion > 0;
    }

    public static string FamilyFromName(string name)
    {
        var n = name.Trim();
        if (n.StartsWith("qwen", StringComparison.OrdinalIgnoreCase))
        {
            return "Qwen";
        }

        if (n.StartsWith("llama", StringComparison.OrdinalIgnoreCase))
        {
            return "Llama";
        }

        if (n.Contains("gemma", StringComparison.OrdinalIgnoreCase))
        {
            return "Gemma";
        }

        if (n.StartsWith("deepseek", StringComparison.OrdinalIgnoreCase))
        {
            return "DeepSeek";
        }

        if (n.StartsWith("kimi", StringComparison.OrdinalIgnoreCase))
        {
            return "Kimi";
        }

        if (n.Contains("mistral", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("mixtral", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("magistral", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("ministral", StringComparison.OrdinalIgnoreCase))
        {
            return "Mistral";
        }

        if (n.StartsWith("phi", StringComparison.OrdinalIgnoreCase))
        {
            return "Phi";
        }

        if (n.StartsWith("glm", StringComparison.OrdinalIgnoreCase))
        {
            return "GLM";
        }

        if (n.StartsWith("gpt-oss", StringComparison.OrdinalIgnoreCase))
        {
            return "GPT-OSS";
        }

        var first = n.Split('-', 2)[0];
        return string.IsNullOrEmpty(first)
            ? "Other"
            : char.ToUpperInvariant(first[0]) + first[1..];
    }

    private static LocalAiModelChoice BuildChoice(
        string id,
        string family,
        string modelName,
        string description,
        bool reasoning,
        string? sizeTag)
    {
        var param = 30d;
        if (sizeTag != null && TryParseParameterBillion(sizeTag, out var parsed))
        {
            param = parsed;
        }

        EstimateHardware(param, out var download, out var minRam, out var recRam, out var minVram);
        return new LocalAiModelChoice(
            id,
            family,
            BuildDisplayName(family, modelName, sizeTag),
            description,
            download,
            minRam,
            recRam,
            minVram,
            param,
            reasoning);
    }

    public static void EstimateHardware(
        double parameterBillion,
        out double downloadGb,
        out double minRamGb,
        out double recommendedRamGb,
        out double minVramGb)
    {
        if (parameterBillion >= 500)
        {
            downloadGb = 400;
            minRamGb = 512;
            recommendedRamGb = 640;
            minVramGb = 320;
            return;
        }

        downloadGb = Math.Max(0.3, Math.Round(parameterBillion * 0.65, 1));
        minRamGb = Math.Max(2, Math.Round(parameterBillion * 1.15, 0));
        recommendedRamGb = Math.Max(minRamGb + 1, Math.Round(parameterBillion * 1.5, 0));
        minVramGb = Math.Max(1, Math.Round(parameterBillion * 0.7, 0));
    }

    public static string BuildDisplayName(string family, string modelName, string? sizeTag)
    {
        var rest = StripFamilyPrefix(family, modelName).Replace('-', ' ').Trim();
        if (string.IsNullOrEmpty(rest))
        {
            rest = modelName;
        }

        rest = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(rest);
        if (string.IsNullOrEmpty(sizeTag))
        {
            return rest;
        }

        return $"{rest} {sizeTag.ToUpperInvariant()}";
    }

    private static string StripFamilyPrefix(string family, string modelName)
    {
        var prefixes = family.Equals("GPT-OSS", StringComparison.OrdinalIgnoreCase)
            ? new[] { "gpt-oss" }
            : new[] { family };

        foreach (var prefix in prefixes)
        {
            if (modelName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && modelName.Length > prefix.Length)
            {
                return modelName[prefix.Length..].TrimStart('-', '.', ' ');
            }
        }

        return modelName;
    }

    private static bool LooksReasoning(string name) =>
        name.Contains("r1", StringComparison.OrdinalIgnoreCase)
        || name.Contains("qwq", StringComparison.OrdinalIgnoreCase)
        || name.Contains("reasoning", StringComparison.OrdinalIgnoreCase);

    private static int FamilyRank(string family)
    {
        var index = Array.FindIndex(FamilyOrder, f => f.Equals(family, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? FamilyOrder.Length : index;
    }
}
