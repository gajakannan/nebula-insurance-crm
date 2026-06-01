# Security Review Report - F0035

**Owner:** Security Reviewer  
**Reviewer:** Codex  
**Date:** 2026-05-24  
**Verdict:** PASS

## Review Scope

- Token-expired, invalid-token, session-revoked, and forbidden ProblemDetails semantics.
- Frontend auth classifier, forced re-auth, route restoration, snapshot storage, idle warning behavior, and telemetry persistence.
- PII and token leakage boundaries in client telemetry and backend telemetry ingest.

## Security Checks

| Area | Result | Notes |
| --- | --- | --- |
| 401/403 semantics | PASS | 401 includes `WWW-Authenticate`; 403 removes it. ProblemDetails types/codes match ADR-024. |
| Token handling | PASS | Access and refresh tokens are not persisted by F0035 telemetry or restore helpers. |
| Mutation replay | PASS | Expired-token mutations renew the session but are not replayed automatically. |
| Route restore | PASS | `return_to` is sanitized to same-origin app paths and rejects auth callback/login routes. |
| Form snapshots | PASS | Dirty form values are stored in `sessionStorage`, scoped by user id, capped at 256 KB, and expire after one hour. |
| Deferred telemetry | PASS | Failure-class telemetry is stored in `localStorage` without form values or token fields and now purges expired entries before drain. |
| Backend telemetry validation | PASS | Event registry, user-id match, payload allowlist, PII key rejection, and route query rejection are enforced server-side. |
| Public route behavior | PASS | Idle timers are suppressed on `/login`, `/auth/callback`, and `/unauthorized`. |

## Evidence

- `AuthProblemDetailsContractTests.cs`
- `SessionTelemetryEndpointTests.cs`
- `authErrorClassifier.test.ts`
- `api.test.ts`
- `sessionRestore.test.ts`
- `sessionTelemetry.test.ts`
- `useIdleWarning.test.tsx`
- `IdleWarningModal.a11y.test.tsx`

## Findings

No high or critical security findings remain open.

## Recommendations

None.
