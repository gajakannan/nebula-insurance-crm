# F0022-S0006: Reassign and rebalance queued work

**Story ID:** F0022-S0006
**Feature:** F0022 — Work Queues, Assignment Rules & Coverage Management
**Title:** Reassign and rebalance queued work
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution Operations Manager
**I want** to reassign and rebalance queued work
**So that** I can correct routing outcomes and reduce overload without leaving the queue workspace

## Context & Background

F0022 preserves manual control. Routing proposes or sets assignments, but authorized managers need an explicit override path with reason and audit evidence.

## Acceptance Criteria

**Happy Path:**
- **Given** I am authorized to manage a queue
- **When** I select one or more queued items and reassign them to an eligible user
- **Then** the selected work updates to the new assignee
- **And** each item records manual override, reason, prior assignee, new assignee, and timestamp
- **And** each item receives a timeline audit event for the reassignment or rebalance action
- **And** the queue worklist reflects the change after reload

**Alternative Flows / Edge Cases:**
- Target assignee is inactive or unauthorized -> reject reassignment.
- Item closed or no longer in queue -> skip item and show item-level result.
- Bulk rebalance partially succeeds -> show success/failure per item.
- Rebalance would assign to covered/out-of-office user -> require manager confirmation or choose eligible backup.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Queue Worklist -> row action | Reassign item | Enabled for manager/admin on open queued work | Assignee updated with override reason | Reload shows new assignee; audit event visible | Manager/Admin only; open work only |
| Queue Worklist -> bulk action | Rebalance selected items | Enabled for manager/admin | Eligible items reassigned; item-level failures preserved | Reload shows updated assignees and result summary | Manager/Admin only |

Required checks for mutation stories:
- [ ] Render-only behavior cannot satisfy the story.
- [ ] Save path has validation and error behavior.
- [ ] Successful mutation records audit/timeline evidence.
- [ ] Tests prove persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Source item, prior assignee, new assignee, override reason, action type, actor, timestamp

**Validation Rules:**
- New assignee must be active, authorized, and eligible for the queue/work type.
- Override reason is required.

## Role-Based Visibility

**Roles that can reassign/rebalance:**
- Distribution Operations Manager, Program Manager, Admin

**Data Visibility:**
- Reassignment actions are available only for records within the actor's authorization scope.

## Non-Functional Expectations

- Reliability: bulk rebalance reports item-level success and failure without partial-state ambiguity.
- Security: reassignment must not grant visibility outside the assignee's authorized scope.

## Audit & Timeline Requirements

- Record manual override, reassignment, and rebalance events with actor, reason, prior assignee, new assignee, source item, and timestamp.
- Preserve failed item-level outcomes for manager review.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Rebalance uses explicit manager-selected targets in MVP, not predictive staffing.

## Dependencies

**Depends On:**
- F0022-S0005 — queue worklists

## Out of Scope

- Predictive staffing
- Automatic SLA-based escalation notifications

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Story filename matches `Story ID` prefix

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
