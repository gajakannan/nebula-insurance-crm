---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0006-S0001: Submission Pipeline List with Intake Status Filtering

**Story ID:** F0006-S0001
**Feature:** F0006 — Submission Intake Workflow
**Title:** Submission pipeline list with intake status filtering
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user or distribution manager
**I want** to view a filterable, sortable list of submissions with intake status indicators
**So that** I can see the full intake pipeline, identify what needs attention, and manage my workload

## Context & Background

The submission pipeline list is the primary operating view for intake users. It must surface all submissions within the user's ABAC scope, allow filtering by intake status (Received, Triaging, WaitingOnBroker, ReadyForUWReview), and support quick scanning by broker, account, LOB, assigned user, and date. This mirrors the renewal pipeline list pattern (F0007-S0001) but for the intake workflow.

## Acceptance Criteria

**Happy Path:**
- **Given** a distribution user with submissions in their ABAC scope
- **When** the user navigates to the Submission Pipeline List
- **Then** submissions are displayed in a paginated table with columns: Status, Account Name, Broker Name, LOB, Effective Date, Assigned To, Created Date, and Stale indicator

**Filtering:**
- **Given** the submission pipeline list is displayed
- **When** the user applies a status filter (e.g., "Triaging")
- **Then** only submissions in the selected status are shown; filter state is preserved during the session

**Multi-filter:**
- **Given** the user applies multiple filters (status + broker + LOB)
- **When** the filters are combined
- **Then** results match all active filter criteria (AND logic)

**Sorting:**
- **Given** the submission pipeline list is displayed
- **When** the user clicks a column header (Created Date, Effective Date, Account Name)
- **Then** the list sorts by that column; default sort is Created Date descending

**Alternative Flows / Edge Cases:**
- No submissions in scope → empty state message: "No submissions found matching your filters"
- User with no ABAC scope → empty list (not an error)
- Paginated results → standard page size (default 25), pagination controls at bottom
- Stale submissions visually flagged (icon or badge) per stale threshold logic (F0006-S0008)

**Checklist:**
- [ ] Pipeline list displays Status, Account Name, Broker Name, LOB, Effective Date, Assigned To, Created Date, Stale indicator
- [ ] Filter by: intake status (multi-select), broker (search), account (search), LOB (dropdown), assigned user (search), stale flag (yes/no)
- [ ] Sort by: Created Date (default desc), Effective Date, Account Name, Status
- [ ] Pagination with configurable page size (default 25)
- [ ] ABAC scope enforced: DistributionUser sees own-scope submissions; DistributionManager sees region-scoped; Admin sees all
- [ ] Row click navigates to Submission Detail (F0006-S0003)
- [ ] Create Submission action available from list view (navigates to create form, F0006-S0002)
- [ ] Loading and empty states handled
- [ ] Responsive layout for desktop and tablet

## Data Requirements

**Required Fields (list response):**
- `id` (uuid): Submission identifier
- `accountName` (string): Display name of linked account
- `brokerName` (string): Display name of linked broker
- `lineOfBusiness` (string, nullable): LOB classification
- `currentStatus` (string): Current workflow state
- `effectiveDate` (date): Requested coverage date
- `assignedToDisplayName` (string): Name of assigned user
- `createdAt` (datetime): Submission creation timestamp
- `isStale` (boolean): Whether submission exceeds stale threshold

**Validation Rules:**
- Filter values must match valid reference data (status, LOB)
- Page size capped at 100

## Role-Based Visibility

**Roles that can view the submission pipeline list:**
- Distribution User — Submissions within assigned opportunity scope
- Distribution Manager — Submissions within region scope
- Underwriter — Submissions assigned to them (read-only)
- Relationship Manager — Submissions for own accounts/brokers (read-only)
- Program Manager — Submissions within own programs (read-only)
- Admin — All submissions (unscoped)

**Data Visibility:**
- InternalOnly: All submission data is internal-only in MVP
- ExternalVisible: None (BrokerUser has no submission access in MVP)

## Non-Functional Expectations

- Performance: List page loads in < 2s for up to 500 submissions; p95 API response < 500ms
- Security: ABAC-scoped queries enforced server-side; no client-side filtering of unauthorized records
- Reliability: Graceful degradation if account or broker name resolution fails (show ID as fallback)

## Dependencies

**Depends On:**
- None (first story; depends on Submission entity existing)

**Related Stories:**
- F0006-S0002 — Create action from list view
- F0006-S0003 — Row click navigates to detail
- F0006-S0008 — Stale indicator data

## Business Rules

1. **ABAC Scope Enforcement:** All list queries are scoped by Casbin ABAC policy (policy.csv §2.3). DistributionUser sees own-scope submissions; DistributionManager sees region-scoped; Underwriter sees assigned-only; RelationshipManager sees own accounts/brokers; ProgramManager sees own programs; Admin sees all. Scope filtering is enforced server-side at the query layer — the client never receives unauthorized records.
2. **InternalOnly Data:** All submission data is internal-only in MVP. BrokerUser has no submission access (no policy line in §2.3 = implicit deny). No ExternalVisible fields exist on the list response.
3. **Stale Flag Computation:** The `isStale` flag on each list item is computed at query time based on the last WorkflowTransition.OccurredAt and configurable thresholds per state (see F0006-S0008). It is not stored as a field on the Submission entity.
4. **Status Reference Data:** Filter options for intake status are driven by seeded ReferenceSubmissionStatus entries, not hardcoded UI values. Only intake states (Received, Triaging, WaitingOnBroker, ReadyForUWReview) are shown in the pipeline list filter by default.

## Out of Scope

- URL-synced filters (deferred to F0023 or future enhancement)
- Saved/named views (deferred to F0023)
- Inline status transitions from list view (transitions happen from detail view)
- Export to CSV or report generation

## UI/UX Notes

- Screens involved: Submission Pipeline List
- Key interactions: Filter bar at top; sortable column headers; paginated table; row click → detail; "Create Submission" button in header area
- Status shown as color-coded pills (consistent with renewal pipeline pattern)
- Stale indicator as warning icon or badge on affected rows

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Default page size of 25 matches other list views in Nebula
- Stale threshold values are defined in F0006-S0008 and surfaced as a boolean flag on each list item

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (ABAC-scoped queries)
- [ ] Audit/timeline logged (not applicable — read-only view)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0006-S0001-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
