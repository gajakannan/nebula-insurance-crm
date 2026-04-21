# F0010-S0002: Add Opportunities Aging Heatmap View

**Story ID:** F0010-S0002
**Feature:** F0010 — Dashboard Opportunities Refactor (Pipeline Board + Insight Views)
**Title:** Add Opportunities Aging Heatmap view
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution User or Underwriter
**I want** an aging heatmap by status and days-in-status bucket
**So that** I can find bottlenecks and stale workload faster

## Context & Background

A heatmap complements stage counts by showing aging concentration, which helps users prioritize follow-up actions. This view is inspired by the compact density pattern in `planning-mds/screens/UserStats2.png`.

## Acceptance Criteria

**Happy Path:**
- **Given** a user is in the Opportunities widget
- **When** they switch to Heatmap view
- **Then** they see a matrix where rows are statuses and columns are aging buckets
- **And** each cell shows a count and intensity encoding
- **And** the selected period (30d/90d/180d/365d) updates the matrix

**Interaction + Permission:**
- **Given** the user hovers/taps a heatmap cell
- **When** interaction is processed
- **Then** a tooltip or popover shows status, bucket label, and count
- **And** any drilldown items shown are ABAC-scoped

**Alternative Flows / Edge Cases:**
- No aging data in selected period -> show a clear "No aging data" state.
- Extremely sparse data -> keep empty buckets visible for orientation.
- Long status names on small screens -> apply truncation with full label available on hover/tap.
- Read-only guard -> this story does not create, update, delete, or transition domain records; audit/timeline mutation events are not required.

**Checklist:**
- [ ] Heatmap view is available in opportunities view switcher
- [ ] Bucket ranges are fixed and documented
- [ ] Cell interactions expose count details
- [ ] Empty/error/loading states are defined
- [ ] ABAC scope behavior is preserved

## Data Requirements

**Required Fields:**
- EntityType (`submission` or `renewal`)
- Status code and display label
- Bucket key (`0-2`, `3-5`, `6-10`, `11-20`, `21+`)
- Count per status/bucket pair

**Optional Fields:**
- Total per status row

**Validation Rules:**
- Bucket counts are non-negative integers.
- Sum of bucket counts for a status equals that status's open count in the same period.
- Buckets are returned in deterministic order.

## Role-Based Visibility

**Roles that can view Opportunities Aging Heatmap:**
- DistributionUser
- DistributionManager
- Underwriter
- RelationshipManager
- ProgramManager
- Admin

**Data Visibility:**
- Opportunities heatmap data is InternalOnly and ABAC scoped.

## Non-Functional Expectations

- Performance: heatmap payload and render p95 < 500ms on dashboard load/switch.
- Accessibility: non-color cues (numeric values) are available for color-impaired users.
- Reliability: view switch must not reset selected period.

## Dependencies

**Depends On:**
- F0010-S0001 (Pipeline Board default and common period controls)

**Related Stories:**
- F0010-S0003 — Add Opportunities Composition Treemap view
- F0010-S0005 — Unify drilldown, responsive layout, and accessibility

## Out of Scope

- Predictive aging forecasts
- User-configurable bucket boundaries in MVP

## Questions & Assumptions

**Open Questions:**
- [ ] Should heatmap default to combined entity view or preserve side-by-side submissions/renewals only?

**Assumptions (to be validated):**
- Fixed aging buckets are sufficient for MVP triage decisions.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: N/A (read-only view)
- [ ] Tests pass
- [ ] Documentation updated
