# F0001-S0002: View Pipeline Summary (Sankey Opportunities)

**Story ID:** F0001-S0002
**Feature:** F0001 — Dashboard
**Title:** View Pipeline Summary (Sankey Opportunities)
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution User or Underwriter
**I want** to see submission and renewal flow visualized as Sankey-style transitions
**So that** I can identify bottlenecks and stage-to-stage movement at a glance.

## Context & Background

Pipeline visibility is critical for distribution and underwriting teams. Count-only summaries show volume but hide movement between stages. The implemented widget uses Sankey-style flow charts (Submissions and Renewals) with stage nodes and transition links so users can quickly see where opportunities are progressing or getting stuck.

## Acceptance Criteria

**Happy Path:**
- **Given** the user is authenticated and on the Dashboard
- **When** the dashboard loads
- **Then** the Pipeline Summary widget displays:
  - **Submissions chart:** Sankey-style flow for submission statuses within the selected window, scoped to the user.
  - **Renewals chart:** Sankey-style flow for renewal statuses within the selected window, scoped to the user.
  - **Window selector:** 30d / 90d / 180d / 365d to control the transition window.
  - Status nodes and links are color-coded by workflow stage using `ColorGroup`:

    | ColorGroup | Tailwind Color | Hex (bg/text) | Meaning | Statuses |
    |------------|---------------|---------------|---------|----------|
    | `intake` | slate | `bg-slate-100 text-slate-700` | New / just arrived | Received, Created, Early |
    | `triage` | blue | `bg-blue-100 text-blue-700` | Being sorted / assessed | Triaging |
    | `waiting` | amber | `bg-amber-100 text-amber-700` | Blocked on external action | WaitingOnBroker, OutreachStarted |
    | `review` | violet | `bg-violet-100 text-violet-700` | Under active review | ReadyForUWReview, InReview |
    | `decision` | emerald | `bg-emerald-100 text-emerald-700` | Near completion | Quoted, BindRequested |
**Node/Link Interaction:**
- **Given** the user hovers or clicks a node/link in the Sankey chart
- **When** interaction is processed
- **Then** a popover shows contextual details for the selected status/transition (including counts and related items).

**Edge Cases:**
- No opportunities or transitions in the selected window → show empty-state message
- User has restricted scope → nodes/links and counts reflect only authorized entities
- Very small viewport width → chart remains usable with horizontal overflow handling
- Popover near viewport edge → auto-reposition to stay visible

**Checklist:**
- [x] Submissions Sankey flow rendered
- [x] Renewals Sankey flow rendered
- [x] ColorGroup-based status coloring applied
- [x] Node/link interaction popovers available
- [x] 30/90/180/365 day flow-window selector available
- [x] Empty-state handling for no opportunities/transitions
- [x] Widget loads within the overall dashboard p95 < 2s target
- [x] Popover loads within 300ms of interaction
- [x] Authorization check: counts and flow data are filtered by authenticated user permissions (ABAC)
- [x] Audit/timeline requirement: N/A (read-only view with no mutation)

## Data Requirements

**Required Fields (status summary):**
- Status: string
- Count: non-negative integer
- EntityType: `submission` | `renewal`
- ColorGroup: one of `intake|triage|waiting|review|decision`

**Required Fields (flow graph):**
- Nodes: status, label, isTerminal, displayOrder, colorGroup, currentCount, inflowCount, outflowCount
- Links: sourceStatus, targetStatus, count

**Validation Rules:**
- Counts must be non-negative integers
- Status labels must match workflow reference data
- Counts must be non-negative
- Flow links use observed workflow transitions within the selected time window

## Role-Based Visibility

**Roles that can view Pipeline Summary:**
- Distribution User — sees submissions/renewals scoped to their department/region
- Underwriter — sees submissions assigned to or accessible by them
- Relationship Manager — sees submissions/renewals linked to their broker relationships
- Program Manager — sees submissions/renewals within their programs
- Admin — sees all (unscoped)

**Data Visibility:**
- All pipeline data is InternalOnly.

## Non-Functional Expectations

- Performance: Flow charts must render within the overall dashboard p95 < 2s target; flow interaction popover target remains p95 < 300ms.
- Security: Backend must enforce Casbin ABAC scope before aggregating counts and transitions.
- Reliability: If flow query fails, display a non-blocking error for this widget and keep other dashboard widgets functional.

## Dependencies

**Depends On:**
- Submission entity with CurrentStatus field and workflow status values
- Renewal entity with CurrentStatus field and workflow status values
- WorkflowTransition entity (for transition graph generation)

**Related Stories:**
- F0001-S0001 — KPI Cards (complementary high-level metrics)
- F0006 — Submission Intake Workflow (provides submission data)
- F0007 — Renewal Pipeline (provides renewal data)

## Out of Scope

- Full workflow drilldown workspace and transition editing (Future)
- Filtering by date range within the widget
- Historical trend comparison (e.g., this week vs last week)
- Terminal-stage deep analytics

## UI/UX Notes

- Screens involved: Dashboard
- Layout: Submissions chart and Renewals chart stacked in the Opportunities card
- Visual model: Sankey-style status nodes with curved transition links
- Interaction: node/link popovers for detail context
- Responsive: horizontal overflow enabled for smaller viewports
- Color palette: 5 `ColorGroup` categories — `intake` (slate), `triage` (blue), `waiting` (amber), `review` (violet), `decision` (emerald); see mapping table in Acceptance Criteria above

## Questions & Assumptions

**Assumptions:**
- Flow visualization is based on transition activity within the selected window (30/90/180/365 days).
- Status summaries and flow graph share the same scoped source data.

## Definition of Done

- [x] Acceptance criteria met
- [x] Edge cases handled (zero data, restricted scope, query failure, popover positioning)
- [x] Permissions enforced (Casbin ABAC scope filtering)
- [x] Audit/timeline logged: N/A (read-only)
- [x] Tests pass (flow endpoint and rendering behavior)
- [x] Accessible: interactive nodes/links remain keyboard and screen-reader friendly
