using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Options;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Drops this circuit's chart lock when the browser disconnects, after a short grace period
/// so a refresh can reclaim it. A closed circuit releases immediately.
/// </summary>
public sealed class ChartCircuitHandler : CircuitHandler
{
    private readonly CircuitScope _scope;
    private readonly ActivePatientRegistry _registry;
    private readonly AuthenticationStateProvider _auth;
    private readonly TimeSpan _grace;
    private int _connectionEpoch;
    private bool _releasedAfterGrace;
    private (int PatientId, string PatientName)? _remembered;

    public ChartCircuitHandler(
        CircuitScope scope,
        ActivePatientRegistry registry,
        AuthenticationStateProvider auth,
        IOptions<ChartLockOptions> options)
    {
        _scope = scope;
        _registry = registry;
        _auth = auth;
        var seconds = options.Value.DisconnectGraceSeconds;
        if (seconds <= 0)
        {
            seconds = 8;
        }

        _grace = TimeSpan.FromSeconds(seconds);
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var userId = await ChartUser.ResolveAsync(_auth);
        if (!string.IsNullOrEmpty(userId))
        {
            _scope.UserId = userId;
        }
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            if (string.IsNullOrEmpty(_scope.UserId))
            {
                _scope.UserId = await ChartUser.ResolveAsync(_auth);
            }

            using (CircuitCall.Push(_scope.UserId, _scope.Id))
            {
                await next(context);
            }
        };
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_scope.UserId))
        {
            return Task.CompletedTask;
        }

        _remembered = _registry.HeldBy(_scope.UserId, _scope.Id);
        _registry.MarkDisconnected(_scope.UserId, _scope.Id);
        var epoch = ++_connectionEpoch;
        var userId = _scope.UserId;
        var circuitId = _scope.Id;
        _ = ReleaseAfterGraceAsync(epoch, userId, circuitId);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _connectionEpoch++;
        if (string.IsNullOrEmpty(_scope.UserId))
        {
            return Task.CompletedTask;
        }

        if (!_releasedAfterGrace)
        {
            _registry.MarkConnected(_scope.UserId, _scope.Id);
            return Task.CompletedTask;
        }

        _releasedAfterGrace = false;
        if (_remembered is not { } held)
        {
            return Task.CompletedTask;
        }

        var decision = _registry.Switch(_scope.UserId, _scope.Id, held.PatientId, held.PatientName);
        if (decision.Status == ChartOpenStatus.Blocked)
        {
            _registry.NotifyClosed(new ChartClosedNotice(
                _scope.UserId,
                _scope.Id,
                held.PatientId,
                held.PatientName,
                decision.OpenPatientId ?? held.PatientId,
                decision.OpenPatientName ?? "another patient"));
        }

        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _connectionEpoch++;
        if (!string.IsNullOrEmpty(_scope.UserId))
        {
            _registry.Release(_scope.UserId, _scope.Id);
        }

        return Task.CompletedTask;
    }

    private async Task ReleaseAfterGraceAsync(int epoch, string userId, string circuitId)
    {
        try
        {
            await Task.Delay(_grace);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (epoch != _connectionEpoch)
        {
            return;
        }

        _registry.Release(userId, circuitId);
        _releasedAfterGrace = true;
    }
}
