---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0016-S0001: Account List with Search and Filtering

**Story ID:** F0016-S0001
**Feature:** F0016 — Account 360 & Insured Management
**Title:** Account list with search and filtering
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user, distribution manager, underwriter, or relationship manager
**I want** a searchable, filterable, paginated account list
**So that** I can find an insured quickly by name, tax id, broker, territory, or region and reach its Account 360 in one click

## Context & Background

The account list is the primary entry point into Account 360. Without a reliable list surface with search and filters, users fall back to creating duplicates and using broker or submission screens as proxy navigation. The list is also the operating surface for distribution managers to audit account ownership in their territory.

## Acceptance Criteria

**Happy Path:**
- **Given** a distribution user is authenticated
- **When** they navigate to the Account List
- **Then** they see a paginated list of accounts in their scope (own region + assigned brokers) showing: account id, display name, status, broker of record, primary producer, territory, last activity date

- **Given** accounts exist across multiple territories
- **When** a distribution manager filters by their territory
- **Then** only accounts in that territory are returned

- **Given** a user searches by display name fragment
- **When** the server query runs
- **Then** accounts with `DisplayName` or `LegalName` containing the fragment (case-insensitive) are returned

**Alternative Flows / Edge Cases:**
- No results → Empty state message "No accounts match your filters"
- Search by tax id returns at most one active match
- Merged and Deleted accounts are excluded by default; an `includeRemoved` toggle (admin only) opts in
- Multiple filters combine with AND semantics
- Sorting an empty list is a no-op

**Checklist:**
- [ ] Columns: display name, status badge, broker of record, primary producer, territory, region, last activity date
- [ ] Filters: status (default Active+Inactive), territory, broker, LOB/industry, region
- [ ] Search by display name, legal name, tax id
- [ ] Sort options: display name, last activity date (default desc), policy count
- [ ] Pagination: 25 per page with page navigation
- [ ] ABAC scoping enforced server-side on the list query
- [ ] Row click navigates to `/accounts/{id}` (detail) — Account 360 is a tab within detail
- [ ] Admin-only `includeRemoved` toggle surfaces Merged / Deleted accounts with their status badge

## Data Requirements

**Required list-item fields:**
- `id`, `displayName`, `status`, `brokerOfRecordId`, `brokerOfRecordName`, `primaryProducerId`, `primaryProducerName`, `territoryCode`, `region`, `lastActivityAt`

**Optional fields:**
- `activePolicyCount`, `openSubmissionCount`, `renewalDueCount` (served from the summary projection when available; null-safe when absent)

**Validation Rules:**
- Query must enforce ABAC scope; cross-scope requests return an empty page, not 403
- Search input length ≤ 200; server trims

## Role-Based Visibility

| Role | Scope |
|------|-------|
| Distribution User | Own region + assigned broker(s) |
| Distribution Manager | Own territory |
| Underwriter | Own assigned book (accounts with an assigned submission or renewal to the user) |
| Relationship Manager | Own managed broker(s) |
| Admin | All |

**Data Visibility:** InternalOnly.

## Non-Functional Expectations

- Performance: p95 ≤ 300 ms for up to 10 000 accounts with filters + pagination
- Security: ABAC enforced in the query; no client-side filtering of privileged data
- Reliability: Deleted / Merged accounts never cause a 500 when `includeRemoved=true`

## Dependencies

**Depends On:**
- F0016-S0002 (accounts must exist to populate the list)

**Related Stories:**
- F0016-S0003 (navigation target), F0016-S0011 (summary counts in list)

## Out of Scope

- Saved / named views (deferred to F0023)
- Kanban / board view
- CSV export
- URL-synced filter state (follow-up)

## UI/UX Notes

- Screens: Account List
- Filter bar at top; table below with sortable headers
- Status badges: Active (neutral), Inactive (muted), Merged (amber with arrow icon → survivor), Deleted (red)
- Clicking Merged row forwards to survivor's detail page

## Questions & Assumptions

**Assumptions:**
- Default sort is last activity date descending
- Default status filter is Active + Inactive; admin can opt in to removed

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (ABAC on list query)
- [ ] Audit/timeline logged: No (read-only)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
