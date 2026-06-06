# Activate Downstream Submission Workflow

## Story Header

**Story ID:** F0019-S0001
**Feature:** F0019 — Submission Quoting, Proposal & Approval Workflow
**Title:** Activate downstream submission workflow
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter
**I want** to advance a triaged submission into underwriting review and the downstream workflow
**So that** the submission can progress toward a quote and bind decision instead of stopping at intake

## Context & Background

F0006 owns the submission workflow only through `ReadyForUWReview`, and the runtime deliberately
rejects `ReadyForUWReview -> InReview` and all later transitions with `409 invalid_transition`.
This story is the **mandated first F0019 slice**: it moves that boundary on purpose, extending the
shared submission state machine (ADR-011) with the downstream states and enabling the first
downstream transition. Every later F0019 story builds on the states declared here.

## Acceptance Criteria

**Happy Path:**
- **Given** a submission in `ReadyForUWReview` and an authorized underwriter
- **When** the underwriter transitions it to `InReview`
- **Then** the submission status becomes `InReview`, and one `WorkflowTransition` record and one
  `ActivityTimelineEvent` are appended **atomically** in the same transaction as the status change.

**Downstream states declared (ADR-011 state machine extension):**
- `InReview`, `Quoted`, `BindRequested`, `Bound` (non-terminal/active) and `Declined`, `Withdrawn`
  (terminal). This story enables `ReadyForUWReview -> InReview`; later stories own the transitions
  into `Quoted`/`BindRequested`/`Bound` (S0002/S0004) and the terminal transitions (S0005).

**Alternative Flows / Edge Cases:**
- Disallowed transition (e.g., `ReadyForUWReview -> Bound`) → rejected with `409 invalid_transition`; no records appended.
- Unauthorized actor attempts the transition → `403 Forbidden`; no state change, no audit record.
- Submission already in `InReview` → re-issuing the same transition is rejected with `409` (no duplicate history).
- Submission not found → `404`.

**Boundary regression (deliberate move):**
- [ ] The existing F0006 guard test that asserts `ReadyForUWReview -> InReview` returns `409` is **updated** to assert the transition now succeeds for authorized roles under F0019, with a comment referencing this story — proving the boundary moved deliberately, not by drift.
- [ ] A regression asserts all **non-enabled** downstream transitions still reject until their owning story ships.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Submission Detail → Workflow → "Move to InReview" | Click transition action | Enabled only when status = `ReadyForUWReview` and actor authorized | Status → `InReview`; `WorkflowTransition` + `ActivityTimelineEvent` appended | Reload shows `InReview`; timeline shows the transition event; list status pill updates | Underwriter/Admin only; rejected for other roles/states |

Required checks:
- [ ] Render-only behavior cannot satisfy this story — the transition must persist and be observable after reload.
- [ ] The transition path validates the from-state and authorization before applying; failures return `409`/`403`.
- [ ] A successful transition appends append-only audit/timeline records.
- [ ] Tests prove an authorized user performs the transition from the named entry point and sees persisted `InReview` after reload.

## Data Requirements

**Required Fields (transition request):**
- `toState`: target downstream state (`InReview` for this story)
- `reason` / transition comment: optional for `InReview`, required for terminal transitions (S0005)
- `actorUserId`, `occurredAt`: captured server-side

**WorkflowTransition record (per ADR-011):** `WorkflowType='Submission'`, `EntityId`, `FromState`, `ToState`, `Reason`, `ActorUserId`, `OccurredAt`.

**Validation Rules:**
- From-state must be a declared predecessor of `toState` (guard before transition).
- No value-bearing quote fields are read or written here (workflow only — boundary held).

## Role-Based Visibility

**Roles that can transition `ReadyForUWReview -> InReview`:**
- Underwriter — permitted (ABAC `submission:transition` + per-transition role guard)
- Admin — permitted
- Distribution user — not permitted for this transition (visibility only)
- BrokerUser — no access to internal workflow transitions

**Data Visibility:**
- InternalOnly content: workflow state, transition history, actor identity.
- ExternalVisible content: none in MVP.

## Non-Functional Expectations

- Performance: transition completes < 500ms p95.
- Security: authorized roles only; ABAC + per-transition guard; unauthorized → `403`.
- Reliability: status change + both audit records are atomic; partial failure rolls back entirely.

## Dependencies

**Depends On:**
- F0006 Submission Intake Workflow — provides the `ReadyForUWReview` boundary this story moves.
- ADR-011 — workflow state machine + append-only transition history contract.

**Related Stories:**
- F0019-S0002 — drives `InReview -> Quoted` via the packet.
- F0019-S0008 — surfaces these transition events on the timeline.

## Business Rules

1. **Deliberate boundary move:** downstream transitions are disabled until this story ships; enabling them is an explicit deliverable with authz, UI, and regression coverage — never a side effect of shared state-machine edits.
2. **Guards before transition:** the from-state and authorization are validated before any state change (ADR-011).
3. **Append-only history:** every successful transition appends `WorkflowTransition` + `ActivityTimelineEvent`; records are never mutated or deleted.

## Out of Scope

- The quote/proposal packet and `InReview -> Quoted` (F0019-S0002).
- Approval checkpoint (F0019-S0003) and bind (F0019-S0004).
- Terminal `Declined`/`Withdrawn` transitions (F0019-S0005) and archive (F0019-S0006).
- Any rating, pricing, or scoring — none introduced (boundary guardrail).

## UI/UX Notes

- Screens involved: Submission Detail (workflow panel + status control).
- Key interactions: enabled transition action for `ReadyForUWReview -> InReview`; disabled/hidden for ineligible states or roles.

## Questions & Assumptions

**Open Questions:**
- [ ] Should `InReview -> ReadyForUWReview` (send back to triage) be allowed in MVP, or is the move forward one-way? (Default assumption: not in MVP; revisit if underwriting asks.)

**Assumptions (to be validated):**
- The existing submission transition surface is reused (not a new parallel path); reuse is documented in Phase B.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (`409`/`403`/`404`)
- [ ] Permissions enforced (ABAC + per-transition guard)
- [ ] Audit/timeline logged (WorkflowTransition + ActivityTimelineEvent, atomic)
- [ ] Boundary regression updated to prove the move is deliberate
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
