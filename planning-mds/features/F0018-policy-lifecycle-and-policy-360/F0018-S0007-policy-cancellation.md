---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0018-S0007: Policy Cancellation (Mid-Term and Flat)

**Story ID:** F0018-S0007
**Feature:** F0018 — Policy Lifecycle & Policy 360
**Title:** Policy cancellation with required reason code and effective date
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter, distribution manager, or admin
**I want** to cancel an `Issued` policy with a required reason code and cancellation effective date, and have the cancellation captured in the audit timeline and version history
**So that** cancelled policies reflect reality for book-of-business reporting, downstream dependents render correctly, and the insured/broker has an auditable explanation

## Context & Background

Cancellation is a terminal-for-writes lifecycle transition (`Issued → Cancelled`) with a reason code, a cancellation effective date, and a computed reinstatement deadline. It does not produce a new `PolicyVersion` by itself (the last issued terms remain the source of truth for what was cancelled), but it appends a `WorkflowTransition` and an `ActivityTimelineEvent`, and it sets the `ReinstatementDeadline` for the reinstatement story (S0008). Both flat cancellation (effective = policy start) and mid-term cancellation (effective > policy start) are supported.

## Acceptance Criteria

**Happy Path:**
- **Given** an `Issued` policy and a user with `policy:cancel` in scope
- **When** they submit `POST /api/policies/{id}/cancel` with `cancellationReasonCode=InsuredRequested`, `cancellationEffectiveDate=2026-05-01`, and the current `rowVersion`
- **Then** the policy transitions to `Cancelled`, `CancelledAt`, `CancellationEffectiveDate`, `CancellationReasonCode` are set, `ReinstatementDeadline` is computed as `CancellationEffectiveDate + LOB.reinstatementWindowDays`, `Policy.RowVersion` increments, a `WorkflowTransition` is appended (`Issued → Cancelled`), and a timeline event `policy.cancelled` is recorded

- **Given** a cancellation succeeds
- **When** dependent views (Account 360 policies rail, Renewal detail, Policy List) refresh
- **Then** Account 360 shows a `Cancelled` status badge for the policy, Renewal detail keeps predecessor/successor links but marks the policy as cancelled, Policy List flips the row status to `Cancelled` and removes the `Cancel` action, and Policy 360 surfaces the reinstatement-window countdown in the header

**Alternative Flows / Edge Cases:**
- Policy not `Issued` → 409 `code=invalid_transition` (cannot cancel Pending / Expired / Cancelled)
- Missing `cancellationReasonCode` → 400 `code=missing_field`
- `Other` reason code without `cancellationReasonDetail` → 400 `code=reason_detail_required`
- `cancellationEffectiveDate > ExpirationDate` → 400 `code=cancellation_after_expiration`
- `cancellationEffectiveDate > today + 30` (future-dated cancellation more than 30 days out) → 400 `code=cancellation_too_future` (MVP: keeps cancellations grounded in near-term operations; scheduled cancellations are a follow-up)
- `cancellationEffectiveDate < EffectiveDate` → normalized to `EffectiveDate` with a warning (flat cancellation semantics)
- Stale `rowVersion` → 412 `code=concurrency_conflict`
- Unauthorized actor → 403
- Idempotent retry: same `Idempotency-Key` returns the same cancellation

**Checklist:**
- [ ] `POST /api/policies/{id}/cancel` with `If-Match` + `Idempotency-Key`
- [ ] Payload: `cancellationReasonCode`, `cancellationReasonDetail` (optional), `cancellationEffectiveDate`
- [ ] Reason codes validated at API layer: `Nonpayment`, `MaterialMisrepresentation`, `IncreasedHazard`, `InsuredRequested`, `CoverageReplaced`, `NonRenewalProcessed`, `Other`
- [ ] State transition `Issued → Cancelled` with `WorkflowTransition` row (reusing ADR-011 pattern)
- [ ] `ReinstatementDeadline` computed using per-LOB window from `WorkflowSlaThreshold`
- [ ] Timeline event `policy.cancelled` with reason code + effective date
- [ ] ABAC `policy:cancel` enforced
- [ ] Cancellation does NOT produce a new `PolicyVersion` (current version remains v_N; reinstatement produces v_N+1)
- [ ] Cancelled policies remain fully readable (Policy 360, list, account 360 rail)
- [ ] Dependent denormalized `PolicyStatusAtRead` on renewal list items updates on next read

## Data Requirements

- See PRD "Core Entity: Policy" and "Cancellation Reason Codes"
- `WorkflowTransition` row captures actor, from/to states, reason code, reason detail, rowVersion

**Validation Rules:**
- `cancellationEffectiveDate` required; parsed as `date`
- `cancellationEffectiveDate` normalization: values < `EffectiveDate` clamped to `EffectiveDate` with a `Warning` header
- `ReinstatementDeadline = CancellationEffectiveDate + LOB.reinstatementWindowDays`; computed and persisted atomically
- Policy must be in `Issued`; any other starting state returns 409

## Role-Based Visibility

| Role | Cancel |
|------|--------|
| Distribution User | No |
| Distribution Manager | Yes (scoped) |
| Underwriter | Yes (scoped) |
| Relationship Manager | No |
| Program Manager | No |
| Admin | Yes |

**Data Visibility:** InternalOnly.

## Non-Functional Expectations

- Performance: cancellation commit p95 ≤ 500 ms
- Reliability: all writes transactional; rollback leaves the policy unchanged
- Idempotency: same `Idempotency-Key` returns the prior result
- Security: Casbin `policy:cancel` gate; dedicated audit entry

## Dependencies

**Depends On:**
- F0018-S0002 (policy exists), F0018-S0003 (detail page as entry point)

**Related Stories:**
- F0018-S0008 (reinstatement; consumes `ReinstatementDeadline`)
- F0018-S0009 (renewal linkage; cancelled policies may still have a successor renewal link)
- F0018-S0010 (timeline entry)

## Out of Scope

- Pro-rata premium refund computation (MVP does not recompute premium at cancellation; downstream billing feature F0025 owns refunds)
- Scheduled / future-dated cancellation beyond 30 days out (follow-up)
- Cancellation approval workflow (single-actor in MVP)
- Carrier-facing cancellation notices / NOTICE/EOI paper generation (deferred to F0027)
- Bulk cancellation across multiple policies

## UI/UX Notes

- Entry point: "Cancel" button on Policy detail header (enabled only when `Status=Issued` and actor is authorized)
- Flow: pick reason code → enter effective date (default today) → optional reason detail → confirm dialog with reinstatement deadline preview → commit
- Post-commit: Policy 360 header flips to `Cancelled` badge, reinstatement countdown surfaces, "Reinstate" affordance appears (covered by S0008)

## Questions & Assumptions

**Assumptions:**
- Reason codes are a fixed set validated at API layer; adding a code is a config change, not a DB migration
- Premium on the policy remains as-was at cancellation; refund semantics belong to F0025
- `NonRenewalProcessed` is used to administratively close out a prior non-renewal notice; not a redundant `InsuredRequested` synonym

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (invalid state, missing reason, bad effective date, concurrency, idempotency)
- [ ] Permissions enforced (Manager / Underwriter / Admin only)
- [ ] Audit/timeline logged: Yes (`policy.cancelled`, `WorkflowTransition`)
- [ ] Tests pass (including reinstatement-deadline computation per LOB)
- [ ] Documentation updated (OpenAPI + reason-code list)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
