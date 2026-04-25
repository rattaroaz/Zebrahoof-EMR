# Log Retention and Access Policy

This policy defines retention, access control, and review cadence for operational and audit logs.

## Retention targets

- Application logs (structured Serilog): 30 days minimum hot retention.
- Audit logs (patient/document/session/audit actions): 365 days minimum.
- Security-relevant authentication logs: 180 days minimum.
- Exception and crash diagnostics: 90 days minimum.

If legal, payer, or client contracts require longer periods, the stricter requirement wins.

## Storage expectations

- Primary: centralized log platform with immutable retention controls.
- Secondary: local rolling file logs (`Logs/`) for short-term fallback and troubleshooting.
- Backups: centralized platform backup/replication managed by infrastructure owners.

## Access control

- Principle: least privilege, role-based access only.
- Allowed roles:
  - On-call engineers: read access to operational logs
  - Security/compliance personnel: read access to security and audit logs
  - Platform admins: controlled write/config access
- Prohibited:
  - Shared credentials
  - Broad anonymous access
  - Direct developer write access in production without change control

## Access review cadence

- Monthly: review all users/groups with production log access.
- Quarterly: manager attestation for access necessity.
- Immediate: revoke access for offboarding or role change.

## Data handling

- Never intentionally log plaintext secrets, passwords, refresh tokens, or full PHI payloads.
- Prefer identifiers, counts, and lengths over raw values where practical.
- Any new logging touching patient data must be reviewed for minimum necessary disclosure.

## Operational checks

- Confirm retention settings after platform changes.
- Verify index lifecycle / archive rules are active and non-overlapping.
- Validate that audit streams are queryable for compliance investigations.
