# F0010: Dashboard Opportunities Refactor (Pipeline Board + Insight Views)

**Feature ID:** F0010
**Feature Name:** Dashboard Opportunities Refactor (Pipeline Board + Insight Views)
**Priority:** High
**Phase:** MVP
**Status:** Done

## Feature Statement

**As a** Distribution User, Underwriter, Relationship Manager, Program Manager, or Admin
**I want** a simpler Opportunities experience with a clear default pipeline view and optional insight views
**So that** I can identify bottlenecks and act on stuck work without parsing a dense Sankey graph

## Business Objective

- **Goal:** Improve dashboard decision speed for opportunities triage and follow-up.
- **Metric:**
  - Median time to identify top bottleneck stage in usability walkthroughs
  - Click-through rate from opportunities widget into status mini-cards
  - Mobile/tablet task completion rate for opportunities review
- **Baseline:** Current Sankey-heavy opportunities section is visually dense and harder to scan, especially on iPad/iPhone.
- **Target:** Default opportunities scan is understandable in one pass on desktop, tablet, and mobile while preserving drilldown detail.

## Problem Statement

- **Current State:** Opportunities currently rely on dual Sankey visualizations that become busy when statuses and transitions increase.
- **Desired State:** Pipeline Board is the default view, with Heatmap, Treemap, and Sunburst available as optional insight views.
- **Impact:** Faster operational understanding, lower cognitive load, and stronger cross-device usability.

## Scope & Boundaries

**In Scope:**
- Replace default Sankey-first experience with Pipeline Board-first opportunities layout.
- Keep 30d/90d/180d/365d period controls and ABAC-scoped counts.
- Add optional insight views:
  - Aging Heatmap (status x age bucket)
  - Composition Treemap
  - Hierarchy Sunburst
- Keep status mini-card drilldowns and "view all" transitions.
- Enforce desktop/iPad/iPhone responsive behavior for opportunities widget.

**Out of Scope:**
- Workflow/status taxonomy changes for submissions or renewals.
- Predictive scoring, forecasting, or AI-generated recommendations.
- New broker-facing opportunities surfaces.
- Full submission or renewal workspace redesign outside dashboard.

## Acceptance Criteria Overview

- [ ] Opportunities default view is Pipeline Board (not Sankey).
- [ ] Heatmap, Treemap, and Sunburst are available as optional views.
- [ ] Drilldown popovers remain available from all applicable views.
- [ ] Dashboard ABAC scope and role boundaries remain unchanged.
- [ ] Mobile/tablet layouts are explicitly optimized for readability and interaction.
- [ ] Error, loading, and empty-state behavior is defined per view.

## UX / Screens

| Screen | Purpose | Key Actions |
|--------|---------|-------------|
| Dashboard — Opportunities Widget | At-a-glance operational pipeline view with optional analytics perspectives | Switch view mode, switch period, open status drilldown mini-cards, navigate to filtered lists |

**Key Workflows:**
1. Pipeline Triage Workflow — User opens dashboard, scans default Pipeline Board, selects a blocked stage, and opens mini-cards.
2. Bottleneck Detection Workflow — User switches to Heatmap view and identifies aging concentration by stage.
3. Composition Analysis Workflow — User switches to Treemap or Sunburst for distribution understanding, then drills into a stage.

## Data Requirements

**Core Entities:**
- Submission opportunity statuses and counts
- Renewal opportunity statuses and counts
- Workflow transition aggregates (for optional insights)

**Validation Rules:**
- All counts are non-negative integers.
- View datasets must be period-scoped to the selected window.
- Role scope filtering must be applied before aggregation.

**Data Relationships:**
- EntityType (`submission` or `renewal`) -> ColorGroup (`intake|triage|waiting|review|decision`) -> Status

## Role-Based Access

| Role | Access Level | Notes |
|------|-------------|-------|
| DistributionUser | Read | ABAC scoped |
| DistributionManager | Read | ABAC scoped |
| Underwriter | Read | ABAC scoped |
| RelationshipManager | Read | ABAC scoped |
| ProgramManager | Read | ABAC scoped |
| Admin | Read | Unscoped internal |
| BrokerUser | Denied | Opportunities widget remains InternalOnly |

## Success Criteria

- Users can identify the highest-volume waiting/review stage without relying on Sankey link parsing.
- Optional views provide complementary insight without replacing operational default clarity.
- Dashboard opportunities remains usable on MacBook, iPad, and iPhone breakpoints.

## Risks & Assumptions

- **Risk:** Adding multiple views may reintroduce complexity if defaults are not strict.
- **Assumption:** Pipeline Board remains the default and primary operational workflow.
- **Mitigation:** Keep advanced views behind explicit view toggle; preserve concise defaults.

## Dependencies

- F0001 dashboard baseline widget composition
- Existing opportunities summary and drilldown contracts
- ABAC policies for dashboard resources

## Related User Stories

- F0010-S0001 - Replace Sankey default with Pipeline Board
- F0010-S0002 - Add Opportunities Aging Heatmap
- F0010-S0003 - Add Opportunities Composition Treemap
- F0010-S0004 - Add Opportunities Hierarchy Sunburst
- F0010-S0005 - Unify Drilldown, Responsive Layout, and Accessibility

## Rollout & Enablement

- Include before/after screenshots for MacBook, iPad, and iPhone views in release notes.
- Add stakeholder walkthrough checklist focused on opportunities comprehension tasks.
