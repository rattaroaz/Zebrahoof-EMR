# Observability Verification Checklist

Use this checklist for each production readiness verification run.

## Pre-check

- [ ] Deployment environment identified (staging/prod).
- [ ] Logging config reviewed (`OpenTelemetry:*`, `Serilog:*`, forwarded headers).
- [ ] Test account and request generator available.

## Trace verification

- [ ] `OpenTelemetry:Enabled=true` in target environment.
- [ ] OTLP endpoint reachable from app.
- [ ] Generate test request with `X-Correlation-Id: verify-<timestamp>`.
- [ ] Confirm corresponding trace appears in backend with expected service name.

## Log verification

- [ ] Generate auth success and auth failure events.
- [ ] Generate patient read/update and document view/rename events.
- [ ] Confirm events appear in centralized log backend.
- [ ] Confirm no plaintext password/secret/token appears in those events.

## Correlation verification

- [ ] Use one correlation ID across multiple calls.
- [ ] Confirm request logs, app logs, and trace IDs can be joined for that ID.
- [ ] Capture at least one query example used for correlation.

## Alert verification

- [ ] Force synthetic threshold crossing in non-prod (or replay known-safe fixture).
- [ ] Confirm alert fires with correct severity.
- [ ] Confirm page/chat route and owner assignment are correct.
- [ ] Confirm alert clear/resolution behavior is correct.

## Evidence capture

- [ ] Copy `docs/OBSERVABILITY_EVIDENCE_TEMPLATE.md` into the release/change ticket or an evidence artifact.
- [ ] Save screenshots or exported query results for traces/logs/alerts.
- [ ] Record run date/time and operator in release notes.
- [ ] Link artifacts to deployment ticket/change request.

## Sign-off

- [ ] On-call engineer sign-off
- [ ] Platform lead sign-off
- [ ] Security/compliance sign-off (when audit scope changed)
