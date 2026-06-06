# Downstream Submission Workflow Timeline and Audit Trail

## Story Header

**Story ID:** F0019-S0008
**Feature:** F0019 — Submission Quoting, Proposal & Approval Workflow
**Title:** Downstream submission workflow timeline and audit trail
**Priority:** Medium
**Phase:** CRM Release MVP

## User Story

**As a** underwriter or distribution user
**I want** to see a complete, ordered timeline of a submission's downstream workflow events
**So that** I can audit how a submission reached its decision and who did what, when

## Context & Background

ADR-011 requires every workflow transition to append both a `WorkflowTransition` and an
`ActivityTimelineEvent`. F0006 surfaced the intake timeline. This story ensures the **downstream**
events introduced by F0019 — `InReview`/`Quoted`/`BindRequested`/`Bound` transitions, approval
grant/decline, terminal `Declined`/`Withdrawn`, packet updates, bind handoff, and archive/reactivate —
are emitted by their owning stories and surfaced coherently on the submission detail timeline. This
is primarily a read/surfacing slice that consolidates the audit trail; the write side is owned by
S0001–S0006.

## Acceptance Criteria

**Happy Path:**
- **Given** a submission that has moved through downstream states
- **When** a user opens the submission detail timeline
- **Then** all downstream `ActivityTimelineEvent`s appear in chronological order with event type, pre-rendered description, actor identity, and timestamp — including transitions, approval decisions (with reason), bind + handoff, terminal decisions (with reason), and archive/reactivate.

**Coverage / consistency:**
- Each downstream mutation owned by S0001–S0006 emits exactly one `ActivityTimelineEvent` (plus a `WorkflowTransition` for state changes), atomically with the change.
- Events are append-only: no edit or delete of past events.

**Alternative Flows / Edge Cases:**
- Submission with no downstream activity yet (still `ReadyForUWReview`) → shows intake events only; no empty/error state.
- Unauthorized actor requests the timeline → `403 Forbidden`.
- Large history → paginated/virtualized; ordering remains stable.
- Submission not found → `404`.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A — read-only story. The timeline **displays** append-only events; it never edits them. Event
writes are owned by the mutation stories (S0001–S0006).

## Data Requirements

**Displayed Fields (timeline event):**
- `eventType`, `eventDescription` (pre-rendered), `actorUserId`, `occurredAt`, optional `reason`/`reasonCode`

**Event types surfaced (emitted by owning stories):**
- `SubmissionTransitioned` (InReview/Quoted/BindRequested/Bound/Declined/Withdrawn)
- `SubmissionApprovalGranted` / `SubmissionApprovalDeclined`
- `SubmissionBound` + handoff note
- `SubmissionPacketUpdated`
- `SubmissionArchived` / `SubmissionReactivated`

**Validation Rules:**
- Events are append-only and immutable; ordering by `occurredAt` then stable tiebreaker.

## Role-Based Visibility

**Roles that can view the timeline:**
- Underwriter, Distribution user, Admin — permitted, ABAC-scoped to the submission
- BrokerUser — no access in MVP

**Data Visibility:**
- InternalOnly content: actor identity, reasons, approval/bind details.
- ExternalVisible content: none in MVP.

## Non-Functional Expectations

- Performance: timeline loads < 1s p95; pagination for long histories.
- Security: ABAC-scoped; unauthorized → `403`.
- Reliability: read view degrades gracefully if a single event payload is malformed (skips/flags it rather than failing the whole timeline).

## Dependencies

**Depends On:**
- F0019-S0001 through S0006 — emit the events this story surfaces.
- ADR-011 — `WorkflowTransition` + `ActivityTimelineEvent` contract and atomicity.

**Related Stories:**
- F0019-S0007 — list-level visibility complements per-submission timeline.

## Business Rules

1. **Append-only audit:** timeline events are immutable history; this story never mutates them.
2. **One event per mutation:** each downstream change produces exactly one timeline event (plus a transition record for state changes) for a clean audit trail.
3. **Coherent surfacing:** downstream events render consistently with F0006 intake events on one timeline.

## Out of Scope

- Emitting the events themselves (owned by S0001–S0006) — this story verifies coverage and surfaces them.
- Cross-entity activity feeds / reporting (F0021, F0023).
- Exporting the audit trail (Future).

## UI/UX Notes

- Screens involved: Submission Detail (Activity Timeline panel).
- Key interactions: chronological event list, reason/actor display, pagination for long histories.

## Questions & Assumptions

**Open Questions:**
- [ ] Should approval reasons and decline reasons be visible to distribution users, or underwriting-only? (Default assumption: visible to internal roles in MVP; revisit if sensitivity requires masking.)

**Assumptions (to be validated):**
- Reuses the F0006/F0007 `ActivityTimelineEvent` rendering pattern; no new timeline component is needed.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (`403`/`404`, empty history, malformed-event resilience)
- [ ] Permissions enforced (ABAC scope)
- [ ] Audit/timeline coverage verified for every downstream mutation (S0001–S0006)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
