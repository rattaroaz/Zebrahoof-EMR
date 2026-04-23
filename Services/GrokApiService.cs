using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// One round-trip in a chat conversation: what the user said and what the
/// assistant answered. Used by <see cref="GrokApiService.ChatAsync"/> to
/// rebuild the message array sent to the Grok API.
/// </summary>
public record ChatTurn(string UserInput, string AssistantResponse);

public class GrokApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GrokApiService> _logger;
    private readonly string? _apiKey;

    public GrokApiService(HttpClient httpClient, ILogger<GrokApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = DotNetEnv.Env.GetString("GROK_API_KEY") ?? Environment.GetEnvironmentVariable("GROK_API_KEY");

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
    /// Send a multi-turn conversation to Grok. The first message is treated as
    /// the system prompt; everything after is alternating user / assistant
    /// content as supplied by the caller.
    /// </summary>
    /// <param name="systemPrompt">Instruction message Grok should follow.</param>
    /// <param name="history">Prior turns to replay as context. May be empty.</param>
    /// <param name="userMessage">The new user message to ask Grok about.</param>
    public async Task<string> ChatAsync(
        string systemPrompt,
        IEnumerable<ChatTurn> history,
        string userMessage)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "Error: Grok API key is not configured. Please add it to your .env file.";
        }

        try
        {
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            foreach (var turn in history)
            {
                if (!string.IsNullOrWhiteSpace(turn.UserInput))
                {
                    messages.Add(new { role = "user", content = turn.UserInput });
                }
                if (!string.IsNullOrWhiteSpace(turn.AssistantResponse))
                {
                    messages.Add(new { role = "assistant", content = turn.AssistantResponse });
                }
            }

            messages.Add(new { role = "user", content = userMessage });

            var requestBody = new
            {
                model = "grok-4.20-reasoning",
                messages = messages.ToArray()
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Grok API Error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                return $"Error from Grok API: {response.StatusCode}. Please check logs for details.";
            }

            var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();

            var result = responseData
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return result ?? "No content returned from Grok.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while calling Grok API");
            return $"Error connecting to Grok API: {ex.Message}";
        }
    }

    public async Task<string> ProcessDocumentAsync(string documentContent, string prompt)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "Error: Grok API key is not configured. Please add it to your .env file.";
        }

        try
        {
            var requestBody = new
            {
                model = "grok-4.20-reasoning",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful medical assistant that analyzes clinical documents." },
                    new { role = "user", content = $"{prompt}\n\nDocument Content:\n{documentContent}" }
                }
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Grok API Error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                return $"Error from Grok API: {response.StatusCode}. Please check logs for details.";
            }

            var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            var result = responseData
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return result ?? "No content returned from Grok.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while calling Grok API");
            return $"Error connecting to Grok API: {ex.Message}";
        }
    }
}
