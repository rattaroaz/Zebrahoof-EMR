using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public static class InboxOrdering
{
    public static bool IsUnacknowledgedCritical(InboxMessage message) =>
        message.IsUrgent && !message.IsAcknowledged;

    public static List<InboxMessage> Sort(IEnumerable<InboxMessage> messages) =>
        messages
            .OrderByDescending(IsUnacknowledgedCritical)
            .ThenByDescending(m => m.SentAt)
            .ToList();
}
