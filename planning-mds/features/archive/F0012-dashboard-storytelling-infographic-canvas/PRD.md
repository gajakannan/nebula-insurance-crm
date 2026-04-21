# F0012: Dashboard Storytelling Infographic Canvas (Flat Canvas + Collapsible Rails)

**Feature ID:** F0012
**Feature Name:** Dashboard Storytelling Infographic Canvas (Flat Canvas + Collapsible Rails)
**Priority:** High
**Phase:** MVP
**Status:** Done (Archived)
**Supersedes:** F0011 — Dashboard Opportunities Flow-First Modernization

## Feature Statement

**As a** Distribution User, Underwriter, Relationship Manager, Program Manager, or Admin
**I want** the entire dashboard presented as one continuous infographic canvas — nudges, KPIs, opportunity flow, terminal outcomes, activity, and tasks flowing as a single flat narrative surface
**So that** I can read the complete operational story from alerts through diagnosis to action without visual interruptions from panel borders, tabs, or mode switches

## Design Philosophy

> **Infographic, not dashboard.** The page is one continuous flat canvas. Content zones are differentiated by spacing, typography, and color weight — never by panel borders, card wrappers, or divider lines. Every section is part of the same visual narrative. Think editorial infographic poster, not component grid.

Key principles:
- **No panel wrappers** — no elevated cards, no bordered containers, no separator lines between content zones.
- **Flat canvas flow** — content flows top-to-bottom as a continuous narrative: nudges → KPI band → opportunity flow → chapter overlays → activity → tasks.
- **Spacing hierarchy** — vertical whitespace and typography size establish section boundaries.
- **Selective emphasis** — color, glow, and animation used sparingly for active/blocked/overdue states, not for decoration.
- **Dark-first** — dark product theme with lower visual noise; light theme parity maintained.

## Business Objective

- **Goal:** Increase dashboard decision speed and reduce context switching in opportunities triage.
- **Metric:**
  - Median time-to-identify top bottleneck stage
  - Median time-to-identify dominant terminal outcome
  - Drilldown open rate from story-canvas stage/outcome interactions
  - Session depth for opportunities analysis without mode switching
- **Baseline:** F0010 improved opportunities visuals but still uses panelized sections with bordered cards and mode-separated insight tabs (Pipeline/Heatmap/Treemap/Sunburst).
- **Target:** One unified infographic canvas becomes the default dashboard experience with zero panel wrappers and zero mode switches.

## Problem Statement

- **Current State:** Dashboard is assembled from isolated panel cards (nudge bar, KPI cards, opportunities panel with tab switching, activity panel, task panel). Each panel has its own borders, elevation, and visual container. Users must mentally stitch together the narrative across disconnected surfaces.
- **Desired State:** One continuous flat infographic canvas where nudges, KPIs, opportunity flow, chapter overlays, activity, and tasks are all sections of the same visual narrative — differentiated by spacing and typography, not borders.
- **Impact:** Lower cognitive load, better scanability, faster triage-to-action flow, and a more modern editorial feel.

## Scope & Boundaries

**In Scope:**
- Integrate nudge/action bar as the top section of the infographic canvas, flowing seamlessly into story controls below it (no separator line or border between them).
- Merge KPI strip and opportunities visualization into the canvas as continuous content zones (no separate KPI/opportunities panel cards or wrappers).
- Render opportunities as a connected left-to-right flow with terminal outcome branch semantics (absorbing F0011-S0001 and F0011-S0002 scope).
- Add chapter-based storytelling controls (`Flow`, `Friction`, `Outcomes`, `Aging`, `Mix`) as in-canvas overlays — replacing F0010's tab-switching model.
- Flow Activity and My Tasks as continuous flat canvas sections below the story content — differentiated by spacing and section headers, not panel borders.
- Preserve collapsible left navigation and collapsible right Neuron rail; canvas width adapts to rail state.
- Eliminate panel borders, card wrappers, elevated surfaces, and divider lines throughout the dashboard — use spacing, typography, and color hierarchy to delineate content zones.
- Apply infographic visual treatment: warm-to-cool color progression across flow stages, selective glow/emphasis for active/blocked states, reduced border noise.
- Maintain dark product theme with light theme parity; follow `planning-mds/screens/design-tokens.md`.

**Out of Scope:**
- Workflow status taxonomy changes for submissions/renewals.
- AI recommendation logic changes inside Neuron rail.
- New external BrokerUser dashboard surfaces.
- Global navigation IA redesign beyond dashboard layout behavior.

**F0011 Supersession:**
F0012 fully supersedes F0011. All F0011 concepts (connected flow canvas, terminal outcomes rail, visual system modernization, secondary insight rebalancing, responsive/accessibility parity) are self-contained within F0012's scope. F0011 is deprecated and will not be implemented separately.

## Acceptance Criteria Overview

- [ ] Nudge/action bar renders as the top section of the infographic canvas with no visual separator between nudges and story controls.
- [ ] KPI metrics and opportunities flow are integrated into one continuous flat canvas without panel wrappers or borders.
- [ ] Opportunities render as connected left-to-right flow with terminal outcome branch semantics (solid/dashed/dotted branch styles).
- [ ] Story-canvas supports chapter controls without mode/tab switching out of the primary context.
- [ ] Activity and My Tasks render as flat canvas sections below the story content, differentiated by spacing and typography — not bordered panels.
- [ ] Left nav and right Neuron rail remain collapsible and the canvas expands/contracts accordingly.
- [ ] No panel borders, card wrappers, or divider lines are present in the dashboard layout.
- [ ] Desktop/tablet/mobile and accessibility behavior is defined, testable, and consistent.

## UX / Screens

| Screen | Purpose | Key Actions |
|--------|---------|-------------|
| Dashboard — Infographic Canvas | Full continuous narrative surface: nudges → KPIs → flow → chapters → activity → tasks | Dismiss nudge, switch period, switch chapter, select stage/outcome, open drilldown, scroll to activity/tasks |

**Formal screen specification:** [`planning-mds/screens/S-DASH-001-infographic-canvas.md`](../../../screens/S-DASH-001-infographic-canvas.md)

**Key Workflows:**
1. **Alert-to-Story Scan** — User sees overdue nudges at top, scans KPI band, reads connected opportunity flow and terminal outcomes in one continuous scroll.
2. **Friction Diagnosis** — User switches to `Friction`/`Aging` chapter overlay; stage nodes highlight bottleneck intensity without leaving the canvas.
3. **Outcome Diagnosis** — User switches to `Outcomes` chapter; terminal outcome rail emphasizes exit reasons with counts and percentages.
4. **Action Handoff** — User scrolls past the story content to Activity and My Tasks sections and executes follow-up work.

## ASCII Wireframe

### Desktop (Collapsible Left + Right Rails)

```text
+-----+-------------------------------------------------------------------------------------------+-----+
|NAV< | MAIN CONTENT                                                                              |>AI  |
|coll.|                                                                                           |coll.|
+-----+-------------------------------------------------------------------------------------------+-----+
| TOP BAR: Dashboard                                                       [theme] [notif] [profile]   |
+-------------------------------------------------------------------------------------------------------+
|                                                                                                       |
| NUDGE ZONE (flat, no border)                                                                          |
| [Overdue] Follow renewal Lost — 6d    [Overdue] Follow renewal Expired — 5d    [Overdue] Review...   |
|                                                                                                       |
| STORY CONTROLS (no separator from nudges)                                                             |
| [30d] [90d] [180d] [365d]                              [Flow] [Friction] [Outcomes] [Aging] [Mix]    |
|                                                                                                       |
| KPI BAND (flat, embedded in canvas)                                                                   |
|   Active Brokers: 173    Open Submissions: 6    Renewal Rate: 45.0%    Avg Turnaround: 24.9 days     |
|                                                                                                       |
| CONNECTED FLOW (left-to-right storyline)                                                              |
| [Received]══>[Triaging]══>[UW Review]══>[Quote Prep]══>[Quoted]══>[Bind Req]══>[Bound ✅]            |
|                             \                     \                                                   |
|                              \===== warm solid ====> [No-Quote ⚠]                                    |
|                               \---- red dashed ----> [Declined ❌]                                     |
|                                \.... gray dotted ...> [Expired ⌛]                                     |
|                                 \---- red dashed ---> [Lost ❌]                                        |
|                                                                                                       |
| IN-CANVAS OVERLAYS (chapter-driven, same surface)                                                     |
|   Aging heat blocks | Mix composition blocks | Radial mini inset                                     |
|                                                                                                       |
|                                            (spacing break — no border)                                |
|                                                                                                       |
| ACTIVITY (flat section, header + timeline list)                                                       |
|   Activity                                                                          TIMELINE          |
|   • Broker Status Changed — Ironwood Wholesale 112 marked Inactive — 2 weeks ago                    |
|   • Broker Created — Meridian Wholesale 024 added — 3 weeks ago                                      |
|                                                                                                       |
|                                            (spacing break — no border)                                |
|                                                                                                       |
| MY TASKS (flat section, header + task list)                                                           |
|   My Tasks                                                                                            |
|   Follow renewal Lost          Mar 5   Open                                                           |
|   Follow renewal Expired       Mar 6   Open                                                           |
|   Review submission in Declined Mar 8   In Progress                                                   |
|                                                                                                       |
+-------------------------------------------------------------------------------------------------------+
```

### Mobile / iPad (Story First, Sections Stacked)

```text
+----------------------------------------------+
| Dashboard                         [menu] [AI]|
+----------------------------------------------+
| Nudge zone (compact, dismissible)            |
| [180d] [Flow] [Outcomes]                     |
+----------------------------------------------+
| KPI strip (compact inline)                   |
| Connected flow (vertical/stacked on phone)   |
| Terminal outcomes                            |
| Chapter overlay toggle                        |
+----------------------------------------------+
|              (spacing break)                 |
| Activity (flat section)                      |
+----------------------------------------------+
|              (spacing break)                 |
| My Tasks (flat section)                      |
+----------------------------------------------+
```

## Chapter-to-Data Mapping

Maps F0010 view modes to F0012 infographic chapters and their data sources.

| F0010 View Mode | F0012 Chapter | Data Source / Endpoint | Overlay Behavior |
|---|---|---|---|
| Pipeline Board | **Flow** (default) | `GET /dashboard/opportunities` + connected sequence metadata | Base flow canvas — no overlay; connected left-to-right rendering |
| *(new from F0011)* | **Friction** | Stage aggregates + emphasis hints (`blocked`, `bottleneck`, `stalled`) | Friction annotation overlay on flow nodes; highlights stages with highest dwell time |
| *(new from F0011)* | **Outcomes** | Terminal outcome aggregates (`outcome_type`, `count`, `percent_of_exits`, `avg_days_to_exit`) | Terminal outcomes rail emphasis; branch path styles (solid/dashed/dotted) |
| Heatmap | **Aging** | `GET /dashboard/opportunities/aging` (status x aging-bucket matrix) | Heat intensity overlay on stage nodes; bucket visualization |
| Treemap + Sunburst | **Mix** | `GET /dashboard/opportunities/hierarchy` (EntityType → ColorGroup → Status) | Composition blocks and radial mini inset as compact overlays |

## Data Requirements

**Core Entities:**
- Nudge/action items (overdue tasks, items requiring attention)
- KPI aggregates (active brokers, open submissions, renewal rate, avg turnaround)
- Opportunities stage aggregates with deterministic sequence metadata and connection strengths
- Terminal outcome aggregates: outcome_type, count, percent_of_exits, avg_days_to_exit
- Stage emphasis hints (normal, active, blocked, bottleneck)
- Timeline activity feed and user task queue (unchanged data sources, new layout position)
- Layout state preferences (period selection, chapter, rail collapse state)

**Validation Rules:**
- Counts and rates are non-negative and scoped by role policy.
- Stage and outcome ordering remains deterministic.
- Chapter switches do not change underlying period scope unexpectedly.
- Collapsed rail state must not hide actionable controls irrecoverably.
- Nudge items must be scoped to the current user's role and assignments.

**Data Relationships:**
- Period selector → KPI + stage flow + outcomes + overlay datasets (single synchronized filter)
- Story chapter → same base dataset with alternate visual emphasis and overlay layers
- Rail state (left/right collapsed/expanded) → canvas available width and responsive rendering mode
- Nudge items → independent of period selector (always show current actionable items)

## Role-Based Access

| Role | Access Level | Notes |
|------|-------------|-------|
| DistributionUser | Read | ABAC scoped dashboard visibility |
| DistributionManager | Read | ABAC scoped dashboard visibility |
| Underwriter | Read | ABAC scoped dashboard visibility |
| RelationshipManager | Read | ABAC scoped dashboard visibility |
| ProgramManager | Read | ABAC scoped dashboard visibility |
| Admin | Read | Internal unscoped |
| BrokerUser | Denied | Dashboard infographic canvas remains InternalOnly |

## Success Criteria

- Users scan from nudge alerts through opportunity flow to action handoff without encountering panel borders or mode switches.
- Users identify the highest-friction stage and highest terminal-outcome category within the same canvas scroll.
- Stakeholder review confirms the infographic canvas supports "single-surface narrative" from alerts → metrics → diagnosis → action.
- Activity and task execution workflows remain clear and operational within their canvas sections.
- No visual panel borders, card wrappers, or divider lines are present in the production dashboard.

## Risks & Assumptions

- **Risk:** Over-visualization in flat canvas reduces legibility of exact counts.
- **Assumption:** Existing opportunities/KPI aggregates plus F0010's aging/hierarchy endpoints can support all chapter overlays without new domain entities.
- **Assumption:** Nudge bar data (overdue tasks, attention items) is already available from existing My Tasks endpoint filtering.
- **Mitigation:** Keep numeric labels persistent and prioritize text/readability over decorative effects. Use the infographic philosophy to reduce noise, not add it.

## Dependencies

- F0010 opportunities refactor baseline (existing Pipeline Board, Heatmap, Treemap, Sunburst views and endpoints) — **Done**
- Visual direction references:
  - current baseline captures in `planning-mds/screens/temp/` (F0010 implementation screenshots)
  - inspiration set in `planning-mds/screens/pipeline1-5.png` (structured timeline infographics), `pipeline6-8.png` (editorial data art), `pipeline9-11.png` (dense flow visualizations)
  - design tokens in `planning-mds/screens/design-tokens.md`

**Note:** F0011 is deprecated and superseded by F0012. All F0011 scope (connected flow, terminal outcomes, visual system, mini-view rebalancing, responsive parity) is self-contained within F0012.

## Related Stories

- F0012-S0001 — Unify nudge bar, KPI band, and connected opportunity flow into one flat infographic canvas
- F0012-S0002 — Add interactive story chapters and in-canvas analytical overlays
- F0012-S0003 — Flow Activity and My Tasks as flat canvas sections below story content
- F0012-S0004 — Preserve collapsible left nav and right Neuron rail with adaptive canvas width
- F0012-S0005 — Ensure responsive, accessibility, and performance parity for infographic canvas

## Rollout & Enablement

- Capture before/after screenshots for MacBook, iPad, and iPhone showing the flat infographic treatment.
- Include review checklist for:
  - no panel borders or card wrappers visible in production
  - nudge-to-story flow continuity (no separator)
  - chapter discoverability and overlay clarity
  - terminal outcome readability and branch path semantics
  - action handoff clarity from story content → activity/tasks sections
