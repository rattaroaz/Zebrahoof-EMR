namespace Zebrahoof_EMR.Services;

public sealed record LocalAiModelChoice(
    string Id,
    string Family,
    string DisplayName,
    string Description,
    double DownloadGb,
    double MinRamGb,
    double RecommendedRamGb,
    double? MinVramGb,
    double ParameterBillion,
    bool Reasoning = false);

public enum LocalAiFitKind
{
    Recommended = 0,
    Usable = 1,
    Slow = 2,
    TooLarge = 3
}

public sealed record LocalAiModelFit(
    LocalAiFitKind Kind,
    string Title,
    string Detail);

public static class LocalAiModels
{
    /// <summary>
    /// Curated Ollama tags with conservative Q4 size/RAM estimates.
    /// Sizes are approximate; Ollama may pull a slightly different quant.
    /// </summary>
    public static readonly LocalAiModelChoice[] Catalog =
    [
        // Qwen 2.5
        Q("qwen2.5:0.5b", "2.5 0.5B", "Smallest Qwen 2.5. Fine for low-RAM machines.", 0.4, 2, 3, 1, 0.5),
        Q("qwen2.5:1.5b", "2.5 1.5B", "Light Qwen 2.5 for constrained PCs.", 1.0, 3, 4, 2, 1.5),
        Q("qwen2.5:3b", "2.5 3B", "Good quality on modest hardware.", 1.9, 4, 6, 3, 3),
        Q("qwen2.5:7b", "2.5 7B", "Balanced default for clinical chat and notes.", 4.7, 8, 10, 6, 7),
        Q("qwen2.5:14b", "2.5 14B", "Stronger reasoning; needs 16 GB class machines.", 9.0, 16, 20, 10, 14),
        Q("qwen2.5:32b", "2.5 32B", "High quality; GPU strongly recommended.", 20.0, 32, 40, 20, 32),
        Q("qwen2.5:72b", "2.5 72B", "Largest Qwen 2.5 dense model.", 47.0, 64, 80, 40, 72),

        // Qwen 2.5 Coder
        Q("qwen2.5-coder:1.5b", "2.5 Coder 1.5B", "Tiny coding assistant.", 1.0, 3, 4, 2, 1.5),
        Q("qwen2.5-coder:3b", "2.5 Coder 3B", "Small coding model.", 1.9, 4, 6, 3, 3),
        Q("qwen2.5-coder:7b", "2.5 Coder 7B", "Coding-tuned 7B.", 4.7, 8, 10, 6, 7),
        Q("qwen2.5-coder:14b", "2.5 Coder 14B", "Stronger coding model.", 9.0, 16, 20, 10, 14),
        Q("qwen2.5-coder:32b", "2.5 Coder 32B", "Large coding model.", 20.0, 32, 40, 20, 32),

        // Qwen 3
        Q("qwen3:0.6b", "3 0.6B", "Smallest Qwen 3.", 0.5, 2, 3, 1, 0.6),
        Q("qwen3:1.7b", "3 1.7B", "Light Qwen 3.", 1.4, 3, 5, 2, 1.7),
        Q("qwen3:4b", "3 4B", "Compact Qwen 3.", 2.5, 6, 8, 4, 4),
        Q("qwen3:8b", "3 8B", "Solid general Qwen 3.", 5.2, 10, 14, 8, 8),
        Q("qwen3:14b", "3 14B", "Larger Qwen 3.", 9.3, 16, 22, 12, 14),
        Q("qwen3:32b", "3 32B", "Dense Qwen 3 32B.", 20.0, 32, 40, 20, 32),
        Q("qwen3-coder:30b", "3 Coder 30B", "MoE coder (~3B active). Strong on a 24 GB GPU.", 19.0, 24, 32, 16, 30),
        Q("qwen3.6:27b", "3.6 27B", "Newer Qwen dense model for a single high-end GPU.", 17.0, 24, 32, 16, 27),

        // DeepSeek
        D("deepseek-r1:1.5b", "R1 1.5B", "Tiny reasoning distill. Slow-ish because it thinks first.", 1.1, 3, 4, 2, 1.5, true),
        D("deepseek-r1:7b", "R1 7B", "Popular reasoning distill.", 4.7, 8, 12, 6, 7, true),
        D("deepseek-r1:8b", "R1 8B", "Strong small reasoning model.", 5.2, 10, 14, 8, 8, true),
        D("deepseek-r1:14b", "R1 14B", "Heavier reasoning; GPU recommended.", 9.0, 16, 22, 10, 14, true),
        D("deepseek-r1:32b", "R1 32B", "Large reasoning model.", 20.0, 32, 40, 20, 32, true),
        D("deepseek-r1:70b", "R1 70B", "Very large reasoning model.", 43.0, 64, 80, 40, 70, true),
        D("deepseek-r1:671b", "R1 671B", "Full DeepSeek-R1. Needs a multi-GPU server.", 400.0, 512, 640, 320, 671, true),

        // Kimi (Moonshot)
        K("kimi-k2", "K2", "Moonshot Kimi K2 MoE. Very large download.", 60.0, 48, 80, 24, 1000),
        K("kimi-k2.6", "K2.6", "Newer Kimi K2.6 agentic / coding model.", 60.0, 48, 80, 24, 1000),
        K("kimi-k2.7-code", "K2.7 Code", "Kimi coding-focused build on K2.6.", 60.0, 48, 80, 24, 1000),

        // Llama
        O("llama3.2:1b", "Llama", "3.2 1B", "Tiny Llama 3.2.", 1.3, 3, 4, 2, 1),
        O("llama3.2:3b", "Llama", "3.2 3B", "Small Llama 3.2.", 2.0, 4, 6, 3, 3),
        O("llama3.1:8b", "Llama", "3.1 8B", "Widely used general 8B.", 4.9, 8, 12, 6, 8),
        O("llama3.1:70b", "Llama", "3.1 70B", "Large Llama 3.1.", 40.0, 64, 80, 40, 70),
        O("llama3.3:70b", "Llama", "3.3 70B", "Newer 70B Llama.", 43.0, 64, 80, 40, 70),

        // Gemma
        O("gemma3:1b", "Gemma", "3 1B", "Small Gemma 3.", 0.8, 3, 4, 2, 1),
        O("gemma3:4b", "Gemma", "3 4B", "Compact multimodal Gemma 3.", 3.3, 6, 8, 4, 4),
        O("gemma3:12b", "Gemma", "3 12B", "Mid-size Gemma 3.", 8.1, 16, 20, 10, 12),
        O("gemma3:27b", "Gemma", "3 27B", "Large Gemma 3.", 17.0, 32, 40, 18, 27),
        O("gemma4:e2b", "Gemma", "4 E2B", "Smallest Gemma 4.", 4.0, 8, 10, 6, 2),
        O("gemma4:12b", "Gemma", "4 12B", "Mid Gemma 4 with vision.", 8.0, 16, 20, 10, 12),
        O("gemma4:31b", "Gemma", "4 31B", "Largest common Gemma 4 dense tag.", 20.0, 32, 40, 20, 31),

        // Other open models
        O("mistral:7b", "Mistral", "7B", "Classic Mistral 7B.", 4.4, 8, 10, 6, 7),
        O("mistral-nemo", "Mistral", "Nemo 12B", "Mistral Nemo 12B.", 7.1, 14, 18, 10, 12),
        O("phi4", "Phi", "4", "Microsoft Phi-4.", 9.1, 14, 18, 10, 14),
        O("phi4-mini", "Phi", "4 Mini", "Smaller Phi-4.", 2.5, 5, 8, 4, 3.8),
        O("gpt-oss:20b", "GPT-OSS", "20B", "OpenAI open-weights 20B. Fits many 16 GB machines.", 13.0, 16, 24, 12, 20),
        O("glm4:9b", "GLM", "4 9B", "Zhipu GLM-4 9B class tag.", 5.5, 10, 14, 8, 9)
    ];

    public static readonly string[] Families =
        Catalog.Select(m => m.Family).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static LocalAiModelChoice? Find(string? id) =>
        string.IsNullOrWhiteSpace(id)
            ? null
            : Catalog.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));

    public static LocalAiModelFit Assess(LocalAiModelChoice model, LocalAiHardwareSnapshot hw)
    {
        if (hw.FreeDiskGb + 0.1 < model.DownloadGb + 2)
        {
            return new LocalAiModelFit(
                LocalAiFitKind.TooLarge,
                "Not enough disk",
                $"{model.DisplayName} needs about {model.DownloadGb:0.#} GB plus workspace. This drive has {hw.FreeDiskGb:0.#} GB free.");
        }

        if (hw.TotalRamGb + 0.1 < model.MinRamGb)
        {
            return new LocalAiModelFit(
                LocalAiFitKind.TooLarge,
                "Not enough RAM",
                $"{model.DisplayName} needs at least {model.MinRamGb:0.#} GB of system RAM. This PC has {hw.TotalRamGb:0.#} GB.");
        }

        var gpuOk = hw.GpuVramGb is >= 4;
        var vram = hw.GpuVramGb ?? 0;

        if (!gpuOk && model.ParameterBillion >= 32)
        {
            return new LocalAiModelFit(
                LocalAiFitKind.TooLarge,
                "Impractical without a GPU",
                $"{model.DisplayName} is {model.ParameterBillion:0.#}B parameters. On CPU-only hardware it will not be usable for clinic work.");
        }

        if (!gpuOk && model.ParameterBillion >= 14)
        {
            return new LocalAiModelFit(
                LocalAiFitKind.Slow,
                "Will be very slow on CPU",
                $"No dedicated GPU was found. A {model.ParameterBillion:0.#}B model will take a long time per reply. Prefer 7B or smaller, or add a GPU.");
        }

        if (gpuOk && model.MinVramGb is { } needVram && vram + 0.1 < needVram && hw.TotalRamGb < model.RecommendedRamGb)
        {
            return new LocalAiModelFit(
                LocalAiFitKind.Slow,
                "GPU is small for this model",
                $"{hw.GpuName} has {vram:0.#} GB VRAM; this model prefers about {needVram:0.#} GB. It may spill into system RAM and run slowly.");
        }

        if (hw.TotalRamGb + 0.1 < model.RecommendedRamGb)
        {
            return new LocalAiModelFit(
                LocalAiFitKind.Slow,
                "Tight on RAM — expect slowness",
                $"{model.DisplayName} is more comfortable with {model.RecommendedRamGb:0.#} GB RAM. This PC has {hw.TotalRamGb:0.#} GB, so replies may swap and stall.");
        }

        if (gpuOk && model.MinVramGb is { } recVram && vram + 0.1 >= recVram && hw.TotalRamGb + 0.1 >= model.RecommendedRamGb)
        {
            return new LocalAiModelFit(
                LocalAiFitKind.Recommended,
                "Fits this PC",
                BuildOkDetail(model, hw, gpuOk));
        }

        if (!gpuOk && model.ParameterBillion <= 8 && hw.TotalRamGb + 0.1 >= model.RecommendedRamGb)
        {
            return new LocalAiModelFit(
                LocalAiFitKind.Recommended,
                "Fits this PC (CPU)",
                BuildOkDetail(model, hw, gpuOk));
        }

        return new LocalAiModelFit(
            LocalAiFitKind.Usable,
            "Should run",
            BuildOkDetail(model, hw, gpuOk));
    }

    public static LocalAiModelChoice SuggestDefault(LocalAiHardwareSnapshot hw)
    {
        var ranked = Catalog
            .Where(m => m.Family is "Qwen" or "DeepSeek" or "Llama" or "Gemma")
            .Select(m => (Model: m, Fit: Assess(m, hw)))
            .Where(x => x.Fit.Kind is LocalAiFitKind.Recommended or LocalAiFitKind.Usable)
            .OrderBy(x => x.Fit.Kind)
            .ThenByDescending(x => x.Model.ParameterBillion)
            .ThenBy(x => x.Model.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var qwen = ranked.FirstOrDefault(x => x.Model.Id.StartsWith("qwen2.5:", StringComparison.OrdinalIgnoreCase)
                                              && x.Model.ParameterBillion is >= 3 and <= 8);
        if (qwen.Model != null)
        {
            return qwen.Model;
        }

        return ranked.FirstOrDefault().Model ?? Catalog.First(m => m.Id == "qwen2.5:3b");
    }

    private static string BuildOkDetail(LocalAiModelChoice model, LocalAiHardwareSnapshot hw, bool gpuOk)
    {
        var accel = gpuOk ? hw.GpuName ?? "GPU" : "CPU only";
        return $"~{model.DownloadGb:0.#} GB download · {model.MinRamGb:0.#}+ GB RAM · {accel}.";
    }

    private static LocalAiModelChoice Q(
        string id, string name, string desc,
        double dl, double minRam, double recRam, double minVram, double param) =>
        new(id, "Qwen", name, desc, dl, minRam, recRam, minVram, param);

    private static LocalAiModelChoice D(
        string id, string name, string desc,
        double dl, double minRam, double recRam, double minVram, double param, bool reasoning) =>
        new(id, "DeepSeek", name, desc, dl, minRam, recRam, minVram, param, reasoning);

    private static LocalAiModelChoice K(
        string id, string name, string desc,
        double dl, double minRam, double recRam, double minVram, double param) =>
        new(id, "Kimi", name, desc, dl, minRam, recRam, minVram, param);

    private static LocalAiModelChoice O(
        string id, string family, string name, string desc,
        double dl, double minRam, double recRam, double minVram, double param) =>
        new(id, family, name, desc, dl, minRam, recRam, minVram, param);
}
