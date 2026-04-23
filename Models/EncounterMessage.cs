using System;
using System.ComponentModel.DataAnnotations;

namespace Zebrahoof_EMR.Models;

/// <summary>
/// A single turn in the patient Encounter tab's conversation with Grok.
/// Persisted so that the discussion survives page reloads and can optionally
/// be replayed back to Grok as conversation history on follow-up turns.
/// </summary>
public class EncounterMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int PatientId { get; set; }

    [MaxLength(256)]
    public string UserId { get; set; } = string.Empty;

    public string UserInput { get; set; } = string.Empty;

    public string GrokResponse { get; set; } = string.Empty;

    /// <summary>
    /// True if the user asked Grok to review prior conversation when this
    /// turn was sent. Useful both for auditing and for visually badging the
    /// turn in the UI.
    /// </summary>
    public bool IncludedHistory { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
