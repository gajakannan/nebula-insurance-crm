# F0002-S0002: Search Brokers

**Story ID:** F0002-S0002
**Feature:** F0002 — Broker & MGA Relationship Management
**Title:** Search brokers by name or license number
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution Manager
**I want** to search brokers by name or license
**So that** I can quickly locate a broker without paging through the full list

## Context & Background

As broker volume grows, list-only browsing becomes inefficient. Search is needed for daily relationship workflows.

## Acceptance Criteria

- **Given** the broker list has multiple records
- **When** I enter a partial broker name
- **Then** matching brokers are returned using case-insensitive filtering

- **Given** I enter an exact license number
- **When** search executes
- **Then** the matching broker is returned as the top result

- **Given** no records match the query
- **When** search executes
- **Then** I see an empty-state message and clear-search action

- **Given** an unauthorized user calls the search endpoint
- **When** authorization is evaluated
- **Then** access is denied

- Error scenario: query strings over max length return a validation error
- Edge case: leading/trailing spaces are trimmed before search evaluation

## Data Requirements

**Required Fields:**
- Query: 1-100 chars after trimming

**Optional Fields:**
- Status filter: Active / Inactive / Pending (all three statuses are filterable; default is no filter — all statuses returned)
- Page: non-negative integer, default 1
- PageSize: integer, default 20, maximum 100

**Pagination:**
- All list responses are paginated. Response includes: `data`, `page`, `pageSize`, `totalCount`, `totalPages`.
- If no results on the requested page, return an empty `data` array (not a 404).
- UI renders Previous/Next controls; disabled when at first/last page.

**Validation Rules:**
- Query must be sanitized before persistence/logging
- Query length must not exceed 100 chars
- License number search is exact match only in MVP (no partial license search)

## Role-Based Visibility

**Roles that can search brokers:**
- DistributionUser — scoped broker search (same scope as their read/update access)
- DistributionManager — region-scoped broker search
- RelationshipManager — full broker search
- Underwriter — read-only broker search (for submission context)
- ProgramManager — scoped broker search (program context)
- Admin — unscoped broker search

**Data Visibility:**
- InternalOnly content: inactive/hidden broker flags
- ExternalVisible content: none in MVP

## Non-Functional Expectations

- Performance: search response p95 < 300ms for 10k broker records
- Security: authorized roles only; no wildcard cross-tenant reads
- Reliability: empty results should not be treated as system failure

## Dependencies

**Depends On:**
- F0002-S0001 - Broker records can be created
- Indexed broker search fields (name, license)

**Related Stories:**
- F0002-S0003 - Broker list screen pagination

## Out of Scope

- Advanced search (multi-field boolean filters)
- Saved searches
- External broker self-service search

## Questions & Assumptions

**Assumptions (confirmed):**
- Name search is case-insensitive substring match
- Soft-deleted brokers are excluded from all search results regardless of status filter

## UI/UX Notes

- Screens involved: Broker List (search bar + results table)
- Layout: Search input + Status dropdown filter at top; paginated results table below
- Table columns: Legal Name, License Number, State, Status, Created Date
- Empty state: "No brokers match your search. Try a different name or license number." with a clear-filters button
- Pagination controls: Previous / Next with current page indicator; page size selector (20 / 50 / 100)

## Definition of Done

- [x] Acceptance criteria met
- [x] Edge case and error scenario tests pass
- [x] Authorization enforced for API and UI entry points
- [ ] Unit and integration tests pass
