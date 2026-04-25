using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Zebrahoof_EMR.Telemetry;

public static class AppOpenTelemetryExtensions
{
    public static IServiceCollection AddAppOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var enabled = configuration.GetValue("OpenTelemetry:Enabled", false);
        if (!enabled)
        {
            return services;
        }

        var endpoint = FirstNonEmpty(
            configuration["OpenTelemetry:OtlpEndpoint"],
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"),
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return services;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            return services;
        }

        var serviceName = FirstNonEmpty(configuration["OpenTelemetry:ServiceName"], "Zebrahoof.EMR")!;
        var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
                      ?? "unknown";

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName: serviceName, serviceVersion: version, serviceInstanceId: Environment.MachineName))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation(o =>
                {
                    o.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/_framework")
                        && !ctx.Request.Path.StartsWithSegments("/_blazor")
                        && !ctx.Request.Path.StartsWithSegments("/_content");
                    o.RecordException = true;
                })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = endpointUri));

        return services;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
