---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0018-S0008: Policy Reinstatement Within LOB-Configurable Window

**Story ID:** F0018-S0008
**Feature:** F0018 — Policy Lifecycle & Policy 360
**Title:** Policy reinstatement within LOB-configurable window
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter or admin
**I want** to reinstate a `Cancelled` policy within its LOB-configurable reinstatement window with a required reason
**So that** coverage can be restored with full auditability when the insured resolves the cancellation cause (e.g., pays outstanding premium), without having to re-issue a brand-new policy

## Context & Background

Reinstatement is a controlled `Cancelled → Issued` transition gated by a per-LOB window (`WorkflowSlaThreshold` per ADR-009). The deadline was computed and persisted at cancellation time (`ReinstatementDeadline = CancellationEffectiveDate + LOB.reinstatementWindowDays`). After the deadline, a new policy must be created; no admin backdoor in MVP. Reinstatement produces a new `PolicyVersion` (`VersionReason=Reinstatement`) capturing the current terms, appends a `WorkflowTransition`, and records a timeline event.

## Acceptance Criteria

**Happy Path:**
- **Given** a `Cancelled` policy whose `ReinstatementDeadline ≥ today` and a user with `policy:reinstate` in scope
- **When** they submit `POST /api/policies/{id}/reinstate` with `reinstatementReason=InsuredPaidOutstandingPremium`, optional `reinstatementDetail`, and the current `rowVersion`
- **Then** the policy transitions to `Issued`, `CancelledAt` / `CancellationEffectiveDate` / `CancellationReasonCode` / `CancellationReasonDetail` / `ReinstatementDeadline` are cleared (moved to the `WorkflowTransition` / timeline), a new `PolicyVersion` is written (`VersionReason=Reinstatement`) with the current coverages re-materialized, `Policy.CurrentVersionId` points to the new version, `Policy.RowVersion` increments, a `WorkflowTransition` is appended, and a timeline event `policy.reinstated` is recorded

- **Given** a reinstated policy
- **When** the user returns to Policy 360
- **Then** header shows `Issued` again; Versions rail has the Reinstatement version on top; Activity shows `policy.cancelled` then `policy.reinstated`; the reinstatement countdown is gone

**Alternative Flows / Edge Cases:**
- Policy not `Cancelled` → 409 `code=invalid_transition`
- `today > ReinstatementDeadline` → 409 `code=reinstatement_window_expired` (customer message directs to create a new policy)
- Missing `reinstatementReason` → 400 `code=missing_field`
- `Other` reason without `reinstatementDetail` → 400 `code=reason_detail_required`
- Stale `rowVersion` → 412 `code=concurrency_conflict`
- Unauthorized actor → 403
- Idempotent retry: same `Idempotency-Key` returns the same reinstatement (no duplicate version / timeline)
- Policy `ExpirationDate < today` when reinstating (cancelled shortly before its natural end) → allowed; the policy returns to `Issued` but the daily expiration job will transition it to `Expired` on the next run. This is intentional so the cancellation-to-expiration audit chain is correct.

**Checklist:**
- [ ] `POST /api/policies/{id}/reinstate` with `If-Match` + `Idempotency-Key`
- [ ] Payload: `reinstatementReason` (required, validated at API layer: `InsuredPaidOutstandingPremium`, `CancellationInError`, `AgreementReached`, `Other`), `reinstatementDetail` (required when `Other`)
- [ ] Window check: `today ≤ ReinstatementDeadline`; server-enforced (client countdown is advisory)
- [ ] New `PolicyVersion` with `VersionReason=Reinstatement` and full snapshots
- [ ] New `PolicyCoverageLine` rows materialized under the new version (coverages restored from the pre-cancellation version)
- [ ] `Policy.CurrentVersionId` updated atomically
- [ ] Cancellation fields cleared on the live row; their values remain recoverable via the `WorkflowTransition` history + the prior `PolicyVersion` snapshot
- [ ] `WorkflowTransition` row captures `Cancelled → Issued` with the reinstatement reason
- [ ] Timeline event `policy.reinstated`
- [ ] ABAC `policy:reinstate` enforced
- [ ] All writes in a single transaction

## Data Requirements

- See PRD "Reinstatement Window" table for default LOB windows
- `WorkflowSlaThreshold` category `PolicyReinstatementWindow` seeded with the defaults (Property 30, GeneralLiability 30, WorkersCompensation 60, ProfessionalLiability 30, Cyber 15, Default 30)

**Validation Rules:**
- Window is enforced at time of reinstatement attempt (`today ≤ ReinstatementDeadline`), not at the time of cancellation
- `ReinstatementDeadline` is read from the live policy row; if somehow null (data corruption) the attempt returns 500 (alerted; should never happen if cancellation path is correct)
- Re-cancellation after reinstatement is allowed; each cancellation/reinstatement cycle appends its own transition history and own version rows

## Role-Based Visibility

| Role | Reinstate |
|------|-----------|
| Distribution User | No |
| Distribution Manager | No |
| Underwriter | Yes (scoped) |
| Relationship Manager | No |
| Program Manager | No |
| Admin | Yes |

**Data Visibility:** InternalOnly.

## Non-Functional Expectations

- Performance: reinstatement commit p95 ≤ 500 ms
- Reliability: all writes transactional
- Idempotency: same `Idempotency-Key` returns the prior result
- Security: Casbin `policy:reinstate` gate; window enforced server-side

## Dependencies

**Depends On:**
- F0018-S0007 (cancellation must have computed the deadline), F0018-S0005 (version machinery)

**Related Stories:**
- F0018-S0010 (timeline)
- ADR-009 extension (`WorkflowSlaThreshold` category for reinstatement windows)

## Out of Scope

- Post-window reinstatement override (no admin backdoor; create a new policy instead)
- Partial reinstatement / reinstatement with changed terms (reinstatement restores prior terms; term changes go through endorsement afterward)
- Reinstatement approval workflow (single-actor in MVP)
- Multi-party notifications (insured / broker emails) — deferred
- Bulk reinstatement

## UI/UX Notes

- Entry point: "Reinstate (within N days)" button on Policy 360 header, visible only when `Status=Cancelled` and `today ≤ ReinstatementDeadline` and actor is authorized
- Confirmation dialog surfaces the original cancellation reason + effective date + remaining days
- Post-commit: header flips to `Issued`; success toast "Policy reinstated. Coverage restored to terms effective {EffectiveDate}"

## Questions & Assumptions

**Assumptions:**
- Reinstatement restores the pre-cancellation coverage set verbatim; subsequent changes go through endorsement
- The `WorkflowSlaThreshold` pattern is extensible to a new `PolicyReinstatementWindow` category; Architect to confirm during Phase B
- `CancellationInError` is the code used when the prior cancellation was a mistake; operationally it is a superset of "agreement reached"

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (window expired, wrong state, concurrency, idempotency)
- [ ] Permissions enforced (Underwriter + Admin only)
- [ ] Audit/timeline logged: Yes (`policy.reinstated`, `WorkflowTransition`)
- [ ] Tests pass (including window-enforcement boundary tests per LOB)
- [ ] Documentation updated (OpenAPI + reason-code list + window-config reference)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
