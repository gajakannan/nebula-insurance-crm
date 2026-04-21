# Feature Security Review Report

Feature: F0010 — Dashboard Opportunities Refactor (Pipeline Board + Insight Views)

## Summary

- Assessment: PASS
- Findings:
  - Critical: 0
  - High: 0
  - Medium: 0
  - Low: 2

## OWASP Top 10 Check

| OWASP Category | Risk | Finding |
|----------------|------|---------|
| A01: Broken Access Control | Low | All new endpoints enforce `dashboard_pipeline` via `HasAccessAsync()`. BrokerUser denied. No privilege escalation vectors. |
| A02: Cryptographic Failures | N/A | No new secrets or encrypted data handling. |
| A03: Injection | Low | `entityType` validated against exact string whitelist. `periodDays` clamped. EF Core parameterized queries used throughout. |
| A04: Insecure Design | N/A | Read-only aggregation. No mutations or state changes. |
| A05: Security Misconfiguration | N/A | No new services, env vars, or infrastructure. |
| A06: Vulnerable Components | N/A | No new NuGet or npm dependencies. |
| A07: Auth Failures | N/A | Existing JWT + `RequireAuthorization()` pipeline applies. |
| A08: Software/Data Integrity | N/A | No new deserialization or supply chain changes. |
| A09: Logging & Monitoring | N/A | No new sensitive data exposed. Existing logging applies. |
| A10: SSRF | N/A | No outbound requests from new endpoints. |

## Findings

### Critical: None
### High: None
### Medium: None

### Low

1. **L-SEC-01: Aggregate counts are org-wide, not per-user ABAC-scoped**
   - Aging and hierarchy endpoints return organization-wide aggregate counts without per-row ABAC filtering. This is consistent with all existing dashboard opportunities endpoints and is by design — aggregate summaries are intentionally org-wide for internal users. BrokerUser is denied at Casbin policy level. Mini-card drilldowns (separate endpoint) handle per-entity scoping.
   - **Risk:** Low. Aggregate counts do not expose individual entity details.

2. **L-SEC-02: `periodDays` silently clamped without warning**
   - Endpoints clamp `periodDays` to 1–730 range without returning a warning. Consistent with existing flow endpoint. Not a security concern — defensive input handling.

## Control Checks

- [x] Authorization coverage complete — all new endpoints check `dashboard_pipeline`; BrokerUser denied by Casbin policy
- [x] Input validation enforced — `entityType` whitelist, `periodDays` range clamping
- [x] No secrets in code — no hardcoded credentials, API keys, or tokens
- [x] Auditability requirements met — N/A (read-only, no mutations)

## Authorization Matrix Verification

| Endpoint | Internal Roles | BrokerUser | Anonymous |
|----------|---------------|------------|-----------|
| `GET /dashboard/opportunities/aging` | Allowed | Denied | 401 |
| `GET /dashboard/opportunities/hierarchy` | Allowed | Denied | 401 |

## Recommendation

**APPROVE** — No critical, high, or medium security findings. Read-only aggregation with proper authorization. No new attack vectors.
