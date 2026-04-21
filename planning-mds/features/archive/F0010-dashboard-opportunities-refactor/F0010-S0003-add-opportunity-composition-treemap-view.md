# F0010-S0003: Add Opportunities Composition Treemap View

**Story ID:** F0010-S0003
**Feature:** F0010 — Dashboard Opportunities Refactor (Pipeline Board + Insight Views)
**Title:** Add Opportunities Composition Treemap view
**Priority:** Medium
**Phase:** MVP

## User Story

**As a** Relationship Manager or Program Manager
**I want** a treemap composition view of open opportunities
**So that** I can prioritize outreach using current workload composition

## Context & Background

Treemap offers an area-based composition summary with less line clutter than Sankey and helps users answer "where volume sits now." This pattern is inspired by `planning-mds/screens/UsrStats3.png`.

## Acceptance Criteria

**Happy Path:**
- **Given** a user is in the Opportunities widget
- **When** they switch to Treemap view
- **Then** they see rectangles sized by open count
- **And** hierarchy is represented as EntityType -> ColorGroup -> Status
- **And** period selection updates treemap proportions

**Interaction + Permission:**
- **Given** a user selects a treemap tile
- **When** interaction is processed
- **Then** details show tile label, count, and share percentage
- **And** stage-level interactions can open ABAC-scoped mini-card drilldowns

**Alternative Flows / Edge Cases:**
- Very small categories -> aggregate into "Other" tile with expandable details.
- No data in selected period -> show explicit empty state.
- Touch device interaction -> tap-to-select behavior replaces hover dependency.
- Read-only guard -> this story does not create, update, delete, or transition domain records; audit/timeline mutation events are not required.

**Checklist:**
- [ ] Treemap view is available in opportunities view switcher
- [ ] Size encoding is proportional to count
- [ ] Legend explains hierarchy and color-group mapping
- [ ] Empty/error/loading states are defined
- [ ] ABAC scope behavior is preserved

## Data Requirements

**Required Fields:**
- Node id
- Parent node id
- Label
- Count
- Level type (`entityType`, `colorGroup`, `status`)

**Optional Fields:**
- Percentage of parent

**Validation Rules:**
- All node counts are non-negative integers.
- Child counts roll up to parent counts.
- Hierarchy is acyclic and rooted at a single top node.

## Role-Based Visibility

**Roles that can view Opportunities Composition Treemap:**
- DistributionUser
- DistributionManager
- Underwriter
- RelationshipManager
- ProgramManager
- Admin

**Data Visibility:**
- Composition data is InternalOnly and ABAC scoped.

## Non-Functional Expectations

- Performance: treemap view switch/render p95 < 500ms.
- Accessibility: provide keyboard focus order and text summary fallback.
- Reliability: data contract mismatch should show non-blocking widget-level error.

## Dependencies

**Depends On:**
- F0010-S0001 (common opportunities shell and period controls)

**Related Stories:**
- F0010-S0004 — Add Opportunities Hierarchy Sunburst view
- F0010-S0005 — Unify drilldown, responsive layout, and accessibility

## Out of Scope

- Multi-dimensional treemap slicing by ad hoc filters in MVP
- Exporting treemap as downloadable image in MVP

## Questions & Assumptions

**Open Questions:**
- [ ] Should "Other" category threshold be fixed or configurable for MVP?

**Assumptions (to be validated):**
- Hierarchy `entityType -> colorGroup -> status` matches user mental model for composition review.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: N/A (read-only view)
- [ ] Tests pass
- [ ] Documentation updated
