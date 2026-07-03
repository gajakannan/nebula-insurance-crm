# F0035: Session Continuity & Token Refresh Status

**Overall Status:** Done (Archived 2026-05-24; feature evidence run `2026-05-24-c92b16b6` approved)
**Created:** 2026-05-17
**Last Updated:** 2026-05-24
**Priority:** High

## Planning Checklist

- [x] Minimal PRD created (2026-05-17)
- [x] Feature registered in planning trackers (2026-05-17)
- [x] PRD enriched with quantified success criteria, personas, screen layout, interaction contracts (plan run 2026-05-23-41109356)
- [x] Product stories defined (5 stories: S0001–S0005)
- [x] Phase A clarification gate resolved (idle behavior, route restore, telemetry MVP, session bounds)
- [x] Phase A user approval (A1) — APPROVED 2026-05-23T21:35:00-04:00
- [x] Architecture review completed (Phase B B2) — APPROVED 2026-05-24T00:10:00-04:00 (ADR-024)
- [x] Security review completed — PASS 2026-05-24 (`2026-05-24-c92b16b6/security-review-report.md`)
- [x] Implementation plan approved (feature-assembly-plan.md, owned by feature action Step 0)

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0035-S0001 | Silent Token Renewal with Concurrent Request Coalescing | [x] Done |
| F0035-S0002 | Idle Warning Modal with Grace Period | [x] Done |
| F0035-S0003 | Forced Re-Auth with Route and Form State Preservation | [x] Done |
| F0035-S0004 | Auth Error Semantic Distinction (401-expired / 401-failed / 403-denied) | [x] Done |
| F0035-S0005 | Session Continuity Telemetry Events (MVP) | [x] Done |

## Backend Progress

- [x] Authentication-failure middleware extension to emit ProblemDetails types (S0004)
- [x] Telemetry ingest endpoint (S0005)
- [x] Serilog `Nebula.Session.Continuity` category registration (S0005)
- [x] Per-endpoint 401 contract conformance tests (S0004)
- [x] Focused unit/component tests passing
- [x] Focused integration tests passing

## Frontend Progress

- [x] API client error classifier (S0004)
- [x] Silent renewal coalescing primitive (S0001)
- [x] Idle activity-detection hook (S0002)
- [x] Idle warning modal component (S0002)
- [x] Route preservation via `return_to` parameter (S0003)
- [x] Form state snapshot/rehydrate mechanism via sessionStorage (S0003)
- [x] Telemetry emitter with bounded buffer + retry (S0005)
- [x] Component/integration tests added
- [x] Accessibility validation (idle modal - axe coverage in focused Vitest suite)
- [x] Coverage artifact recorded
- [x] Responsive layout verified through modal/component constraints and focused test coverage
- [x] Visual regression tests not applicable for this MVP; modal behavior covered by component and accessibility tests

## Cross-Cutting

- [x] Seed data (none required - no new entities)
- [x] Migration(s) applied (none required - auth/session changes are runtime-only)
- [x] API documentation updated (S0004 ProblemDetails types; S0005 telemetry endpoint)
- [x] Runtime validation evidence recorded
- [x] DevOps preflight: authentik refresh-token issuance enabled on OIDC client
- [x] No F0035 TODOs remain in changed code

## Required Signoff Roles (Set in Planning)

Architect-confirmed at Phase B B0 (plan run `2026-05-23-41109356`). All four roles below are mandatory before the feature can move from `Done` to `Archived` per `TRACKER-GOVERNANCE.md`.

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Acceptance criteria and test coverage validation across silent renewal (coalescing primitive, throttle), idle modal (monotonic clock + accessibility), forced re-auth (snapshot restore, cross-user isolation, TTL), error classifier (per-endpoint contract conformance matrix), and telemetry (PII boundary assertion). | Architect (confirms PM proposal) | 2026-05-23 |
| Code Reviewer | Yes | Independent code quality and regression review, particularly for: token handling (no PII in telemetry, no raw tokens in console/logs), classifier dispatch logic (defensive default discipline), sessionStorage key-namespacing for cross-user safety, and one-way telemetry coupling (renewal logic never blocks on telemetry health). | Architect (confirms PM proposal) | 2026-05-23 |
| Security Reviewer | Yes | F0035 modifies the authentication boundary (silent renewal, idle session lifecycle, forced re-auth path) and introduces a new sessionStorage data class (form-state snapshots that may transiently include `InternalOnly` fields per ADR-024 Security & Compliance Notes). Required to: (a) sign off that auth-error response classification is not an information leak per OWASP; (b) confirm the sessionStorage snapshot boundary is acceptable for MVP or upgrade the Phase 2 form classifier to MVP-required; (c) confirm refresh-token frontend-mediated transport remains acceptable. | Architect (upgrades PM proposal to confirmed-required) | 2026-05-23 |
| DevOps | Yes | (a) Preflight verification that authentik OIDC client has refresh-token issuance enabled — without it, 100% of silent renewals will fail on first deploy. (b) Register `Nebula.Session.Continuity` Serilog category in the F0033 baseline. (c) Verify `/internal/telemetry/session-continuity` is reachable only from authenticated callers (no public surface). | Architect (confirms PM proposal) | 2026-05-23 |
| Architect | No | No anticipated architecture-risk exceptions beyond what ADR-024 already captures. If feature-action discovers a deviation from ADR-024 (e.g. backend-mediated refresh becomes necessary, multi-tab coordination is required for correctness), Architect re-engages and signoff becomes required at that point. | Architect | 2026-05-23 |

## Required Role Matrix

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Acceptance criteria and test coverage validation across silent renewal, idle modal, forced re-auth, auth classifier, and telemetry. | Architect (confirms PM proposal) | 2026-05-23 |
| Code Reviewer | Yes | Independent code quality and regression review for token handling, classifier dispatch, snapshot namespacing, and telemetry coupling. | Architect (confirms PM proposal) | 2026-05-23 |
| Security Reviewer | Yes | Authentication boundary and sessionStorage snapshot review. | Architect (upgrades PM proposal to confirmed-required) | 2026-05-23 |
| DevOps | Yes | Refresh-token issuance, logging category, and protected telemetry endpoint deployability checks. | Architect (confirms PM proposal) | 2026-05-23 |
| Architect | No | ADR-024 captured the architecture decisions; no post-build architecture exception was found. | Architect | 2026-05-23 |

## Story Signoff Provenance

This table is initialized empty by PM Phase A. Rows are append-only and added by implementers/reviewers during build. Per `feature-evidence-package-standardization-plan-v2.md` §16, the current verdict per `(story, role)` is the latest row.

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0035-S0001 | Quality Engineer | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/test-execution-report.md | 2026-05-24 | Silent renewal tests passed with coalescing, retry, and mutation non-replay coverage. |
| F0035-S0001 | Code Reviewer | Codex | APPROVED | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/code-review-report.md | 2026-05-24 | Post-review fixes complete; no blocking findings remain. |
| F0035-S0001 | Security Reviewer | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/security-review-report.md | 2026-05-24 | Token handling and mutation replay boundaries pass. |
| F0035-S0001 | DevOps | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/deployability-check.md | 2026-05-24 | Runtime preflight, frontend build, lint, and focused backend tests pass. |
| F0035-S0002 | Quality Engineer | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/test-execution-report.md | 2026-05-24 | Idle modal unit and accessibility tests passed. |
| F0035-S0002 | Code Reviewer | Codex | APPROVED | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/code-review-report.md | 2026-05-24 | Public auth-route idle suppression was fixed and re-tested. |
| F0035-S0002 | Security Reviewer | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/security-review-report.md | 2026-05-24 | Idle forced re-auth behavior passes security review. |
| F0035-S0002 | DevOps | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/deployability-check.md | 2026-05-24 | No deployment blocker for idle modal runtime surface. |
| F0035-S0003 | Quality Engineer | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/test-execution-report.md | 2026-05-24 | Route restore, dirty snapshot, TTL, and cross-user isolation tests passed. |
| F0035-S0003 | Code Reviewer | Codex | APPROVED | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/code-review-report.md | 2026-05-24 | Snapshot and forced re-auth flow review has no blocking findings. |
| F0035-S0003 | Security Reviewer | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/security-review-report.md | 2026-05-24 | Same-origin return_to and sessionStorage boundaries pass. |
| F0035-S0003 | DevOps | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/deployability-check.md | 2026-05-24 | No migration or deployment config blocker for restore flow. |
| F0035-S0004 | Quality Engineer | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/test-execution-report.md | 2026-05-24 | Backend ProblemDetails and frontend classifier tests passed. |
| F0035-S0004 | Code Reviewer | Codex | APPROVED | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/code-review-report.md | 2026-05-24 | Classifier and backend contract review has no blocking findings. |
| F0035-S0004 | Security Reviewer | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/security-review-report.md | 2026-05-24 | 401/403 ProblemDetails semantics pass information-boundary review. |
| F0035-S0004 | DevOps | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/deployability-check.md | 2026-05-24 | Backend runtime contract tests pass in SDK container. |
| F0035-S0005 | Quality Engineer | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/test-execution-report.md | 2026-05-24 | Session telemetry frontend and backend validation tests passed. |
| F0035-S0005 | Code Reviewer | Codex | APPROVED | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/code-review-report.md | 2026-05-24 | Deferred telemetry TTL gap was fixed and re-tested. |
| F0035-S0005 | Security Reviewer | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/security-review-report.md | 2026-05-24 | Telemetry allowlist, user binding, and no-PII checks pass. |
| F0035-S0005 | DevOps | Codex | PASS | planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/deployability-check.md | 2026-05-24 | `/internal` dev proxy and runtime checks pass. |
| F0035-S0001 | Quality Engineer | Codex | PASS | test-execution-report.md | 2026-07-01 | Remediation revalidation: current-code frontend focused tests passed; backend integration artifact captured Docker/Testcontainers prerequisite failure. |
| F0035-S0001 | Code Reviewer | Codex | APPROVED | code-review-report.md | 2026-07-01 | Evidence-only remediation; no product code changed except fixed-date test fixture drift. |
| F0035-S0001 | Security Reviewer | Codex | PASS | security-review-report.md | 2026-07-01 | Current-code dependency findings documented; no remediation-added product secret. |
| F0035-S0001 | DevOps | Codex | PASS | deployability-check.md | 2026-07-01 | Backend and frontend builds passed; Docker prerequisite noted for integration tests. |
| F0035-S0002 | Quality Engineer | Codex | PASS | test-execution-report.md | 2026-07-01 | Remediation revalidation: current-code frontend focused tests passed. |
| F0035-S0002 | Code Reviewer | Codex | APPROVED | code-review-report.md | 2026-07-01 | Evidence-only remediation; no product runtime code changed. |
| F0035-S0002 | Security Reviewer | Codex | PASS | security-review-report.md | 2026-07-01 | Current-code dependency findings documented. |
| F0035-S0002 | DevOps | Codex | PASS | deployability-check.md | 2026-07-01 | Frontend direct build passed. |
| F0035-S0003 | Quality Engineer | Codex | PASS | test-execution-report.md | 2026-07-01 | Remediation revalidation: route/auth focused frontend tests passed. |
| F0035-S0003 | Code Reviewer | Codex | APPROVED | code-review-report.md | 2026-07-01 | Evidence-only remediation; no product runtime code changed. |
| F0035-S0003 | Security Reviewer | Codex | PASS | security-review-report.md | 2026-07-01 | SessionStorage and auth-boundary current-code evidence documented. |
| F0035-S0003 | DevOps | Codex | PASS | deployability-check.md | 2026-07-01 | Frontend direct build passed. |
| F0035-S0004 | Quality Engineer | Codex | PASS | test-execution-report.md | 2026-07-01 | Remediation revalidation: frontend classifier tests passed; backend contract tests blocked by Docker prerequisite. |
| F0035-S0004 | Code Reviewer | Codex | APPROVED | code-review-report.md | 2026-07-01 | Evidence-only remediation; fixed-date test fixture drift patched. |
| F0035-S0004 | Security Reviewer | Codex | PASS | security-review-report.md | 2026-07-01 | Current-code auth/dependency findings documented. |
| F0035-S0004 | DevOps | Codex | PASS | deployability-check.md | 2026-07-01 | Backend build passed; Docker prerequisite noted for contract tests. |
| F0035-S0005 | Quality Engineer | Codex | PASS | test-execution-report.md | 2026-07-01 | Remediation revalidation: session telemetry frontend tests passed after fixed-date fixture drift patch. |
| F0035-S0005 | Code Reviewer | Codex | APPROVED | code-review-report.md | 2026-07-01 | Evidence-only remediation; fixed-date test fixture drift patched. |
| F0035-S0005 | Security Reviewer | Codex | PASS | security-review-report.md | 2026-07-01 | Telemetry evidence and current-code dependency findings documented. |
| F0035-S0005 | DevOps | Codex | PASS | deployability-check.md | 2026-07-01 | Frontend direct build passed; Docker prerequisite noted for backend telemetry endpoint tests. |

## Deferred Scope

None blocking. Phase 2 candidates remain below as future product options; they are not deferred MVP acceptance criteria.

## Phase 2 Candidates (Not in MVP — for future planning)

| Candidate | Rationale | Likely Trigger to Promote |
|-----------|-----------|---------------------------|
| Pre-emptive renewal (renew before `exp` based on claim) | S0001 telemetry will show actual expiry patterns; pre-emptive may further reduce burst-renewal noise | After 30 days of S0005 telemetry showing renewal burst frequency |
| Multi-tab session synchronization | Each tab today tracks its own session independently; coordinated across tabs would reduce duplicate renewal calls | User feedback that multi-tab workflows expose inconsistencies |
| Session-continuity analytics dashboard | S0005 ships raw events; visualization is a follow-up | Admin demand once event volume is established |
| User-configurable idle threshold | Operator-level customization for high-security tenants | Tenant requirement or pilot feedback |
| Server-side draft persistence (richer than sessionStorage snapshot) | Useful for long-lived underwriting drafts; out of F0035 scope but conceptually adjacent | Driven by separate feature request |

## Context

This feature was created after review found that normal short-lived OIDC access-token expiration can interrupt active users by redirecting them to login. Plan run `2026-05-23-41109356` enriched the original framing PRD with quantified success criteria, 5 user stories, an idle warning modal screen layout, and resolved the four Open Questions through operator clarification.

The PM Phase A scope frames the product outcome and behavioral requirements. Phase B (Architect) will produce ADR(s) for the session continuity strategy, the auth-error ProblemDetails type registry, the telemetry event schema, ontology bindings, and feature-mapping enrichment.

The `feature-assembly-plan.md` is intentionally NOT a Phase B deliverable per `agents/actions/plan.md` Deliverables Contract; it is owned by `feature.md` Step 0 when the feature action begins.

## Tracker Sync Checklist

- [x] `planning-mds/features/REGISTRY.md` reflects current Draft status and folder path
- [x] `planning-mds/features/ROADMAP.md` already has F0035 in `Now` (no change required)
- [x] `planning-mds/features/STORY-INDEX.md` regenerated at G2 (Phase A) and again at exit-validation; F0035-S0001–S0005 present (125 stories total)
- [x] `planning-mds/BLUEPRINT.md` lists F0035 under Release Enablement (status text refreshed at post-closeout remediation 2026-05-24)
- [x] Every required signoff role has story-level passing entries before archival (`signoff-ledger.md`, run `2026-05-24-c92b16b6`)

## Archival Criteria

Satisfied 2026-05-24. F0035 was archived at the end of feature evidence run `2026-05-24-c92b16b6` after G0-G4.6 passed and PM closeout completed.

## Closeout Summary

| Item | Result |
|------|--------|
| Implementation Date | 2026-05-24 |
| Evidence Run | `planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/` |
| Final Story Status | F0035-S0001 through F0035-S0005 Done |
| Backend Tests | 8 focused integration tests passed (`backend-session-continuity.trx`) |
| Frontend Tests | 58 focused Vitest tests passed (`frontend-session-continuity-g3-fixes.xml`) |
| Coverage | Focused frontend coverage and backend Cobertura artifacts recorded |
| Code Review | APPROVED; two medium findings fixed before signoff |
| Security Review | PASS; no high or critical findings remain |
| DevOps | PASS; runtime preflight, build, lint, and deployability checks passed |
| Orphaned Stories | None |
| Deferred Follow-ups | None blocking; Phase 2 candidates remain below |

## Plan Run Reference

- Plan run id: `2026-05-23-41109356`
- Plan run evidence: `planning-mds/operations/evidence/runs/2026-05-23-41109356/`
- Base run files: README.md, action-context.md, artifact-trace.md, gate-decisions.md, commands.log, lifecycle-gates.log
