namespace Zebrahoof_EMR.Services;

/// <summary>One id per Blazor circuit. Scoped services are not shared across tabs or users.</summary>
public sealed class CircuitScope
{
    public string Id { get; } = Guid.NewGuid().ToString("N");

    public string? UserId { get; set; }
}
