using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Zebrahoof_EMR.Logging;

/// <summary>
/// Structured API access log (one line per request) with operation name, timing, and correlation.
/// Serilog request completion for <c>/api</c> is suppressed to avoid duplicate lines; this middleware still uses <see cref="ILogger"/> which routes to Serilog.
/// </summary>
public sealed class ApiRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public ApiRequestLoggingMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var sampleRate = _configuration.GetValue("Serilog:RequestLogSamplingRate", 1.0);
            var slowMs = _configuration.GetValue("Serilog:SlowRequestWarningMs", 2000);
            var status = context.Response.StatusCode;
            var skipForSampling = status < 400 && sw.ElapsedMilliseconds < 500 && sampleRate < 1.0 &&
                                  Random.Shared.NextDouble() > sampleRate;
            if (!skipForSampling)
            {
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Zebrahoof_EMR.Http.Api");
                var op = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value;
                context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var cid);
                var correlationId = cid as string;

                var level = status >= 500
                    ? LogLevel.Error
                    : status >= 400
                        ? LogLevel.Warning
                        : sw.ElapsedMilliseconds >= slowMs
                            ? LogLevel.Warning
                            : LogLevel.Information;

                logger.Log(
                    level,
                    "Api {Method} {Path} -> {StatusCode} in {ElapsedMs}ms Operation={Operation} CorrelationId={CorrelationId} RequestId={RequestId}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    status,
                    sw.ElapsedMilliseconds,
                    op,
                    correlationId,
                    context.TraceIdentifier);
            }
        }
    }
}
