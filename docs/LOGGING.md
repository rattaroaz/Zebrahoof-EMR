# Logging and observability runbook

This application uses **Serilog** for structured logs, **correlation IDs** on HTTP requests, optional **OpenTelemetry** traces to OTLP, optional **Serilog → OTLP** for logs, **API request middleware** for `/api` routes, and **audit** events for sensitive patient and document operations.

## Log output locations

- **Console**: always (configured per environment in `appsettings*.json`).
- **Rolling files**: under `Logs/` in the content root (path and retention vary by environment; Production uses JSON compact format).
- **Bootstrap**: a minimal console logger is used until the host finishes Serilog configuration.

## Correlation ID

- Middleware assigns or forwards a correlation ID for each request.
- Prefer sending header **`X-Correlation-Id`** from clients so distributed traces and support tickets can tie UI actions to server logs.
- Serilog request logging and API middleware include correlation/request identifiers in templates where configured.

## Environment variables

| Variable | Purpose |
|----------|---------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Base OTLP endpoint for exporters that read this convention. |
| `OTEL_EXPORTER_OTLP_LOGS_ENDPOINT` | OTLP logs endpoint (used by Serilog OpenTelemetry sink when set). |
| `ENABLE_SERILOG_SELFLOG` | Set to `1` to append Serilog internal diagnostics to `Logs/serilog-selflog.txt` (use sparingly; can include sink errors). |

Empty config values are ignored when resolving OTLP endpoints, so environment variables still work when `appsettings*.json` leaves endpoint keys blank.

## Configuration keys

### OpenTelemetry (traces)

- **`OpenTelemetry:Enabled`**: `true` to register tracing and export spans via OTLP. Default in repo configs is `false`.
- **`OpenTelemetry:OtlpEndpoint`**: gRPC OTLP endpoint URL (e.g. your collector). Can be overridden by `OTEL_EXPORTER_OTLP_ENDPOINT` where the extension reads environment configuration (see `Telemetry/AppOpenTelemetryExtensions.cs`).
- **`OpenTelemetry:ServiceName`**: logical service name on the resource; defaults to `Zebrahoof.EMR`.

### Serilog (sampling and OTLP logs)

- **`Serilog:RequestLogSamplingRate`**: `0.0`–`1.0` fraction of `/api` request audit lines emitted by `ApiRequestLoggingMiddleware` (default `1.0` = all).
- **`Serilog:SlowRequestWarningMs`**: elapsed time above which successful requests may be logged at **Warning** (see `Program.cs` Serilog request logging and API middleware).
- **`Serilog:OtlpLogs:Endpoint`**: when non-empty, Serilog also writes logs to this OTLP endpoint (see `Logging/SerilogHostConfiguration.cs`). Environment variables above take precedence when set.

### Forwarded headers

- **`ForwardedHeaders:TrustAllProxies`**: when `true`, **KnownIPNetworks** and **KnownProxies** are cleared so any proxy can set `X-Forwarded-For` / `X-Forwarded-Proto`. **Production should keep this `false`** unless you explicitly terminate TLS at a trusted reverse proxy and understand the spoofing risk. Repo defaults use `false`.

## Operational notes

- **Passwords** are never written to logs. Login outcomes use user identifiers suitable for security monitoring (e.g. user id on failed password), not raw credentials.
- **Patient search** audit metadata records term length and counts, not the query text.
- **Grok** and long clinical payloads are truncated/redacted where implemented; treat any new logging as PHI-sensitive by default.
- **`/health`** and other excluded paths are tuned down in Serilog request completion to reduce noise (`Logging/RequestLoggingExclusions.cs`); `/api` lines are owned by `ApiRequestLoggingMiddleware` to avoid duplicate completion events.

## Quick verification

1. Run the app locally and confirm console output includes expected startup messages.
2. Issue a request with `X-Correlation-Id: test-123` and confirm the same value appears in log properties for that request.
3. With `OpenTelemetry:Enabled` true and a collector URL set, confirm spans arrive at the collector.
4. With `Serilog:OtlpLogs:Endpoint` or `OTEL_EXPORTER_OTLP_LOGS_ENDPOINT` set, confirm log records appear in your log backend.

## Related production docs

- Alert rules and escalation: `docs/ALERTING.md`
- Versioned alert rule catalog: `docs/ALERT_RULES.yml`
- Retention and access policy: `docs/LOG_RETENTION_ACCESS.md`
- Verification run checklist: `docs/OBSERVABILITY_VERIFICATION.md`
- Verification evidence template: `docs/OBSERVABILITY_EVIDENCE_TEMPLATE.md`
