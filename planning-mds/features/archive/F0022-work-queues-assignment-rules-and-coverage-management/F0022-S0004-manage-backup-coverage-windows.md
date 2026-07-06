# F0022-S0004: Manage backup coverage windows

**Story ID:** F0022-S0004
**Feature:** F0022 — Work Queues, Assignment Rules & Coverage Management
**Title:** Manage backup coverage windows
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution Operations Manager
**I want** to define explicit backup coverage windows
**So that** work can continue during planned absence without relying on informal handoffs

## Context & Background

Coverage does not activate from inferred inactivity. Managers or authorized users define explicit windows that the routing engine can evaluate ahead of territory/ownership and workload balancing.

## Acceptance Criteria

**Happy Path:**
- **Given** I am authorized to manage coverage
- **When** I create a coverage window for a queue member with backup assignee, start, end, and covered work types
- **Then** eligible work during the window routes to the backup assignee according to rule precedence
- **And** the coverage window is visible in the queue coverage view
- **And** coverage creation is audited

**Alternative Flows / Edge Cases:**
- Coverage window has no end date -> reject for MVP.
- Coverage overlaps for the same person/work type -> require a clear validation error.
- Backup assignee is inactive or unauthorized -> reject save.
- Coverage expires -> future routing stops using it without deleting history.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Work Queues -> Coverage tab | Create coverage window | Enabled for manager/admin | Coverage record saved | Reload shows active/upcoming window; audit event visible | Manager/Admin only |
| Work Queues -> Coverage tab | Edit/end coverage window | Enabled for manager/admin | Coverage dates/backup updated or ended | Reload shows updated status; future routing uses update | Manager/Admin only |

Required checks for mutation stories:
- [ ] Render-only behavior cannot satisfy the story.
- [ ] Save path has validation and error behavior.
- [ ] Successful mutation records audit/timeline evidence.
- [ ] Tests prove persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Covered user, backup user, queue/work type, start, end, status, reason

**Validation Rules:**
- Start must be before end.
- Backup user must be active and authorized for the covered work scope.

## Role-Based Visibility

**Roles that can manage coverage:**
- Distribution Operations Manager, Program Manager, Admin

**Data Visibility:**
- Coverage entries expose only authorized users and queues.

## Non-Functional Expectations

- Reliability: expired coverage windows remain historical evidence but do not affect future routing.
- Security: backups must be eligible for the covered work scope before activation.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Coverage windows are managed inside Nebula for MVP; calendar/HR integrations are deferred.

## Dependencies

**Depends On:**
- F0022-S0001 — queue membership
- F0022-S0002 — precedence rules

## Business Rules

1. Coverage activates only from explicit coverage windows.
2. Inactivity alone does not activate coverage.

## Out of Scope

- HR absence management
- Calendar integrations
- Inferred availability detection

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Story filename matches `Story ID` prefix

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
