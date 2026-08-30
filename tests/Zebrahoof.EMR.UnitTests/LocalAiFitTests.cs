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
}
