# Test Plan - F0035

**Owner:** Quality Engineer  
**Date:** 2026-05-24  
**Verdict:** PASS

## Coverage Goals

| Area | Required Verification |
| --- | --- |
| Backend auth contracts | Expired, invalid, revoked, and forbidden auth responses produce ADR-024 ProblemDetails and correct `WWW-Authenticate` behavior. |
| Backend telemetry ingest | Valid event batch, PII rejection, user mismatch, and invalid-token challenge behavior. |
| Frontend classifier and renewal | ProblemDetails precedence, conflict detection, fallback path, one coalesced renewal, one GET retry, mutation non-replay. |
| Forced re-auth restore | Safe `return_to`, same-user snapshot consume, different-user cleanup, callback telemetry drain, explicit sign-out cleanup. |
| Idle warning | 25-minute threshold, grace expiry forced re-auth, stay-signed-in renewal, explicit sign-out, alertdialog a11y. |
| Deployability | Frontend build, lint, Vite proxy path, no migration/Docker changes, runtime tests execute in container. |

## Planned Commands

- `docker run ... dotnet test tests/Nebula.Tests/Nebula.Tests.csproj --filter 'FullyQualifiedName~SessionTelemetryEndpointTests|FullyQualifiedName~AuthProblemDetailsContractTests'`
- `pnpm --dir experience exec vitest run <F0035 focused frontend tests>`
- `pnpm --dir experience exec vitest run <F0035 focused frontend tests> --coverage`
- `pnpm --dir experience lint`
- `pnpm --dir experience build`

## Acceptance Mapping

| Story | Test Evidence |
| --- | --- |
| S0001 recoverable renewal | `api.test.ts`, `sessionRenewal.test.ts`, backend auth contract tests. |
| S0002 idle warning | `useIdleWarning.test.tsx`, `IdleWarningModal.test.tsx`, `IdleWarningModal.a11y.test.tsx`. |
| S0003 route/form restore | `sessionRestore.test.ts`, `AuthCallbackPage.test.tsx`, `useSessionTeardown.test.tsx`. |
| S0004 auth-error semantics | `AuthProblemDetailsContractTests.cs`, `authErrorClassifier.test.ts`, `api.test.ts`. |
| S0005 telemetry | `SessionTelemetryEndpointTests.cs`, `sessionTelemetry.test.ts`, `deferredTelemetryBuffer` coverage through telemetry tests. |
