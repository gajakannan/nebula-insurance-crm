# F0024-S0004: Transition a service case through servicing statuses

**Story ID:** F0024-S0004
**Feature:** F0024 — Claims & Service Case Tracking
**Title:** Transition a service case through servicing statuses
**Priority:** High
**Phase:** MVP+

## User Story

**As a** service user or manager
**I want** to transition a service case through clear servicing statuses
**So that** everyone can see whether the issue is new, active, waiting, resolved, or closed

## Context & Background

Service-case status gives the first operational workflow without becoming claims adjudication. Status transitions must be explicit and auditable.

## Acceptance Criteria

**Happy Path:**
- **Given** an authorized internal user opens a service case in Intake, In Progress, Waiting, or Resolved
- **When** they select an allowed next status and provide any required transition note
- **Then** the case status changes
- **And** reloading the case shows the new status
- **And** a transition/audit event records actor, prior status, new status, timestamp, and note

**Alternative Flows / Edge Cases:**
- Invalid transition attempts are rejected and leave the case unchanged.
- Closing a case requires a resolution summary.
- Waiting status requires a waiting reason or next follow-up date.
- Closed cases are read-only unless Phase B approves a reopen path.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Service Case Detail status control | Select next status, enter required note, Confirm | Enabled for authorized internal users when transition is valid | Writes service-case status transition and timeline/audit event | New status persists after reload; timeline shows transition | Intake -> In Progress/Waiting; In Progress -> Waiting/Resolved; Waiting -> In Progress/Resolved; Resolved -> Closed |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Status, transition actor, transition timestamp.

**Optional Fields:**
- Transition note, waiting reason, resolution summary.

**Validation Rules:**
- Only allowed transition pairs are accepted.
- Resolution summary is required for Closed.
- Waiting reason or follow-up date is required for Waiting.

## Role-Based Visibility

**Roles that can transition:**
- Underwriter, Distribution Manager, assigned Distribution User, Admin.

**Data Visibility:**
- InternalOnly content: status notes and resolution summary.
- ExternalVisible content: none in MVP+.

## Non-Functional Expectations

- Performance: transition completes within normal CRM mutation expectations.
- Security: transition authorization is enforced before state change.
- Reliability: status transition and audit/timeline event are committed consistently.

## Dependencies

**Depends On:**
- F0024-S0001 — service case exists.

**Related Stories:**
- F0024-S0003 — ownership/follow-up fields.
- F0024-S0006 — audit-safe history.

## Out of Scope

- Claim coverage decisions.
- Payment or reserve status.
- Carrier claim lifecycle synchronization.

## UI/UX Notes

- Screens involved: Service Case Detail.
- Key interactions: status dropdown/action menu, transition confirmation, validation messaging.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking for Phase A approval.

**Assumptions (to be validated):**
- Status vocabulary can remain service-oriented and not mirror carrier claim statuses.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0024-S0004-...`)
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
