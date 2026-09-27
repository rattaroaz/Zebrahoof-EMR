using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class LocalAiFitTests
{
    [Fact]
    public void Catalog_IncludesOnlySupportedFamilies()
    {
        Assert.Contains(LocalAiModels.Catalog, m => m.Family == "Qwen" && m.Id.StartsWith("qwen2.5:"));
        Assert.Contains(LocalAiModels.Catalog, m => m.Id.StartsWith("qwen3:"));
        Assert.Contains(LocalAiModels.Catalog, m => m.Id == "qwen3.8:27b");
        Assert.Contains(LocalAiModels.Catalog, m => m.Family == "DeepSeek" && m.Id.Contains("deepseek-r1"));
        Assert.Contains(LocalAiModels.Catalog, m => m.Family == "Gemma");
        Assert.Contains(LocalAiModels.Catalog, m => m.Family == "GPT-OSS");
        Assert.DoesNotContain(LocalAiModels.Catalog, m => m.Family is "Kimi" or "Llama" or "Mistral" or "Phi" or "GLM");
        Assert.All(LocalAiModels.Catalog, m => Assert.True(LocalAiModels.IsSupportedFamily(m.Family)));
        Assert.True(LocalAiModels.Catalog.Length >= 20);
    }

    [Fact]
    public void Assess_TooLargeWhenRamIsBelowMinimum()
    {
        var hw = Pc(ram: 8, disk: 200, gpuVram: null);
        var model = LocalAiModels.Find("qwen2.5:32b")!;

        var fit = LocalAiModels.Assess(model, hw);

        Assert.Equal(LocalAiFitKind.TooLarge, fit.Kind);
        Assert.Contains("RAM", fit.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Assess_TooLargeWhenDiskIsShort()
    {
        var hw = Pc(ram: 64, disk: 3, gpuVram: 24);
        var model = LocalAiModels.Find("qwen2.5:7b")!;

        var fit = LocalAiModels.Assess(model, hw);

        Assert.Equal(LocalAiFitKind.TooLarge, fit.Kind);
        Assert.Contains("disk", fit.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Assess_TooLargeForHugeModelOnCpu()
    {
        var hw = Pc(ram: 64, disk: 500, gpuVram: null);
        var model = LocalAiModels.Find("deepseek-r1:32b")!;

        var fit = LocalAiModels.Assess(model, hw);

        Assert.Equal(LocalAiFitKind.TooLarge, fit.Kind);
    }

    [Fact]
    public void Assess_SlowFor14BOnCpu()
    {
        var hw = Pc(ram: 32, disk: 200, gpuVram: null);
        var model = LocalAiModels.Find("qwen2.5:14b")!;

        var fit = LocalAiModels.Assess(model, hw);

        Assert.Equal(LocalAiFitKind.Slow, fit.Kind);
        Assert.Contains("CPU", fit.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Assess_SlowWhenRamIsTight()
    {
        var hw = Pc(ram: 8, disk: 200, gpuVram: 8);
        var model = LocalAiModels.Find("qwen2.5:7b")!;

        var fit = LocalAiModels.Assess(model, hw);

        Assert.Equal(LocalAiFitKind.Slow, fit.Kind);
    }

    [Fact]
    public void Assess_RecommendedWhenGpuAndRamFit()
    {
        var hw = Pc(ram: 32, disk: 400, gpuVram: 12, gpuName: "RTX 4070");
        var model = LocalAiModels.Find("qwen2.5:7b")!;

        var fit = LocalAiModels.Assess(model, hw);

        Assert.Equal(LocalAiFitKind.Recommended, fit.Kind);
    }

    [Fact]
    public void SuggestDefault_PrefersRunnableQwenOnModestPc()
    {
        var hw = Pc(ram: 16, disk: 200, gpuVram: null);
        var suggested = LocalAiModels.SuggestDefault(hw);

        Assert.Equal("Qwen", suggested.Family);
        var fit = LocalAiModels.Assess(suggested, hw);
        Assert.True(fit.Kind is LocalAiFitKind.Recommended or LocalAiFitKind.Usable);
        Assert.True(suggested.ParameterBillion <= 8);
    }

    [Fact]
    public void SelectTopRunnablePerFamily_KeepsFiveMostPowerfulThatFitEachFamily()
    {
        var hw = Pc(ram: 16, disk: 400, gpuVram: null);
        var catalog = new[]
        {
            M("qwen:0.5b", "Qwen", 0.5, minRam: 2),
            M("qwen:1b", "Qwen", 1, minRam: 2),
            M("qwen:3b", "Qwen", 3, minRam: 4),
            M("qwen:7b", "Qwen", 7, minRam: 8),
            M("qwen:8b", "Qwen", 8, minRam: 10),
            M("qwen:14b", "Qwen", 14, minRam: 16),
            M("qwen:32b", "Qwen", 32, minRam: 32),
            M("ds:1.5b", "DeepSeek", 1.5, minRam: 3),
            M("ds:7b", "DeepSeek", 7, minRam: 8),
            M("ds:8b", "DeepSeek", 8, minRam: 10),
            M("ds:14b", "DeepSeek", 14, minRam: 16),
            M("ds:32b", "DeepSeek", 32, minRam: 32),
            M("ds:70b", "DeepSeek", 70, minRam: 64),
            M("gemma:1b", "Gemma", 1, minRam: 3),
            M("gemma:4b", "Gemma", 4, minRam: 6),
            M("gpt:20b", "GPT-OSS", 20, minRam: 16)
        };

        var offered = LocalAiModels.SelectTopRunnablePerFamily(catalog, hw);
        var qwen = offered.Where(m => m.Family == "Qwen").Select(m => m.Id).ToArray();
        var deepseek = offered.Where(m => m.Family == "DeepSeek").Select(m => m.Id).ToArray();

        Assert.Equal(5, qwen.Length);
        Assert.Equal(["qwen:14b", "qwen:8b", "qwen:7b", "qwen:3b", "qwen:1b"], qwen);
        Assert.DoesNotContain("qwen:32b", qwen);
        Assert.DoesNotContain("qwen:0.5b", qwen);
        Assert.Equal(["ds:14b", "ds:8b", "ds:7b", "ds:1.5b"], deepseek);
        Assert.DoesNotContain(offered, m => m.Id is "ds:32b" or "ds:70b");
        Assert.Contains(offered, m => m.Id == "gemma:4b");
        Assert.Contains(offered, m => m.Id == "gpt:20b");
    }

    [Fact]
    public void SelectTopRunnablePerFamily_KeepsInstalledEvenWhenTooLarge()
    {
        var hw = Pc(ram: 8, disk: 50, gpuVram: null);
        var catalog = new[]
        {
            M("qwen:3b", "Qwen", 3, minRam: 4),
            M("qwen:32b", "Qwen", 32, minRam: 32)
        };

        var offered = LocalAiModels.SelectTopRunnablePerFamily(catalog, hw, alwaysIncludeIds: ["qwen:32b"]);

        Assert.Contains(offered, m => m.Id == "qwen:32b");
        Assert.Contains(offered, m => m.Id == "qwen:3b");
    }

    [Fact]
    public void SelectTopRunnablePerFamily_PrefersReasoningWhenSizeTies()
    {
        var hw = Pc(ram: 32, disk: 400, gpuVram: 12);
        var catalog = new[]
        {
            M("qwen:7b-base", "Qwen", 7, minRam: 8, reasoning: false, download: 4),
            M("qwen:7b-think", "Qwen", 7, minRam: 8, reasoning: true, download: 4),
            M("qwen:3b", "Qwen", 3, minRam: 4),
            M("qwen:1b", "Qwen", 1, minRam: 2),
            M("qwen:0.6b", "Qwen", 0.6, minRam: 2),
            M("qwen:0.5b", "Qwen", 0.5, minRam: 2)
        };

        var offered = LocalAiModels.SelectTopRunnablePerFamily(catalog, hw);
        var qwen = offered.Where(m => m.Family == "Qwen").Select(m => m.Id).ToArray();

        Assert.Equal("qwen:7b-think", qwen[0]);
        Assert.Equal("qwen:7b-base", qwen[1]);
        Assert.DoesNotContain("qwen:0.5b", qwen);
    }

    [Fact]
    public void IsOfferedOnThisMachine_RejectsWeakerThanTopFive()
    {
        var hw = Pc(ram: 16, disk: 400, gpuVram: null);
        var catalog = Enumerable.Range(1, 8)
            .Select(i => M($"qwen:{i}b", "Qwen", i, minRam: 2))
            .ToArray();

        Assert.True(LocalAiModels.IsOfferedOnThisMachine("qwen:8b", catalog, hw));
        Assert.False(LocalAiModels.IsOfferedOnThisMachine("qwen:1b", catalog, hw));
        Assert.False(LocalAiModels.IsOfferedOnThisMachine("qwen:99b", catalog, hw));
    }

    [Fact]
    public void SuggestDefault_PrefersAlreadyInstalledModel()
    {
        var hw = Pc(ram: 16, disk: 200, gpuVram: null);
        var suggested = LocalAiModels.SuggestDefault(hw, LocalAiModels.Catalog, ["qwen2.5:3b"]);

        Assert.Equal("qwen2.5:3b", suggested.Id);
    }

    [Fact]
    public void BytesToGb_Converts()
    {
        Assert.Equal(1.0, LocalAiHardwareProbe.BytesToGb(1024L * 1024 * 1024));
    }

    private static LocalAiHardwareSnapshot Pc(double ram, double disk, double? gpuVram, string? gpuName = null) =>
        new()
        {
            TotalRamGb = ram,
            AvailableRamGb = ram * 0.6,
            CpuCores = 8,
            GpuName = gpuVram is null ? null : gpuName ?? "GPU",
            GpuVramGb = gpuVram,
            FreeDiskGb = disk,
            DiskRoot = "C:\\"
        };

    private static LocalAiModelChoice M(
        string id,
        string family,
        double param,
        double minRam,
        bool reasoning = false,
        double download = 1) =>
        new(id, family, id, id, download, minRam, minRam + 2, minRam / 2, param, reasoning);
}
