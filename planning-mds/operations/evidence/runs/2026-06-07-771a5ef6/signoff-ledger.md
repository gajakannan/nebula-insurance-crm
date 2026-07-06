# Signoff Ledger — F0017 run 2026-06-07-771a5ef6

## Required Role Matrix

| Role | Required | Verdict | Verdict Artifact | Notes |
|------|----------|---------|------------------|-------|
| Architect | Yes | PASS | `g0-assembly-plan-validation.md` | Assembly plan and ADR-026 alignment accepted. |
| Quality Engineer | Yes | PASS WITH RECOMMENDATIONS | `test-execution-report.md` | Backend 24/24 after G3 repair; frontend feature test/lint/build pass. |
| Code Reviewer | Yes | APPROVED WITH RECOMMENDATIONS | `code-review-report.md` | G3 blocker repaired; no unresolved blocking findings. |
| DevOps | Yes | PASS WITH RECOMMENDATIONS | `deployability-check.md` | EF migration and build/runtime evidence accepted with release-hardening follow-ups. |
| Security Reviewer | No | N/A | N/A | Not forced because F0017 defers hierarchy-aware access enforcement to F0037. |

## Current Signoff State

G5 signoff is approved with recommendations. Operator approval was provided in chat on 2026-07-02 and recorded as G4 in `gate-decisions.md`.

Blocking status:
- No blocking G2/G3/G4 findings remain.
- Required role artifacts are present.
- F0017 tracker state is now Active/Done, so G6+ validation runs with completion-evidence semantics.

## Current Story Signoff References

| Story | Role | STATUS Verdict | Ledger Artifact |
|-------|------|----------------|-----------------|
| F0017-S0001 | Quality Engineer | PASS | `test-execution-report.md` |
| F0017-S0001 | Code Reviewer | APPROVED | `code-review-report.md` |
| F0017-S0001 | DevOps | PASS | `deployability-check.md` |
| F0017-S0001 | Architect | PASS | `g0-assembly-plan-validation.md` |
| F0017-S0002 | Quality Engineer | PASS | `test-execution-report.md` |
| F0017-S0002 | Code Reviewer | APPROVED | `code-review-report.md` |
| F0017-S0002 | DevOps | PASS | `deployability-check.md` |
| F0017-S0002 | Architect | PASS | `g0-assembly-plan-validation.md` |
| F0017-S0003 | Quality Engineer | PASS | `test-execution-report.md` |
| F0017-S0003 | Code Reviewer | APPROVED | `code-review-report.md` |
| F0017-S0003 | DevOps | PASS | `deployability-check.md` |
| F0017-S0003 | Architect | PASS | `g0-assembly-plan-validation.md` |
| F0017-S0004 | Quality Engineer | PASS | `test-execution-report.md` |
| F0017-S0004 | Code Reviewer | APPROVED | `code-review-report.md` |
| F0017-S0004 | DevOps | PASS | `deployability-check.md` |
| F0017-S0004 | Architect | PASS | `g0-assembly-plan-validation.md` |
| F0017-S0005 | Quality Engineer | PASS | `test-execution-report.md` |
| F0017-S0005 | Code Reviewer | APPROVED | `code-review-report.md` |
| F0017-S0005 | DevOps | PASS | `deployability-check.md` |
| F0017-S0005 | Architect | PASS | `g0-assembly-plan-validation.md` |

## Recommendation Acceptances

PM Acceptance Line: OpenAPI advisory — remediated on 2026-07-03 by pinning `Microsoft.OpenApi` to `2.9.0`; `dotnet restore Nebula.slnx` completed without the prior advisory warning.

PM Acceptance Line: Frontend bundle size — accepted as non-blocking for F0017 signoff; track Vite large-chunk warning for route-level splitting or chunk policy.

PM Acceptance Line: Frontend panel coverage — accepted as non-blocking for F0017 signoff; add broader tests for `OwnershipPanel` and `TerritoriesPanel` before G8 if those are release-critical UI.

PM Acceptance Line: Migration snapshot drift — accepted as a branch hygiene follow-up; prior evidence states the F0017 migration was scoped to its four tables while broader snapshot drift is pre-existing.

## Waivers And Omissions

- No required G5 artifact is omitted.
- No coverage waiver is active; prior frontend-CI waiver is superseded by current local frontend validation.
- Security review is omitted only because it is not required by the effective role matrix and `security_sensitive_scope=false`.
