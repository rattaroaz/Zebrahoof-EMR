using System.Runtime.InteropServices;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Resolves on-disk locations and official download names for the bundled Ollama engine.
/// </summary>
public static class LocalAiEnginePaths
{
    public const string WindowsZipFileName = "ollama-windows-amd64.zip";
    public const string LinuxTarballFileName = "ollama-linux-amd64.tgz";
    public const string LatestReleaseDownloadBase =
        "https://github.com/ollama/ollama/releases/latest/download/";

    public static string GetDefaultEngineDirectory(string contentRoot)
    {
        return Path.Combine(contentRoot, "App_Data", "ai-engine", "ollama");
    }

    public static string GetModelsDirectory(string contentRoot)
    {
        return Path.Combine(contentRoot, "App_Data", "ai-engine", "models");
    }

    public static string GetPreferredModelPath(string contentRoot) =>
        Path.Combine(contentRoot, "App_Data", "ai-engine", "preferred-model.txt");

    public static string? TryReadPreferredModel(string contentRoot)
    {
        try
        {
            var path = GetPreferredModelPath(contentRoot);
            if (!File.Exists(path))
            {
                return null;
            }

            var value = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static void WritePreferredModel(string contentRoot, string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return;
        }

        var path = GetPreferredModelPath(contentRoot);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var trimmed = modelId.Trim();
        var temp = path + ".tmp";
        File.WriteAllText(temp, trimmed);
        File.Copy(temp, path, overwrite: true);
        try
        {
            File.Delete(temp);
        }
        catch (IOException)
        {
        }
    }

    public static string GetUserOllamaModelsDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".ollama", "models");
    }

    public static IReadOnlyList<string> GetModelSearchDirectories(string contentRoot)
    {
        var dirs = new List<string> { GetModelsDirectory(contentRoot) };
        var userDir = GetUserOllamaModelsDirectory();
        if (!string.Equals(dirs[0], userDir, StringComparison.OrdinalIgnoreCase))
        {
            dirs.Add(userDir);
        }

        return dirs;
    }

    public static IReadOnlyList<string> ListInstalledModelTagsForApp(string contentRoot) =>
        MergeTags(GetModelSearchDirectories(contentRoot).SelectMany(ListInstalledModelTags));

    public static IReadOnlyList<string> ListInstalledModelTags(string modelsDirectory)
    {
        var manifests = Path.Combine(modelsDirectory, "manifests");
        if (!Directory.Exists(manifests))
        {
            return Array.Empty<string>();
        }

        var tags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(manifests, "*", SearchOption.AllDirectories);
        }
        catch (IOException)
        {
            return Array.Empty<string>();
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }

        foreach (var file in files)
        {
            if (TryParseManifestTag(manifests, file, out var tag))
            {
                tags.Add(tag);
            }
        }

        return tags.ToArray();
    }

    public static bool TryParseManifestTag(string manifestsRoot, string manifestFile, out string tag)
    {
        tag = string.Empty;
        if (string.IsNullOrWhiteSpace(manifestFile))
        {
            return false;
        }

        var rel = Path.GetRelativePath(manifestsRoot, manifestFile);
        if (string.IsNullOrWhiteSpace(rel) || rel.StartsWith("..", StringComparison.Ordinal))
        {
            return false;
        }

        var parts = rel.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return false;
        }

        var tagPart = parts[^1];
        var name = parts[^2];
        var ns = parts[^3];
        if (string.IsNullOrWhiteSpace(tagPart) || string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        tag = string.Equals(ns, "library", StringComparison.OrdinalIgnoreCase)
            ? $"{name}:{tagPart}"
            : $"{ns}/{name}:{tagPart}";
        return true;
    }

    public static IReadOnlyList<string> MergeTags(params IEnumerable<string>?[] sources)
    {
        var tags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            if (source == null)
            {
                continue;
            }

            foreach (var name in source)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    tags.Add(name.Trim());
                }
            }
        }

        return tags.ToArray();
    }

    public static string FormatSavedModelsSummary(IReadOnlyList<string> tags, int show = 6)
    {
        if (tags.Count == 0)
        {
            return string.Empty;
        }

        var shown = tags.Take(show).ToArray();
        var text = string.Join(", ", shown);
        var extra = tags.Count - shown.Length;
        return extra > 0 ? $"{text}, and {extra} more" : text;
    }

    public static string GetDownloadsDirectory(string contentRoot)
    {
        return Path.Combine(contentRoot, "App_Data", "ai-engine", "downloads");
    }

    public static string GetArchiveFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WindowsZipFileName;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return LinuxTarballFileName;
        }

        throw new PlatformNotSupportedException(
            "Automatic local AI install is supported on Windows and Linux only.");
    }

    public static string GetArchiveDownloadUrl() => LatestReleaseDownloadBase + GetArchiveFileName();

    public static string? FindOllamaExecutable(string engineDirectory)
    {
        if (string.IsNullOrWhiteSpace(engineDirectory))
        {
            return null;
        }

        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ollama.exe" : "ollama";
        var direct = Path.Combine(engineDirectory, exeName);
        if (File.Exists(direct))
        {
            return direct;
        }

        if (!Directory.Exists(engineDirectory))
        {
            return FindSystemOllamaExecutable();
        }

        try
        {
            var nested = Directory.EnumerateFiles(engineDirectory, exeName, SearchOption.AllDirectories)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(nested))
            {
                return nested;
            }
        }
        catch (IOException)
        {
            // Directory disappeared or is inaccessible.
        }

        return FindSystemOllamaExecutable();
    }

    public static string? FindSystemOllamaExecutable()
    {
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ollama.exe" : "ollama";
        var candidates = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            candidates.Add(Path.Combine(localAppData, "Programs", "Ollama", exeName));
            candidates.Add(Path.Combine(programFiles, "Ollama", exeName));
        }
        else
        {
            candidates.Add("/usr/local/bin/ollama");
            candidates.Add("/usr/bin/ollama");
        }

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return FindOnPath(exeName);
    }

    public static string? FindOnPath(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var full = Path.Combine(dir.Trim(), fileName);
                if (File.Exists(full))
                {
                    return full;
                }
            }
            catch (ArgumentException)
            {
                // Invalid path segment.
            }
        }

        return null;
    }

    public static LocalAiPhase ComputePhase(
        bool engineInstalled,
        bool engineRunning,
        bool modelReady,
        bool isBusyInstalling,
        bool isBusyStarting,
        bool isBusyPulling,
        bool hasError)
    {
        if (isBusyInstalling)
        {
            return LocalAiPhase.InstallingEngine;
        }

        if (isBusyStarting)
        {
            return LocalAiPhase.Starting;
        }

        if (isBusyPulling)
        {
            return LocalAiPhase.DownloadingModel;
        }

        if (hasError && !engineRunning)
        {
            return LocalAiPhase.Error;
        }

        if (engineInstalled && engineRunning && modelReady)
        {
            return LocalAiPhase.Ready;
        }

        if (!engineInstalled)
        {
            return LocalAiPhase.NotInstalled;
        }

        return LocalAiPhase.NeedsSetup;
    }
}
