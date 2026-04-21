# F0010-S0005: Unify Drilldown, Responsive Layout, and Accessibility

**Story ID:** F0010-S0005
**Feature:** F0010 — Dashboard Opportunities Refactor (Pipeline Board + Insight Views)
**Title:** Unify drilldown, responsive layout, and accessibility across opportunities views
**Priority:** High
**Phase:** MVP

## User Story

**As a** dashboard user on desktop, tablet, or phone
**I want** consistent drilldown behavior and accessible interactions across opportunities views
**So that** I can complete the same core workflow regardless of device size

## Context & Background

Current opportunities section is dense on smaller viewports. This story standardizes interaction behavior and responsive layout using the existing inspiration captures for MacBook, iPad, and iPhone proportions.

## Acceptance Criteria

**Happy Path:**
- **Given** a user is in Opportunities Pipeline, Heatmap, Treemap, or Sunburst view
- **When** they select a status target
- **Then** they can open a mini-card drilldown with consistent information structure
- **And** the same period selector and entity toggle state is preserved across view switches

**Responsive + Permission:**
- **Given** the widget is viewed on MacBook, iPad, or iPhone class breakpoints
- **When** layout adapts
- **Then** text remains readable, controls remain reachable, and drilldown remains usable
- **And** all visible data remains ABAC-scoped and role-authorized

**Accessibility + Edge Cases:**
- Keyboard navigation supports switching views, selecting data targets, opening and closing drilldowns, and returning focus.
- Screen reader labels expose entity type, status, count, and selected period context.
- Reduced-motion preference disables non-essential chart transitions.
- Error states in one view do not block switching to other views.
- Read-only guard -> this story does not create, update, delete, or transition domain records; audit/timeline mutation events are not required.

**Checklist:**
- [ ] Drilldown interaction model is consistent across view types
- [ ] Responsive behavior validated for desktop/tablet/mobile
- [ ] Accessibility contract documented and testable
- [ ] ABAC and role boundaries preserved
- [ ] Non-blocking view-level error handling implemented

## Data Requirements

**Required Fields:**
- Drilldown target identifier (entityType + status or hierarchy node)
- Selected period and view mode
- Mini-card list payload and total count

**Optional Fields:**
- Context breadcrumb for hierarchy views

**Validation Rules:**
- Drilldown target must map to valid status/hierarchy leaf.
- Drilldown payload must follow existing mini-card schema constraints.

## Role-Based Visibility

**Roles that can use Opportunities drilldowns:**
- DistributionUser
- DistributionManager
- Underwriter
- RelationshipManager
- ProgramManager
- Admin

**Data Visibility:**
- Drilldown item data is InternalOnly and ABAC scoped.

## Non-Functional Expectations

- Performance: interaction-to-drilldown p95 < 300ms.
- Accessibility: keyboard and screen-reader flows satisfy dashboard standards.
- Reliability: focus management and popover positioning remain stable at viewport edges.

## Dependencies

**Depends On:**
- F0010-S0001
- F0010-S0002
- F0010-S0003
- F0010-S0004

**Related Stories:**
- F0001-S0003 — View My Tasks (interaction consistency baseline)
- F0001-S0004 — View Broker Activity Feed (dashboard accessibility baseline)

## Out of Scope

- New role definitions or permission model changes
- Export/print workflows for opportunities visualizations

## Questions & Assumptions

**Open Questions:**
- [ ] Should iPhone view default to a single entity type tab to reduce vertical load?

**Assumptions (to be validated):**
- Shared drilldown model is sufficient across all view modes without per-view bespoke popovers.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: N/A (read-only view)
- [ ] Tests pass
- [ ] Documentation updated
