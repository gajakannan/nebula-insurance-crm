---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0006-S0004: Submission Intake Status Transitions

**Story ID:** F0006-S0004
**Feature:** F0006 — Submission Intake Workflow
**Title:** Submission intake status transitions
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user or distribution manager
**I want** to advance a submission through its intake workflow states (Received → Triaging → WaitingOnBroker / ReadyForUWReview)
**So that** submission progress is tracked, auditable, and enforces the correct handoff to underwriting

## Context & Background

The submission intake workflow has four states with defined transitions and role-based gating. Distribution users own all intake transitions. The key business rule is that advancing to ReadyForUWReview requires the completeness check to pass and an underwriter to be assigned. The transition to WaitingOnBroker represents a deliberate pause for broker follow-up. Every transition is recorded as an immutable WorkflowTransition and ActivityTimelineEvent per ADR-011.

## Acceptance Criteria

**Happy Path — Begin Triage:**
- **Given** a submission in Received state
- **When** a distribution user triggers "Begin Triage"
- **Then** the submission status changes to Triaging, a WorkflowTransition (Received→Triaging) is appended, an ActivityTimelineEvent is created, and UpdatedAt/UpdatedByUserId are set

**Happy Path — Follow Up with Broker:**
- **Given** a submission in Triaging state with missing information
- **When** the distribution user triggers "Wait on Broker" with an optional reason/note
- **Then** the submission status changes to WaitingOnBroker, the reason is stored in the WorkflowTransition record, and both audit records are appended

**Happy Path — Advance to Underwriting:**
- **Given** a submission in Triaging state where completeness check passes and AssignedToUserId references an underwriter
- **When** the distribution user triggers "Ready for UW Review"
- **Then** the submission status changes to ReadyForUWReview, transition records are appended, and the submission is visible to the assigned underwriter

**Happy Path — Advance from WaitingOnBroker:**
- **Given** a submission in WaitingOnBroker state where completeness check now passes and an underwriter is assigned
- **When** the distribution user triggers "Ready for UW Review"
- **Then** the submission status changes to ReadyForUWReview with transition records appended

**Alternative Flows / Edge Cases:**
- Invalid transition (e.g., Received → ReadyForUWReview) → HTTP 409 with `code=invalid_transition`
- Completeness check fails on Triaging → ReadyForUWReview → HTTP 409 with `code=missing_transition_prerequisite`, detail listing missing fields/documents
- No underwriter assigned on Triaging → ReadyForUWReview → HTTP 409 with `code=missing_transition_prerequisite`, detail: "AssignedToUserId must reference a user with Underwriter role"
- User lacks route/action-level `submission:transition` permission or transition-role permission → HTTP 403
- Submission is soft-deleted, not found, or hidden by entity-scope visibility rules → HTTP 404
- Concurrent transition (stale `If-Match` / rowVersion) → HTTP 412 with `code=precondition_failed`
- Transition from a downstream state (InReview, Quoted, etc.) via F0006 → HTTP 409 with `code=invalid_transition` (downstream transitions owned by F0019)

**Checklist:**
- [ ] Allowed transitions enforced: Received→Triaging, Triaging→WaitingOnBroker, Triaging→ReadyForUWReview, WaitingOnBroker→ReadyForUWReview
- [ ] Role gating: Distribution User, Distribution Manager, and Admin for all intake transitions
- [ ] Triaging→ReadyForUWReview guard: completeness check must pass (required fields + document categories)
- [ ] Triaging→ReadyForUWReview guard: AssignedToUserId must reference a user with Underwriter role
- [ ] WaitingOnBroker→ReadyForUWReview: same guards as Triaging→ReadyForUWReview
- [ ] Triaging→WaitingOnBroker: optional reason/note captured in transition record
- [ ] Each transition appends one WorkflowTransition record (FromState, ToState, Reason, ActorUserId, OccurredAt)
- [ ] Each transition appends one ActivityTimelineEvent record
- [ ] Transition + audit records created atomically (single DB transaction)
- [ ] Invalid transitions return HTTP 409 with RFC 7807 ProblemDetails
- [ ] Action-level or transition-policy denials return HTTP 403; hidden/out-of-scope submissions return HTTP 404
- [ ] Optimistic concurrency enforced via `rowVersion` + `If-Match`

## Data Requirements

**Required Fields (transition request):**
- `toState` (string): Target state

**Optional Fields:**
- `reason` (string): Transition reason/comment (recommended for WaitingOnBroker)

**Validation Rules:**
- Transition must be in the allowed transition set for the current state
- Actor must have the required Casbin permission (`submission:transition`) and role for the transition
- Completeness guards enforced for transitions to ReadyForUWReview
- Assignment guard enforced for transitions to ReadyForUWReview
- Optimistic concurrency check on submission record

## Role-Based Visibility

**Roles that can transition (intake scope):**
- Distribution User — Received→Triaging, Triaging→WaitingOnBroker, Triaging→ReadyForUWReview, WaitingOnBroker→ReadyForUWReview
- Distribution Manager — All intake transitions (for any submission in scope)
- Admin — All transitions

**Data Visibility:**
- Transition history is visible to all roles that can read the submission
- WorkflowTransition records are immutable

## Non-Functional Expectations

- Performance: Transition completes in < 500ms
- Security: Role-based transition gating enforced server-side; ABAC scoping on the submission
- Reliability: Optimistic concurrency prevents conflicting transitions; transition + audit record creation is atomic (single DB transaction)

## Dependencies

**Depends On:**
- F0006-S0002 — Submissions must exist before they can be transitioned
- F0006-S0005 — Completeness evaluation used as a transition guard

**Related Stories:**
- F0006-S0003 — Transition buttons on detail view action bar
- F0006-S0006 — Assignment must be set before ReadyForUWReview
- F0006-S0007 — Timeline records created by each transition

## Business Rules

1. **Completeness Gate for ReadyForUWReview:** Transitions to ReadyForUWReview (from Triaging or WaitingOnBroker) require the completeness check to pass — all required fields populated and required document categories linked (when F0020 available). Failure returns HTTP 409 with `code=missing_transition_prerequisite` listing every missing item.
2. **Underwriter Assignment Prerequisite:** Transitions to ReadyForUWReview require `AssignedToUserId` to reference a user with the Underwriter role. A non-null assignment to a non-underwriter user does not satisfy this guard.
3. **Forward-Only Transitions:** Intake transitions are forward-only in MVP. There is no undo, revert, or backward transition. Corrections are handled by moving forward through the workflow. Compensating transitions are explicitly deferred.
4. **Transition Atomicity (ADR-011):** Every transition atomically performs three operations in a single DB transaction: (a) update Submission.CurrentStatus, (b) append one WorkflowTransition record, (c) append one ActivityTimelineEvent. If any part fails, the entire operation rolls back.
5. **Two-Layer Authorization:** Transition authorization uses two layers per ADR-011: (a) Casbin ABAC gates `submission:transition` action per role (policy.csv §2.3), (b) application-layer guard enforces which specific from→to transitions each role may perform. Route/action-level deny returns HTTP 403. When the caller has general transition capability but the specific submission is outside resolved visibility scope, the API may cloak that outcome as HTTP 404.
6. **Optimistic Concurrency:** All transitions enforce optimistic concurrency via `rowVersion` + `If-Match` to prevent conflicting concurrent transitions on the same submission. Stale versions return HTTP 412 with `code=precondition_failed`.
7. **F0006/F0019 Boundary:** F0006 owns only intake transitions (Received→Triaging, Triaging→WaitingOnBroker, Triaging→ReadyForUWReview, WaitingOnBroker→ReadyForUWReview). Any attempt to transition from ReadyForUWReview onward via F0006 returns HTTP 409 with `code=invalid_transition`. Downstream states are owned by F0019.

## Out of Scope

- Compensating transitions (undo/revert) — corrections are forward-only in MVP
- Downstream transitions (ReadyForUWReview → InReview, etc.) — owned by F0019
- Automated transitions triggered by external events
- Transition approval workflows (multi-step approval before advancing)

## UI/UX Notes

- Screens involved: Submission Detail (action bar)
- Key interactions: Click transition button → optional reason dialog (for WaitingOnBroker) → submit → status badge updates, timeline refreshes
- WaitingOnBroker dialog: text field for follow-up reason/note
- ReadyForUWReview button: disabled with tooltip if completeness fails or no underwriter assigned; tooltip shows what is missing
- Transition buttons are contextual — only valid next-state buttons shown for the current state

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Transitions are triggered from the Submission Detail action bar (not from the list view)
- Distribution Manager can perform all intake transitions on any submission in their ABAC scope
- Completeness failure on transition returns a structured error listing each missing item

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (role-based transition gating + ABAC)
- [ ] Audit/timeline logged (WorkflowTransition + ActivityTimelineEvent per transition)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0006-S0004-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
