# F0010-S0001: Replace Sankey Default with Pipeline Board

**Story ID:** F0010-S0001
**Feature:** F0010 — Dashboard Opportunities Refactor (Pipeline Board + Insight Views)
**Title:** Replace Sankey default with Pipeline Board
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution User or Underwriter
**I want** the Opportunities widget to open with a Pipeline Board default view
**So that** I can understand open stage volume quickly without decoding dense transition links

## Context & Background

Current Opportunities presentation is Sankey-heavy and gets visually dense as statuses and transitions increase. A board-style stage summary aligns with dashboard "scan-first" behavior on desktop and especially on iPad/iPhone.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated internal user opens the Dashboard
- **When** the Opportunities widget loads
- **Then** the default view is Pipeline Board for both Submissions and Renewals
- **And** each stage displays status name, count, and color-group semantics
- **And** the 30d/90d/180d/365d selector updates board counts

**Interaction + Permission:**
- **Given** a user can read opportunities data
- **When** they click or tap a stage
- **Then** a drilldown popover opens with mini-cards scoped by ABAC authorization
- **And** unauthorized users do not receive data outside their scope

**Alternative Flows / Edge Cases:**
- No open opportunities in selected period -> show empty board message and keep period controls active.
- Partial data failure (one entity type fails) -> show non-blocking error state for that section and keep the other section usable.
- Small viewport width -> board columns are horizontally scrollable with readable labels.
- Read-only guard -> this story does not create, update, delete, or transition domain records; audit/timeline mutation events are not required.

**Checklist:**
- [ ] Pipeline Board renders as default opportunities view
- [ ] Sankey is removed from default dashboard path
- [ ] Stage drilldowns still work from board view
- [ ] Empty/error/loading states are defined
- [ ] ABAC scope behavior remains intact

## Data Requirements

**Required Fields:**
- EntityType (`submission` or `renewal`)
- Status code and display label
- Open count by status
- ColorGroup (`intake|triage|waiting|review|decision`)

**Optional Fields:**
- Terminal outcome summary counts for context row

**Validation Rules:**
- Stage counts must be non-negative integers.
- Display order follows reference status order.
- Scope filtering is applied before counts are returned.

## Role-Based Visibility

**Roles that can view Opportunities Pipeline Board:**
- DistributionUser
- DistributionManager
- Underwriter
- RelationshipManager
- ProgramManager
- Admin

**Data Visibility:**
- Opportunities data is InternalOnly and ABAC scoped.

## Non-Functional Expectations

- Performance: opportunities board load p95 < 500ms after dashboard render.
- Security: ABAC enforcement remains unchanged from existing opportunities policy.
- Reliability: failures in one subsection do not block the entire dashboard.

## Dependencies

**Depends On:**
- F0001 dashboard shell and opportunities card placement
- Existing opportunities status counts contract

**Related Stories:**
- F0010-S0002 — Add Opportunities Aging Heatmap view
- F0010-S0005 — Unify drilldown, responsive layout, and accessibility

## Out of Scope

- Redesign of KPI, Tasks, Activity Feed, or Nudges widgets
- New workflow statuses or transition rules
- BrokerUser opportunities access policy changes

## Questions & Assumptions

**Open Questions:**
- [ ] Should Sankey remain available as a non-default "Flow Detail" mode in a later feature?

**Assumptions (to be validated):**
- Pipeline Board-first is the preferred operational default for dashboard users.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: N/A (read-only view)
- [ ] Tests pass
- [ ] Documentation updated
