# Decline and Withdraw Terminal Decisions

## Story Header

**Story ID:** F0019-S0005
**Feature:** F0019 — Submission Quoting, Proposal & Approval Workflow
**Title:** Decline and withdraw terminal decisions
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** underwriter or distribution user
**I want** to record a declined or withdrawn outcome on a submission with a reason
**So that** submissions that will not bind reach a clear, auditable terminal state

## Context & Background

Not every submission binds. This story adds the negative terminal outcomes to the downstream state
machine: `Declined` (the carrier/underwriting will not offer terms or approval was declined) and
`Withdrawn` (the broker/insured pulled the submission). Both require a reason code, are guarded by
state and authorization rules, and append audit history (ADR-011). Terminal states make a submission
eligible for archive (F0019-S0006).

## Acceptance Criteria

**Happy Path (Decline):**
- **Given** a submission in an active downstream state (`InReview`, `Quoted`, or `BindRequested`)
- **When** an authorized user declines it with a required `reasonCode`
- **Then** the submission moves to `Declined`, append-only `WorkflowTransition` + `ActivityTimelineEvent` ("declined", reason) are written, and further forward transitions are rejected.

**Happy Path (Withdraw):**
- **Given** a submission in an active downstream state
- **When** an authorized user withdraws it with a required `reasonCode`
- **Then** the submission moves to `Withdrawn` with the same audit guarantees.

**Alternative Flows / Edge Cases:**
- Decline/withdraw without a `reasonCode` → `400` (reason required); `reasonDetail` required when `reasonCode = Other`.
- Decline/withdraw from a terminal state (`Bound`/`Declined`/`Withdrawn`) → `409 invalid_transition`.
- Unauthorized actor → `403 Forbidden`.
- Re-issuing the same terminal transition → `409` (no duplicate history).
- Submission not found → `404`.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Submission Detail → Workflow → "Decline" / "Withdraw" | Click, enter reason code | Enabled for active downstream states + authorized actor | Status → `Declined`/`Withdrawn` + append-only audit | Reload shows terminal status; timeline shows reason; forward actions disabled | Underwriter/Admin for Decline; Distribution user/Underwriter/Admin for Withdraw |

Required checks:
- [ ] Render-only behavior cannot satisfy this story — the terminal transition persists after reload.
- [ ] Reason code is required; invalid/missing → `400`; bad state → `409`.
- [ ] A successful terminal transition appends append-only audit/timeline records.
- [ ] Tests prove an authorized user declines/withdraws and an unauthorized user is denied (`403`).

## Data Requirements

**Required Fields:**
- `toState`: `Declined | Withdrawn`
- `reasonCode`: from a controlled list; `reasonDetail` required when `reasonCode = Other`
- `actorUserId`, `occurredAt`

**Validation Rules:**
- Only allowed from active downstream states; terminal states reject.
- Reason code mandatory; controlled vocabulary (no free-form-only outcome).

## Role-Based Visibility

**Roles that can decline/withdraw:**
- Underwriter — Decline and Withdraw (ABAC `submission:transition` + per-transition guard)
- Distribution user — Withdraw permitted; Decline not permitted
- Admin — both
- BrokerUser — no access

**Data Visibility:**
- InternalOnly content: terminal reason codes/details, actor identity.
- ExternalVisible content: none in MVP.

## Non-Functional Expectations

- Performance: terminal transition < 500ms p95.
- Security: authorized roles only; unauthorized → `403`.
- Reliability: status change + audit records atomic.

## Dependencies

**Depends On:**
- F0019-S0001 — downstream states + transition surface.
- ADR-011 — guard conditions (reason codes) + append-only history.

**Related Stories:**
- F0019-S0003 — a declined approval may lead to a `Declined` submission.
- F0019-S0006 — terminal states are the precondition for archive.

## Business Rules

1. **Reason required:** terminal outcomes always carry a controlled `reasonCode` (and detail for `Other`) for reporting and audit.
2. **Terminal is terminal:** no forward transitions out of `Declined`/`Withdrawn` in MVP (reinstatement is Future, if ever).
3. **Distinct authority for Decline:** declining is an underwriting decision; withdrawing reflects broker/insured intent and is available more broadly.

## Out of Scope

- Reinstatement / re-opening of terminal submissions (Future).
- Archive/deactivate behavior (F0019-S0006).
- Any eligibility scoring or rating to justify a decline (boundary guardrail — decline is a recorded human decision).

## UI/UX Notes

- Screens involved: Submission Detail (workflow actions Decline/Withdraw with reason-code picker).
- Key interactions: reason-code selection, confirmation, terminal status pill, forward actions disabled after terminal.

## Questions & Assumptions

**Open Questions:**
- [ ] Final controlled `reasonCode` lists for Decline vs Withdraw — confirm with underwriting (e.g., Decline: outside-appetite, insufficient-info, pricing-not-competitive[recorded, not computed]; Withdraw: broker-withdrew, bound-elsewhere, duplicate).

**Assumptions (to be validated):**
- Reuses the F0007 Lost-reason pattern (`reasonCode` + `reasonDetail` when `Other`) for consistency.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (`400`/`409`/`403`/`404`)
- [ ] Permissions enforced
- [ ] Audit/timeline logged (terminal transition + reason)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
