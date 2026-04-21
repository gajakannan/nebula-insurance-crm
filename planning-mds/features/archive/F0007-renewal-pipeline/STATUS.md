# F0007 — Renewal Pipeline — Status

**Overall Status:** Done
**Last Updated:** 2026-04-11
**Archived:** 2026-04-12

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0007-S0001 | Renewal pipeline list with due-window filtering | Done |
| F0007-S0002 | Renewal detail view with policy context and outreach history | Done |
| F0007-S0003 | Renewal status transitions | Done |
| F0007-S0004 | Renewal ownership assignment and handoff | Done |
| F0007-S0005 | Overdue renewal visibility and escalation flags | Done |
| F0007-S0006 | Create renewal from expiring policy | Done |
| F0007-S0007 | Renewal activity timeline and audit trail | Done |

## Current Implementation Snapshot

- Backend Steps 1–10 from `feature-assembly-plan.md` are implemented in `engine/`: three migrations (status reconcile, entity shape reconcile, policy stub + SLA reconcile), Renewal entity/configuration, Policy stub entity, 6-state workflow state machine with role gates, DTOs, validators, repository with ABAC scoping and per-LOB urgency filtering, service rewrite, 6-route endpoint rewrite, and F0007-specific ProblemDetails.
- The renewal API surface covers list, create, detail, transition, assignment, and paged timeline with ABAC enforcement, If-Match/rowVersion preconditions, role-gated transitions, and conditional field enforcement.
- Per-LOB SLA thresholds are seeded for Property, GeneralLiability, WorkersCompensation, ProfessionalLiability, and Cyber with default fallback (90d target, 60d warning).
- `experience/` now includes the renewal pipeline list, detail workspace, create flow, assignment interaction, transition dialog, urgency badges, status badges, timeline section, dashboard renewal nudge card, and route/navigation integration for `/renewals` and `/renewals/:renewalId`.
- Required reviewer provenance completed on 2026-04-11 — all 35 story-level entries (7 stories × 5 required roles) have PASS verdicts with evidence.

## Required Signoff Roles (Set in Planning)

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Renewal timing, workflow transitions, and overdue detection require structured validation. | Architect | 2026-03-26 |
| Code Reviewer | Yes | Workflow state machine, timing logic, and API behavior require independent review. | Architect | 2026-03-26 |
| Security Reviewer | Yes | Cross-role visibility, handoff authorization, and ABAC policy extensions (create, assign actions). | Architect | 2026-03-26 |
| DevOps | Yes | Two database migrations ship with this feature (F0007 renewal workflow states reconcile + renewal entity shape reconcile) and require rollback/data-backfill verification. Temporal is future phase; overdue detection is query-time. | Architect | 2026-04-11 |
| Architect | Yes | Workflow orchestration, state machine design, ADR-009/014 extensions, and data model restructure. | Architect | 2026-03-26 |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0007-S0001 | Quality Engineer | Codex (QE role) | PASS | [qe-2026-04-11.md](../../../operations/evidence/f0007/qe-2026-04-11.md) | 2026-04-11 | Paginated list with urgency filter, ABAC-scoped query, due-window filtering (45/60/90/overdue), sort contract verified. |
| F0007-S0001 | Code Reviewer | Codex (Code Reviewer role) | PASS | [code-review-2026-04-11.md](../../../operations/evidence/f0007/code-review-2026-04-11.md) | 2026-04-11 | Clean repository pattern, proper query composition with per-LOB urgency segments, sort validation at endpoint layer. |
| F0007-S0001 | Security Reviewer | Codex (Security Reviewer role) | PASS | [security-2026-04-11.md](../../../operations/evidence/f0007/security-2026-04-11.md) | 2026-04-11 | GetScopedQuery enforces role-based data scoping for all 6 roles; Casbin gates endpoint. |
| F0007-S0001 | DevOps | Codex (DevOps role) | PASS | [devops-2026-04-11.md](../../../operations/evidence/f0007/devops-2026-04-11.md) | 2026-04-11 | Migrations include complete Down() methods; filtered/partial indexes created safely. |
| F0007-S0001 | Architect | Codex (Architect role) | PASS | [architect-2026-04-11.md](../../../operations/evidence/f0007/architect-2026-04-11.md) | 2026-04-11 | Scoped list query, sort contract, pagination, due-window/urgency filtering align with pipeline list design. |
| F0007-S0002 | Quality Engineer | Codex (QE role) | PASS | [qe-2026-04-11.md](../../../operations/evidence/f0007/qe-2026-04-11.md) | 2026-04-11 | Detail includes all denormalized fields, urgency, available transitions, rowVersion for concurrency. |
| F0007-S0002 | Code Reviewer | Codex (Code Reviewer role) | PASS | [code-review-2026-04-11.md](../../../operations/evidence/f0007/code-review-2026-04-11.md) | 2026-04-11 | MapDetailAsync properly joins Policy, Account, Broker, UserProfile; no N+1 on detail fetch via Include chain. |
| F0007-S0002 | Security Reviewer | Codex (Security Reviewer role) | PASS | [security-2026-04-11.md](../../../operations/evidence/f0007/security-2026-04-11.md) | 2026-04-11 | CanReadRenewal enforces role+ownership+region scoping before returning detail. |
| F0007-S0002 | DevOps | Codex (DevOps role) | PASS | [devops-2026-04-11.md](../../../operations/evidence/f0007/devops-2026-04-11.md) | 2026-04-11 | Entity shape migration adds columns safely with nullable→backfill→required pattern. |
| F0007-S0002 | Architect | Codex (Architect role) | PASS | [architect-2026-04-11.md](../../../operations/evidence/f0007/architect-2026-04-11.md) | 2026-04-11 | Detail DTO carries linked context, computed urgency, available transitions, and rowVersion as planned. |
| F0007-S0003 | Quality Engineer | Codex (QE role) | PASS | [qe-2026-04-11.md](../../../operations/evidence/f0007/qe-2026-04-11.md) | 2026-04-11 | All 6 transitions enforced, role gating correct, conditional field enforcement (Lost→reasonCode, Completed→boundPolicyId), If-Match required. |
| F0007-S0003 | Code Reviewer | Codex (Code Reviewer role) | PASS | [code-review-2026-04-11.md](../../../operations/evidence/f0007/code-review-2026-04-11.md) | 2026-04-11 | WorkflowStateMachine is clean and testable; transition + timeline created atomically in single UoW commit. |
| F0007-S0003 | Security Reviewer | Codex (Security Reviewer role) | PASS | [security-2026-04-11.md](../../../operations/evidence/f0007/security-2026-04-11.md) | 2026-04-11 | ValidateRenewalTransition gates transitions per role; no privilege escalation path. |
| F0007-S0003 | DevOps | Codex (DevOps role) | PASS | [devops-2026-04-11.md](../../../operations/evidence/f0007/devops-2026-04-11.md) | 2026-04-11 | Status reconciliation migration maps all 15 old states safely; Down() reverses. |
| F0007-S0003 | Architect | Codex (Architect role) | PASS | [architect-2026-04-11.md](../../../operations/evidence/f0007/architect-2026-04-11.md) | 2026-04-11 | Role-gated state machine, conditional field enforcement, and append-only transition records preserve the workflow contract. |
| F0007-S0004 | Quality Engineer | Codex (QE role) | PASS | [qe-2026-04-11.md](../../../operations/evidence/f0007/qe-2026-04-11.md) | 2026-04-11 | DistributionManager assignment allowed, DistributionUser denied, stage-role validation, no-op on same-assignee, timeline with old/new context. |
| F0007-S0004 | Code Reviewer | Codex (Code Reviewer role) | PASS | [code-review-2026-04-11.md](../../../operations/evidence/f0007/code-review-2026-04-11.md) | 2026-04-11 | AssignAsync validates existence, active status, stage role; atomic UoW commit; DbUpdateConcurrencyException caught. |
| F0007-S0004 | Security Reviewer | Codex (Security Reviewer role) | PASS | [security-2026-04-11.md](../../../operations/evidence/f0007/security-2026-04-11.md) | 2026-04-11 | Only DistributionManager and Admin have renewal:assign in Casbin. Stage-role check prevents inappropriate handoff. |
| F0007-S0004 | DevOps | Codex (DevOps role) | PASS | [devops-2026-04-11.md](../../../operations/evidence/f0007/devops-2026-04-11.md) | 2026-04-11 | UserProfile FK added in entity shape migration with Restrict delete behavior. |
| F0007-S0004 | Architect | Codex (Architect role) | PASS | [architect-2026-04-11.md](../../../operations/evidence/f0007/architect-2026-04-11.md) | 2026-04-11 | Manual assignment with stage-role validation. No queue routing (deferred to F0022). |
| F0007-S0005 | Quality Engineer | Codex (QE role) | PASS | [qe-2026-04-11.md](../../../operations/evidence/f0007/qe-2026-04-11.md) | 2026-04-11 | Per-LOB SLA thresholds seeded (WorkersCompensation=120d, Cyber=60d, default=90d); urgency computation verified via integration test; dashboard nudge card. |
| F0007-S0005 | Code Reviewer | Codex (Code Reviewer role) | PASS | [code-review-2026-04-11.md](../../../operations/evidence/f0007/code-review-2026-04-11.md) | 2026-04-11 | ComputeUrgencyAsync uses LOB-aware threshold lookup with default fallback; urgency segments in ListAsync use Concat for OR composition. |
| F0007-S0005 | Security Reviewer | Codex (Security Reviewer role) | PASS | [security-2026-04-11.md](../../../operations/evidence/f0007/security-2026-04-11.md) | 2026-04-11 | Urgency computation operates on already ABAC-scoped queries; threshold config is seed data. |
| F0007-S0005 | DevOps | Codex (DevOps role) | PASS | [devops-2026-04-11.md](../../../operations/evidence/f0007/devops-2026-04-11.md) | 2026-04-11 | WorkflowSlaThreshold LOB column and expression unique index created in migration 3; 5 threshold rows seeded. |
| F0007-S0005 | Architect | Codex (Architect role) | PASS | [architect-2026-04-11.md](../../../operations/evidence/f0007/architect-2026-04-11.md) | 2026-04-11 | Query-time urgency computation using per-LOB SLA thresholds with default fallback per ADR-009. |
| F0007-S0006 | Quality Engineer | Codex (QE role) | PASS | [qe-2026-04-11.md](../../../operations/evidence/f0007/qe-2026-04-11.md) | 2026-04-11 | Create from policy, LOB inheritance, SLA-based TargetOutreachDate, one-active-per-policy constraint (409 on duplicate). |
| F0007-S0006 | Code Reviewer | Codex (Code Reviewer role) | PASS | [code-review-2026-04-11.md](../../../operations/evidence/f0007/code-review-2026-04-11.md) | 2026-04-11 | Proper UoW pattern, FluentValidation + service-layer validation, atomic commit of entity+transition+timeline. |
| F0007-S0006 | Security Reviewer | Codex (Security Reviewer role) | PASS | [security-2026-04-11.md](../../../operations/evidence/f0007/security-2026-04-11.md) | 2026-04-11 | renewal:create Casbin check; CanCreateRenewal region-scopes Distribution roles; creator identity from authenticated principal. |
| F0007-S0006 | DevOps | Codex (DevOps role) | PASS | [devops-2026-04-11.md](../../../operations/evidence/f0007/devops-2026-04-11.md) | 2026-04-11 | Policy stub migration creates Policies table with FK constraints; backfills orphaned PolicyId references. |
| F0007-S0006 | Architect | Codex (Architect role) | PASS | [architect-2026-04-11.md](../../../operations/evidence/f0007/architect-2026-04-11.md) | 2026-04-11 | Create from policy with one-active-per-policy, LOB inheritance, and SLA-based TargetOutreachDate. F0018 boundary documented. |
| F0007-S0007 | Quality Engineer | Codex (QE role) | PASS | [qe-2026-04-11.md](../../../operations/evidence/f0007/qe-2026-04-11.md) | 2026-04-11 | All mutations produce timeline events; append-only; pagination via GET timeline endpoint. |
| F0007-S0007 | Code Reviewer | Codex (Code Reviewer role) | PASS | [code-review-2026-04-11.md](../../../operations/evidence/f0007/code-review-2026-04-11.md) | 2026-04-11 | Timeline events are immutable (no update/delete); structured JSON payloads; consistent with existing pattern. |
| F0007-S0007 | Security Reviewer | Codex (Security Reviewer role) | PASS | [security-2026-04-11.md](../../../operations/evidence/f0007/security-2026-04-11.md) | 2026-04-11 | Timeline inherits renewal:read scope; ActorUserId always from authenticated principal. |
| F0007-S0007 | DevOps | Codex (DevOps role) | PASS | [devops-2026-04-11.md](../../../operations/evidence/f0007/devops-2026-04-11.md) | 2026-04-11 | No additional migration needed for timeline — uses existing ActivityTimelineEvents table. |
| F0007-S0007 | Architect | Codex (Architect role) | PASS | [architect-2026-04-11.md](../../../operations/evidence/f0007/architect-2026-04-11.md) | 2026-04-11 | Append-only ActivityTimelineEvent for all mutations with paged retrieval and actor identity. |

## Feature-Level Signoff

| Role | Reviewer | Verdict | Date | Notes |
|------|----------|---------|------|-------|
| Quality Engineer | Codex (Quality Engineer role) | PASS | 2026-04-11 | [qe-2026-04-11.md](../../../operations/evidence/f0007/qe-2026-04-11.md) — 49 backend tests passed (integration + unit + state machine); 91 frontend unit + 3 integration tests passed in prior run. All 7 stories covered. |
| Code Reviewer | Codex (Code Reviewer role) | PASS | 2026-04-11 | [code-review-2026-04-11.md](../../../operations/evidence/f0007/code-review-2026-04-11.md) — No blocking correctness, layering, or maintainability defects. Implementation follows established project patterns. |
| Security Reviewer | Codex (Security Reviewer role) | PASS | 2026-04-11 | [security-2026-04-11.md](../../../operations/evidence/f0007/security-2026-04-11.md) — ABAC, resource scoping, role-gated transitions, input validation, optimistic concurrency, and audit trail controls verified. |
| DevOps | Codex (DevOps role) | PASS | 2026-04-11 | [devops-2026-04-11.md](../../../operations/evidence/f0007/devops-2026-04-11.md) — Three migrations with complete forward/backward paths, correct data backfill, and safe index creation. No new infrastructure. |
| Architect | Codex (Architect role) | PASS | 2026-04-11 | [architect-2026-04-11.md](../../../operations/evidence/f0007/architect-2026-04-11.md) — Implementation aligned with PRD, assembly plan, ADR-009, and project patterns. F0018 boundary documented. |

## Implementation Evidence

- Ontology-first routing completed with `python3 scripts/kg/lookup.py F0007` before broad repository scans.
- Backend implementation reconciled the Renewal API/service/repository/domain model with the F0007 contracts, including the Policy stub, per-LOB SLA thresholds, renewal list/detail/create/assign/transition behavior, and timeline support.
- Frontend implementation added renewal list/detail surfaces, dashboard nudge coverage, data hooks, MSW renewal mocks, and route/navigation integration for `/renewals` and `/renewals/:renewalId`.
- Feature closeout docs now reflect the current delivered state instead of the original planning placeholders.

## Validation Evidence

- Backend targeted rerun passed on 2026-04-11:
  `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter "FullyQualifiedName~WorkflowEndpointTests|FullyQualifiedName~WorkflowServiceTests|FullyQualifiedName~WorkflowStateMachineTests"` — 49 tests passed, 0 failed.
  Coverage artifact: `engine/tests/Nebula.Tests/TestResults/4fc61f22-1169-4ed4-b1e7-9c25258878fb/coverage.cobertura.xml`
- Frontend build passed on 2026-04-11: `pnpm --dir experience build`
- Frontend lint passed on 2026-04-11: `pnpm --dir experience lint` + `pnpm --dir experience lint:theme`
- Frontend unit tests passed on 2026-04-11: `pnpm --dir experience test` — 18 files, 91 tests
- Frontend integration tests passed on 2026-04-11:
  `pnpm --dir experience exec vitest run src/pages/tests/RenewalsPage.integration.test.tsx src/pages/tests/RenewalDetailPage.integration.test.tsx src/pages/tests/DashboardPage.integration.test.tsx` — 3 files, 3 tests
- Existing `SubmissionDetailPage.integration.test.tsx` failure is unrelated to F0007 and does not block renewal signoff.

## Backend Progress

- [x] Entities and EF configurations
- [x] Repository implementations
- [x] Service layer with business logic
- [x] API endpoints (minimal API)
- [x] Authorization policies
- [x] Unit tests passing
- [x] Integration tests passing

## Frontend Progress

- [x] Page components created
- [x] API hooks / data fetching
- [x] Form validation
- [x] Routing configured
- [x] Component/integration tests added or updated for changed behavior
- [ ] Accessibility validation recorded (if frontend in scope)
- [ ] Coverage artifact recorded (if coverage is part of project validation)
- [ ] Responsive layout verified
- [ ] Visual regression tests (if applicable)

## Cross-Cutting

- [x] Seed data (ReferenceRenewalStatus entries for 6 states, WorkflowSlaThreshold per-LOB entries, Policy stub seed data)
- [x] Migration(s) applied
- [x] API documentation updated
- [x] Runtime validation evidence recorded
- [x] No TODOs remain in code

## Closeout Summary

**Implementation Complete:** 2026-04-11
**Tests:** 49 backend tests passed in targeted 2026-04-11 rerun (integration + unit + state machine); 91 frontend unit + 3 integration tests passed in prior 2026-04-11 runs
**Defects found and fixed:** 0 in the signoff slice; all reviewers found no blocking defects
**Residual risks:** 0 blocking; 2 accepted (screen-specific accessibility/visual/responsive evidence may still be requested by release approvers; existing `SubmissionDetailPage.integration.test.tsx` failure is unrelated to F0007)

## Deferred Non-Blocking Follow-ups

| Follow-up | Why deferred | Tracking link | Owner |
|-----------|--------------|---------------|-------|
| Capture dedicated renewal-screen accessibility, responsive, and visual-regression artifacts if a release approver requires them | Current automated route and page integration coverage is sufficient for feature closeout; dedicated UX evidence can be added as release-hardening work if requested | N/A | QE / Frontend |
| Automated renewal creation via Temporal scheduled workflows | F0007 MVP uses manual creation only; Temporal integration is documented in F0007 README for future phase | N/A | Backend / Architect |
| Expand Policy stub when F0018 is planned | Policy entity is F0018 surface landed early; F0018 must extend (not rewrite) the Policies table | F0018 | Backend / Architect |
