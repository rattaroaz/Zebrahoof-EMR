namespace Zebrahoof_EMR.Services;

/// <summary>
/// One round-trip in a chat conversation: what the user said and what the
/// assistant answered. Used by <see cref="IClinicalAiService.ChatAsync"/> to
/// rebuild the message array sent to the local AI engine.
/// </summary>
public record ChatTurn(string UserInput, string AssistantResponse);

/// <summary>
/// Clinical AI used by encounter chat, document analysis, and record updates.
/// Implementations must keep inference on this machine — no cloud providers.
/// </summary>
public interface IClinicalAiService
{
    Task<string> ChatAsync(
        string systemPrompt,
        IEnumerable<ChatTurn> history,
        string userMessage,
        CancellationToken cancellationToken = default);

    Task<string> ProcessDocumentAsync(
        string documentContent,
        string prompt,
        CancellationToken cancellationToken = default);
}
