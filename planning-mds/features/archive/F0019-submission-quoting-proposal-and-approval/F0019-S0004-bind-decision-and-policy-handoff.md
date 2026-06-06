# Bind Decision and Policy Handoff

## Story Header

**Story ID:** F0019-S0004
**Feature:** F0019 — Submission Quoting, Proposal & Approval Workflow
**Title:** Bind decision and policy handoff
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter
**I want** to request and confirm a bind on an approved, quoted submission
**So that** the submission reaches a final bound decision and hands off to policy creation

## Context & Background

Bind is the terminal positive outcome of the submission journey. It is gated by a granted approval
(F0019-S0003) and a ready packet (F0019-S0002). The submission moves `Quoted -> BindRequested -> Bound`.
On `Bound`, F0019 emits a handoff to F0018 policy creation/correlation — a **handoff point**, not
embedded issuance or billing. The bind action must be idempotent so retries or duplicate clicks do
not double-process the business outcome.

## Acceptance Criteria

**Happy Path:**
- **Given** a submission in `Quoted` with packet `Approved`
- **When** an authorized underwriter requests bind and then confirms it
- **Then** the submission moves `Quoted -> BindRequested -> Bound`, append-only `WorkflowTransition` records and `ActivityTimelineEvent`s ("bind requested", "bound") are written, and a policy-creation handoff signal is emitted to F0018 with correlation back to the submission.

**Alternative Flows / Edge Cases:**
- Bind attempted without a granted approval → `409` (approval required); no transition.
- Bind attempted with an incomplete packet → `409`.
- Unauthorized actor → `403 Forbidden`.
- Duplicate confirm (retry) on an already-`Bound` submission → idempotent: returns success without a second transition or duplicate handoff.
- F0018 handoff fails downstream → submission remains `Bound`; the handoff is recorded as pending/retryable (the bind outcome is not rolled back by a downstream policy hiccup), surfaced for follow-up.
- Submission not found → `404`.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Submission Detail → Workflow → "Move to BindRequested" | Click | Enabled when `Quoted` + packet `Approved` + authorized | `Quoted -> BindRequested` + audit | Reload shows `BindRequested`; timeline event | Underwriter/Admin |
| Submission Detail → Workflow → "Confirm Bind" | Click (idempotent) | Enabled when `BindRequested` + authorized | `BindRequested -> Bound` + audit + F0018 handoff signal | Reload shows `Bound`; timeline shows bound + handoff; packet read-only | Underwriter/Admin |

Required checks:
- [ ] Render-only behavior cannot satisfy this story — bind persists, packet becomes read-only, and handoff is recorded after reload.
- [ ] Bind validates approval + readiness + authorization before transition; failures return `409`/`403`.
- [ ] Bind is idempotent; a retry does not duplicate the transition or the handoff.
- [ ] A successful bind appends append-only audit/timeline records and emits the F0018 handoff.

## Data Requirements

**Required Fields (bind action):**
- `submissionId`, `actorUserId`, `occurredAt`
- `idempotencyKey`: to coalesce retries/duplicate confirms

**Handoff payload (to F0018):** `submissionId`, recorded reference facts snapshot (recorded values only), packet/document references; correlation id linking the resulting policy back to the submission.

**Validation Rules:**
- Bind requires `decision=Granted` approval and a complete/`Approved` packet.
- No premium/rating is computed during bind; recorded values are passed through as-is.

## Role-Based Visibility

**Roles that can request/confirm bind:**
- Underwriter — permitted (ABAC `submission:transition` for bind states + per-transition guard)
- Admin — permitted
- Distribution user — view only
- BrokerUser — no access

**Data Visibility:**
- InternalOnly content: bind decision, handoff correlation, recorded terms snapshot.
- ExternalVisible content: none in MVP.

## Non-Functional Expectations

- Performance: each bind transition < 700ms p95; handoff emission is asynchronous/decoupled.
- Security: authorized roles only; unauthorized → `403`.
- Reliability: transitions + audit atomic; bind idempotent; downstream handoff failure does not corrupt the `Bound` state.

## Dependencies

**Depends On:**
- F0019-S0002 — ready packet.
- F0019-S0003 — granted approval gates bind.
- F0018 Policy Lifecycle — receives the policy-creation handoff/correlation.
- ADR-011 — transition history; ADR-012 — referenced documents in the handoff.

**Related Stories:**
- F0019-S0008 — timeline surfaces bind + handoff events.

## Business Rules

1. **Bind is gated:** requires granted approval and a complete/approved packet; otherwise rejected.
2. **Handoff, not issuance:** F0019 signals F0018 for policy creation/correlation; it does not create policies, issue documents, or post billing.
3. **Idempotent bind:** retries/duplicate confirms produce exactly one bound outcome and one handoff.
4. **No computation:** recorded terms are passed through; Nebula performs no rating at bind.

## Out of Scope

- Policy issuance, document generation, and billing (F0018 / F0027 / out of scope).
- Carrier rating round-trip or premium calculation (boundary guardrail).
- Terminal `Declined`/`Withdrawn` (F0019-S0005).

## UI/UX Notes

- Screens involved: Submission Detail (workflow actions: Move to BindRequested, Confirm Bind).
- Key interactions: bind actions disabled until approval granted; clear "bound" terminal state; packet locks after bind.

## Questions & Assumptions

**Open Questions:**
- [ ] Is `BindRequested` a distinct user step in MVP, or can authorized users bind directly from `Quoted` after approval? (Default assumption: keep `BindRequested` as an explicit step for auditability; confirm with underwriting.)

**Assumptions (to be validated):**
- F0018 exposes (or Phase B defines) a policy-creation-from-bind handoff contract.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (`409`/`403`/`404`, idempotent retry, handoff-failure path)
- [ ] Permissions enforced
- [ ] Audit/timeline logged (bind requested, bound, handoff)
- [ ] Idempotency proven by test
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
