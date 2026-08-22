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
