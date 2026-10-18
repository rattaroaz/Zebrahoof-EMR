using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Zebrahoof_EMR.Hubs;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Forgets one user's open chart and cached charts. Drafts are cleared only on
/// an explicit sign-out. Idle timeout and cookie expiry keep them.
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

    public void ForgetCharts(ClaimsPrincipal? user) => Release(user, clearDrafts: false);

    public void BeginExplicitSignOut(ClaimsPrincipal? user) => Release(user, clearDrafts: true);

    public Task BroadcastAsync(ClaimsPrincipal? user) => BroadcastCoreAsync(user);

    private void Release(ClaimsPrincipal? user, bool clearDrafts)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var chartKey = ChartUser.Resolve(user);
        _charts.ClearUser(chartKey);
        _registry.ReleaseUser(chartKey);
        if (!clearDrafts)
        {
            return;
        }

        _drafts.SignOut(user.Identity?.Name);
        _drafts.SignOut(chartKey);
        _drafts.SignOut(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    private async Task BroadcastCoreAsync(ClaimsPrincipal? user)
    {
        if (_hub == null || user == null)
        {
            return;
        }

        var names = new[]
        {
            user.Identity?.Name,
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value
        };

        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            await _hub.Clients.Group(SignOutChannel.Group(name)).SendAsync("SignedOut");
        }
    }
}
