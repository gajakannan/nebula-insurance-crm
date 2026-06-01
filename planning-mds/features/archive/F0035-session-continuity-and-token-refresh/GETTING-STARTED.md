# F0035 — Getting Started

> Skeleton authored by PM during Phase A. Implementers fill in concrete file paths, seed values, and verification steps as the build action proceeds.

## Prerequisites

- authentik IdP healthy with refresh-token issuance enabled on the Nebula OIDC client (DevOps Phase B preflight)
- F0005 IdP migration baseline in place (claims normalization, principal key data model)
- F0009 authentication + role-based login operational (current `/login` and `/auth/callback` flows)
- F0033 Serilog structured logging baseline active (target sink for `Nebula.Session.Continuity` category)

## Key Files

- Frontend API client / auth-error classifier: `experience/src/services/api.ts`, `experience/src/features/session-continuity/authErrorClassifier.ts` (S0004)
- Silent renewal coalescing primitive: `experience/src/features/session-continuity/sessionRenewal.ts` (S0001)
- Idle activity-detection hook: `experience/src/features/session-continuity/useIdleWarning.ts` (S0002)
- Idle warning modal component: `experience/src/features/session-continuity/IdleWarningModal.tsx` (S0002)
- Session restore (form snapshot + return_to): `experience/src/features/session-continuity/sessionRestore.ts`, `experience/src/features/session-continuity/dirtyFormRegistry.tsx` (S0003)
- Telemetry emitter: `experience/src/features/session-continuity/sessionTelemetry.ts`, `experience/src/features/session-continuity/deferredTelemetryBuffer.ts` (S0005)
- Backend auth-failure middleware extension: `engine/src/Nebula.Api/Program.cs`, `engine/src/Nebula.Api/Helpers/ProblemDetailsHelper.cs` (S0004)
- Telemetry ingest endpoint: `engine/src/Nebula.Api/Endpoints/SessionTelemetryEndpoints.cs` (S0005)
- Telemetry validation service: `engine/src/Nebula.Api/Services/SessionContinuityTelemetryService.cs` (S0005)
- Event schema: `planning-mds/schemas/session-continuity-event.schema.json` (created at Phase B; governed by ADR-024 §5)
- ProblemDetails type URIs: `https://nebula.local/problems/auth/{token-expired,invalid-token,session-revoked}`, `https://nebula.local/problems/authz/forbidden` (S0004 — Architect Phase B)

## Seed Data

None required. F0035 introduces no new entities or reference data.

## Verification Steps

Final feature evidence run `2026-05-24-c92b16b6` used:

- Backend focused integration tests: `AuthProblemDetailsContractTests` and `SessionTelemetryEndpointTests` (8 passed)
- Frontend focused Vitest suite: API classifier/renewal, session continuity helpers, auth callback/login/session teardown, idle modal and accessibility (58 passed)
- Frontend coverage: `artifacts/coverage/frontend-session-continuity/`
- Frontend lint: exit 0 with pre-existing warnings outside F0035
- Frontend production build: exit 0 with existing chunk-size warning
- Evidence validators: G0, G1, G2, G3, G4.5, and G4.6 exited 0 before PM closeout

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
- Plan run evidence: `planning-mds/operations/evidence/runs/2026-05-23-41109356/`
