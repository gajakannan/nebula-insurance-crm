---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0007-S0001: Renewal Pipeline List with Due-Window Filtering

**Story ID:** F0007-S0001
**Feature:** F0007 — Renewal Pipeline
**Title:** Renewal pipeline list with due-window filtering
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user or distribution manager
**I want** a renewal pipeline list view that I can filter by due window (90/60/45 days and overdue)
**So that** I can focus my outreach on the renewals that need attention first and ensure nothing expires unworked

## Context & Background

The renewal pipeline list is the primary operating surface for renewal management. Distribution users need to see their assigned renewals organized by urgency, while managers need team-wide visibility. Due-window filtering (90/60/45 days before expiry, plus overdue) is the core mechanism for prioritizing daily renewal work. Without this, users fall back to spreadsheets and ad hoc tracking.

## Acceptance Criteria

**Happy Path:**
- **Given** a distribution user is authenticated and has assigned renewals
- **When** they navigate to the Renewal Pipeline list
- **Then** they see a paginated list of renewals showing: renewal ID, account name, broker name, LOB, current status, policy expiration date, assigned user, and overdue/approaching indicator

- **Given** renewals exist across multiple due windows
- **When** the user selects the "90-day" filter
- **Then** only renewals with policy expiration within 90 days are shown

- **Given** renewals exist in Identified state past their LOB-specific target outreach date
- **When** the user selects the "Overdue" filter
- **Then** only overdue renewals are shown, sorted by expiration date ascending (most urgent first)

**Alternative Flows / Edge Cases:**
- No renewals in selected window → Empty state with message "No renewals in this window"
- User has no assigned renewals → Empty state with message "No renewals assigned to you"
- Distribution manager views "All" tab → Sees all team renewals, not just their own
- Renewal with null LOB → Uses default timing thresholds; LOB displays as "—"
- Multiple filters combined (status + due window + LOB) → Filters apply additively (AND logic)

**Checklist:**
- [ ] List displays: renewal ID, account name, broker name, LOB, status badge, expiration date, assigned user, overdue flag
- [ ] Due-window filter options: All, 90-day, 60-day, 45-day, Overdue
- [ ] Status filter: All, Identified, Outreach, InReview, Quoted (excludes terminal states by default)
- [ ] Owner filter: My Renewals (default for non-managers), All Renewals (default for managers)
- [ ] LOB filter: All + each known LOB value
- [ ] Sort options: Expiration date (default ascending), account name, status, assigned user
- [ ] Pagination: 25 items per page with page navigation
- [ ] Overdue renewals display a visual indicator (badge or icon) regardless of active filters
- [ ] Clicking a renewal row navigates to Renewal Detail (F0007-S0002)

## Data Requirements

**Required Fields (list item):**
- Renewal ID: Row identifier and navigation target
- Account Name: Insured context
- Broker Name: Broker of record
- Line of Business: LOB classification
- Current Status: Workflow state badge
- Policy Expiration Date: Drives urgency and sort order
- Assigned User: Current owner display name
- Overdue Indicator: Visual flag when overdue

**Optional Fields:**
- Target Outreach Date: Shown in expanded row or tooltip

**Validation Rules:**
- Due-window filter computes from `PolicyExpirationDate` relative to current date
- Overdue filter uses LOB-specific `WorkflowSlaThreshold` outreach target days

## Role-Based Visibility

**Roles that can view the renewal pipeline list:**
- Distribution User — Sees own assigned renewals by default; can filter to team view if permitted
- Distribution Manager — Sees all team renewals by default
- Underwriter — Sees renewals in InReview/Quoted assigned to them
- Relationship Manager — Read-only access to renewals linked to managed broker relationships
- Program Manager — Read-only access to renewals within their programs
- Admin — Full visibility

**Data Visibility:**
- InternalOnly content: All renewal list data is internal-only
- ExternalVisible content: None (no external broker access in MVP)

## Non-Functional Expectations

- Performance: List loads in < 2s for up to 500 renewals; paginated queries for larger sets
- Security: ABAC enforcement on list query — users only see renewals within their authorized scope
- Reliability: Empty state handling for no results; graceful degradation if policy data is temporarily unavailable

## Dependencies

**Depends On:**
- F0007-S0006 — Renewals must exist (created from expiring policies) to populate the list

**Related Stories:**
- F0007-S0002 — Renewal detail view (navigation target from list)
- F0007-S0005 — Overdue visibility logic powers the overdue filter and indicators

## Out of Scope

- Bulk operations (select multiple, mass reassign)
- URL-synced filter state (future enhancement)
- Saved/named filter views (deferred to F0023)
- Kanban/board view of renewal pipeline (future enhancement)
- Export to CSV/Excel

## UI/UX Notes

- Screens involved: Renewal Pipeline List
- Key interactions: Filter bar at top with due-window toggles, status dropdown, owner toggle, LOB dropdown. Table/list below with sortable column headers. Row click navigates to detail.
- Overdue indicator: Red badge or icon on the row, always visible regardless of filter state
- Approaching indicator: Amber badge for renewals within warning window but not yet overdue

## Questions & Assumptions

**Open Questions:**
- None — all business rules provided by stakeholder

**Assumptions (to be validated):**
- Default sort is by policy expiration date ascending (most urgent first)
- Terminal states (Completed, Lost) are excluded from the default list view but accessible via an explicit "Closed" filter toggle

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (ABAC scoping on list query)
- [ ] Audit/timeline logged: No (read-only view)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0007-S0001-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
