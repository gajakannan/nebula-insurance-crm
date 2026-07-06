# F0022-S0005: Queue worklists and aging visibility

**Story ID:** F0022-S0005
**Feature:** F0022 — Work Queues, Assignment Rules & Coverage Management
**Title:** Queue worklists and aging visibility
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution Operations Manager
**I want** queue worklists with aging, backlog, and no-match visibility
**So that** I can see where operational work is stuck and intervene quickly

## Context & Background

F0023 provides operational workload reporting. F0022 adds queue-native worklists and routing visibility so managers can operate queues, not just read aggregate reports.

## Acceptance Criteria

**Happy Path:**
- **Given** routed work exists
- **When** I open a queue worklist
- **Then** I see queued tasks, submissions, and renewals with source type, age, status, owner/assignee, routing reason, and source link
- **And** I can filter by work type, status, age band, assignee, and no-match/fallback state
- **And** selecting a row opens the authoritative source record

**Alternative Flows / Edge Cases:**
- Empty queue -> show an empty state with active filters.
- Unauthorized source record -> omit from worklist and counts.
- `Unassigned Operations Queue` has items -> show no-match reason prominently.
- Partial source failure -> show partial-data message without corrupting other queue rows.

## Interaction Contract

N/A — read-only story. This story exposes queue worklists and source navigation; reassignment and rebalance mutations are owned by F0022-S0006.

## Data Requirements

**Required Fields:**
- Queue ID, source type, source ID, source display label, age, status, assignee, routing reason, source route

**Validation Rules:**
- Counts and rows must apply the same authorization filter.
- Date/age bands use the user's locale calendar day.

## Role-Based Visibility

**Roles that can view queue worklists:**
- Queue members, authorized managers, Admin

**Data Visibility:**
- Worklist rows and counts must not reveal unauthorized records.

## Non-Functional Expectations

- Performance: queue worklists must be pageable and filterable for operational use.
- Security: counts and rows use the same authorization predicate.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- F0022 queue worklists complement F0023 reports rather than replacing the reporting workspace.

## Dependencies

**Depends On:**
- F0022-S0003 — routed work outcomes
- F0023 — operational reporting substrate for workload visibility concepts

## Out of Scope

- Saved views beyond what existing reporting/saved-view surfaces support
- Free-form report builder

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A — read-only story)
- [ ] Tests pass
- [ ] Story filename matches `Story ID` prefix

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
