using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Zebrahoof_EMR.Logging;

/// <summary>
/// Accepts <see cref="HeaderName"/> from the client or generates a new id, exposes it to Serilog
/// and downstream code via <see cref="ItemKey"/>, and echoes the id in the response.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string ItemKey = "Zebrahoof.CorrelationId";
    public const string HeaderName = "X-Correlation-Id";
    public const string LogPropertyName = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var id = GetOrCreateId(context);
        context.Items[ItemKey] = id;
        if (!context.Response.HasStarted)
        {
            context.Response.Headers[HeaderName] = id;
        }

        using (LogContext.PushProperty(LogPropertyName, id))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var fromHeader))
        {
            var v = fromHeader.ToString();
            if (!string.IsNullOrWhiteSpace(v))
            {
                return v.Length > 128 ? v[..128] : v;
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}
