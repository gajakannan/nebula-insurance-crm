# Add distribution rollup reporting

**Story ID:** F0037-S0004
**Feature:** F0037 - Hierarchy-Aware Access Scoping & Distribution Rollups
**Title:** Add distribution rollup reporting
**Priority:** High
**Phase:** CRM Release MVP+

## User Story

**As a** distribution operations manager
**I want** production, workflow, and activity rollups grouped by hierarchy, territory, and producer
**So that** I can manage channel performance from trustworthy scoped totals and drill into the underlying visible work.

## Context & Background

F0023 operational reports expose projection rows and workflow metrics, while F0017 gives hierarchy/territory/producer dimensions. This story adds rollup reporting on the same scoped projection substrate so totals reconcile to visible source rows.

## Acceptance Criteria

**Happy Path:**
- **Given** a user with resolved distribution scope and report filters
- **When** the user requests distribution rollups for production, workflow, or activity
- **Then** the response returns grouped rows, totals, grouping metadata, `asOf`, generated timestamp, and scope-preserving drilldown links.

**Alternative Flows / Edge Cases:**
- Grouping by hierarchy node rolls up child nodes that are visible to the user and excludes hidden siblings.
- Grouping by territory includes only visible records effective for the selected `asOf`.
- Grouping by producer includes only visible producer-owned records effective for the selected `asOf`.
- If a metric family is not supported by available projection data, the response marks the metric unavailable rather than fabricating a value.
- Rollup totals reconcile to the same scoped operational report projection rows.
- Empty scope returns safe empty/no-access behavior without hidden totals.
- Unauthorized or permission-denied users cannot request rollups and receive the platform's approved no-access behavior.

**Checklist:**
- [ ] Rollups reuse the F0023 projection/reporting substrate.
- [ ] Rollups apply the shared scope before grouping and aggregation.
- [ ] Response includes grouped rows, totals, grouping key, `asOf`, generated timestamp, and drilldown links.
- [ ] Permission checks are asserted for each approved rollup grouping and metric family.
- [ ] Reconciliation tests compare rollup totals to scoped source rows.
- [ ] Phase B decides whether rollups are query-time or materialized; MVP assumes query-time unless proven otherwise.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only story.

## Data Requirements

**Required Fields:**
- Grouping key: Hierarchy node, territory, or producer.
- Metric family: Production, workflow, or activity.
- Scoped projection rows: Source rows used for aggregation.
- Totals: Aggregated visible metric totals.
- As-of date: Effective-date context for hierarchy/territory/producer scope.
- Generated timestamp: Response creation time.
- Drilldown links: Scope-preserving links to source report/search results.

**Optional Fields:**
- Trend context: Prior-period comparison when already available in projections.
- Unavailable metric reason: Explains why a metric cannot be computed from available projection data.

**Validation Rules:**
- Requested grouping keys must be allowed and inside the user's resolved scope.
- Aggregations must exclude hidden rows before totals are computed.
- Drilldown links must not encode hidden identifiers.

## Role-Based Visibility

**Roles that can view scoped distribution rollups:**
- Admin - Full internal rollups.
- ProgramManager - Program/hierarchy/territory scoped rollups.
- DistributionManager - Managed subtree and territory scoped rollups.
- RelationshipManager - Assigned relationship, territory, and producer scoped rollups where policy allows.

**Data Visibility:**
- InternalOnly content: Rollup rows, totals, source drilldowns, and metric availability.
- ExternalVisible content: None through F0037 internal rollups.

## Non-Functional Expectations

- Performance: Rollup requests should meet operational report performance expectations for ordinary hierarchy and territory filters.
- Security: Aggregation must not leak hidden counts or totals.
- Reliability: Rollup totals must be deterministic and reconcilable for a fixed filter set and `asOf`.

## Dependencies

**Depends On:**
- F0037-S0001 - Shared scope resolver.
- F0037-S0003 - Projection visibility for search/reports/insights.
- F0017 - Hierarchy/territory/producer dimensions.
- F0023 - Operational report projections.

**Related Stories:**
- F0037-S0005 - Presents rollups and drilldowns in the UI.
- F0037-S0006 - Provides reconciliation and security evidence.

## Business Rules

1. **Roll up visible rows only:** Every total is computed from rows the user can see.
2. **Metric availability honesty:** Missing projection data is reported as unavailable, not zero, unless zero is a true scoped result.
3. **Scope-preserving drilldown:** Drilldown filters must reproduce the rollup row's visible row set or a documented subset.

## Out of Scope

- Commission, split, revenue, or invoicing rollups.
- Data warehouse or BI export.
- Materialized jobs unless Phase B explicitly approves them.

## UI/UX Notes

- Screens involved: Operational reports / distribution rollup view.
- Key interactions: Choose grouping and metric family, scan totals, drill into visible source rows.

## Questions & Assumptions

**Open Questions:**
- [ ] Phase B must confirm exact metric definitions for production, workflow, and activity from available projection fields.

**Assumptions (to be validated):**
- Query-time rollups over scoped F0023 projections are sufficient for MVP performance.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only reporting; generated timestamp and request context are sufficient evidence)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
