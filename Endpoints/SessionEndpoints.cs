using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        SessionService sessionService)
    {
        var sessions = await sessionService.GetActiveSessionsAsync();
        return Results.Ok(new { sessions });
    }

    private static async Task<IResult> GetActiveSessionCount(
        HttpContext context,
        SessionService sessionService)
    {
        var count = await sessionService.GetActiveSessionCountAsync();
        return Results.Ok(new { count });
    }
}
