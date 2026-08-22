using Microsoft.AspNetCore.Mvc;
using Zebrahoof_EMR.Services;

namespace Zebrahoof_EMR.Endpoints;

public static class LocalAiEndpoints
{
    public static void MapLocalAiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/local-ai")
            .RequireAuthorization()
            .WithTags("LocalAi");

        group.MapGet("/status", GetStatus)
            .WithName("GetLocalAiStatus")
            .WithSummary("Local AI engine status");

        group.MapPost("/install", Install)
            .WithName("InstallLocalAi")
            .WithSummary("Download and install the local AI engine and Qwen model");

        group.MapPost("/start", Start)
            .WithName("StartLocalAi")
            .WithSummary("Start the installed local AI engine");

        group.MapPost("/pull", Pull)
            .WithName("PullLocalAiModel")
            .WithSummary("Download a local model onto this machine");

        group.MapPost("/cancel", Cancel)
            .WithName("CancelLocalAi")
            .WithSummary("Stop an in-flight engine or model download");

        group.MapGet("/hardware", GetHardware)
            .WithName("GetLocalAiHardware")
            .WithSummary("This machine's RAM, GPU, and disk for model-fit warnings");

        group.MapGet("/models", GetModels)
            .WithName("GetLocalAiModels")
            .WithSummary("Catalog of local models with fit warnings for this PC");
    }

    private static IResult GetStatus([FromServices] LocalAiEngineService engine)
    {
        return Results.Ok(engine.GetSnapshot());
    }

    private static async Task<IResult> Install(
        [FromServices] LocalAiEngineService engine,
        [FromBody] LocalAiActionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            await engine.InstallAndPrepareAsync(request?.Model, cancellationToken);
            return Results.Ok(engine.GetSnapshot());
        }
        catch (OperationCanceledException)
        {
            return Results.Ok(engine.GetSnapshot());
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message, status = engine.GetSnapshot() });
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> Start(
        [FromServices] LocalAiEngineService engine,
        CancellationToken cancellationToken)
    {
        try
        {
            await engine.StartEngineAsync(cancellationToken);
            return Results.Ok(engine.GetSnapshot());
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message, status = engine.GetSnapshot() });
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> Pull(
        [FromServices] LocalAiEngineService engine,
        [FromBody] LocalAiActionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            await engine.PullModelAsync(request?.Model, cancellationToken);
            return Results.Ok(engine.GetSnapshot());
        }
        catch (OperationCanceledException)
        {
            return Results.Ok(engine.GetSnapshot());
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message, status = engine.GetSnapshot() });
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static IResult Cancel([FromServices] LocalAiEngineService engine)
    {
        var cancelled = engine.CancelCurrent();
        return Results.Ok(new { cancelled, status = engine.GetSnapshot() });
    }

    private static IResult GetHardware([FromServices] LocalAiEngineService engine)
    {
        return Results.Ok(engine.ProbeHardware());
    }

    private static IResult GetModels([FromServices] LocalAiEngineService engine)
    {
        var hw = engine.ProbeHardware();
        var models = LocalAiModels.Catalog.Select(m =>
        {
            var fit = LocalAiModels.Assess(m, hw);
            return new
            {
                m.Id,
                m.Family,
                m.DisplayName,
                m.Description,
                m.DownloadGb,
                m.MinRamGb,
                m.RecommendedRamGb,
                m.MinVramGb,
                m.ParameterBillion,
                m.Reasoning,
                fit = fit.Kind.ToString(),
                fitTitle = fit.Title,
                fitDetail = fit.Detail
            };
        });
        return Results.Ok(new { hardware = hw, suggested = LocalAiModels.SuggestDefault(hw).Id, models });
    }
}

public sealed class LocalAiActionRequest
{
    public string? Model { get; set; }
}
