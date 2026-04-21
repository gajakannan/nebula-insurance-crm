# F0011: Dashboard Opportunities Flow-First Modernization (Connected Pipeline + Terminal Outcomes)

**Feature ID:** F0011
**Feature Name:** Dashboard Opportunities Flow-First Modernization (Connected Pipeline + Terminal Outcomes)
**Priority:** High
**Phase:** MVP
**Status:** Draft

## Feature Statement

**As a** Distribution User, Underwriter, Relationship Manager, Program Manager, or Admin
**I want** a single flow-first opportunities experience with explicit terminal outcomes and lighter visual noise
**So that** I can identify bottlenecks, conversion, and losses in one pass across desktop and mobile

## Business Objective

- **Goal:** Improve decision speed and confidence in opportunities triage workflows.
- **Metric:**
  - Median time-to-identify the top bottleneck stage
  - Median time-to-identify the top terminal outcome
  - Drilldown open rate from opportunities stages/outcome nodes
  - Mobile opportunities review completion rate
- **Baseline:** F0010 replaced Sankey default with Pipeline Board and optional insight views, but stage flow and terminal outcomes still require multiple visual hops.
- **Target:** Users can identify flow bottlenecks and terminal outcome concentration without leaving the primary opportunities panel.

## Problem Statement

- **Current State:** Opportunities view is cleaner than legacy Sankey, but primary visual still reads as disconnected tiles and does not foreground terminal outcomes.
- **Desired State:** A connected left-to-right flow canvas becomes the primary operational view, with terminal outcomes presented in a dedicated outcome rail.
- **Impact:** Faster triage, lower cognitive load, and better visibility into win/loss/expiry outcomes.

## Scope & Boundaries

**In Scope:**
- Replace tile-heavy opportunities primary panel with a connected flow-first canvas.
- Introduce left-to-right milestone spine with stage nodes and ribbon-style transitions.
- Add terminal outcomes rail with distinct visual branch semantics.
- Apply warm-to-cool stage color rhythm progression across open stages.
- Keep dark dashboard theme while reducing border noise and introducing soft elevation.
- Add selective visual emphasis for active and blocked stages.
- Reposition radial-inspired analytics to secondary mini-views, not the primary workflow.
- Define responsive behavior where mobile prioritizes stacked stage cards and bottleneck list.

**Out of Scope:**
- Workflow status taxonomy changes for submissions or renewals.
- Predictive scoring, forecasting, or AI-generated recommendations.
- New external or BrokerUser opportunities surfaces.
- Full dashboard redesign beyond opportunities widget boundaries.

## Acceptance Criteria Overview

- [ ] Primary opportunities view is a connected flow-first canvas with left-to-right reading.
- [ ] Terminal outcomes are visible in a dedicated outcomes rail with clear state semantics.
- [ ] Warm-to-cool progression is consistently applied to stage progression.
- [ ] Active/blocked emphasis is visible without reducing readability or accessibility.
- [ ] Secondary radial ideas exist only as optional mini-views, not main workflow views.
- [ ] Desktop/tablet/mobile interaction parity and accessibility requirements are defined and testable.

## UX / Screens

| Screen | Purpose | Key Actions |
|--------|---------|-------------|
| Dashboard — Opportunities Widget | Primary operational pipeline triage and outcomes scan | Switch period, select stage, select outcome node, open drilldown, inspect secondary mini-views |

**Key Workflows:**
1. Flow Triage Workflow — User opens dashboard, scans connected stage flow, and opens drilldown on blocked stage.
2. Outcome Diagnosis Workflow — User reads outcomes rail to identify top terminal state and opens outcome drilldown.
3. Secondary Analysis Workflow — User opens mini-view panels (aging/radial) for deeper pattern context and returns to primary flow.

## ASCII Wireframe

### Desktop (Flow-First + Outcomes Rail)

```text
+--------------------------------------------------------------------------------------------------------------------------+
| OPPORTUNITIES                                                                                      30d 90d [180d] 365d |
+--------------------------------------------------------------------------------------------------------------------------+
| OPEN FLOW (primary)                                                                                                      |
| [Received]====[Triaging]====[UW Review]*====[Quote Prep]====[Quoted]====[Bind Req]====[Bound Path]                     |
|      42           31             18                10            6              4                                       |
|                     \\                                                                                                   |
|                      \\====[Waiting Docs]!====>                                                                         |
|                                                                                                                          |
| Milestones:  <SLA 2d> ----------------- <SLA 5d> ----------------- <SLA 10d> ----------------- <Renewal>               |
|                                                                                                                          |
| SECONDARY MINI-VIEWS (supporting, not primary): [Aging Mini] [Radial Mini] [Hierarchy Mini]                            |
+---------------------------------------------------------------+----------------------------------------------------------+
| STAGE DRILLDOWN PANEL (contextual)                           | TERMINAL OUTCOMES RAIL                                  |
| - Selected stage label + count                               | [Bound]             34 | 48% | avg 12d                  |
| - Top mini-cards                                             | [No Quote]          11 | 16% | avg  9d                  |
| - View all link                                              | [Declined]          14 | 20% | avg  7d                  |
|                                                              | [Expired/Lapsed]     8 | 11% | avg 22d                  |
|                                                              | [Lost/Withdrawn]     4 |  5% | avg 15d                  |
+---------------------------------------------------------------+----------------------------------------------------------+
```

Legend:
- `====` ribbon connection (thicker = higher volume)
- `*` active stage emphasis
- `!` blocked stage emphasis
- Warm-to-cool stage color progression runs left -> right

Terminal branch semantics in primary flow:

```text
MAIN FLOW (left -> right)                                  TERMINAL OUTCOMES RAIL
[Received]=>[Triaging]=>[UW Review]=>[Quote Prep]=>[Quoted]=>[Bind Req]=>[Bound]
                                        \                         \
                                         \===== warm solid ======> [No-Quote]
                                          \---- red dashed ------> [Declined]
                                           \.... gray dotted ....> [Expired]
                                            \---- red dashed -----> [Lost to Competitor]
```

Terminal path style rules:
- `solid` = successful/positive conversion path
- `red dashed` = negative terminal outcomes
- `gray dotted` = passive/time-based terminal outcomes

### Mobile (Simplified Primary Actions)

```text
+--------------------------------------------+
| OPPORTUNITIES                [180d]        |
+--------------------------------------------+
| Bottlenecks                                 |
| 1) UW Review (18) *                         |
| 2) Waiting Docs (12) !                      |
+--------------------------------------------+
| Stages (stacked cards)                      |
| [Received 42]                               |
| [Triaging 31]                               |
| [UW Review 18] *                            |
| [Quote Prep 10]                             |
| [Quoted 6]                                  |
+--------------------------------------------+
| Outcomes                                    |
| [Bound 34 | 48%]                            |
| [Declined 14 | 20%]                         |
| [Expired 8 | 11%]                           |
+--------------------------------------------+
| Mini-Views: [Aging] [Radial] [Hierarchy]    |
+--------------------------------------------+
```

## Data Requirements

**Core Entities:**
- Submission opportunity statuses and counts
- Renewal opportunity statuses and counts
- Workflow transition aggregates for terminal outcomes
- Stage sequence and milestone metadata for rendering order

**Validation Rules:**
- Counts and rates are non-negative.
- Stage and outcome ordering is deterministic.
- Period selection applies consistently to all summary datasets.
- Role scope filtering is applied before aggregation.

**Data Relationships:**
- EntityType (`submission` | `renewal`) -> Stage sequence -> Terminal outcomes
- Stage node -> Drilldown target -> Mini-card items

## Role-Based Access

| Role | Access Level | Notes |
|------|-------------|-------|
| DistributionUser | Read | ABAC scoped |
| DistributionManager | Read | ABAC scoped |
| Underwriter | Read | ABAC scoped |
| RelationshipManager | Read | ABAC scoped |
| ProgramManager | Read | ABAC scoped |
| Admin | Read | Internal unscoped |
| BrokerUser | Denied | Opportunities widget remains InternalOnly |

## Success Criteria

- Users can identify highest-volume bottleneck and highest-volume terminal outcome in one dashboard pass.
- Terminal outcomes become first-class in opportunities triage without requiring separate charts.
- Mobile opportunities review remains readable and actionable without dense grid/chart interactions.

## Risks & Assumptions

- **Risk:** Over-stylizing the flow could reduce data legibility.
- **Assumption:** Existing opportunities datasets can support terminal outcome rollups without schema changes.
- **Mitigation:** Keep visual hierarchy data-first; enforce contrast and text-first labels.

## Dependencies

- F0010 opportunities refactor baseline (Pipeline Board + Heatmap/Treemap/Sunburst)
- Existing dashboard opportunities summary/flow/drilldown endpoints and ABAC policy
- `planning-mds/screens/pipeline*.png` inspiration direction and `planning-mds/screens/temp/` baseline captures

## Related User Stories

- F0011-S0001 — Replace Pipeline Board tiles with connected flow-first canvas default
- F0011-S0002 — Add dedicated terminal outcomes rail and outcome drilldowns
- F0011-S0003 — Apply modern opportunities visual system (dark depth + stage emphasis)
- F0011-S0004 — Rebalance secondary insights (aging and radial mini-views)
- F0011-S0005 — Ensure responsive and accessibility parity for the new opportunities flow

## Rollout & Enablement

- Capture before/after screenshots for MacBook, iPad, and iPhone breakpoints.
- Include a stakeholder walkthrough checklist focused on bottleneck and terminal-outcome detection tasks.
