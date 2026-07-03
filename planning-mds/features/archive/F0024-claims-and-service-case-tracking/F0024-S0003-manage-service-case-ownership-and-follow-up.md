# F0024-S0003: Manage service case ownership, priority, and follow-up

**Story ID:** F0024-S0003
**Feature:** F0024 — Claims & Service Case Tracking
**Title:** Manage service case ownership, priority, and follow-up
**Priority:** High
**Phase:** MVP+

## User Story

**As a** service user or manager
**I want** to update owner, priority, due date, and follow-up details on a service case
**So that** active post-bind work has clear accountability and next action

## Context & Background

Service cases need lightweight work management without taking over F0022's broader queue/routing scope. This story provides manual ownership and follow-up updates only.

## Acceptance Criteria

**Happy Path:**
- **Given** an authorized internal user opens a service case detail
- **When** they change owner, priority, due date, or follow-up summary and save
- **Then** the case detail reflects the new values
- **And** reloading the case preserves the changes
- **And** the timeline/audit history records changed fields and actor

**Alternative Flows / Edge Cases:**
- Missing owner, priority, or due date on an active case shows validation and does not save.
- Unauthorized reassignment or update attempts are rejected.
- Closed cases are read-only except for Phase B-approved correction/reopen behavior.
- Follow-up task creation uses the existing task foundation when enabled from the case detail.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Service Case Detail | Edit owner, priority, due date, or follow-up fields, then Save | Enabled for authorized internal users on non-closed cases | Updates case work-management fields and emits audit/timeline event | Updated fields persist after reload; history shows before/after summary | Managers may reassign within scope; assigned users may update follow-up fields where authorized |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Owner, priority, due date, status.

**Optional Fields:**
- Follow-up summary, linked task, last follow-up date.

**Validation Rules:**
- Owner must be an active internal user in eligible scope.
- Due date is required for Intake, In Progress, and Waiting cases.
- Priority must use the approved priority set.

## Role-Based Visibility

**Roles that can update:**
- Distribution Manager, Underwriter, assigned Distribution User, Admin.

**Data Visibility:**
- InternalOnly content: owner/follow-up fields and service notes.
- ExternalVisible content: none in MVP+.

## Non-Functional Expectations

- Performance: update completes within normal CRM mutation expectations.
- Security: reassignment is authorization-checked.
- Reliability: update and audit/timeline event are committed consistently.

## Dependencies

**Depends On:**
- F0024-S0001 — service case exists.
- F0004 — existing task/follow-up foundation when a task is created.

**Related Stories:**
- F0024-S0004 — status transitions.
- F0024-S0006 — audit history.

## Out of Scope

- Automatic routing rules.
- Backup coverage rotation.
- Workload balancing and queue assignment rules.

## UI/UX Notes

- Screens involved: Service Case Detail.
- Key interactions: edit owner/priority/due date, create or link follow-up task, save.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking for Phase A approval.

**Assumptions (to be validated):**
- Advanced routing remains in F0022 and is not part of this MVP+ slice.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0024-S0003-...`)
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
