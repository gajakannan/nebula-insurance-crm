# F0011-S0002: Add Terminal Outcomes Rail and Outcome Drilldowns

**Story ID:** F0011-S0002
**Feature:** F0011 — Dashboard Opportunities Flow-First Modernization (Connected Pipeline + Terminal Outcomes)
**Title:** Add terminal outcomes rail and outcome drilldowns
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution Manager or Underwriter
**I want** a dedicated terminal outcomes rail beside the primary flow
**So that** I can quickly see where opportunities are ending and drill into loss/win drivers

## Context & Background

Current dashboard opportunities workflows emphasize open-stage volume but do not make terminal outcomes first-class in the primary scan path. Users need immediate visibility into conversion and negative exits without switching to separate charts.

## Acceptance Criteria

**Happy Path:**
- **Given** a user opens the opportunities flow view
- **When** data loads
- **Then** a dedicated outcomes rail is rendered alongside the primary stage flow
- **And** each outcome node shows count and percentage of total exits in the selected period
- **And** each outcome node shows average time-to-exit in days
- **And** terminal branch paths use deterministic style semantics:
  - solid for successful/positive conversion
  - red dashed for negative outcomes
  - gray dotted for passive/time-based outcomes

**Interaction + Permission:**
- **Given** a user selects an outcome node
- **When** drilldown is requested
- **Then** outcome mini-cards open with ABAC-scoped records
- **And** outcome drilldown keeps the current period and context visible

**Alternative Flows / Edge Cases:**
- No terminal outcomes in selected period -> show explicit "No terminal outcomes" state while keeping flow view active.
- Outcome mapping missing for a terminal status -> include that status under "Other terminal" fallback grouping.
- API timeout for outcome details -> show non-blocking error for outcomes rail while stage flow remains usable.
- Read-only guard -> this story does not create, update, delete, or transition domain records; audit/timeline mutation events are not required.

**Checklist:**
- [ ] Outcomes rail is visible in the primary opportunities view
- [ ] Outcome node count, percent, and average exit days are displayed
- [ ] Terminal branch path styles match documented outcome semantics
- [ ] Outcome drilldowns are available
- [ ] Period selector updates outcomes rail and drilldowns
- [ ] Non-blocking error and empty states are defined

## Data Requirements

**Required Fields:**
- Outcome key
- Outcome label
- Outcome count
- Outcome percent-of-total
- Average days to exit
- Drilldown target descriptor

**Optional Fields:**
- Outcome trend versus previous equivalent period

**Validation Rules:**
- Outcome counts are non-negative integers.
- Outcome percentages sum to approximately 100% (rounding tolerance allowed).
- Average days to exit is non-negative and null-safe when count is zero.

## Role-Based Visibility

**Roles that can view outcomes rail and drilldowns:**
- DistributionUser
- DistributionManager
- Underwriter
- RelationshipManager
- ProgramManager
- Admin

**Data Visibility:**
- Outcomes summary and drilldown data are InternalOnly and ABAC scoped.

## Non-Functional Expectations

- Performance: outcomes summary and interaction response p95 < 500ms.
- Security: outcome drilldown enforces existing opportunities authorization boundaries.
- Reliability: outcomes rail can fail independently without taking down stage flow UI.

## Dependencies

**Depends On:**
- F0011-S0001 (connected flow default)
- Existing workflow transition data for terminal status aggregation

**Related Stories:**
- F0011-S0004 — Rebalance secondary insights as mini-views
- F0011-S0005 — Ensure responsive and accessibility parity

## Out of Scope

- New terminal status definitions
- Root-cause analytics or attribution modeling

## Questions & Assumptions

**Open Questions:**
- [ ] Confirm final outcome category set and mapping from terminal statuses (for example: Bound, Declined/No Quote, Expired/Lapsed, Withdrawn/Lost).

**Assumptions (to be validated):**
- Existing transition history provides enough data to compute average days to exit.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: N/A (read-only view)
- [ ] Tests pass
- [ ] Documentation updated
