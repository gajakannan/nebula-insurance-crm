# Apply visibility to search, saved views, insights, and reports

**Story ID:** F0037-S0003
**Feature:** F0037 - Hierarchy-Aware Access Scoping & Distribution Rollups
**Title:** Apply visibility to search, saved views, insights, and reports
**Priority:** Critical
**Phase:** CRM Release MVP+

## User Story

**As a** relationship manager
**I want** global search, saved views, broker insights, and operational reports to use my distribution scope
**So that** I can search and analyze assigned business without seeing sibling or unauthorized channel records.

## Context & Background

F0023 introduced global search, saved views, operational reports, and projection visibility. F0008 added broker insights. F0037 must extend that existing projection visibility flow with hierarchy/territory/producer scope so all derived search and analytics surfaces stay no-leak.

## Acceptance Criteria

**Happy Path:**
- **Given** a user with a resolved distribution scope and an existing saved view, search query, insight, or operational report
- **When** the user runs the surface with optional hierarchy filters and an `asOf` date
- **Then** the system returns only scoped rows, counts, facets, suggestions, drilldowns, and insight metrics.

**Alternative Flows / Edge Cases:**
- Saved views created before F0037 reapply the current user's scope at execution time.
- A saved view shared by a broader-scope user does not expand the receiver's visibility.
- Search suggestions exclude hidden records.
- Facets and result counts exclude hidden records before aggregation.
- Broker insight scorecards, trends, and benchmarks exclude hidden sibling or external records.
- Filtered-away results show a safe empty state without hidden-record counts.
- Unauthorized or permission-denied users receive the platform's approved no-access behavior without exposing hidden records.

**Checklist:**
- [ ] Search document queries call the shared scope resolver.
- [ ] Operational report projection queries call the shared scope resolver.
- [ ] Broker insight projection queries call the shared scope resolver.
- [ ] Saved-view execution stores user filters but recomputes scope for the executing user.
- [ ] Permission checks are asserted for search, saved-view execution, broker insights, and operational reports.
- [ ] Tests prove predicates run before counts, facets, suggestions, drilldowns, and metric aggregation.
- [ ] Audit evidence records that no new saved-view mutation is introduced by this story; existing F0023 saved-view audit behavior remains unchanged.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Saved Views -> Apply existing view | Select a saved view with hierarchy-aware filters | Read-only execution; saved-view mutation is owned by F0023 | No mutation for this story | Re-running the saved view after reload reapplies the current user's scope and the saved filters | Internal users only; receiver scope cannot be expanded by shared view ownership |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified. N/A - this story reuses existing saved-view persistence and only validates scoped execution.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason. N/A - no new mutation.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Search document broker, territory, producer, owner, region, and source-record metadata.
- Operational report projection broker, territory, producer, owner, region, workflow, activity, and metric fields.
- Broker insight projection broker, source-record, and benchmark membership fields.
- Saved-view filters and sharing metadata.
- Scope object and `asOf` date.

**Optional Fields:**
- `rootNodeId`: Narrows query to an allowed hierarchy subtree.
- `territoryId`: Narrows query to an allowed territory.
- `producerUserId`: Narrows query to an allowed producer.
- Explanation code: Supports diagnostics and security tests.

**Validation Rules:**
- Requested hierarchy filters must be inside the user's resolved scope.
- Invalid or out-of-scope filters return safe empty/no-access behavior.
- Saved views cannot persist or replay a broader scope than the executing user holds.

## Role-Based Visibility

**Roles that can run scoped search/report/insight surfaces:**
- Admin - Full internal scope.
- ProgramManager - Program/hierarchy/territory scoped search and analytics.
- DistributionManager - Managed subtree and territory scoped search and analytics.
- RelationshipManager - Assigned relationship, territory, and producer scoped search and analytics.
- DistributionUser - Assigned source-record scoped search and analytics.
- Underwriter - Assigned workload scoped search and analytics.
- ServiceUser - Assigned service/workflow scoped search and analytics.

**Data Visibility:**
- InternalOnly content: Search records, report rows, insight metrics, saved-view filters, and projection metadata.
- ExternalVisible content: None through F0037 internal surfaces.

## Non-Functional Expectations

- Performance: Scope predicates should compose with existing projection indexes and not create avoidable full scans.
- Security: No rows, counts, suggestions, facets, drilldowns, or metrics may include hidden records.
- Reliability: Existing F0023/F0008 surfaces must keep their non-F0037 behavior while adding hierarchy-aware scope.

## Dependencies

**Depends On:**
- F0037-S0001 - Shared scope resolver.
- F0037-S0002 - No-leak direct/read behavior.
- F0023 - Search, saved views, operational reports, projection visibility.
- F0008 - Broker insight projections.

**Related Stories:**
- F0037-S0004 - Uses the same projection visibility path for rollups.
- F0037-S0005 - Adds UI controls and states for scoped surfaces.

## Business Rules

1. **Execution-time scope:** Saved views preserve filters but never preserve another user's visibility.
2. **Facet safety:** Facets and suggestions must be computed from visible rows only.
3. **Drilldown safety:** Drilldown links must carry scope-compatible filters and revalidate scope on arrival.

## Out of Scope

- Building a new search engine.
- Replacing saved-view persistence.
- Redesigning broker insights outside permission-safe scope behavior.

## UI/UX Notes

- Screens involved: Global search, saved views, broker insights, operational reports.
- Key interactions: Users can apply existing filters and saved views; results quietly honor current scope.

## Questions & Assumptions

**Open Questions:**
- [ ] Phase B must confirm whether hierarchy filters are added to all F0023 report contracts at once or only to the rollup-oriented surfaces.

**Assumptions (to be validated):**
- Existing projections include enough broker/territory/producer/owner metadata to filter without a separate reporting store.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only scoped execution; saved-view mutation remains owned by F0023)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
