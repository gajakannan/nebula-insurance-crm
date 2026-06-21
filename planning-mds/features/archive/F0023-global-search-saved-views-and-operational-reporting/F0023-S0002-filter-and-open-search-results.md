# F0023-S0002: Filter, sort, and open search results

**Story ID:** F0023-S0002
**Feature:** F0023 - Global Search, Saved Views & Operational Reporting
**Title:** Filter, sort, and open search results
**Priority:** High
**Phase:** MVP

## User Story

**As a** Relationship Manager
**I want** to narrow global search results with CRM filters and open the source record
**So that** I can distinguish similar records and continue work from the correct screen

## Context & Background

Global search creates the first result set. Users also need object-type, status, owner, date, line-of-business, and region filters that match existing CRM dimensions and preserve context while they navigate.

## Acceptance Criteria

**Happy Path:**
- **Given** I have submitted a global search
- **When** I apply object type, status, owner, due date, region, or line-of-business filters
- **Then** the result list updates to the authorized records matching all selected criteria
- **And** the active criteria are visible in the workspace URL or equivalent deep-linkable state
- **And** opening a result navigates to the source record screen
- **And** no audit/timeline event is created because filtering and source navigation are read-only actions

**Alternative Flows / Edge Cases:**
- A selected filter has zero authorized matches -> show an empty filtered state and preserve the selected filters.
- A result no longer exists -> show a not-found state and keep the search criteria available.
- A result becomes unauthorized between search and open -> show a forbidden outcome on source navigation without exposing hidden details.

**Checklist:**
- [ ] Filters are available only when the source data dimension exists.
- [ ] Sorting supports at least relevance/default, updated date, due date, and status where applicable.
- [ ] Back navigation returns to the same search criteria.
- [ ] Narrow layout moves filters behind a filter control without hiding active criteria.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only story. Applying filters and opening records does not mutate CRM records.

## Data Requirements

**Required Fields:**
- Query, selected object types, status values, owner IDs, due-date range, region, line of business.
- Source record ID and source route.

**Optional Fields:**
- Sort field and sort direction.
- Result page number and page size.

**Validation Rules:**
- Invalid filter keys are rejected with a user-safe validation message.
- Page size is bounded by the architecture-approved maximum.

## Role-Based Visibility

**Roles that can filter and open results:**
- Distribution user, Distribution manager, Relationship manager, Program manager, Underwriter, Admin - may filter and open records within authorization scope.

**Data Visibility:**
- InternalOnly content remains hidden from unauthorized users.
- Source navigation uses the source feature's existing authorization behavior.

## Non-Functional Expectations

- Performance: filtering uses bounded result pages and does not block the authenticated shell.
- Security: filters never reveal hidden object existence through counts.
- Reliability: criteria survive browser reload and back/forward navigation.

## Dependencies

**Depends On:**
- F0023-S0001 - grouped global search results.
- Existing source routes for broker, account, policy, submission, renewal, and task records.

**Related Stories:**
- F0023-S0003 - personal saved views reuse the same criteria.
- F0023-S0007 - permission-safe counts and hidden-record behavior.

## Out of Scope

- Custom user-defined fields in filters.
- Free-form query builder.
- Export-first reporting workflows.

## UI/UX Notes

- Screens involved: Search Results Workspace.
- Key interactions: add/remove filter, sort, paginate, open result, return to prior criteria.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Each source feature remains responsible for its own detail-screen authorization and not-found behavior.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only filtering/navigation)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0023-S0002-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
