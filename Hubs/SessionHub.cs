using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Zebrahoof_EMR.Services;

namespace Zebrahoof_EMR.Hubs;

[Authorize]
public class SessionHub : Hub
{
    private readonly SessionService _sessionService;

    public SessionHub(SessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public override async Task OnConnectedAsync()
    {
        var sessionId = GetSessionId();
        if (sessionId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var sessionId = GetSessionId();
        if (sessionId != Guid.Empty)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.ToString());
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task Heartbeat()
    {
        var sessionId = GetSessionId();
        if (sessionId == Guid.Empty || await _sessionService.IsIdleExpiredAsync(sessionId))
        {
            await Clients.Caller.SendAsync("ForceLogout", "Session expired");
            if (sessionId != Guid.Empty)
            {
                await _sessionService.RevokeSessionAsync(sessionId, "idle_timeout");
            }
            Context.Abort();
            return;
        }

        await _sessionService.UpdateLastSeenAsync(sessionId);
        var (idleRemaining, absoluteRemaining) = await _sessionService.GetRemainingTimesAsync(sessionId);

        await Clients.Caller.SendAsync("IdleTimeUpdated",
            idleRemaining.TotalSeconds,
            absoluteRemaining.TotalSeconds);

        if (idleRemaining <= TimeSpan.FromMinutes(2))
        {
            await Clients.Caller.SendAsync("IdleWarning", "Session will expire soon due to inactivity.");
        }
    }

    private Guid GetSessionId()
    {
        var query = Context.GetHttpContext()?.Request.Query;
        if (query == null || !query.TryGetValue("sessionId", out var values))
        {
            return Guid.Empty;
        }

        return Guid.TryParse(values, out var sessionId) ? sessionId : Guid.Empty;
    }
}
