namespace Zebrahoof_EMR.Services;

public sealed class LocalAiOptions
{
    public const string SectionName = "LocalAi";
    public const string DefaultBaseUrl = "http://127.0.0.1:11434";
    public const string DefaultModel = "qwen2.5:7b";

    /// <summary>Local OpenAI-compatible / Ollama host. Must be loopback.</summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>Ollama model tag to pull and use (e.g. qwen2.5:7b).</summary>
    public string Model { get; set; } = DefaultModel;

    /// <summary>If the engine is already installed, start it when the app boots.</summary>
    public bool AutoStart { get; set; } = true;

    public int RequestTimeoutMinutes { get; set; } = 15;

    /// <summary>Optional override for the extracted engine directory.</summary>
    public string? EngineDirectory { get; set; }
}
