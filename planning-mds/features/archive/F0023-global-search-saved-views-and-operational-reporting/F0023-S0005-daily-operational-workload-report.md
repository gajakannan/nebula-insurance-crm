# F0023-S0005: Daily operational workload report

**Story ID:** F0023-S0005
**Feature:** F0023 - Global Search, Saved Views & Operational Reporting
**Title:** Daily operational workload report
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution Operations Manager
**I want** a daily workload report by owner, status, and due window
**So that** I can identify overloaded owners and records requiring attention today

## Context & Background

Managers currently rebuild due-work and owner workload lists manually. The first operational report should answer daily management questions over existing workflow and task records.

## Acceptance Criteria

**Happy Path:**
- **Given** I am authorized to view operational reports
- **When** I open the workload report
- **Then** I see counts for due today, due this week, past due, and open work by owner/status
- **And** each count can be drilled into a filtered list of source records
- **And** each drilldown row opens the authoritative source screen

**Alternative Flows / Edge Cases:**
- No work matches a due window -> show zero with an empty drilldown state.
- A source record becomes unauthorized -> remove it from counts and drilldowns.
- A source feature is unavailable -> show a partial-data message naming the unavailable object type without blocking the rest of the report.
- System error -> show user-safe error and preserve filters for retry.

**Checklist:**
- [ ] Report includes submissions, renewals, policies where due data exists, and tasks.
- [ ] Filters include owner, status, due window, region, line of business, and object type where available.
- [ ] Drilldowns reuse the same permission and criteria behavior as search results.
- [ ] Saved views can capture workload report criteria.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only report story. The report does not assign work or mutate source workflow state; assignment remains owned by upstream workflow/task features.

## Data Requirements

**Required Fields:**
- Object type, source record ID, owner, status, due date/due window, source route.

**Optional Fields:**
- Region, line of business, broker, account, team scope, priority.

**Validation Rules:**
- Date filters use inclusive calendar ranges in the user's locale.
- Missing due dates are grouped separately from due/past-due buckets.

## Role-Based Visibility

**Roles that can view workload reports:**
- Distribution manager, Program manager, Admin - full manager report within authorization scope.
- Distribution user, Relationship manager, Underwriter - may view own/team report slices when authorized.

**Data Visibility:**
- Counts and drilldown rows include only records authorized for the current user.
- InternalOnly data remains hidden from unauthorized users.

## Non-Functional Expectations

- Performance: report summaries use bounded result sets and asynchronous loading states where needed.
- Security: report counts cannot reveal hidden records.
- Reliability: partial-source failures are visible and do not corrupt other report sections.

## Dependencies

**Depends On:**
- F0003/F0004 - task ownership and due dates.
- F0006/F0007 - submission and renewal workflow states.
- F0018/F0019 - policy/submission source context where applicable.

**Related Stories:**
- F0023-S0003 - personal saved views for report criteria.
- F0023-S0004 - team saved views/default report views.
- F0023-S0007 - permission-safe counts.

## Out of Scope

- Work assignment actions.
- Workload balancing rules.
- Scheduled report emails.
- Free-form report builder.

## UI/UX Notes

- Screens involved: Operational Reports Workspace.
- Key interactions: filter report, view summary counts, drill into source record list, open source record.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Workload report uses existing owner/status/due fields and does not create new workflow ownership semantics.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only report)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0023-S0005-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
