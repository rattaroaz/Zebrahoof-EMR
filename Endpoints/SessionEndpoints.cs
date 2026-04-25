using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zebrahoof_EMR.Services;

namespace Zebrahoof_EMR.Endpoints;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/sessions");

        // Allow anonymous access for testing - these return aggregate data only
        group.MapGet("/active", GetActiveSessions).AllowAnonymous();
        group.MapGet("/count", GetActiveSessionCount).AllowAnonymous();
    }

    private static async Task<IResult> GetActiveSessions(
        HttpContext context,
        SessionService sessionService,
        ILoggerFactory loggerFactory)
    {
        var log = loggerFactory.CreateLogger("Zebrahoof_EMR.Api.Sessions");
        var sessions = await sessionService.GetActiveSessionsAsync();
        log.LogInformation(
            "Anonymous active-sessions query; count {Count} authenticated {Authenticated}",
            sessions.Count,
            context.User?.Identity?.IsAuthenticated == true);
        return Results.Ok(new { sessions });
    }

    private static async Task<IResult> GetActiveSessionCount(
        HttpContext context,
        SessionService sessionService,
        ILoggerFactory loggerFactory)
    {
        var log = loggerFactory.CreateLogger("Zebrahoof_EMR.Api.Sessions");
        var count = await sessionService.GetActiveSessionCountAsync();
        log.LogInformation(
            "Anonymous session-count query; count {Count} authenticated {Authenticated}",
            count,
            context.User?.Identity?.IsAuthenticated == true);
        return Results.Ok(new { count });
    }
}
