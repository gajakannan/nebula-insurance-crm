# F0007 Architect Evidence — 2026-04-11

**Feature:** F0007 — Renewal Pipeline
**Reviewer:** Codex (Architect role)
**Date:** 2026-04-11
**Verdict:** PASS

## Scope Reviewed

End-to-end architecture alignment of the F0007 implementation against the PRD, feature-assembly-plan, ADR-009 (LOB classification and SLA configuration), and ADR-011 (workflow state machines and transition history).

## Architecture Alignment

### PRD Alignment

| PRD Requirement | Implementation | Status |
|-----------------|----------------|--------|
| 6-state workflow: Identified → Outreach → InReview → Quoted → Completed/Lost | `WorkflowStateMachine.RenewalTransitions` + `OpportunityStatusCatalog.RenewalStatuses` | Aligned |
| Role-gated transitions (Distribution→intake, Underwriter→review) | `RenewalTransitionRoles` dictionary with `ValidateRenewalTransition` | Aligned |
| Lost requires reasonCode; Completed requires boundPolicyId/renewalSubmissionId | Service-level prerequisite checks + validator rules | Aligned |
| Per-LOB timing windows via WorkflowSlaThreshold | 5 LOB-specific rows + default fallback | Aligned (ADR-009 extension) |
| One-active-per-policy constraint | Filtered unique index + service-level `HasActiveRenewalForPolicyAsync` check | Aligned |
| Renewal creation from expiring policy | `CreateAsync` with policy lookup, LOB inheritance, SLA-based TargetOutreachDate | Aligned |
| Append-only timeline and audit trail | `ActivityTimelineEvent` for create/transition/assign; `WorkflowTransition` append-only | Aligned |
| Dashboard renewal nudge card | `RenewalPipelineNudgeCard` component, backed by existing dashboard nudge infrastructure | Aligned |
| Pipeline list with due-window filtering | 6-route API with dueWindow/urgency/status/LOB filters, paginated | Aligned |
| Manual assignment only (no queue routing) | `PUT /renewals/{id}/assignment` with stage-role validation | Aligned |

### Assembly Plan Alignment

| Build Step | Planned | Delivered | Notes |
|------------|---------|-----------|-------|
| Step 1: Policy stub | F0018 stub with core columns | Policy entity, configuration, seed data, migration | Stub lifecycle documented; F0018 handoff path clear |
| Step 2: Restructure Renewals | Drop old columns, add new, re-seed statuses | Migration 007 with data backfill and indexes | Three-step nullable→backfill→required pattern used correctly |
| Step 3: WorkflowSlaThreshold per-LOB | Add LineOfBusiness column, expression unique index | Migration 008 with 5 LOB-specific threshold rows | `COALESCE` expression index preserves uniqueness with nullable LOB |
| Step 4: Domain entity + catalog | Renewal rewrite, WorkflowStateMachine rewrite | Complete | 14 fields, 6 nav props, role-gated state machine |
| Step 5: DTOs | 6 new/rewritten DTOs | Complete | Record types with computed fields (Urgency, AvailableTransitions) |
| Step 6: Validators | Create, transition, assignment validators | Complete | FluentValidation with conditional rules |
| Step 7: Repository | IRenewalRepository + implementation with ABAC scoping | Complete | ListAsync with per-LOB urgency segments, sort, pagination |
| Step 8: Service | Full service rewrite with create, transition, assign, list, detail | Complete | UoW pattern, timeline events, concurrency handling |
| Step 9: Endpoints | 6 routes with Casbin enforcement | Complete | Consistent with F0006 endpoint pattern |
| Step 10: ProblemDetails + DI | DuplicateRenewal, MissingTransitionPrerequisite helpers | Complete | Error codes follow RFC 7807 pattern |

### Deviations from Assembly Plan

| Deviation | Rationale | Assessment |
|-----------|-----------|------------|
| `RenewalListQuery` uses named parameters instead of the assembly plan's `RenewalListQueryParams` binding record | The endpoint binds query string params directly and constructs the query record in the handler — cleaner than `[AsParameters]` for optional nullable types | Acceptable |
| Detail DTO expanded with `AccountIndustry`, `AccountPrimaryState`, `BrokerLicenseNumber`, `BrokerState`, `PolicyEffectiveDate`, `PolicyPremium` beyond assembly plan | Denormalized display fields provide richer context in the detail view without additional API calls | Acceptable enrichment |
| List item DTO expanded with similar denormalized fields | Consistent with detail DTO approach | Acceptable |
| `CanReadRenewal` and `CanCreateRenewal` implemented as service-level methods instead of a separate authorization service | Keeps resource-level scoping close to the business logic; Casbin handles coarse-grained access | Acceptable — consistent with F0006 pattern |
| `RenewalService` constructor takes `IReferenceDataRepository` instead of accessing `db.Policies` directly | Better layering — the reference data repository abstracts policy lookup behind an existing interface | Improvement over assembly plan |

### Cross-Cutting Patterns

| Pattern | Compliance |
|---------|------------|
| Soft delete + query filter | `HasQueryFilter(e => !e.IsDeleted)` on both Renewal and Policy. `HasActiveRenewalForPolicyAsync` explicitly checks `!IsDeleted`. |
| Optimistic concurrency (xmin) | `RowVersion` mapped to PostgreSQL `xmin` with `IsConcurrencyToken()`. If-Match precondition at endpoint layer. |
| Append-only history | WorkflowTransition and ActivityTimelineEvent — no update/delete surfaces exposed. |
| Principal key pattern (ADR-006) | `AssignedToUserId`, `CreatedByUserId`, `UpdatedByUserId` all use `Guid` (uuid) referencing `UserProfile.Id`. |
| Field rename pattern (F0005) | `AssignedToUserId` (not `AssignedTo` string), `ActorUserId` on timeline events — consistent with the F0005 rename convention. |
| LOB classification (ADR-009) | `LineOfBusiness` on Renewal entity; `WorkflowSlaThreshold` extended with LOB; urgency computation uses LOB-aware threshold lookup with default fallback. |

### F0018 Boundary

The Policy stub is correctly positioned as F0018 surface area landed early. The migration is named `F0018_PolicyStubAndF0007RenewalSlaReconcile` to signal ownership. The assembly plan documents the handoff contract: F0018 must extend (not rewrite) the Policies table, preserve PolicyNumber uniqueness, and the existing FKs.

## Story-Level Architecture Verdicts

| Story | Architecture Finding | Verdict |
|-------|---------------------|---------|
| F0007-S0001 | Scoped list query, sort contract, pagination, due-window/urgency filtering, and per-LOB threshold segments align with the pipeline list design. | PASS |
| F0007-S0002 | Detail DTO carries linked context (policy, account, broker), computed urgency, available transitions per user role, and rowVersion for concurrency. | PASS |
| F0007-S0003 | Role-gated state machine, conditional field enforcement, append-only transition records, and atomic UoW commit preserve the workflow contract. | PASS |
| F0007-S0004 | Manual assignment with stage-role validation, terminal-state blocking, previous-assignee timeline context, and concurrency handling. No queue routing (deferred to F0022). | PASS |
| F0007-S0005 | Query-time urgency computation using per-LOB SLA thresholds with default fallback. Overdue = past TargetOutreachDate in Identified status. Dashboard nudge card integration. | PASS |
| F0007-S0006 | Create from policy with one-active-per-policy constraint, LOB inheritance, SLA-based TargetOutreachDate, and initial transition + timeline records. | PASS |
| F0007-S0007 | Append-only ActivityTimelineEvent for all mutations (create, transition, assign). Paged timeline endpoint. Actor identity from authenticated principal. | PASS |

## Verdict

The F0007 implementation is architecturally aligned with the PRD, assembly plan, ADR-009, and established project patterns. All deviations from the assembly plan are improvements or acceptable enrichments. The F0018 boundary is clearly documented and migration-named. No architectural defects found.
