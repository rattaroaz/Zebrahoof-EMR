using System.Net.Http.Json;
using System.Text.Json;
using Zebrahoof_EMR.Logging;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Talks to the local Ollama engine on loopback. Replaces the former cloud Grok client.
/// </summary>
public sealed class LocalAiService : IClinicalAiService
{
    private readonly HttpClient _httpClient;
    private readonly LocalAiEngineService _engine;
    private readonly ILogger<LocalAiService> _logger;

    public LocalAiService(
        HttpClient httpClient,
        LocalAiEngineService engine,
        ILogger<LocalAiService> logger)
    {
        _httpClient = httpClient;
        _engine = engine;
        _logger = logger;
    }

    public async Task<string> ChatAsync(
        string systemPrompt,
        IEnumerable<ChatTurn> history,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var readyError = await EnsureReadyAsync(cancellationToken);
        if (readyError != null)
        {
            return readyError;
        }

        var messages = LocalAiProtocol.BuildMessages(systemPrompt, history, userMessage);
        return await SendChatAsync(messages, cancellationToken);
    }

    public async Task<string> ProcessDocumentAsync(
        string documentContent,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var readyError = await EnsureReadyAsync(cancellationToken);
        if (readyError != null)
        {
            return readyError;
        }

        var user = string.IsNullOrWhiteSpace(documentContent)
            ? prompt
            : $"{prompt}\n\nDocument Content:\n{documentContent}";

        var messages = LocalAiProtocol.BuildMessages(
            "You are a helpful medical assistant that analyzes clinical documents. Keep PHI on this machine; do not suggest sending data elsewhere.",
            Array.Empty<ChatTurn>(),
            user);

        return await SendChatAsync(messages, cancellationToken);
    }

    private async Task<string?> EnsureReadyAsync(CancellationToken cancellationToken)
    {
        var snapshot = _engine.GetSnapshot();
        if (!snapshot.CanChat)
        {
            snapshot = await _engine.RefreshStatusAsync(cancellationToken);
        }

        if (snapshot.CanChat)
        {
            return null;
        }

        if (snapshot.EngineInstalled && !snapshot.EngineRunning)
        {
            try
            {
                await _engine.StartEngineAsync(cancellationToken);
                snapshot = await _engine.RefreshStatusAsync(cancellationToken);
                if (snapshot.CanChat)
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto-start of local AI before a request failed");
            }
        }

        if (snapshot.CanChat)
        {
            return null;
        }

        if (!snapshot.EngineInstalled)
        {
            return "Error: Local AI is not installed. Open Settings → Local AI and install Qwen. Chart data stays on this machine.";
        }

        if (!snapshot.EngineRunning)
        {
            return "Error: Local AI engine is installed but not running. Open Settings → Local AI and start the engine.";
        }

        return $"Error: The local model '{snapshot.Model}' is not downloaded. Open Settings → Local AI and download Qwen.";
    }

    private async Task<string> SendChatAsync(List<LocalAiChatMessage> messages, CancellationToken cancellationToken)
    {
        var model = _engine.EffectiveModel;
        var request = new LocalAiChatRequest
        {
            Model = model,
            Messages = messages,
            Stream = false
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/chat",
                request,
                LocalAiProtocol.JsonOptions,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var apiError = LocalAiProtocol.ExtractError(body);
                _logger.LogError(
                    "Local AI error {StatusCode} — {BodySummary}",
                    response.StatusCode,
                    SafeLogContent.DescribeWithoutRawContent(body));
                return string.IsNullOrEmpty(apiError)
                    ? $"Error from local AI: {response.StatusCode}. Check Settings → Local AI and the application logs."
                    : $"Error from local AI: {apiError}";
            }

            var text = LocalAiProtocol.ExtractAssistantText(body);
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            _logger.LogWarning(
                "Local AI response had no assistant text. {BodySummary}",
                SafeLogContent.DescribeWithoutRawContent(body));
            return "No content returned from the local AI engine.";
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Local AI request timed out for model {Model}", model);
            return "Error: The local AI request timed out. Try a smaller Qwen model (3B) or wait until the engine finishes loading.";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Local AI HTTP failure");
            return "Error: Could not reach the local AI engine. Open Settings → Local AI and start it.";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Local AI returned invalid JSON");
            return "Error: The local AI engine returned an unreadable response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while calling local AI");
            return $"Error connecting to the local AI engine: {ex.Message}";
        }
    }
}
