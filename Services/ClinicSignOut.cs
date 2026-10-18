using System.Security.Claims;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Forgets one user's open chart and cached charts. Every sign-out path calls this
/// while the user id is still known.
/// </summary>
public sealed class ClinicSignOut
{
    private readonly PatientChartCache _charts;
    private readonly ActivePatientRegistry _registry;
    private readonly NoteDraftService _drafts;

    public ClinicSignOut(PatientChartCache charts, ActivePatientRegistry registry, NoteDraftService? drafts = null)
    {
        _charts = charts;
        _registry = registry;
        _drafts = drafts ?? new NoteDraftService();
    }

    public void ForgetCharts(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return;
        }

        ForgetCharts(ChartUser.Resolve(user));
        _drafts.ClearUser(user.Identity?.Name);
        _drafts.ClearUser(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    public void ForgetCharts(string? userId)
    {
        _charts.ClearUser(userId);
        _registry.ReleaseUser(userId);
        _drafts.ClearUser(userId);
    }
}
