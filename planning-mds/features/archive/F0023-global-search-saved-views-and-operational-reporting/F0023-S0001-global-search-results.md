# F0023-S0001: Global search returns grouped CRM results

**Story ID:** F0023-S0001
**Feature:** F0023 - Global Search, Saved Views & Operational Reporting
**Title:** Global search returns grouped CRM results
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** Relationship Manager
**I want** one global search entry that returns grouped CRM records
**So that** I can find the correct broker, account, policy, submission, renewal, or task without visiting each list page

## Context & Background

Nebula already has entity-specific lists and 360/workflow screens. Users need one authenticated shell search surface that finds high-value CRM records and sends them to the authoritative source screen.

## Acceptance Criteria

**Happy Path:**
- **Given** I am an authenticated internal user on any CRM screen
- **When** I enter a search term of at least 2 non-space characters in the global search entry
- **Then** I see grouped result sections for matching in-scope object types
- **And** each result row includes object type, primary display label, stable identifier when available, status when available, owner when available, and a source-screen link
- **And** each group shows only the count of records I am authorized to see

**Alternative Flows / Edge Cases:**
- Empty query -> show no result request and keep focus in the search input.
- No matching authorized records -> show "No results match this search" without implying hidden records exist.
- Search service unavailable -> show a user-safe error and a retry action without clearing the typed query.
- Unauthorized records -> omit the record, snippet, suggestion, and count; do not show a forbidden placeholder.

**Checklist:**
- [ ] In-scope groups include broker, MGA/program context, account, policy, submission, renewal, and task when matching records exist.
- [ ] Result rows open existing source screens rather than creating duplicate detail pages.
- [ ] The search entry is reachable from the authenticated application shell.
- [ ] Result rendering works with keyboard submission and screen-reader labels.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only story. The user submits search criteria and opens existing records; no CRM data is mutated.

## Data Requirements

**Required Fields:**
- Search term: trimmed text, minimum 2 characters, maximum 100 characters.
- Object type: broker, MGA/program, account, policy, submission, renewal, or task.
- Result label: display label appropriate to the source object.
- Source route: link to the authoritative CRM screen.

**Optional Fields:**
- Stable identifier: policy number, submission number, renewal reference, task title, broker license, or account reference when present.
- Status, owner, due date, line of business, region.

**Validation Rules:**
- Queries shorter than 2 non-space characters do not execute.
- Queries over 100 characters show a validation message and do not execute.

## Role-Based Visibility

**Roles that can search:**
- Distribution user, Distribution manager, Relationship manager, Program manager, Underwriter, Admin - may search internal records within their ABAC authorization scope.

**Data Visibility:**
- InternalOnly content: included only for internal roles with record access.
- ExternalVisible content: external broker/MGA global search is out of scope for F0023 MVP.
- Authorization is enforced before results, snippets, suggestions, and counts are returned.

## Non-Functional Expectations

- Performance: first page of grouped authorized results returns within product performance targets defined in Phase B.
- Security: no unauthorized result details or counts are exposed.
- Reliability: typed query remains available after retryable errors.

## Dependencies

**Depends On:**
- F0016 - account source records and Account 360 routes.
- F0018 - policy source records and Policy 360 routes.
- F0019 - submission quote/bind source context.
- F0006/F0007 - submission and renewal workflow source screens.
- F0003/F0004 - task and user ownership context.

**Related Stories:**
- F0023-S0002 - filters and source navigation.
- F0023-S0007 - permission-safe behavior.

## Out of Scope

- External broker/MGA search.
- Full document-content search.
- Free-form analytics queries.

## UI/UX Notes

- Screens involved: Global Search Overlay, Search Results Workspace.
- Key interactions: type query, submit, review grouped results, open source record.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- "Key CRM objects" for the first release means broker, MGA/program context, account, policy, submission, renewal, and task.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only search; search telemetry may be captured outside story completion)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0023-S0001-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
