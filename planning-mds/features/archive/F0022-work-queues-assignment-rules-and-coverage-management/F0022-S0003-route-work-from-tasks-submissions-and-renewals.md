# F0022-S0003: Route work from tasks, submissions, and renewals

**Story ID:** F0022-S0003
**Feature:** F0022 — Work Queues, Assignment Rules & Coverage Management
**Title:** Route work from tasks, submissions, and renewals
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** Distribution Operations Manager
**I want** tasks, submissions, and renewals routed through the queue engine
**So that** the initial release covers the operational work users already manage

## Context & Background

The initial routed work types are tasks, submissions, and renewals. F0022 consumes existing source workflows and ownership models; it does not redefine the submission or renewal state machines.

## Acceptance Criteria

**Happy Path:**
- **Given** active queues and assignment rules exist
- **When** a task, submission, or renewal becomes route-eligible
- **Then** routing evaluates the active rules in approved precedence order
- **And** the work item receives a queue assignment and assignee outcome when a rule matches
- **And** the routing outcome records rule version, matched reason, selected queue, selected assignee, and timestamp
- **And** the source record timeline receives an audit event for the routing outcome

**Alternative Flows / Edge Cases:**
- No rule matches -> route to `Unassigned Operations Queue`.
- Source record is no longer authorized for the selected assignee -> do not assign; route to fallback and record exception.
- Duplicate routing trigger -> do not create duplicate queue entries.
- Source record closes before routing -> skip routing and record skipped reason.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Source workflow action / background routing trigger | Route eligible work | System mutation from source workflow event | Queue assignment outcome is created or updated idempotently | Reload source record and queue worklist show same assignment | Only active, route-eligible source records |
| Queue Worklist -> item detail | Open routed item | Read-only for routing result | No mutation | Routing reason visible after reload | Authorized users only |

Required checks for mutation stories:
- [ ] Render-only behavior cannot satisfy routing.
- [ ] Failed routing has validation/error behavior.
- [ ] Successful routing records audit/timeline evidence.
- [ ] Tests prove persisted queue assignment after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Source type, source ID, queue ID, assignee ID when assigned, rule version, match reason, routing timestamp, routing status

**Validation Rules:**
- Routing must be idempotent for repeated source events.
- Routing must preserve source workflow status; it does not transition submissions or renewals by itself.

## Role-Based Visibility

**Roles that can view routed work:**
- Assigned user, queue members, authorized managers, Admin

**Data Visibility:**
- Queue worklists and counts include only authorized records.

## Non-Functional Expectations

- Reliability: routing is idempotent for repeated source events.
- Observability: routing outcomes include rule version, match reason, queue, assignee, and fallback reason when applicable.

## Audit & Timeline Requirements

- Record routing outcome events for matched, skipped, fallback, and exception cases.
- Include source type/source ID, rule version, selected queue, selected assignee when present, and timestamp.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Route eligibility is determined from existing task, submission, and renewal lifecycle state rather than new workflow states in F0022.

## Dependencies

**Depends On:**
- F0022-S0001 — queues
- F0022-S0002 — rules
- F0004, F0006, F0007 — source work foundations

## Out of Scope

- Changing source workflow state machines
- Carrier/outbound communication automation

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Story filename matches `Story ID` prefix

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
