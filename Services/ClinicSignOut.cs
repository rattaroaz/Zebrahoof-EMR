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

    public ClinicSignOut(PatientChartCache charts, ActivePatientRegistry registry)
    {
        _charts = charts;
        _registry = registry;
    }

    public void ForgetCharts(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return;
        }

        ForgetCharts(ChartUser.Resolve(user));
    }

    public void ForgetCharts(string? userId)
    {
        _charts.ClearUser(userId);
        _registry.ReleaseUser(userId);
    }
}
