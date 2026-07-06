# Security Review Report — F0028

## Verdict

PASS WITH RECOMMENDATIONS

## Security Scope

F0028 is security sensitive because appetite notes, appointment context, and underwriter relationship intelligence are commercially sensitive internal data.

## Findings

- F0028 endpoints are registered behind authorization; unauthenticated `/carrier-markets` returns `401 Unauthorized`.
- Runtime authorization policy rows for carrier market actions are covered by Casbin authorization tests.
- No local development secret value was committed; the restored `.env` remains gitignored.
- Build surfaced existing `Microsoft.OpenApi 2.0.0` NU1903 advisory. This package advisory is inherited and not introduced by F0028.
- Dedicated commercial dependency, secrets, SAST, and DAST scanners are not configured in the local harness; local checks used build/test/runtime/reviewer signals.

## Recommendations

- [medium] Upgrade or replace inherited `Microsoft.OpenApi 2.0.0` dependency outside F0028 - owner: Security Reviewer; follow-up: Dependency maintenance backlog.
- [medium] Add CI scanner automation for dependency, secrets, SAST, and DAST evidence - owner: Security Reviewer; follow-up: Security pipeline hardening.
- [low] Re-run unauthenticated and role-scoped endpoint checks in staging after migration application - owner: Security Reviewer; follow-up: Staging validation.

## Scan Record

| Scan Class | Result | Evidence |
|------------|--------|----------|
| Dependency | PASS WITH RECOMMENDATIONS | Build warning `NU1903` recorded as inherited follow-up |
| Secrets | PASS | `.env` remains gitignored; no secret values added to tracked evidence |
| SAST | PASS WITH RECOMMENDATIONS | Compiler, focused tests, and reviewer inspection |
| DAST | PASS WITH RECOMMENDATIONS | Local `/healthz` and unauthenticated `/carrier-markets` smoke checks |
