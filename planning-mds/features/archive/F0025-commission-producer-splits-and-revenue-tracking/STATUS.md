# F0025 — Commission, Producer Splits & Revenue Tracking — Status

**Overall Status:** Done and archived
**Last Updated:** 2026-07-07

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0025-S0001 | Commission workspace search and policy context | Done |
| F0025-S0002 | Commission schedule maintenance | Done |
| F0025-S0003 | Producer split assignment | Done |
| F0025-S0004 | Expected commission calculation review | Done |
| F0025-S0005 | Commission adjustment and approval | Done |
| F0025-S0006 | Revenue attribution rollups | Done |

## Required Role Matrix

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Commission calculations, split attribution, adjustments, and rollups require acceptance evidence. | Product Manager (Phase A, to be confirmed by Architect) | 2026-07-07 |
| Code Reviewer | Yes | Economic logic, source attribution, and downstream reporting require independent review. | Product Manager (Phase A, to be confirmed by Architect) | 2026-07-07 |
| Security Reviewer | Yes | Commission, split, and revenue data are sensitive internal economic data. | Product Manager (Phase A, to be confirmed by Architect) | 2026-07-07 |
| DevOps | Yes | Phase B is expected to introduce persisted records, migrations, policy seed changes, or reporting surfaces. | Product Manager (Phase A, to be confirmed by Architect) | 2026-07-07 |
| Architect | Yes | Phase B must define economic data boundaries, authorization, audit, calculation, and KG bindings before build. | Product Manager (Phase A) | 2026-07-07 |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0025-S0001 | Quality Engineer | - | N/A | - | - | Populate during feature action. |
| F0025-S0001 | Code Reviewer | - | N/A | - | - | Populate during feature action. |
| F0025-S0001 | Security Reviewer | - | N/A | - | - | Populate during feature action. |
| F0025-S0001 | DevOps | - | N/A | - | - | Populate during feature action. |
| F0025-S0001 | Architect | - | N/A | - | - | Populate during plan/feature action. |
| F0025-S0002 | Quality Engineer | - | N/A | - | - | Populate during feature action. |
| F0025-S0002 | Code Reviewer | - | N/A | - | - | Populate during feature action. |
| F0025-S0002 | Security Reviewer | - | N/A | - | - | Populate during feature action. |
| F0025-S0002 | DevOps | - | N/A | - | - | Populate during feature action. |
| F0025-S0002 | Architect | - | N/A | - | - | Populate during plan/feature action. |
| F0025-S0003 | Quality Engineer | - | N/A | - | - | Populate during feature action. |
| F0025-S0003 | Code Reviewer | - | N/A | - | - | Populate during feature action. |
| F0025-S0003 | Security Reviewer | - | N/A | - | - | Populate during feature action. |
| F0025-S0003 | DevOps | - | N/A | - | - | Populate during feature action. |
| F0025-S0003 | Architect | - | N/A | - | - | Populate during plan/feature action. |
| F0025-S0004 | Quality Engineer | - | N/A | - | - | Populate during feature action. |
| F0025-S0004 | Code Reviewer | - | N/A | - | - | Populate during feature action. |
| F0025-S0004 | Security Reviewer | - | N/A | - | - | Populate during feature action. |
| F0025-S0004 | DevOps | - | N/A | - | - | Populate during feature action. |
| F0025-S0004 | Architect | - | N/A | - | - | Populate during plan/feature action. |
| F0025-S0005 | Quality Engineer | - | N/A | - | - | Populate during feature action. |
| F0025-S0005 | Code Reviewer | - | N/A | - | - | Populate during feature action. |
| F0025-S0005 | Security Reviewer | - | N/A | - | - | Populate during feature action. |
| F0025-S0005 | DevOps | - | N/A | - | - | Populate during feature action. |
| F0025-S0005 | Architect | - | N/A | - | - | Populate during plan/feature action. |
| F0025-S0006 | Quality Engineer | - | N/A | - | - | Populate during feature action. |
| F0025-S0006 | Code Reviewer | - | N/A | - | - | Populate during feature action. |
| F0025-S0006 | Security Reviewer | - | N/A | - | - | Populate during feature action. |
| F0025-S0006 | DevOps | - | N/A | - | - | Populate during feature action. |
| F0025-S0006 | Architect | - | N/A | - | - | Populate during plan/feature action. |
| F0025-S0001 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-07 | G0 assembly plan validation passed for run 2026-07-07-9859bad4. |
| F0025-S0001 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-07 | G2 focused backend/frontend validation passed. |
| F0025-S0001 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-07 | Runtime preflight and deployability evidence passed. |
| F0025-S0001 | Code Reviewer | Code Reviewer | APPROVED WITH RECOMMENDATIONS | code-review-report.md | 2026-07-07 | No Critical or High blockers; recommendations deferred to closeout acceptance. |
| F0025-S0001 | Security Reviewer | Security Reviewer | PASS WITH RECOMMENDATIONS | security-review-report.md | 2026-07-07 | No Critical or High blockers; scanner/source-scope recommendations deferred to closeout acceptance. |
| F0025-S0002 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-07 | G0 assembly plan validation passed for run 2026-07-07-9859bad4. |
| F0025-S0002 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-07 | G2 focused backend/frontend validation passed. |
| F0025-S0002 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-07 | Runtime preflight and deployability evidence passed. |
| F0025-S0002 | Code Reviewer | Code Reviewer | APPROVED WITH RECOMMENDATIONS | code-review-report.md | 2026-07-07 | No Critical or High blockers; recommendations deferred to closeout acceptance. |
| F0025-S0002 | Security Reviewer | Security Reviewer | PASS WITH RECOMMENDATIONS | security-review-report.md | 2026-07-07 | No Critical or High blockers; scanner/source-scope recommendations deferred to closeout acceptance. |
| F0025-S0003 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-07 | G0 assembly plan validation passed for run 2026-07-07-9859bad4. |
| F0025-S0003 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-07 | G2 focused backend/frontend validation passed. |
| F0025-S0003 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-07 | Runtime preflight and deployability evidence passed. |
| F0025-S0003 | Code Reviewer | Code Reviewer | APPROVED WITH RECOMMENDATIONS | code-review-report.md | 2026-07-07 | No Critical or High blockers; recommendations deferred to closeout acceptance. |
| F0025-S0003 | Security Reviewer | Security Reviewer | PASS WITH RECOMMENDATIONS | security-review-report.md | 2026-07-07 | No Critical or High blockers; scanner/source-scope recommendations deferred to closeout acceptance. |
| F0025-S0004 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-07 | G0 assembly plan validation passed for run 2026-07-07-9859bad4. |
| F0025-S0004 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-07 | G2 focused backend/frontend validation passed. |
| F0025-S0004 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-07 | Runtime preflight and deployability evidence passed. |
| F0025-S0004 | Code Reviewer | Code Reviewer | APPROVED WITH RECOMMENDATIONS | code-review-report.md | 2026-07-07 | No Critical or High blockers; recommendations deferred to closeout acceptance. |
| F0025-S0004 | Security Reviewer | Security Reviewer | PASS WITH RECOMMENDATIONS | security-review-report.md | 2026-07-07 | No Critical or High blockers; scanner/source-scope recommendations deferred to closeout acceptance. |
| F0025-S0005 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-07 | G0 assembly plan validation passed for run 2026-07-07-9859bad4. |
| F0025-S0005 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-07 | G2 focused backend/frontend validation passed. |
| F0025-S0005 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-07 | Runtime preflight and deployability evidence passed. |
| F0025-S0005 | Code Reviewer | Code Reviewer | APPROVED WITH RECOMMENDATIONS | code-review-report.md | 2026-07-07 | No Critical or High blockers; recommendations deferred to closeout acceptance. |
| F0025-S0005 | Security Reviewer | Security Reviewer | PASS WITH RECOMMENDATIONS | security-review-report.md | 2026-07-07 | No Critical or High blockers; scanner/source-scope recommendations deferred to closeout acceptance. |
| F0025-S0006 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-07 | G0 assembly plan validation passed for run 2026-07-07-9859bad4. |
| F0025-S0006 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-07 | G2 focused backend/frontend validation passed. |
| F0025-S0006 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-07 | Runtime preflight and deployability evidence passed. |
| F0025-S0006 | Code Reviewer | Code Reviewer | APPROVED WITH RECOMMENDATIONS | code-review-report.md | 2026-07-07 | No Critical or High blockers; recommendations deferred to closeout acceptance. |
| F0025-S0006 | Security Reviewer | Security Reviewer | PASS WITH RECOMMENDATIONS | security-review-report.md | 2026-07-07 | No Critical or High blockers; scanner/source-scope recommendations deferred to closeout acceptance. |
| F0025-S0001 | Code Reviewer | Product Manager | APPROVED | pm-closeout.md | 2026-07-07 | PM accepted Code Reviewer recommendations at closeout; no Critical or High blockers. |
| F0025-S0001 | Security Reviewer | Product Manager | PASS | pm-closeout.md | 2026-07-07 | PM accepted Security Reviewer recommendations at closeout; no Critical or High blockers. |
| F0025-S0002 | Code Reviewer | Product Manager | APPROVED | pm-closeout.md | 2026-07-07 | PM accepted Code Reviewer recommendations at closeout; no Critical or High blockers. |
| F0025-S0002 | Security Reviewer | Product Manager | PASS | pm-closeout.md | 2026-07-07 | PM accepted Security Reviewer recommendations at closeout; no Critical or High blockers. |
| F0025-S0003 | Code Reviewer | Product Manager | APPROVED | pm-closeout.md | 2026-07-07 | PM accepted Code Reviewer recommendations at closeout; no Critical or High blockers. |
| F0025-S0003 | Security Reviewer | Product Manager | PASS | pm-closeout.md | 2026-07-07 | PM accepted Security Reviewer recommendations at closeout; no Critical or High blockers. |
| F0025-S0004 | Code Reviewer | Product Manager | APPROVED | pm-closeout.md | 2026-07-07 | PM accepted Code Reviewer recommendations at closeout; no Critical or High blockers. |
| F0025-S0004 | Security Reviewer | Product Manager | PASS | pm-closeout.md | 2026-07-07 | PM accepted Security Reviewer recommendations at closeout; no Critical or High blockers. |
| F0025-S0005 | Code Reviewer | Product Manager | APPROVED | pm-closeout.md | 2026-07-07 | PM accepted Code Reviewer recommendations at closeout; no Critical or High blockers. |
| F0025-S0005 | Security Reviewer | Product Manager | PASS | pm-closeout.md | 2026-07-07 | PM accepted Security Reviewer recommendations at closeout; no Critical or High blockers. |
| F0025-S0006 | Code Reviewer | Product Manager | APPROVED | pm-closeout.md | 2026-07-07 | PM accepted Code Reviewer recommendations at closeout; no Critical or High blockers. |
| F0025-S0006 | Security Reviewer | Product Manager | PASS | pm-closeout.md | 2026-07-07 | PM accepted Security Reviewer recommendations at closeout; no Critical or High blockers. |

## Phase A Notes

- F0025 was promoted to Now by operator decision on 2026-07-07.
- Completed dependencies are F0017, F0018, and F0028.
- Scope is expected commission visibility, producer split attribution, adjustment capture, and rollup reporting only.
- Full accounting, billing, payments, reconciliation, producer payouts, and external self-service compensation portal are out of scope.

## Phase B Notes

- ADR-032 defines the `CommissionRevenue` module boundary, persisted expected commission review records, effective-dated commission schedules, effective-dated producer split assignments, single-step adjustment approval, source-authorized rollups, and timeline audit requirements.
- G5 approval was recorded on 2026-07-07; feature action may start next through the `nebula-agents` harness.

## Backend Progress

- 2026-07-07: Feature run `2026-07-07-9859bad4` completed G0 and G1, then added the backend F0025 commission revenue slice.
- Added commission domain entities, DTOs, validators, repositories, EF configurations, API endpoints, DI registrations, and migration `20260707150000_F0025_CommissionRevenue`.
- Added focused unit coverage for commission validators and Casbin `commission` policy rows.
- 2026-07-07: Completed G2 implementation validation for run `2026-07-07-9859bad4`.
- Added F0025 frontend workspace/detail routes, sidebar navigation, feature-local hooks/types/tests, KG code-index binding, and revenue attribution projection refresh after calculation/approved adjustment.
- G2 evidence validator passed for stage G2. G3 code/security review should inspect source-record authorization granularity and one-row-per-expected-commission projection granularity.
- 2026-07-07: Completed G3 code/security review for run `2026-07-07-9859bad4`.
- G3 patched source-record visibility for expected commission search/detail/mutations, adjustments, policy splits, and rollups before review signoff.
- Code Review: APPROVED WITH RECOMMENDATIONS. Security Review: PASS WITH RECOMMENDATIONS. No Critical or High blockers remain; Medium follow-ups cover source-scope regression tests, scanner reruns in CI/staging, and projection granularity confirmation.
- Validation evidence:
  - `dotnet build engine/src/Nebula.Application/Nebula.Application.csproj --no-restore -m:1 -v:minimal -nr:false -p:UseSharedCompilation=false` -> PASS
  - `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore -m:1 -v:minimal -nr:false -p:UseSharedCompilation=false` -> PASS with two pre-existing dashboard nullable warnings.
  - `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore -m:1 -v:minimal -nr:false -p:UseSharedCompilation=false --filter "FullyQualifiedName~CommissionRevenue|FullyQualifiedName~CasbinAuthorizationServiceTests.CommissionPolicy"` -> PASS after rerun with testhost socket permission; 22 passed.

## Closeout Summary

- Archived on 2026-07-07 after feature run `2026-07-07-9859bad4` passed G0-G8 under the `nebula-agents` harness.
- Scope delivered: commission workspace search/detail, schedule maintenance, producer split assignment, expected commission calculation review, adjustment request/decision, revenue attribution rollups, authorization enforcement, audit timeline events, and frontend routes.
- Story completion: 6 of 6 stories done.
- Validation: backend build passed; focused backend tests passed with 22 tests; frontend lint/theme/build passed; F0025 frontend Vitest suite passed with 2 tests; KG validation and tracker validation passed.
- Defects fixed during review: G3 patched source-record visibility for expected commission search/detail/mutations, adjustments, policy splits, and rollups.
- Deferred follow-ups accepted for post-closeout hardening: add dedicated source-scope regression tests, rerun dependency/secrets/SAST/DAST scans in CI or staging, confirm multi-participant projection granularity, run staging DAST where feasible, and clean up the unrelated broad frontend localStorage suite issue.
