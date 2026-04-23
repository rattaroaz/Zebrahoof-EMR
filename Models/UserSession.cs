using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zebrahoof_EMR.Models;

public class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? DeviceName { get; set; }
    public string? IpAddress { get; set; }
    public bool IsRevoked { get; set; }
    public int IdleTimeoutMinutes { get; set; } = 15;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }
}
