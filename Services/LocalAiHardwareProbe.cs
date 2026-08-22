using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Zebrahoof_EMR.Services;

public sealed record LocalAiHardwareSnapshot
{
    public double TotalRamGb { get; init; }
    public double AvailableRamGb { get; init; }
    public int CpuCores { get; init; }
    public string? GpuName { get; init; }
    public double? GpuVramGb { get; init; }
    public double FreeDiskGb { get; init; }
    public string DiskRoot { get; init; } = string.Empty;

    public string Summary
    {
        get
        {
            var gpu = string.IsNullOrWhiteSpace(GpuName)
                ? "No dedicated GPU detected (CPU inference)"
                : GpuVramGb is { } vram
                    ? $"{GpuName} · {vram:0.#} GB VRAM"
                    : GpuName;
            return $"{TotalRamGb:0.#} GB RAM ({AvailableRamGb:0.#} GB free) · {CpuCores} CPU cores · {gpu} · {FreeDiskGb:0.#} GB disk free";
        }
    }
}

/// <summary>
/// Reads RAM, disk, CPU, and best-effort GPU info so the UI can warn before a
/// download that will not fit or will crawl.
/// </summary>
public static class LocalAiHardwareProbe
{
    public static LocalAiHardwareSnapshot Probe(string contentRoot)
    {
        var (totalRam, availRam) = ReadRam();
        var (gpuName, gpuVram) = ReadGpu();
        var (freeDisk, diskRoot) = ReadDisk(contentRoot);

        return new LocalAiHardwareSnapshot
        {
            TotalRamGb = totalRam,
            AvailableRamGb = availRam,
            CpuCores = Math.Max(1, Environment.ProcessorCount),
            GpuName = gpuName,
            GpuVramGb = gpuVram,
            FreeDiskGb = freeDisk,
            DiskRoot = diskRoot
        };
    }

    internal static (double TotalGb, double AvailableGb) ReadRam()
    {
        if (OperatingSystem.IsWindows() && TryReadWindowsRam(out var total, out var avail))
        {
            return (BytesToGb(total), BytesToGb(avail));
        }

        var info = GC.GetGCMemoryInfo();
        var committed = info.TotalAvailableMemoryBytes > 0
            ? info.TotalAvailableMemoryBytes
            : Math.Max(info.TotalCommittedBytes, 8L * 1024 * 1024 * 1024);
        return (BytesToGb(committed), BytesToGb(Math.Max(0, committed - info.MemoryLoadBytes)));
    }

    internal static (double FreeGb, string Root) ReadDisk(string contentRoot)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(contentRoot));
            if (string.IsNullOrEmpty(root))
            {
                root = Path.GetPathRoot(Environment.CurrentDirectory) ?? "";
            }

            var drive = new DriveInfo(root);
            if (drive.IsReady)
            {
                return (BytesToGb(drive.AvailableFreeSpace), drive.Name);
            }
        }
        catch (Exception)
        {
            // Fall through.
        }

        return (0, contentRoot);
    }

    internal static double BytesToGb(long bytes) => Math.Round(bytes / 1024d / 1024d / 1024d, 1);

    internal static double BytesToGb(ulong bytes) => Math.Round(bytes / 1024d / 1024d / 1024d, 1);

    private static bool TryReadWindowsRam(out ulong total, out ulong available)
    {
        total = 0;
        available = 0;
        try
        {
            var status = new MemoryStatusEx();
            if (!GlobalMemoryStatusEx(status))
            {
                return false;
            }

            total = status.ullTotalPhys;
            available = status.ullAvailPhys;
            return total > 0;
        }
        catch
        {
            return false;
        }
    }

    private static (string? Name, double? VramGb) ReadGpu()
    {
        var nvidia = TryNvidiaSmi();
        if (nvidia.Name != null)
        {
            return nvidia;
        }

        if (OperatingSystem.IsWindows())
        {
            return TryWindowsCimGpu();
        }

        return (null, null);
    }

    private static (string? Name, double? VramGb) TryNvidiaSmi()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=name,memory.total --format=csv,noheader,nounits",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            if (process == null)
            {
                return (null, null);
            }

            if (!process.WaitForExit(4000))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return (null, null);
            }

            var line = process.StandardOutput.ReadToEnd()
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(line))
            {
                return (null, null);
            }

            var parts = line.Split(',', 2, StringSplitOptions.TrimEntries);
            var name = parts[0];
            double? vram = null;
            if (parts.Length > 1 && double.TryParse(parts[1], out var mb) && mb > 0)
            {
                vram = Math.Round(mb / 1024d, 1);
            }

            return string.IsNullOrWhiteSpace(name) ? (null, null) : (name, vram);
        }
        catch
        {
            return (null, null);
        }
    }

    private static (string? Name, double? VramGb) TryWindowsCimGpu()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -Command \"Get-CimInstance Win32_VideoController | Where-Object { $_.Name -and $_.Name -notmatch 'Microsoft Basic' } | Select-Object -First 2 Name, AdapterRAM | ConvertTo-Json -Compress\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            if (process == null)
            {
                return (null, null);
            }

            if (!process.WaitForExit(5000))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return (null, null);
            }

            var json = process.StandardOutput.ReadToEnd().Trim();
            if (string.IsNullOrWhiteSpace(json))
            {
                return (null, null);
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in root.EnumerateArray())
                {
                    var parsed = ParseCimGpu(el);
                    if (parsed.Name != null)
                    {
                        return parsed;
                    }
                }

                return (null, null);
            }

            return ParseCimGpu(root);
        }
        catch
        {
            return (null, null);
        }
    }

    internal static (string? Name, double? VramGb) ParseCimGpu(JsonElement el)
    {
        var name = el.TryGetProperty("Name", out var nameEl) ? nameEl.GetString() : null;
        double? vram = null;
        if (el.TryGetProperty("AdapterRAM", out var ramEl) && ramEl.TryGetInt64(out var bytes) && bytes > 0)
        {
            // Win32 AdapterRAM is often a 32-bit value that saturates at 4 GB.
            vram = BytesToGb((ulong)bytes);
        }

        if (string.IsNullOrWhiteSpace(name) ||
            name.Contains("Microsoft Basic", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        return (name, vram);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);
}
