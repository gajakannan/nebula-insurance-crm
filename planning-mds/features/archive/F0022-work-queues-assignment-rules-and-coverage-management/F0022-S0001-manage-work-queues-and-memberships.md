# F0022-S0001: Manage work queues and memberships

**Story ID:** F0022-S0001
**Feature:** F0022 — Work Queues, Assignment Rules & Coverage Management
**Title:** Manage work queues and memberships
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** Distribution Operations Manager
**I want** to create and maintain named work queues and queue memberships
**So that** operational work has explicit team ownership before centralized F0032 governance exists

## Context & Background

F0004 provides personal task assignment, while F0006 and F0007 provide source workflow ownership. F0022 adds the queue foundation those workflows can route into.

## Acceptance Criteria

**Happy Path:**
- **Given** I am an authorized manager
- **When** I create a queue with a name, work type, active status, and members
- **Then** the queue is saved and appears in the queue list
- **And** its membership is visible from the queue detail surface
- **And** an audit/timeline event records the queue creation and membership selection

**Alternative Flows / Edge Cases:**
- Duplicate queue name within the same work type -> reject with a clear validation error.
- Queue without active members -> allowed only when inactive.
- Deactivating a queue with open work -> show a blocking warning and require work to be reassigned or moved first.
- Unauthorized user -> deny create/update/delete actions without changing queue state.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Work Queues -> New Queue | Create queue and select members | Enabled for manager/admin | Queue and membership records created | Reload shows queue and members; audit event visible | Manager/Admin only |
| Work Queues -> Queue Detail | Edit name, status, and members | Enabled for manager/admin when queue can be changed | Queue metadata and membership updated | Reload shows updated queue; audit event visible | Manager/Admin only |

Required checks for mutation stories:
- [ ] Render-only behavior cannot satisfy the story.
- [ ] Save path has validation and error behavior.
- [ ] Successful mutation records audit/timeline evidence.
- [ ] Tests prove persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Queue name: human-readable manager label
- Work type: task, submission, or renewal
- Active status: whether routing can target the queue
- Members: eligible assignees for routed work

**Validation Rules:**
- Queue name is required.
- Work type is required.
- Active queues require at least one active member.

## Role-Based Visibility

**Roles that can manage queues:**
- Distribution Operations Manager, Program Manager, Admin

**Data Visibility:**
- Users see queues only within their authorized operational scope.
- InternalOnly content remains hidden from unauthorized users.

## Non-Functional Expectations

- Security: manager-only queue mutations must be enforced server-side.
- Reliability: save failures must leave prior queue state unchanged.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Queue membership uses active internal users already available to assignment surfaces.

## Dependencies

**Depends On:**
- F0004 — task assignment foundation
- F0006 — submission workflow foundation
- F0007 — renewal workflow foundation

## Out of Scope

- F0032 publish/rollback governance
- Predictive staffing
- External broker queue administration

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated if needed
- [ ] Story filename matches `Story ID` prefix

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
