using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zebrahoof_EMR.Logging;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// One round-trip in a chat conversation: what the user said and what the
/// assistant answered. Used by <see cref="GrokApiService.ChatAsync"/> to
/// rebuild the message array sent to the Grok API.
/// </summary>
public record ChatTurn(string UserInput, string AssistantResponse);

public class GrokApiService
{
    /// <summary>
    /// Default: Grok 4.20 multi-agent (Responses API only; not Chat Completions).
    /// Override with <c>GROK_MODEL</c> or <c>Grok:Model</c>.
    /// </summary>
    public const string DefaultMultiAgentModelId = "grok-4.20-multi-agent";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<GrokApiService> _logger;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly bool _store;
    private readonly bool _isMultiAgent;
    private readonly string? _reasoningEffort;
    private readonly IReadOnlyList<string> _serverTools;

    public GrokApiService(HttpClient httpClient, ILogger<GrokApiService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = DotNetEnv.Env.GetString("GROK_API_KEY") ?? Environment.GetEnvironmentVariable("GROK_API_KEY");
        _model = Environment.GetEnvironmentVariable("GROK_MODEL")
            ?? configuration["Grok:Model"]
            ?? DefaultMultiAgentModelId;

        _store = ParseBool(
            Environment.GetEnvironmentVariable("GROK_STORE"),
            configuration.GetValue<bool?>("Grok:Store")) ?? false;

        _isMultiAgent = _model.Contains("multi-agent", StringComparison.OrdinalIgnoreCase);

        _reasoningEffort = Environment.GetEnvironmentVariable("GROK_REASONING_EFFORT")
            ?? configuration["Grok:ReasoningEffort"];

        var toolsFromConfig = configuration.GetSection("Grok:ServerTools").Get<string[]>() ?? Array.Empty<string>();
        var toolsEnv = Environment.GetEnvironmentVariable("GROK_SERVER_TOOLS");
        _serverTools = string.IsNullOrWhiteSpace(toolsEnv)
            ? toolsFromConfig
            : toolsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("GROK_API_KEY is not set in environment variables.");
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        _httpClient.BaseAddress = new Uri("https://api.x.ai/v1/");
    }

    /// <summary>
    /// Send a multi-turn conversation to Grok via the xAI Responses API.
    /// </summary>
    public async Task<string> ChatAsync(
        string systemPrompt,
        IEnumerable<ChatTurn> history,
        string userMessage)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "Error: Grok API key is not configured. Please add it to your .env file.";
        }

        var input = new List<GrokInputMessage>
        {
            new() { Role = "system", Content = systemPrompt }
        };

        foreach (var turn in history)
        {
            if (!string.IsNullOrWhiteSpace(turn.UserInput))
            {
                input.Add(new GrokInputMessage { Role = "user", Content = turn.UserInput });
            }

            if (!string.IsNullOrWhiteSpace(turn.AssistantResponse))
            {
                input.Add(new GrokInputMessage { Role = "assistant", Content = turn.AssistantResponse });
            }
        }

        input.Add(new GrokInputMessage { Role = "user", Content = userMessage });

        return await SendResponsesAsync(input);
    }

    public async Task<string> ProcessDocumentAsync(string documentContent, string prompt)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "Error: Grok API key is not configured. Please add it to your .env file.";
        }

        var input = new List<GrokInputMessage>
        {
            new()
            {
                Role = "system",
                Content = "You are a helpful medical assistant that analyzes clinical documents."
            },
            new()
            {
                Role = "user",
                Content = $"{prompt}\n\nDocument Content:\n{documentContent}"
            }
        };

        return await SendResponsesAsync(input);
    }

    private async Task<string> SendResponsesAsync(List<GrokInputMessage> input)
    {
        try
        {
            var request = new GrokResponsesRequest
            {
                Model = _model,
                Input = input,
                Store = _store
            };

            if (_isMultiAgent && IsValidReasoningEffort(_reasoningEffort))
            {
                request.Reasoning = new GrokReasoning { Effort = _reasoningEffort! };
            }

            if (_isMultiAgent && _serverTools.Count > 0)
            {
                request.Tools = _serverTools
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => new GrokTool { Type = t.Trim() })
                    .ToList();
            }

            var json = JsonSerializer.Serialize(request, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("responses", content);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Grok API Error: {StatusCode} - {ErrorContentPrefix}", response.StatusCode,
                    SafeLogContent.Truncate(responseBody, SafeLogContent.DefaultMaxLength));
                return $"Error from Grok API: {response.StatusCode}. Please check logs for details.";
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            var text = ExtractResponsesOutputText(root);
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            _logger.LogWarning("Grok response had no assistant text. Body prefix: {Prefix}",
                SafeLogContent.Truncate(responseBody, SafeLogContent.ShortMaxLength));
            return "No content returned from Grok.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while calling Grok API");
            return $"Error connecting to Grok API: {ex.Message}";
        }
    }

    private static bool? ParseBool(string? env, bool? config)
    {
        if (!string.IsNullOrWhiteSpace(env))
        {
            if (bool.TryParse(env, out var b))
            {
                return b;
            }

            if (string.Equals(env, "1", StringComparison.Ordinal) || string.Equals(env, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(env, "0", StringComparison.Ordinal) || string.Equals(env, "no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return config;
    }

    private static bool IsValidReasoningEffort(string? effort) =>
        effort is "low" or "medium" or "high" or "xhigh";

    /// <summary>
    /// Parses the xAI / OpenAI Responses API payload: <c>output[]</c> items with
    /// <c>type: message</c> and <c>content[]</c> parts of <c>type: output_text</c>.
    /// </summary>
    private static string? ExtractResponsesOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "message")
            {
                continue;
            }

            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var pt) && pt.GetString() == "output_text"
                    && part.TryGetProperty("text", out var textEl))
                {
                    var t = textEl.GetString();
                    if (!string.IsNullOrEmpty(t))
                    {
                        sb.Append(t);
                    }
                }
            }
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    private sealed class GrokResponsesRequest
    {
        public string Model { get; set; } = "";
        public List<GrokInputMessage> Input { get; set; } = new();
        public bool Store { get; set; }
        public GrokReasoning? Reasoning { get; set; }
        public List<GrokTool>? Tools { get; set; }
    }

    private sealed class GrokInputMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private sealed class GrokReasoning
    {
        public string Effort { get; set; } = "low";
    }

    private sealed class GrokTool
    {
        public string Type { get; set; } = "";
    }
}
