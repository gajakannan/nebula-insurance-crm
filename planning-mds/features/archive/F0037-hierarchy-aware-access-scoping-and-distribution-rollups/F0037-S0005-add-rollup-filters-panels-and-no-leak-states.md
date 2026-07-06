# Add rollup filters, panels, drilldowns, and no-leak states

**Story ID:** F0037-S0005
**Feature:** F0037 - Hierarchy-Aware Access Scoping & Distribution Rollups
**Title:** Add rollup filters, panels, drilldowns, and no-leak states
**Priority:** High
**Phase:** CRM Release MVP+

## User Story

**As a** distribution leader
**I want** distribution-scope filters, rollup panels, drilldowns, and safe empty/no-access states in the CRM
**So that** I can analyze visible production, workflow, and activity without confusing hidden data for missing data.

## Context & Background

F0037-S0004 adds rollup reporting. The CRM needs UI controls that reuse F0023 report patterns, expose hierarchy/territory/producer filtering safely, and distinguish empty, filtered-away, no-access, stale-data, and system-error states without leaking hidden-record counts.

## Acceptance Criteria

**Happy Path:**
- **Given** an authorized internal user on the operational reports experience
- **When** the user selects `asOf`, hierarchy root, territory, producer, grouping, and metric family filters within their scope
- **Then** the UI renders scoped rollup panels and rows with drilldowns that preserve the selected filters.

**Alternative Flows / Edge Cases:**
- Out-of-scope filter values are not selectable or produce safe no-access behavior.
- Empty in-scope results show an empty state that does not imply hidden record counts.
- Filtered-away results explain that current filters have no visible results.
- No-access state appears when the user lacks any internal F0037 scope.
- Stale-data state appears when backend response marks rollup data stale.
- System-error state appears when the report cannot load, with retry behavior and no hidden-record detail.
- Narrow view preserves filtering and drilldown access without horizontal overflow.

**Checklist:**
- [ ] UI reuses existing operational report/search components where practical.
- [ ] Filters include approved hierarchy, territory, producer, grouping, metric family, and `asOf` controls.
- [ ] Drilldowns preserve scope-compatible filters.
- [ ] Empty, filtered-away, no-access, stale, and system-error states are visually distinct.
- [ ] Frontend tests cover filters, rollup panels, drilldowns, no-leak states, saved-view reapplication, and accessibility.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Operational Reports -> Distribution Rollups | Change filters and run report | Filter controls enabled only for in-scope values | No persisted mutation unless existing F0023 saved-view flow is used | Refresh or revisit uses URL/query state or saved-view filters while recomputing current user scope | Internal users with F0037 report permission |
| Distribution Rollup Row -> Drilldown | Open scoped source rows | Drilldown action enabled only when row has visible rows | No mutation | Drilldown reload preserves scope-compatible filter parameters and revalidates scope | Same or narrower scope than rollup row |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified. N/A - filter execution and drilldown are read-only; saved-view persistence remains F0023-owned.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason. N/A - no new mutation.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Available filter options scoped to the user.
- Selected `asOf`, root node, territory, producer, grouping, and metric family.
- Rollup response rows, totals, generated timestamp, stale indicator, and drilldown links.
- UI state type: Empty, filtered-away, no-access, stale, or system-error.

**Optional Fields:**
- Saved-view id: Reuses existing F0023 saved-view mechanics where available.
- Trend context: Displayed only when backend marks it available.

**Validation Rules:**
- Out-of-scope filter ids must not be accepted silently as broad access.
- Drilldown links must revalidate scope on arrival.
- Error and empty states must not mention hidden-record counts.

## Role-Based Visibility

**Roles that can use rollup UI:**
- Admin - Full internal rollup UI.
- ProgramManager - Program/hierarchy/territory scoped rollup UI.
- DistributionManager - Managed subtree and territory scoped rollup UI.
- RelationshipManager - Assigned relationship, territory, and producer scoped rollup UI where policy allows.

**Data Visibility:**
- InternalOnly content: Filter options, rollup rows, totals, generated timestamp, and drilldown links.
- ExternalVisible content: None through F0037 rollup UI.

## Non-Functional Expectations

- Accessibility: Keyboard navigation, focus order, labels, and status announcements must meet existing CRM expectations.
- Performance: Filter changes should show loading state and avoid layout jump.
- Security: UI must not infer hidden totals from disabled states, option counts, or error copy.

## Dependencies

**Depends On:**
- F0037-S0003 - Scoped search/report/insight behavior.
- F0037-S0004 - Rollup reporting contract.
- F0023 - Existing operational report and saved-view UI patterns.

**Related Stories:**
- F0037-S0006 - Validates no-leak UI states and security evidence.

## Business Rules

1. **No-leak states:** UI copy must not reveal whether hidden records exist.
2. **Filter option safety:** Filter options are scoped to visible hierarchy, territory, and producer values.
3. **Drilldown revalidation:** UI drilldowns are convenience links, not authorization decisions.

## Out of Scope

- New charting platform.
- Export to BI tools.
- External portal rollup UI.

## UI/UX Notes

- Screens involved: Operational reports, global search drilldown target, saved-view application.
- Key interactions: Select filters, scan rollup panels, drill into visible source rows, recover from no-access/empty/stale/error states.

## Questions & Assumptions

**Open Questions:**
- [ ] Phase B must confirm whether rollup filters live in existing F0023 report routes or a dedicated distribution rollup route.

**Assumptions (to be validated):**
- Existing CRM report controls can be extended without a full report navigation redesign.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only UI; saved-view mutation remains F0023-owned)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
