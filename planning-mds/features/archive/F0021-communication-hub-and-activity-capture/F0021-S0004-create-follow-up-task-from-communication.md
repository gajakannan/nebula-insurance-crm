# F0021-S0004: Create a follow-up task from a communication

**Story ID:** F0021-S0004
**Feature:** F0021 — Communication Hub & Activity Capture
**Title:** Create a follow-up task from a communication
**Priority:** High
**Phase:** MVP

## User Story

**As a** coordinator, underwriter, or relationship manager
**I want** to create a follow-up task while capturing or reviewing a communication
**So that** next steps are assigned and tracked without losing communication context

## Context & Background

F0004 provides task creation and assignment. F0021 should reuse that capability and link follow-up tasks back to communication events.

## Acceptance Criteria

**Happy Path:**
- **Given** a user is capturing a communication event
- **When** they select "Create follow-up task", provide task title, assignee, due date, and save
- **Then** the communication event is saved
- **And** a linked task is created using existing task behavior
- **And** the communication detail shows the follow-up task status
- **And** the task links back to the communication event and primary record
- **And** timeline/audit evidence records that the follow-up task was created from the communication event

**Alternative Flows / Edge Cases:**
- Follow-up is optional; communication can save without a task.
- If task creation validation fails, the user sees task validation errors and no partial follow-up is created.
- If communication save succeeds but task creation cannot be completed atomically, Phase B must define retry/rollback behavior before implementation.
- Users without task creation permission cannot create the follow-up.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Add Communication drawer | Enable follow-up, complete task fields, Save | Follow-up fields enabled when user has task create permission | Creates communication event and linked task | Communication detail shows task chip/status; Task Center shows task after reload | User must be allowed to create task for selected assignee |
| Communication detail | Click Create Follow-up | Enabled for users with access to communication and task create permission | Creates task linked to existing communication event | Communication detail shows linked task after reload | Existing communication must not be redacted in a way that blocks task context |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Task title.
- Assignee.
- Linked communication event id.
- Linked primary entity.

**Optional Fields:**
- Due date.
- Priority.
- Description.

**Validation Rules:**
- Task validation reuses existing task rules.
- Follow-up task must reference the communication event.

## Role-Based Visibility

**Roles that can create follow-up:**
- Users who can view the communication and create a task under existing task authorization.

**Data Visibility:**
- Task detail visibility follows existing task rules.
- Communication link shown only to users who can view the communication.

## Non-Functional Expectations

- Reliability: no orphan follow-up link; task and communication linkage remain consistent.

## Dependencies

**Depends On:**
- F0004 — task creation UI and assignment.
- F0021-S0001 — communication event capture.

**Related Stories:**
- F0021-S0002 — display follow-up status.

## Out of Scope

- New workflow engine for follow-ups.
- Automated task recommendations.

## UI/UX Notes

- Screens involved: Add Communication drawer and communication detail.
- Key interactions: optional follow-up panel, assignee picker, due date, save.

## Questions & Assumptions

**Open Questions:**
- [ ] Phase B must define transaction/rollback behavior for communication + task creation.

**Assumptions (to be validated):**
- Existing task API can represent a communication-linked task through linked entity fields or a Phase B-approved extension.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0021-S0004-...`)
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
