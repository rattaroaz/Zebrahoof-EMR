using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Zebrahoof_EMR.Services;

/// <summary>Drops this circuit's chart lock when the tab or browser disconnects.</summary>
public sealed class ChartCircuitHandler : CircuitHandler
{
    private readonly CircuitScope _scope;
    private readonly ActivePatientRegistry _registry;
    private readonly AuthenticationStateProvider _auth;

    public ChartCircuitHandler(
        CircuitScope scope,
        ActivePatientRegistry registry,
        AuthenticationStateProvider auth)
    {
        _scope = scope;
        _registry = registry;
        _auth = auth;
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var userId = await ChartUser.ResolveAsync(_auth);
        if (!string.IsNullOrEmpty(userId))
        {
            _scope.UserId = userId;
        }
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_scope.UserId))
        {
            _registry.Release(_scope.UserId, _scope.Id);
        }

        return Task.CompletedTask;
    }
}
