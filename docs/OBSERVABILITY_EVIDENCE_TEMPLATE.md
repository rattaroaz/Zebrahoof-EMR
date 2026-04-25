# Observability Verification Evidence

Use this template to record a completed staging or production observability validation run.

## Run metadata

- Environment:
- App version / commit:
- Operator:
- Date/time:
- Monitoring backend:
- OTLP endpoint:

## Correlation test

- Test correlation ID:
- Request path(s):
- Trace link or query:
- Log link or query:
- Result: PASS / FAIL

## Trace export

- Service name observed:
- Span count:
- Example trace ID:
- Result: PASS / FAIL

## Log export

- Auth success/failure event found: PASS / FAIL
- API request event found: PASS / FAIL
- Patient audit event found: PASS / FAIL
- Document audit event found: PASS / FAIL
- No password/secret/token observed: PASS / FAIL

## Alert validation

- `auth-failure-spike`: PASS / FAIL / NOT RUN
- `account-lockout-spike`: PASS / FAIL / NOT RUN
- `api-error-rate`: PASS / FAIL / NOT RUN
- `slow-api-latency`: PASS / FAIL / NOT RUN
- `audit-pipeline-interruption`: PASS / FAIL / NOT RUN
- `otlp-exporter-health`: PASS / FAIL / NOT RUN

## Evidence links

- Trace screenshot/export:
- Log screenshot/export:
- Alert screenshot/export:
- Change/release ticket:

## Sign-off

- On-call engineer:
- Platform lead:
- Security/compliance:
