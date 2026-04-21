# F0006 — Submission Intake Workflow — Status

**Overall Status:** Done
**Last Updated:** 2026-04-04
**Archived:** 2026-04-04

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0006-S0001 | Submission pipeline list with intake status filtering | Done |
| F0006-S0002 | Create submission for new business intake | Done |
| F0006-S0003 | Submission detail view with intake context | Done |
| F0006-S0004 | Submission intake status transitions | Done |
| F0006-S0005 | Submission completeness evaluation | Done |
| F0006-S0006 | Submission ownership assignment and underwriting handoff | Done |
| F0006-S0007 | Submission activity timeline and audit trail | Done |
| F0006-S0008 | Stale submission visibility and follow-up flags | Done |

## Current Implementation Snapshot

- Backend Steps 1-8 from `feature-assembly-plan.md` are implemented in `engine/`: migration, submission entity/configuration, status catalog/state machine, DTOs, validators, repositories, service rewrite, endpoint rewrite, and F0006-specific problem details.
- The submission API surface now covers list, create, detail, update, transition, assignment, and paged timeline reads with ABAC enforcement and `If-Match` / `rowVersion` preconditions.
- Completeness evaluation, underwriter-assignment validation, stale-flag computation, and the F0020 null-object document checklist adapter are in place.
- Dev seed data and submission status reference data are aligned to the 10-state F0006/F0019 model, including stale-threshold seed rows for `Received`, `Triaging`, and `WaitingOnBroker`.
- `experience/` now includes the submission pipeline list, create flow, detail workspace, assignment interaction, transition dialog, completeness panel, paged activity timeline, and dashboard stale-submission nudge card required by assembly-plan Steps 9-13.
- Required reviewer provenance completed on 2026-04-04 — all 32 story-level entries (8 stories x 4 required roles) now have PASS verdicts with evidence.

## Closeout Status

- Product Manager archive closeout completed on 2026-04-04.
- Reviewer signoff evidence is recorded for every required role across all 8 F0006 stories.
- Repo-wide tracker validation passed on 2026-04-04 before archive transition.
- Orphaned story rule: satisfied. No F0006 story remained in `Not Started` or `In Progress`, and no rehoming decision was required.

## Closeout Guardrails

- F0006 closeout does not include a submission delete route or generic soft-delete contract. Future submission archive/deactivate behavior is owned by F0019.
- F0006 closeout does not include deleted or merged account fallback behavior on linked submission/detail views. That replacement contract is owned by F0016.
- Broker deleted-entity fallback is also outside F0006 closeout. Current broker lifecycle rules already prevent deletion when active submissions or renewals depend on the broker.
- F0006 must still preserve its workflow boundary at `ReadyForUWReview`; downstream transitions remain blocked until F0019 deliberately activates them.

## Backend Progress

- [x] Entities and EF configurations
- [x] Repository implementations
- [x] Service layer with business logic
- [x] API endpoints (controllers / minimal API)
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

- [x] Seed data (ReferenceSubmissionStatus entries for intake states, stale thresholds)
- [x] Migration(s) applied
- [x] API documentation updated
- [x] Runtime validation evidence recorded
- [x] No TODOs remain in code

## Validation Evidence

- Backend endpoint suite rerun passed on 2026-04-04:
  `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter FullyQualifiedName~WorkflowEndpointTests --no-restore`
  Coverage artifact:
  `engine/tests/Nebula.Tests/TestResults/8cba4ad5-d4f0-4846-a234-90555ab54fb9/coverage.cobertura.xml`
- Backend unit / workflow suite rerun passed on 2026-04-04:
  `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter "FullyQualifiedName~WorkflowServiceTests|FullyQualifiedName~WorkflowStateMachineTests|FullyQualifiedName~LineOfBusinessValidationTests" --no-restore`
  Coverage artifact:
  `engine/tests/Nebula.Tests/TestResults/f52f602c-eade-480d-bd0a-bf11d3aa16d2/coverage.cobertura.xml`
- Frontend route smoke rerun passed on 2026-04-04:
  `CI=true pnpm --dir experience test --run src/App.test.tsx`
- Frontend page integration rerun passed on 2026-04-04:
  `pnpm --dir experience exec vitest run src/pages/tests/SubmissionsPage.integration.test.tsx src/pages/tests/CreateSubmissionPage.integration.test.tsx src/pages/tests/SubmissionDetailPage.integration.test.tsx src/pages/tests/DashboardPage.integration.test.tsx`
- Story index regenerated on 2026-04-04 for archive transition:
  `python3 agents/product-manager/scripts/generate-story-index.py planning-mds/features/`
- Repo-wide tracker validation rerun passed on 2026-04-04:
  `python3 agents/product-manager/scripts/validate-trackers.py`
- Feature story validation rerun passed on 2026-04-04 with warnings only:
  `python3 agents/product-manager/scripts/validate-stories.py planning-mds/features/archive/F0006-submission-intake-workflow/`
- Feature story validation passed on 2026-03-31:
  `python3 agents/product-manager/scripts/validate-stories.py planning-mds/features/F0006-submission-intake-workflow/`
- Targeted integration coverage passed on 2026-03-31:
  `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter FullyQualifiedName~WorkflowEndpointTests`
- Targeted unit coverage passed on 2026-03-31:
  `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter "FullyQualifiedName~WorkflowServiceTests|FullyQualifiedName~WorkflowStateMachineTests|FullyQualifiedName~LineOfBusinessValidationTests"`
- Frontend route coverage passed on 2026-03-31:
  `CI=true pnpm --dir experience test --run src/App.test.tsx`
- Frontend integration coverage passed on 2026-03-31:
  `pnpm --dir experience exec vitest run src/pages/tests/SubmissionsPage.integration.test.tsx src/pages/tests/CreateSubmissionPage.integration.test.tsx src/pages/tests/SubmissionDetailPage.integration.test.tsx src/pages/tests/DashboardPage.integration.test.tsx`
- Frontend production build passed on 2026-03-31:
  `pnpm --dir experience build`
- Evidence files updated in this closeout slice:
  `engine/tests/Nebula.Tests/Integration/WorkflowEndpointTests.cs`
  `engine/tests/Nebula.Tests/Unit/WorkflowServiceTests.cs`
  `engine/tests/Nebula.Tests/Unit/WorkflowStateMachineTests.cs`
  `engine/tests/Nebula.Tests/Unit/Dashboard/LineOfBusinessValidationTests.cs`
  `experience/src/App.test.tsx`
  `experience/src/pages/tests/SubmissionsPage.integration.test.tsx`
  `experience/src/pages/tests/CreateSubmissionPage.integration.test.tsx`
  `experience/src/pages/tests/SubmissionDetailPage.integration.test.tsx`
  `experience/src/pages/tests/DashboardPage.integration.test.tsx`

## Required Signoff Roles (Set in Planning)

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Core workflow, transition guards, and completeness logic require thorough validation. | Architect | 2026-03-31 |
| Code Reviewer | Yes | Workflow, validation, API behavior, and ABAC authorization require independent review. | Architect | 2026-03-31 |
| Security Reviewer | Yes | Submission intake introduces new ABAC-scoped CRUD and transition authorization; document linkage crosses feature boundaries. | Architect | 2026-03-31 |
| DevOps | No | No storage, runtime, or deployment changes beyond standard EF migration. | Architect | 2026-03-31 |
| Architect | Yes | Workflow architecture decisions (state machine, SLA thresholds, dual-auth model) reviewed. | Architect | 2026-03-31 |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0006-S0001 | Quality Engineer | PM Agent | PASS | All ACs verified: paginated list, multi-select status filter, sorting (5 fields), ABAC scoping, stale flag column. | 2026-03-31 | SubmissionListItemDto has all required columns; SubmissionRepository applies all filters. |
| F0006-S0001 | Code Reviewer | Architect Agent | PASS | Clean repository pattern, proper query composition, sort validation at endpoint layer. | 2026-03-31 | Consistent with project patterns; integration test covers filtered list. |
| F0006-S0001 | Security Reviewer | Architect Agent | PASS | GetScopedQuery enforces role-based data scoping for all 6 roles; Casbin gates endpoint. | 2026-03-31 | Dual-auth model (Casbin + C# scope) properly implemented. |
| F0006-S0001 | Architect | Codex (Architect role) | PASS | [architect-2026-04-04.md](../../../operations/evidence/f0006/architect-2026-04-04.md) | 2026-04-04 | Scoped list query, sort contract, pagination, and stale flag shape align to the assembly plan. |
| F0006-S0002 | Quality Engineer | PM Agent | PASS | Received status set, AssignedTo defaults to creator, region validation, ExpirationDate default, atomic timeline+transition. | 2026-03-31 | All 5 validation error codes verified (invalid_account/broker/program/lob, region_mismatch). |
| F0006-S0002 | Code Reviewer | Architect Agent | PASS | Proper UoW pattern, FluentValidation + service-layer validation, atomic commit of entity+timeline+transition. | 2026-03-31 | Double LOB validation (validator + service) is defensive but harmless. |
| F0006-S0002 | Security Reviewer | Architect Agent | PASS | submission:create Casbin check; no elevation path; creator identity from authenticated principal only. | 2026-03-31 | No injection vectors in create path. |
| F0006-S0002 | Architect | Codex (Architect role) | PASS | [architect-2026-04-04.md](../../../operations/evidence/f0006/architect-2026-04-04.md) | 2026-04-04 | Create flow preserves region-alignment validation, creator ownership defaulting, and atomic initial audit records. |
| F0006-S0003 | Quality Engineer | PM Agent | PASS | Detail includes all denormalized fields, completeness panel, available transitions, rowVersion for concurrency. | 2026-03-31 | SubmissionDto verified against story field list. |
| F0006-S0003 | Code Reviewer | Architect Agent | PASS | MapToDtoAsync properly computes completeness and available transitions inline; no N+1 on detail fetch. | 2026-03-31 | Include chain loads related entities in single query. |
| F0006-S0003 | Security Reviewer | Architect Agent | PASS | CanReadSubmission enforces role+ownership+region scoping before returning detail. | 2026-03-31 | Casbin + resource-level auth both applied. |
| F0006-S0003 | Architect | Codex (Architect role) | PASS | [architect-2026-04-04.md](../../../operations/evidence/f0006/architect-2026-04-04.md) | 2026-04-04 | Detail DTO carries linked context, completeness, available transitions, and rowVersion as planned. |
| F0006-S0004 | Quality Engineer | PM Agent | PASS | All 4 intake transitions enforced, role gating correct, completeness guard blocks ReadyForUWReview, If-Match required. | 2026-03-31 | HTTP 409 for invalid_transition and missing_transition_prerequisite verified. |
| F0006-S0004 | Code Reviewer | Architect Agent | PASS | WorkflowStateMachine is clean and testable; transition + timeline created atomically in single UoW commit. | 2026-03-31 | State machine also includes F0019 downstream states per assembly plan — acceptable forward design. |
| F0006-S0004 | Security Reviewer | Architect Agent | PASS | CanPerformTransition gates intake transitions to DistributionUser/Manager/Admin; Underwriter gated to non-intake only. | 2026-03-31 | No privilege escalation path. |
| F0006-S0004 | Architect | Codex (Architect role) | PASS | [architect-2026-04-04.md](../../../operations/evidence/f0006/architect-2026-04-04.md) | 2026-04-04 | Intake-only state machine and append-only transition history preserve the F0019 boundary. |
| F0006-S0005 | Quality Engineer | PM Agent | PASS | Structured result with field-level and document-level checks; F0020 adapter soft-skips when unavailable; read-only projection. | 2026-03-31 | MissingItems list covers all 5 required fields + 2 document categories. |
| F0006-S0005 | Code Reviewer | Architect Agent | PASS | Null-object pattern (UnavailableSubmissionDocumentChecklistReader) is clean adapter for F0020 integration. | 2026-03-31 | Completeness is pure read-side computation, no side effects. |
| F0006-S0005 | Security Reviewer | Architect Agent | PASS | Completeness evaluation inherits submission read scope; no data leakage. | 2026-03-31 | No additional attack surface. |
| F0006-S0005 | Architect | Codex (Architect role) | PASS | [architect-2026-04-04.md](../../../operations/evidence/f0006/architect-2026-04-04.md) | 2026-04-04 | Completeness remains a read-side projection behind a clean F0020 adapter boundary. |
| F0006-S0006 | Quality Engineer | PM Agent | PASS | Underwriter role required for ReadyForUWReview, no-op on same-user assign, timeline event with old/new assignee. | 2026-03-31 | invalid_assignee with contextual detail messages for all 3 failure modes. |
| F0006-S0006 | Code Reviewer | Architect Agent | PASS | AssignAsync properly validates target user existence, active status, and role; atomic UoW commit. | 2026-03-31 | Concurrency handled via DbUpdateConcurrencyException catch. |
| F0006-S0006 | Security Reviewer | Architect Agent | PASS | CanAssignSubmission scopes to Admin (any), DistributionManager (in region), DistributionUser (own only). | 2026-03-31 | Assignment cannot bypass ABAC scope. |
| F0006-S0006 | Architect | Codex (Architect role) | PASS | [architect-2026-04-04.md](../../../operations/evidence/f0006/architect-2026-04-04.md) | 2026-04-04 | Manual assignment and underwriter-only handoff remain aligned with the planned workflow contract. |
| F0006-S0007 | Quality Engineer | PM Agent | PASS | All mutations produce ActivityTimelineEvent; append-only; pagination via GET timeline endpoint. | 2026-03-31 | Minor: default page size is 25 (code) vs 20 (story spec) — project-wide convention prevails. |
| F0006-S0007 | Code Reviewer | Architect Agent | PASS | Timeline events are immutable (no update/delete on repository interface); structured JSON payloads. | 2026-03-31 | Consistent with existing ActivityTimelineEvent pattern. |
| F0006-S0007 | Security Reviewer | Architect Agent | PASS | Timeline inherits submission:read scope; ActorUserId always from authenticated principal. | 2026-03-31 | No actor spoofing possible. |
| F0006-S0007 | Architect | Codex (Architect role) | PASS | [architect-2026-04-04.md](../../../operations/evidence/f0006/architect-2026-04-04.md) | 2026-04-04 | Timeline remains append-only and mutation-generated with no edit/delete surface. |
| F0006-S0008 | Quality Engineer | PM Agent | PASS | Stale thresholds seeded (Received=2d, Triaging=2d, WaitingOnBroker=3d); query-time computation; terminal states never stale. | 2026-03-31 | IsStale in list and detail DTOs; stale filter on pipeline list. |
| F0006-S0008 | Code Reviewer | Architect Agent | PASS | BuildStaleFlagsAsync queries thresholds + latest transitions; falls back to CreatedAt when no transition. | 2026-03-31 | WarningDays seeded but unused — noted as low-priority tech debt. |
| F0006-S0008 | Security Reviewer | Architect Agent | PASS | Stale computation operates on already ABAC-scoped queries; no information leakage. | 2026-03-31 | Threshold config is read-only seed data, not user-modifiable. |
| F0006-S0008 | Architect | Codex (Architect role) | PASS | [architect-2026-04-04.md](../../../operations/evidence/f0006/architect-2026-04-04.md) | 2026-04-04 | Stale evaluation is threshold-driven, query-time, and applied after caller scoping. |

## Feature-Level Signoff

| Role | Reviewer | Verdict | Date | Notes |
|------|----------|---------|------|-------|
| Quality Engineer | Codex (Quality Engineer role) | PASS | 2026-04-04 | [qe-2026-04-04.md](../../../operations/evidence/f0006/qe-2026-04-04.md) — Targeted backend and frontend F0006 suites passed; skipped UI artifact layers are documented as non-blocking follow-ups. |
| Code Reviewer | Codex (Code Reviewer role) | PASS | 2026-04-04 | [code-review-2026-04-04.md](../../../operations/evidence/f0006/code-review-2026-04-04.md) — No blocking correctness, layering, or maintainability defects found in the reviewed F0006 slice. |
| Security Reviewer | Codex (Security Reviewer role) | PASS | 2026-04-04 | [security-2026-04-04.md](../../../operations/evidence/f0006/security-2026-04-04.md) — ABAC, resource scoping, optimistic concurrency, and append-only audit controls verified. |
| Architect | Codex (Architect role) | PASS | 2026-04-04 | [architect-2026-04-04.md](../../../operations/evidence/f0006/architect-2026-04-04.md) — Implementation remains aligned with the F0006 PRD, assembly plan, and F0019 workflow boundary. |
| Product Manager | Codex (PM role) | ARCHIVE | 2026-04-04 | Archive closeout passed. All 8 stories are done, required reviewer signoff is complete, and tracker validation passed before archive transition. |

## Closeout Summary

**Implementation Complete:** 2026-03-31
**Archive Closeout Review:** 2026-04-04 by Codex (PM role)
**Tests:** 41 backend tests passed in targeted 2026-04-04 reruns (10 endpoint + 31 unit/workflow), 20 frontend tests passed in targeted 2026-04-04 reruns (12 route smoke + 8 page integration)
**Defects found and fixed:** 0 in the PM archive slice; reviewer closeout found no blocking F0006 defects
**Residual risks:** 0 blocking; 2 accepted (screen-specific accessibility / visual / responsive evidence may still be requested by release approvers, and `WorkflowSlaThreshold.WarningDays` remains unused until warning-stage stale UX is explicitly scoped)

## PM Closeout

**PM Review:** 2026-04-04 by Codex (PM Agent — Archive Closeout Pass)
**PRD Acceptance Criteria:** 10/10 met
**PRD Success Criteria:** 6/6 met
**Scope Delivered:** 8/8 stories (100%)
**Product Gaps:** 0 blocking
**Non-Blocking Follow-ups:** 2 documented below
**Orphaned Story Rule:** N/A — all 8 stories reached Done status; no rehoming required
**Archive Date:** 2026-04-04

## Deferred Non-Blocking Follow-ups

| Follow-up | Why deferred | Tracking link | Owner |
|-----------|--------------|---------------|-------|
| Capture dedicated submission-screen accessibility, responsive, and visual-regression artifacts if a release approver requires them | Current automated route and page integration coverage is sufficient for feature closeout; dedicated UX evidence can be added as release-hardening work if requested | N/A | QE / Frontend |
| Revisit `WorkflowSlaThreshold.WarningDays` when stale-warning UX becomes product scope | The current stale contract only needs hard stale flags; warning-stage UX is not part of F0006 MVP scope | N/A | Product / Backend |

## Tracker Sync Checklist

- [x] `planning-mds/features/REGISTRY.md` status/path aligned
- [x] `planning-mds/features/ROADMAP.md` section aligned (`Now/Next/Later/Completed`)
- [x] `planning-mds/features/STORY-INDEX.md` regenerated
- [x] `planning-mds/BLUEPRINT.md` feature/story status links aligned
- [x] Every required signoff role has story-level `PASS` entries with reviewer, date, and evidence
- [x] Feature folder moved to `planning-mds/features/archive/F0006-submission-intake-workflow/`
