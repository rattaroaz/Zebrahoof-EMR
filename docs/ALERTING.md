# Alerting Specification (Production)

This document defines the minimum alert set for Zebrahoof EMR production observability.

The versioned rule catalog lives in `docs/ALERT_RULES.yml`; use it as the source-of-truth when configuring Datadog, Grafana/Loki, Azure Monitor, Splunk, or another monitoring backend.

## Ownership and escalation

- Primary owner: On-call engineer
- Secondary owner: EMR platform lead
- Tertiary owner: Engineering manager
- Escalation policy:
  - P1: page immediately, acknowledge within 5 minutes
  - P2: page immediately, acknowledge within 15 minutes
  - P3: ticket + chat notification, acknowledge within business day

## Alert rules

1) Authentication failure spike (P2)
- Signal: count of login failures
- Query basis: `SourceContext = Zebrahoof_EMR.Auth.Account` and message contains `Login failed`
- Threshold: >= 25 failures in 5 minutes OR >= 3x baseline in 15 minutes
- Action: investigate potential credential-stuffing or outage

2) Account lockout spike (P2)
- Signal: count of lockout events
- Query basis: log message contains `account locked`
- Threshold: >= 10 lockouts in 10 minutes
- Action: investigate abuse, review IP distribution and WAF/rate limits

3) API error rate (P1)
- Signal: HTTP 5xx request ratio on `/api`
- Query basis: API request middleware events
- Threshold: > 5% 5xx for 5 minutes with at least 100 requests
- Action: page on-call, begin incident response

4) Slow API latency (P2)
- Signal: request latency percentile
- Query basis: `/api` request telemetry
- Threshold: p95 > 2000 ms for 10 minutes
- Action: inspect downstream dependencies (DB, AI calls, external services)

5) Audit pipeline interruption (P1)
- Signal: audit event volume drops unexpectedly
- Query basis: `patient_*`, `document_*`, and auth/session audit actions
- Threshold: zero audit events for 15 minutes during expected traffic window
- Action: verify app pipeline and storage sink immediately

6) OTLP exporter health degradation (P2)
- Signal: exporter errors, dropped batch telemetry, sink backpressure
- Query basis: Serilog self-log (`ENABLE_SERILOG_SELFLOG=1`) and collector health metrics
- Threshold: any sustained exporter error > 5 minutes
- Action: fail over to file-only logs while collector is remediated

## Runbook links

- Logging configuration and keys: `docs/LOGGING.md`
- Versioned alert catalog: `docs/ALERT_RULES.yml`
- Retention and access policy: `docs/LOG_RETENTION_ACCESS.md`
- Verification procedure: `docs/OBSERVABILITY_VERIFICATION.md`
