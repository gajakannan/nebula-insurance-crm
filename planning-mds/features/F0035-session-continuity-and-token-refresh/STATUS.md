# F0035: Session Continuity & Token Refresh Status

**Overall Status:** Draft (planning complete — Phase A + Phase B approved; ready for `agents/actions/feature.md`. Plan run `2026-05-23-41109356`.)
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
- [ ] Security review completed — pending (during feature action; Security Reviewer is Architect-confirmed required role)
- [ ] Implementation plan approved (feature-assembly-plan.md, owned by feature action Step 0)

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0035-S0001 | Silent Token Renewal with Concurrent Request Coalescing | [ ] Not Started |
| F0035-S0002 | Idle Warning Modal with Grace Period | [ ] Not Started |
| F0035-S0003 | Forced Re-Auth with Route and Form State Preservation | [ ] Not Started |
| F0035-S0004 | Auth Error Semantic Distinction (401-expired / 401-failed / 403-denied) | [ ] Not Started |
| F0035-S0005 | Session Continuity Telemetry Events (MVP) | [ ] Not Started |

## Backend Progress

- [ ] Authentication-failure middleware extension to emit ProblemDetails types (S0004)
- [ ] Telemetry ingest endpoint (S0005)
- [ ] Serilog `Nebula.Session.Continuity` category registration (S0005)
- [ ] Per-endpoint 401 contract conformance tests (S0004)
- [ ] Unit tests passing
- [ ] Integration tests passing

## Frontend Progress

- [ ] API client error classifier (S0004)
- [ ] Silent renewal coalescing primitive (S0001)
- [ ] Idle activity-detection hook (S0002)
- [ ] Idle warning modal component (S0002)
- [ ] Route preservation via `return_to` parameter (S0003)
- [ ] Form state snapshot/rehydrate mechanism via sessionStorage (S0003)
- [ ] Telemetry emitter with bounded buffer + retry (S0005)
- [ ] Component/integration tests added
- [ ] Accessibility validation (idle modal — WCAG 2.1 AA via @axe-core/playwright)
- [ ] Coverage artifact recorded
- [ ] Responsive layout verified (idle modal desktop + narrow)
- [ ] Visual regression tests (if applicable)

## Cross-Cutting

- [ ] Seed data (none required — no new entities)
- [ ] Migration(s) applied (none expected — auth/session changes are runtime-only)
- [ ] API documentation updated (S0004 ProblemDetails types; S0005 telemetry endpoint)
- [ ] Runtime validation evidence recorded
- [ ] DevOps preflight: authentik refresh-token issuance enabled on OIDC client
- [ ] No TODOs remain in code

## Required Signoff Roles (Set in Planning)

Architect-confirmed at Phase B B0 (plan run `2026-05-23-41109356`). All four roles below are mandatory before the feature can move from `Done` to `Archived` per `TRACKER-GOVERNANCE.md`.

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Acceptance criteria and test coverage validation across silent renewal (coalescing primitive, throttle), idle modal (monotonic clock + accessibility), forced re-auth (snapshot restore, cross-user isolation, TTL), error classifier (per-endpoint contract conformance matrix), and telemetry (PII boundary assertion). | Architect (confirms PM proposal) | 2026-05-23 |
| Code Reviewer | Yes | Independent code quality and regression review, particularly for: token handling (no PII in telemetry, no raw tokens in console/logs), classifier dispatch logic (defensive default discipline), sessionStorage key-namespacing for cross-user safety, and one-way telemetry coupling (renewal logic never blocks on telemetry health). | Architect (confirms PM proposal) | 2026-05-23 |
| Security Reviewer | Yes | F0035 modifies the authentication boundary (silent renewal, idle session lifecycle, forced re-auth path) and introduces a new sessionStorage data class (form-state snapshots that may transiently include `InternalOnly` fields per ADR-024 Security & Compliance Notes). Required to: (a) sign off that auth-error response classification is not an information leak per OWASP; (b) confirm the sessionStorage snapshot boundary is acceptable for MVP or upgrade the Phase 2 form classifier to MVP-required; (c) confirm refresh-token frontend-mediated transport remains acceptable. | Architect (upgrades PM proposal to confirmed-required) | 2026-05-23 |
| DevOps | Yes | (a) Preflight verification that authentik OIDC client has refresh-token issuance enabled — without it, 100% of silent renewals will fail on first deploy. (b) Register `Nebula.Session.Continuity` Serilog category in the F0033 baseline. (c) Verify `/internal/telemetry/session-continuity` is reachable only from authenticated callers (no public surface). | Architect (confirms PM proposal) | 2026-05-23 |
| Architect | No | No anticipated architecture-risk exceptions beyond what ADR-024 already captures. If feature-action discovers a deviation from ADR-024 (e.g. backend-mediated refresh becomes necessary, multi-tab coordination is required for correctness), Architect re-engages and signoff becomes required at that point. | Architect | 2026-05-23 |

## Story Signoff Provenance

This table is initialized empty by PM Phase A. Rows are append-only and added by implementers/reviewers during build. Per `feature-evidence-package-standardization-plan-v2.md` §16, the current verdict per `(story, role)` is the latest row.

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|

## Deferred Scope

(none — Phase A is intentionally comprehensive for MVP delivery; Phase 2 candidates are listed below)

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
- [ ] Every required signoff role will have story-level `PASS` entries before archival (deferred to build/closeout)

## Archival Criteria

Per framework Definition of Done; archival happens at the end of the build action for F0035, not at plan close.

## Plan Run Reference

- Plan run id: `2026-05-23-41109356`
- Plan run evidence: `planning-mds/operations/evidence/2026-05-23-41109356/`
- Base run files: README.md, action-context.md, artifact-trace.md, gate-decisions.md, commands.log, lifecycle-gates.log
