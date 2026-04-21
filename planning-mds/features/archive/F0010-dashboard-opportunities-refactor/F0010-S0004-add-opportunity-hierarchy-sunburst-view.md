# F0010-S0004: Add Opportunities Hierarchy Sunburst View

**Story ID:** F0010-S0004
**Feature:** F0010 — Dashboard Opportunities Refactor (Pipeline Board + Insight Views)
**Title:** Add Opportunities Hierarchy Sunburst view
**Priority:** Medium
**Phase:** MVP

## User Story

**As a** Distribution Manager or Program Manager
**I want** a sunburst hierarchy view for opportunities
**So that** I can read proportion and hierarchy in one compact visualization

## Context & Background

Sunburst complements treemap by showing hierarchical structure in concentric rings and works well as an optional analytic view for exploratory understanding.

## Acceptance Criteria

**Happy Path:**
- **Given** a user is in the Opportunities widget
- **When** they switch to Sunburst view
- **Then** they see hierarchy rings for EntityType -> ColorGroup -> Status
- **And** segment size encodes open count proportion
- **And** center summary shows total open count for selected period

**Interaction + Permission:**
- **Given** a user selects a segment
- **When** interaction is processed
- **Then** they see segment label, count, and percent of parent
- **And** status-level segments can open ABAC-scoped mini-card drilldowns

**Alternative Flows / Edge Cases:**
- Narrow screen where radial labels collide -> switch to compact legend + breadcrumb labels.
- No data in selected period -> show explicit empty state.
- Keyboard-only use -> segment navigation is available through ordered focusable legend items.

**Checklist:**
- [ ] Sunburst view is available in opportunities view switcher
- [ ] Hierarchy rings match documented entity/group/status model
- [ ] Segment interactions return details
- [ ] Empty/error/loading states are defined
- [ ] ABAC scope behavior is preserved

## Data Requirements

**Required Fields:**
- Node id
- Parent node id
- Label
- Count
- Depth level index

**Optional Fields:**
- Relative percentage of parent

**Validation Rules:**
- Node counts are non-negative integers.
- Child totals roll up to parent totals.
- Depth ordering is deterministic.

## Role-Based Visibility

**Roles that can view Opportunities Hierarchy Sunburst:**
- DistributionUser
- DistributionManager
- Underwriter
- RelationshipManager
- ProgramManager
- Admin

**Data Visibility:**
- Sunburst data is InternalOnly and ABAC scoped.

## Non-Functional Expectations

- Performance: sunburst payload and render p95 < 500ms.
- Accessibility: color is not the only cue; labels and values remain accessible.
- Reliability: malformed hierarchy data results in safe fallback message.

## Dependencies

**Depends On:**
- F0010-S0001 (common opportunities shell and period controls)
- F0010-S0003 (shared hierarchy aggregate model)

**Related Stories:**
- F0010-S0005 — Unify drilldown, responsive layout, and accessibility

## Out of Scope

- Animated transition playback between hierarchy levels
- Historical comparison overlays in MVP

## Questions & Assumptions

**Open Questions:**
- [ ] Should sunburst include terminal outcomes by default or keep focus on open stages only?

**Assumptions (to be validated):**
- Sunburst remains an optional analysis mode, not the operational default.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: N/A (read-only view)
- [ ] Tests pass
- [ ] Documentation updated
