using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Zebrahoof_EMR.Hubs;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Forgets one user's open chart and cached charts. Drafts are cleared only on
/// an explicit sign-out, and only for the browser that signed out. Idle timeout
/// and cookie expiry keep them. Other signed-in devices stay signed in.
/// </summary>
public sealed class ClinicSignOut
{
    private readonly PatientChartCache _charts;
    private readonly ActivePatientRegistry _registry;
    private readonly NoteDraftService _drafts;
    private readonly IHubContext<SessionHub>? _hub;

    public ClinicSignOut(
        PatientChartCache charts,
        ActivePatientRegistry registry,
        NoteDraftService? drafts = null,
        IHubContext<SessionHub>? hub = null)
    {
        _charts = charts;
        _registry = registry;
        _drafts = drafts ?? new NoteDraftService();
        _hub = hub;
    }

    public void ForgetCharts(ClaimsPrincipal? user) => ReleaseCharts(user);

    public void BeginExplicitSignOut(ClaimsPrincipal? user, string? browserId = null)
    {
        ReleaseCharts(user);
        _drafts.Revoke(browserId);
    }

    public Task BroadcastSessionAsync(Guid sessionId)
    {
        if (_hub == null || sessionId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return _hub.Clients.Group(sessionId.ToString()).SendAsync("SignedOut");
    }

    private void ReleaseCharts(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var chartKey = ChartUser.Resolve(user);
        _charts.ClearUser(chartKey);
        _registry.ReleaseUser(chartKey);
    }
}
