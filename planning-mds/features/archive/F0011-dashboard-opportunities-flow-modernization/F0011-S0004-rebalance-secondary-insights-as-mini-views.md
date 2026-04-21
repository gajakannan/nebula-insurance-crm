# F0011-S0004: Rebalance Secondary Insights as Mini-Views

**Story ID:** F0011-S0004
**Feature:** F0011 — Dashboard Opportunities Flow-First Modernization (Connected Pipeline + Terminal Outcomes)
**Title:** Rebalance secondary insights as mini-views
**Priority:** Medium
**Phase:** MVP

## User Story

**As a** Relationship Manager or Program Manager
**I want** secondary analytics to stay available as compact mini-views
**So that** I can get deeper insight when needed without losing the main operational flow context

## Context & Background

F0010 introduced multiple full-size view modes (Pipeline, Heatmap, Treemap, Sunburst). Current direction keeps flow/outcomes primary and uses secondary analytics as optional supporting context instead of equal-weight primary canvases.

## Acceptance Criteria

**Happy Path:**
- **Given** a user is on the opportunities flow view
- **When** they review secondary insights
- **Then** they can access compact mini-views for aging and radial/hierarchy patterns
- **And** primary flow and outcomes remain visible as the dominant interaction surface

**Interaction + Permission:**
- **Given** a user selects a secondary mini-view
- **When** details are requested
- **Then** expanded detail opens in contextual panel/drawer/modal without replacing the primary flow by default
- **And** any detail drilldowns remain ABAC scoped

**Alternative Flows / Edge Cases:**
- Secondary mini-view dataset unavailable -> show mini-view specific empty/error state and keep primary flow active.
- Narrow viewport -> mini-views collapse into horizontal list without obstructing core stage/outcome actions.
- Unsupported browser SVG behavior -> show summary list fallback with counts.
- Read-only guard -> this story does not create, update, delete, or transition domain records; audit/timeline mutation events are not required.

**Checklist:**
- [ ] Secondary insights are available as mini-views, not competing primary canvases
- [ ] Primary flow remains visible while mini-view interactions occur
- [ ] Mini-view expand interactions are defined
- [ ] Empty/error/loading behavior is defined per mini-view
- [ ] ABAC scope behavior is preserved

## Data Requirements

**Required Fields:**
- Mini-view identifier
- Mini-view summary count payload
- Optional expanded detail payload reference

**Optional Fields:**
- Mini-view trend delta

**Validation Rules:**
- Mini-view summaries are period-scoped.
- Mini-view IDs are deterministic and stable across sessions.
- Expanded detail requests reference valid mini-view IDs.

## Role-Based Visibility

**Roles that can view secondary mini-views:**
- DistributionUser
- DistributionManager
- Underwriter
- RelationshipManager
- ProgramManager
- Admin

**Data Visibility:**
- Mini-view data remains InternalOnly and ABAC scoped.

## Non-Functional Expectations

- Performance: expanding a mini-view detail p95 < 400ms.
- Accessibility: mini-view controls are keyboard and screen-reader accessible.
- Reliability: mini-view failures remain isolated and non-blocking to primary flow.

## Dependencies

**Depends On:**
- F0011-S0001 (primary connected flow)
- F0011-S0002 (terminal outcomes rail)

**Related Stories:**
- F0011-S0003 — Apply modern opportunities visual system
- F0011-S0005 — Ensure responsive and accessibility parity

## Out of Scope

- New advanced analytics models
- User-customizable analytics dashboards

## Questions & Assumptions

**Open Questions:**
- [ ] Confirm final mini-view set for MVP: keep aging + radial hierarchy summaries only, or retain treemap alongside radial summaries.

**Assumptions (to be validated):**
- Existing heatmap/treemap/sunburst contracts can be reused for mini-view summaries and expanded details.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: N/A (read-only view)
- [ ] Tests pass
- [ ] Documentation updated
