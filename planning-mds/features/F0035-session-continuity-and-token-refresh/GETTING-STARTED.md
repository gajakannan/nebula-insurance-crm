# F0035 — Getting Started

> Skeleton authored by PM during Phase A. Implementers fill in concrete file paths, seed values, and verification steps as the build action proceeds.

## Prerequisites

- authentik IdP healthy with refresh-token issuance enabled on the Nebula OIDC client (DevOps Phase B preflight)
- F0005 IdP migration baseline in place (claims normalization, principal key data model)
- F0009 authentication + role-based login operational (current `/login` and `/auth/callback` flows)
- F0033 Serilog structured logging baseline active (target sink for `Nebula.Session.Continuity` category)

## Key Files (to be populated during build)

- Frontend API client / auth-error classifier: `experience/src/<path>` (S0004)
- Silent renewal coalescing primitive: `experience/src/<path>` (S0001)
- Idle activity-detection hook: `experience/src/<path>` (S0002)
- Idle warning modal component: `experience/src/<path>` (S0002)
- Session restore (form snapshot + return_to): `experience/src/<path>` (S0003)
- Telemetry emitter: `experience/src/<path>` (S0005)
- Backend auth-failure middleware extension: `engine/src/<path>` (S0004)
- Telemetry ingest endpoint: `engine/src/<path>` (S0005)
- Event schema: `planning-mds/schemas/session-continuity-event.schema.json` (created at Phase B; governed by ADR-024 §5)
- ProblemDetails type URIs: `https://nebula.local/problems/auth/{token-expired,invalid-token,session-revoked}`, `https://nebula.local/problems/authz/forbidden` (S0004 — Architect Phase B)

## Seed Data

None required. F0035 introduces no new entities or reference data.

## Verification Steps

To be filled in by implementers as work lands. Expected verification surface:

- Frontend tests (Vitest): coalescing semaphore, idle timer monotonic clock, classifier dispatch matrix
- Frontend integration tests: 6-concurrent expiry coalescing, full forced-re-auth journey with dirty form
- Backend integration tests: per-endpoint 401 contract conformance (every protected endpoint emits valid `WWW-Authenticate` + ProblemDetails)
- E2E smoke (Playwright): expire-and-continue happy path; expire-during-mutation-with-form-state path; idle-warning-shown happy path
- Accessibility (@axe-core/playwright): idle modal WCAG 2.1 AA
- Telemetry verification: events visible in Serilog query under `Nebula.Session.Continuity` category

## Configuration

| Setting | MVP Value | Source | Notes |
|---------|-----------|--------|-------|
| Idle threshold | 1_500_000 ms (25 min) | frontend config | S0002; not user-configurable in MVP |
| Idle grace period | 300_000 ms (5 min) | frontend config | S0002; not user-configurable in MVP |
| Active-session rolling window | 4 hours | frontend config | S0001 / S0003 |
| Active-session hard cap | 8 hours from sign-in | frontend config | S0001 / S0003 |
| Renewal-loop throttle window | 5 seconds | frontend config | S0001; suppresses repeated renewal attempts |
| Form snapshot max serialized size | 256 KB | frontend config | S0003 |
| Form snapshot TTL | 1 hour | frontend config | S0003 |
| Telemetry emitter in-memory buffer | 50 events | frontend config | S0005 (healthy-session path) |
| Telemetry retry policy (in-memory) | exponential backoff, 3 retries then drop | frontend config | S0005 |
| Failure-class deferred-emit buffer (`localStorage`) | 100 entries per user (LRU eviction) | frontend config | S0005 + ADR-024 §5a — silent-renewal-fail, forced-redirect, auth-classifier-fallback, auth-classifier-conflict only |
| Deferred-emit buffer per-entry TTL | 7 days from event `timestamp` | frontend config | S0005 + ADR-024 §5a; older entries purged at drain |
| Deferred-emit buffer key prefix | `nebula.telemetry-defer.v1.<user_id>.<event_uuid>` | frontend constant | S0005 + ADR-024 §5a; per-user isolation prevents cross-user leak |
| Deferred-emit drain trigger | OIDC bootstrap completion (F0009-S0002 callback success) | frontend hook | S0005 + ADR-024 §5a |
| Deferred-emit drain batch size | up to 10 events per POST | frontend config | S0005 + ADR-024 §6 |

These values are starting points; Architect may parameterize at the build action if security-team review identifies tunability needs.

## Plan Run Reference

- Plan run id: `2026-05-23-41109356`
- Plan run evidence: `planning-mds/operations/evidence/2026-05-23-41109356/`
