using System.Text.Json;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Request/response helpers for the Ollama native chat API. Kept static so
/// parsing can be unit-tested without a live engine.
/// </summary>
public static class LocalAiProtocol
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static List<LocalAiChatMessage> BuildMessages(
        string systemPrompt,
        IEnumerable<ChatTurn> history,
        string userMessage)
    {
        var messages = new List<LocalAiChatMessage>
        {
            new() { Role = "system", Content = systemPrompt }
        };

        foreach (var turn in history)
        {
            if (!string.IsNullOrWhiteSpace(turn.UserInput))
            {
                messages.Add(new LocalAiChatMessage { Role = "user", Content = turn.UserInput });
            }

            if (!string.IsNullOrWhiteSpace(turn.AssistantResponse))
            {
                messages.Add(new LocalAiChatMessage { Role = "assistant", Content = turn.AssistantResponse });
            }
        }

        messages.Add(new LocalAiChatMessage { Role = "user", Content = userMessage });
        return messages;
    }

    public static string? ExtractAssistantText(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        // Ollama native: { "message": { "content": "..." } }
        if (root.TryGetProperty("message", out var message) &&
            message.ValueKind == JsonValueKind.Object &&
            message.TryGetProperty("content", out var nativeContent))
        {
            var text = nativeContent.GetString();
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
        }

        // OpenAI-compatible fallback: { "choices": [ { "message": { "content": "..." } } ] }
        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
        {
            foreach (var choice in choices.EnumerateArray())
            {
                if (choice.TryGetProperty("message", out var choiceMessage) &&
                    choiceMessage.TryGetProperty("content", out var contentEl))
                {
                    var text = contentEl.GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
                }

                if (choice.TryGetProperty("text", out var textEl))
                {
                    var text = textEl.GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
                }
            }
        }

        if (root.TryGetProperty("response", out var responseEl))
        {
            var text = responseEl.GetString();
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
        }

        return null;
    }

    public static string? ExtractError(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var errorEl))
            {
                return errorEl.ValueKind == JsonValueKind.String
                    ? errorEl.GetString()
                    : errorEl.ToString();
            }
        }
        catch (JsonException)
        {
            // Not JSON — ignore.
        }

        return null;
    }
}

public sealed class LocalAiChatRequest
{
    public string Model { get; set; } = string.Empty;
    public List<LocalAiChatMessage> Messages { get; set; } = new();
    public bool Stream { get; set; }
}

public sealed class LocalAiChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
