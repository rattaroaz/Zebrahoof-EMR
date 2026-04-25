using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.OpenTelemetry;

namespace Zebrahoof_EMR.Logging;

/// <summary>
/// Central Serilog setup: configuration binding plus enrichers that are awkward to express in JSON.
/// </summary>
public static class SerilogHostConfiguration
{
    public static void Configure(HostBuilderContext context, LoggerConfiguration loggerConfiguration)
    {
        loggerConfiguration.ReadFrom.Configuration(context.Configuration);

        var asm = Assembly.GetEntryAssembly() ?? typeof(SerilogHostConfiguration).Assembly;
        var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = !string.IsNullOrWhiteSpace(informational)
            ? informational
            : asm.GetName().Version?.ToString() ?? "unknown";

        loggerConfiguration.Enrich.WithProperty("ApplicationVersion", version);

        var otlpLogs = FirstNonEmpty(
            context.Configuration["Serilog:OtlpLogs:Endpoint"],
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"),
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));
        if (!string.IsNullOrWhiteSpace(otlpLogs))
        {
            loggerConfiguration.WriteTo.OpenTelemetry(o =>
            {
                o.Endpoint = otlpLogs;
                o.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = FirstNonEmpty(context.Configuration["OpenTelemetry:ServiceName"], "Zebrahoof.EMR")!,
                    ["service.version"] = version
                };
            });
        }

        if (string.Equals(Environment.GetEnvironmentVariable("ENABLE_SERILOG_SELFLOG"), "1", StringComparison.Ordinal))
        {
            var dir = Path.Combine(context.HostingEnvironment.ContentRootPath, "Logs");
            Directory.CreateDirectory(dir);
            var selfPath = Path.Combine(dir, "serilog-selflog.txt");
            SelfLog.Enable(msg =>
            {
                try
                {
                    File.AppendAllText(selfPath, $"{DateTimeOffset.UtcNow:O} {msg}");
                }
                catch
                {
                    // ignore secondary failures
                }
            });
        }
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
