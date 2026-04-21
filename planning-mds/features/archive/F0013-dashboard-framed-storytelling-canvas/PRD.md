# F0013: Dashboard Framed Storytelling Canvas

**Feature ID:** F0013
**Feature Name:** Dashboard Framed Storytelling Canvas
**Priority:** High
**Phase:** MVP
**Status:** Done (Archived)
**Corrects:** F0012 (archived), F0011 (abandoned), F0010 (abandoned)

## Feature Statement

**As a** Distribution User, Underwriter, Relationship Manager, Program Manager, or Admin
**I want** the dashboard presented as a framed storytelling canvas — the app chrome (nav, rails, top bar) forms the frame, operational panels (nudges, activity, tasks) float with depth and glow, and the opportunity flow tells its story as a vertical timeline where each stage has a contextual mini-visualization that best fits that stage's narrative
**So that** I get the best of both worlds: the polished framed-canvas identity of the original dashboard design and the narrative storytelling power of an infographic timeline where each stop tells its own chapter of the submission journey

## Design Philosophy

> **Framed canvas, not flat poster.** The app chrome (left nav, right Neuron rail, top bar) forms a visible frame. Inside the frame, the dashboard is a canvas with three visual layers — not one flat surface.

### Three-Layer Visual Hierarchy

| Layer | Treatment | Components |
|-------|-----------|------------|
| **Frame** | App chrome — solid edges, always present | Left nav rail, right Neuron rail, top bar |
| **Operational panels** | Glass-card, float/raised depth, soft hover/focus glow | Nudge cards, Activity panel, My Tasks panel |
| **Story canvas** | Flat infographic zone — vertical timeline, contextual mini-visuals, chapter overlays | Opportunity flow, KPI band, chapter controls |

Key principles:
- **Operational panels keep their identity** — nudge cards, Activity, and My Tasks use `glass-card` with depth and the soft violet/fuchsia hover glow from the design system. They float above the canvas.
- **Story canvas is the infographic zone** — the opportunity flow area between KPI band and Activity is where the storytelling lives. This zone is flat, borderless, and narrative.
- **Vertical timeline, not table cells** — the opportunity flow renders as a vertical timeline spine with stage nodes alternating left and right, inspired by `pipeline5.png` and `handdrawn-timeline.jpeg`.
- **Contextual mini-visualizations** — each stage node on the timeline has a mini-visualization chosen to best tell that stage's story. One stop might be a donut (composition), another a mini bar chart (broker distribution), another a gauge (SLA health). The chart type is selected per-stage based on what's most meaningful, not forced to be uniform.
- **Size proportional to volume** — mini-visuals scale with the stage's item count. A stage with 300 items gets a larger visual than a stage with 2.
- **Chapters as uniform override** — chapter controls (Flow/Friction/Outcomes) override the contextual defaults and force ALL stops to show the same dimension for cross-stage comparison. Aging and mix dimensions are handled via per-stop alternate toggles (S0003) rather than global chapters.
- **Editorial palette** — muted coral + steel blue accent pair on deep navy (dark) or warm gray (light), inspired by `pipeline5.png` and `pipeline4.png`. Replaces the neon violet/fuchsia SaaS palette with a calmer data-journalism tone.
- **Dark-first** — dark product theme with lower visual noise; light theme parity maintained.

### What F0012 Got Wrong

F0012 interpreted "infographic" as "flatten everything." It stripped borders, depth, and glow from ALL dashboard components — including operational panels (nudges, activity, tasks) that need those identity elements to feel interactive and purposeful. The result lost the framed-canvas feel shown in the original handdrawn wireframe (`planning-mds/screens/handdrawn.jpeg`).

### What This Feature Corrects

1. **Restores glass-card / depth / glow** on nudge cards, Activity panel, and My Tasks panel.
2. **Restores the framed canvas identity** — the nav/rails/top bar act as a frame enclosing the dashboard canvas.
3. **Replaces flat rectangular stage cells** with a vertical timeline where each stop has a contextual mini-visualization (donut, bar chart, gauge, etc.) chosen to best fit that stage's narrative.
4. **Keeps what F0012 got right** — chapter controls, period synchronization, collapsible rails, connected flow concept, terminal outcome branches.

## Business Objective

- **Goal:** Restore dashboard visual identity while improving opportunity flow storytelling with timeline-bar and radial-chart visualization.
- **Metric:**
  - Median time-to-identify top bottleneck stage (via radial popover scan)
  - Drilldown open rate from radial popover interactions
  - User satisfaction with framed-canvas feel vs. flat-canvas feel (A/B or stakeholder review)
- **Baseline:** F0012 implemented a fully flat canvas that lost depth, glow, and framed identity on operational panels. Opportunity flow uses flat rectangular cells with counts.
- **Target:** Restore framed-canvas identity with three-layer visual hierarchy. Replace flat cells with timeline-bar + radial popovers that reveal composition, not just counts.

## Problem Statement

- **Current State:** F0012 flattened everything — nudge cards, activity, and tasks lost their glass-card depth and glow. The opportunity flow is a row of flat rectangular cells showing counts. The dashboard no longer feels like a framed canvas with depth.
- **Desired State:** The dashboard feels like a framed canvas: app chrome (left nav, right Neuron rail, top bar) sits flush to viewport edges, the main content area is enclosed in a bordered inset container with rounded corners (sidebar-08 pattern), operational panels float with depth and glow inside that container, and the story canvas zone tells the opportunity narrative through a timeline bar with radial chart popovers at each stage.
- **Impact:** Restores the polished, purposeful feel of the original design while adding richer data visualization (composition via radials, not just counts).

## Scope & Boundaries

**In Scope:**
- Restore glass-card, float/raised depth, and soft hover/focus glow on nudge cards, Activity panel, and My Tasks panel.
- Replace flat rectangular opportunity flow cells with a vertical timeline (spine top-to-bottom, stops alternating left-right).
- Each timeline stop has a contextual mini-visualization — the chart type (donut, bar chart, gauge, progress ring, etc.) is chosen per-stage to best tell that stage's story.
- Mini-visualization size scales proportionally with the stage's item count.
- Chapter controls (Flow/Friction/Outcomes) override contextual defaults — switching chapters forces all stops to show the same dimension for uniform cross-stage comparison.
- Terminal outcome branches render at the timeline bottom (keeping the connected flow semantics from F0012).
- Per-stop alternate view toggles (S0003) allow exploring different dimensions (geography, broker breakdown, aging, etc.) at individual stops.
- Maintain period synchronization (30d / 90d / 180d / 365d) across KPIs, timeline, and radial data.
- KPI band remains as inline flat content within the story canvas zone (no card borders on KPIs).
- Maintain collapsible left nav and right Neuron rail with adaptive canvas width.
- Responsive behavior across desktop (1280+), tablet landscape (1024), tablet portrait (768), phone (375).
- Maintain all F0012 backend contract changes (periodDays on KPIs, avgDwellDays + emphasis on flow nodes).

**Out of Scope:**
- Workflow status taxonomy changes for submissions/renewals.
- AI recommendation logic changes inside Neuron rail.
- New external BrokerUser dashboard surfaces.
- Global navigation IA redesign beyond dashboard layout behavior.
- Adding new backend endpoints — this feature uses existing endpoints from F0010/F0012.
- Changing the chapter set (Flow/Friction/Outcomes remains the same).

## Acceptance Criteria Overview

- [ ] Nudge cards render with glass-card treatment, depth, and soft hover/focus glow.
- [ ] Activity panel renders as a raised/float panel with glass-card and glow.
- [ ] My Tasks panel renders as a raised/float panel with glass-card and glow.
- [ ] Opportunity flow renders as a vertical timeline (spine top-to-bottom, stops alternating left-right).
- [ ] Each timeline stop has a ghost-bordered story panel (on the same side as the stage node) containing a contextual mini-visualization on top and a narrative callout (2-3 data-driven bullets) stacked below, separated by a divider. Ghost border uses `--callout-border` token (blue in dark, salmon in light).
- [ ] Mini-visualization size scales proportionally with stage item count.
- [ ] Callout bullets are dynamically computed from stage data (not static copy).
- [ ] Chapter controls (Flow/Friction/Outcomes) override contextual defaults — all stops switch to the same dimension for comparison.
- [ ] Terminal outcome branches fan out from the spine's bottom point (root of the trunk), not from the last stage node.
- [ ] KPI band renders as inline flat content in the story canvas zone (no card borders).
- [ ] Period selector (30d/90d/180d/365d) synchronizes all data sources.
- [ ] Collapsible left nav and right Neuron rail work with adaptive canvas width.
- [ ] Dashboard feels like a framed canvas — app chrome is flush to viewport edges, main content sits in a bordered inset container (sidebar-08 pattern, 0.75rem gap, rounded corners), panels float inside, story zone narrates.

### Session 3 PM Verification (2026-03-17)

Verified against Session 2 evidence:
- `planning-mds/operations/evidence/f0013/qe-2026-03-17.md`
- `planning-mds/operations/evidence/f0013/security-2026-03-17.md`
- `planning-mds/operations/evidence/f0013/code-review-2026-03-17.md`

Verification result:
- Acceptance criteria are not yet satisfied for closeout.
- Blocking gaps remain in QE and Code Review gates (contrast, visual test determinism/portability, decomposition completion).
- Security gate is conditional pass with scanner/tooling follow-ups pending.

### Session 4 PM Verification (2026-03-19)

Verified against current repo state plus:
- `planning-mds/operations/evidence/f0013/pm-2026-03-19.md`
- commits `09d23be` and `7482207`

Verification result:
- Acceptance criteria are still not validated for final closeout.
- At review time, required signoff evidence had not been refreshed after the 2026-03-18 and 2026-03-19 implementation changes.
- At review time, the feature package remained incomplete for closeout (`S-DASH-002` missing; `GETTING-STARTED.md` stale against implementation).

### Session 5 Gate Rerun (2026-03-19)

Verified against refreshed gate evidence:
- `planning-mds/operations/evidence/f0013/qe-2026-03-19.md`
- `planning-mds/operations/evidence/f0013/code-review-2026-03-19.md`
- `planning-mds/operations/evidence/f0013/security-2026-03-19.md`
- `planning-mds/operations/evidence/f0013/devops-2026-03-19.md`

Verification result:
- Acceptance criteria are still not validated for final closeout.
- QE remains blocked / non-pass because backend regression execution hits `MSB3021` and the frontend toolchain is unstable in the current workspace.
- Code Review remains rejected on unresolved contrast, decomposition, palette-token, and proxy portability findings.
- Security remains conditional pass because scanner coverage and targeted authz rerun are incomplete.
- DevOps signoff is now evidenced, but it is a fail because clean frontend/backend build proof is still missing in this workspace.

### Session 6 Gate Remediation Verification (2026-03-19)

Verified against refreshed post-remediation gate evidence:
- `planning-mds/operations/evidence/f0013/qe-2026-03-19.md`
- `planning-mds/operations/evidence/f0013/code-review-2026-03-19.md`
- `planning-mds/operations/evidence/f0013/security-2026-03-19.md`
- `planning-mds/operations/evidence/f0013/devops-2026-03-19.md`

Verification result:
- The previously blocking technical signoff gates are now green for the current repo state.
- QE is PASS after targeted backend regression plus isolated frontend static/visual reruns.
- Code Review is APPROVED after the contrast, decomposition, semantic-token, and proxy/mock fixes.
- Security is PASS after targeted authz/scoping reruns plus user-confirmed human authz validation.
- DevOps is PASS after clean backend build proof and isolated frontend build/test proof were recorded.
- PM final closeout re-review was unblocked by these refreshed gates and completed later on 2026-03-19.

### Session 7 PM Final Closeout (2026-03-19)

Verified against:
- `planning-mds/operations/evidence/f0013/pm-2026-03-19.md`
- `planning-mds/operations/evidence/f0013/qe-2026-03-19.md`
- `planning-mds/operations/evidence/f0013/code-review-2026-03-19.md`
- `planning-mds/operations/evidence/f0013/security-2026-03-19.md`
- `planning-mds/operations/evidence/f0013/devops-2026-03-19.md`

Verification result:
- PRD acceptance criteria are satisfied for final closeout.
- Required story-level signoff provenance is complete and passing for every required role.
- Tracker/docs state is synchronized with the implemented feature and archived path.
- F0013 now lives at `planning-mds/features/archive/F0013-dashboard-framed-storytelling-canvas/`.

### Decision Gate Outcome (2026-03-17)

- Option A selected by user: mark F0013 Done and keep active folder.
- Done status is recorded as a product-owner override with known unresolved gate findings preserved in `STATUS.md`.

## UX / Screens

| Screen | Purpose | Key Actions |
|--------|---------|-------------|
| Dashboard — Framed Storytelling Canvas | Three-layer canvas: frame + operational panels + story timeline | Dismiss nudge, switch period, switch chapter, hover/click stage radial, open drilldown, scroll to activity/tasks |

**Formal screen specification:** `planning-mds/screens/S-DASH-002-framed-storytelling-canvas.md`

**Visual references:**
- Original handdrawn wireframe: `planning-mds/screens/handdrawn.jpeg`
- Current implementation (light): `planning-mds/screens/temp/MacBook Pro-1773533878403.jpeg`
- Current implementation (dark): `planning-mds/screens/temp/MacBook Pro-1773533934943.jpeg`

**Key Workflows:**
1. **Alert-to-Story Scan** — User sees nudge cards (raised, glowing) at top, scans KPI band, scrolls the vertical timeline reading each stop's contextual mini-visual.
2. **Stage Story Reading** — Each timeline stop tells its own story: "Who submitted?" at Received (bar chart), "Are we on track?" at Triaging (SLA gauge), "What mix?" at Quote Prep (donut). No interaction needed — the story is visible on the timeline itself.
3. **Per-Stop Exploration** — User toggles a stop's alternate view (e.g., geographic map at Received, wait time by broker at Waiting on Broker) to explore a different dimension at that specific stage.
4. **Uniform Comparison Mode** — User switches chapters (e.g., Friction); all stops switch to the same dimension (dwell time bands) for cross-stage comparison; per-stop toggles are hidden.
5. **Action Handoff** — User scrolls past the timeline to Activity and My Tasks (raised panels with depth) and executes follow-up work.

## ASCII Wireframe

### Desktop (Framed Canvas with Vertical Timeline)

```text
+-----+-------------------------------------------------------------------------------------------+-----+
|NAV  | TOP BAR: Dashboard                                             [theme] [notif] [profile] |  AI |
|rail |                                                                                           | rail|
+-----+-------------------------------------------------------------------------------------------+-----+
|     |                                                                                           |     |
|     | +--NUDGE CARDS (glass-card, depth, glow-on-hover)--------------------------------------+ |     |
|     | | [Overdue] Follow renewal Lost    [Overdue] Follow renewal Expired    [Overdue] Review  | |     |
|     | +--------------------------------------------------------------------------------------+ |     |
|     |                                                                                           |     |
|     | STORY CONTROLS (flat, part of canvas)                                                     |     |
|     | [30d] [90d] [180d] [365d]                                    [Flow] [Friction] [Outcomes]     |
|     |                                                                                           |     |
|     | KPI BAND (flat, embedded in canvas)                                                       |     |
|     |   Active Brokers: 173    Open Submissions: 6    Renewal Rate: 45.0%    Avg Turn: 24.9d   |     |
|     |                                                                                           |     |
|     | VERTICAL TIMELINE (story spine, stops alternate left-right)                                |     |
|     |                                                                                           |     |
|     |    +--[icon grid]-------+          |          • **Property** dominates at 42%    |     |
|     |    | 🏠🏠🏠🚗🚗⚓      |          |          • Marine up 18% vs prior           |     |
|     |    | 🏠🏠🏗️🏗️⚖️⚓      |     ● Received (60) • 6 lines of business          |     |
|     |    | Insurance Types    |          |                                                      |     |
|     |    +-------------------+          |                                                      |     |
|     |                                    |                                                      |     |
|     |  • 78% triaged within SLA    ● Triaging (45)                                              |     |
|     |  • 3 approaching deadline         |          +--[SLA gauge]--------+                     |     |
|     |  • Avg triage: 1.2 days           |          |  ◐ 78% on-time     |                     |     |
|     |                                    |          |  ◔ 15% approaching  |                     |     |
|     |                                    |          |  ◕  7% overdue      |                     |     |
|     |                                    |          +---------------------+                     |     |
|     |                                    |                                                      |     |
|     |    +--[donut]-----------+          |          • **Sarah** handles 34%             |     |
|     |    |    (( 102 ))       |     ● UW Review (102) • 8 waiting >7 days              |     |
|     |    |  UW-A ██  UW-B ██ |          |          • Renewal mix: 28%                  |     |
|     |    |  UW-C ██  Other   |          |                                                      |     |
|     |    +-------------------+          |                                                      |     |
|     |                                    |                                                      |     |
|     |  • Bind rate: 38%             ● Quoted (300)                                               |     |
|     |  • Avg days to bind: 12.4         |          +--[stacked bar]------+                     |     |
|     |  • 72 bound this period           |          | Subs ████████ (210) |                     |     |
|     |                                    |          | Renew ████   (90)   |                     |     |
|     |                                    |          +---------------------+                     |     |
|     |                                    |                                                      |     |
|     |    • 8 awaiting final bind    ● Bind Requested (8)                                        |     |
|     |    • Oldest: 3 days                |                                                      |     |
|     |    • All within SLA                |                                                      |     |
|     |                                   / \                                                     |     |
|     |                                  /   \                                                    |     |
|     |                           ● Bound    ● Declined   ● Expired   ● Lost                     |     |
|     |                             (72)       (29)         (36)        (117)                     |     |
|     |                                                                                           |     |
|     | +--ACTIVITY (glass-card, raised, glow-on-hover)----------------------------------------+ |     |
|     | | Activity                                                                   TIMELINE  | |     |
|     | | * Broker Status Changed — Ironwood Wholesale 112 marked Inactive — 2w ago            | |     |
|     | +--------------------------------------------------------------------------------------+ |     |
|     |                                                                                           |     |
|     | +--MY TASKS (glass-card, raised, glow-on-hover)----------------------------------------+ |     |
|     | | My Tasks                                                                              | |     |
|     | | Follow renewal Lost          Mar 5   Open                                             | |     |
|     | +--------------------------------------------------------------------------------------+ |     |
|     |                                                                                           |     |
+-----+-------------------------------------------------------------------------------------------+-----+
```

### Contextual Mini-Visualization Per Stage (Default — No Chapter Override)

Each stop has a chart type chosen to answer the most meaningful question for that stage:

```text
  [icon grid]         ● Received         • **Property** dominates at 42%
  🏠🏠🏠🚗🚗⚓            |              • Marine up 18% vs prior
  🏠🏠🏗️🏗️⚖️⚓            |              • 6 lines of business
                           |
                      ● Triaging         [SLA gauge/arc]
  • 78% within SLA         |              On-time / approaching
  • 3 approaching          |              / overdue
  • Avg: 1.2 days          |
                           |
  [progress ring]     ● Waiting on Bkr   • Avg wait: 4.3 days
  Avg Wait                 |              • Ironwood: 3 items >7d
                           |              • 5 nearing SLA breach
                           |
                      ● UW Review        [donut chart]
  • Sarah handles 34%      |              UW workload
  • 8 waiting >7d          |
  • Renewal mix: 28%       |
                           |
  [stacked bar]       ● Quote Prep       • 210 subs, 90 renewals
  Subs vs Renewals         |              • Renewals up 22%
                           |              • 3 programs = 60%
                           |
                      ● Quoted           [donut chart]
  • Bind rate: 38%         |              Conversion rate
  • Avg to bind: 12.4d     |
  • 72 bound this period   |
                           |
                      ● Bind Requested   • 8 awaiting final bind
                           |              • Oldest: 3 days
                          / \             • All within SLA
                         /   \
                  ● Bound    ● Declined   ● Expired   ● Lost
                    (72)       (29)         (36)        (117)
```

### Chapter Override Mode (All Stops Switch to Same Dimension)

When a chapter is active, ALL stops switch from their contextual defaults to a uniform visualization for cross-stage comparison:

```text
Chapter: Friction (all stops show dwell time — per-stop toggles hidden)
     ● Received    [donut: <1d ██ | 1-3d ██ | 3-7d ██ | 7d+ ██]
     ● Triaging    [donut: <1d ██ | 1-3d ██ | 3-7d ██ | 7d+ ██]   ← emphasis ring: bottleneck
     ● UW Review   [donut: <1d ██ | 1-3d ██ | 3-7d ██ | 7d+ ██]   ← emphasis ring: blocked
     ● Quoted      [donut: <1d ██ | 1-3d ██ | 3-7d ██ | 7d+ ██]
     ...
```

## Stage-to-Visualization Mapping (Default Context)

Each stage has a **primary** mini-visualization (shown by default) and optional **alternates** the user can toggle per-stop. The primary answers the most important question for that stage; alternates offer different angles. A small dot/chevron indicator on the mini-visual lets the user cycle through available views.

| Stage | Primary (default) | Alternate 1 | Alternate 2 |
|-------|-------------------|-------------|-------------|
| Received | Icon grid / waffle chart — "What kind of work?" (insurance type icons: property, casualty, marine, etc.) | Mini bar chart (top brokers by volume) | Geographic map (submission origins) |
| Triaging | SLA gauge/arc — "Are we triaging on time?" | Geographic map (regional SLA variance) | — |
| Waiting on Broker | Progress ring — "How long are we waiting?" | Bar chart (wait time by broker) | — |
| UW Review | Donut — "Who is reviewing?" (UW workload) | Bar chart (by line of business) | — |
| Quote Preparation | Stacked bar — "What kind of work?" (subs vs renewals) | Donut (by program) | — |
| Quoted | Donut — "Conversion potential?" (quote-to-bind rate) | Bar chart (quote age distribution) | — |
| Bind Requested | Count badge — "Almost there" | — | — |
| Terminal outcomes | Count + percentage labels | — | — |

**Toggle behavior:** Per-stop, not global. Chapter override (S0004) hides per-stop toggles and shows uniform views. Returning to Flow chapter restores the last-selected alternate.

## Chapter Override Mapping

When a chapter is active, contextual defaults are replaced with a uniform dimension across ALL stops. Per-stop alternate toggles (S0003) are hidden during chapter override.

| Chapter | All Stops Show | Chart Type (uniform) | Overlay Behavior |
|---------|---------------|---------------------|-----------------|
| **Flow** (default) | Contextual per-stage defaults + per-stop alternate toggles | Mixed (per table above) | No override — each stop tells its own story; user can toggle alternates per-stop |
| **Friction** | Dwell time bands (<1d, 1-3d, 3-7d, 7d+) | Donut (uniform) | Emphasis rings on nodes (bottleneck/blocked/active); per-stop toggles hidden |
| **Outcomes** | N/A (highlights terminal branches) | N/A | Terminal branch paths glow; stage mini-visuals dim; per-stop toggles hidden |

> **Why only 3 chapters?** Aging and mix dimensions (age buckets, broker/program breakdown) are available as per-stop alternate views (S0003). Chapters are reserved for cross-stage comparison modes that require timeline-level overlays (emphasis rings, terminal branch highlighting) — things per-stop toggles can't do.

## Data Requirements

**Core Entities:**
- Nudge/action items (overdue tasks, items requiring attention) — existing endpoint
- KPI aggregates (active brokers, open submissions, renewal rate, avg turnaround) — existing endpoint with `periodDays` parameter
- Opportunities stage aggregates with sequence metadata and connection strengths — existing endpoint
- Flow node friction data (avgDwellDays, emphasis) — added in F0012 backend
- Terminal outcome aggregates (outcome_type, count, percent_of_exits, avg_days_to_exit) — existing endpoint
- Aging bucket matrix — existing endpoint
- Hierarchy/mix data — existing endpoint
- Timeline activity feed and user task queue — existing endpoints

**New Frontend Data Needs (no new endpoints):**
- Radial popover data is derived from existing stage aggregates by cross-referencing with entity-type counts, aging buckets, or hierarchy data depending on active chapter.
- The radial composition is a frontend computation layering chapter-specific data onto stage-level aggregates.

**Validation Rules:**
- Counts and rates are non-negative and scoped by role policy.
- Stage and outcome ordering remains deterministic (displayOrder).
- Chapter switches do not change period scope.
- Radial popover segments must sum to the stage's total count.
- Collapsed rail state must not hide actionable controls.

## Role-Based Access

| Role | Access Level | Notes |
|------|-------------|-------|
| DistributionUser | Read | ABAC scoped dashboard visibility |
| DistributionManager | Read | ABAC scoped dashboard visibility |
| Underwriter | Read | ABAC scoped dashboard visibility |
| RelationshipManager | Read | ABAC scoped dashboard visibility |
| ProgramManager | Read | ABAC scoped dashboard visibility |
| Admin | Read | Internal unscoped |
| BrokerUser | Denied | Dashboard remains InternalOnly |

## Success Criteria

- Dashboard feels like a framed canvas — app chrome encloses it, operational panels have depth and glow, story zone tells the narrative.
- Users hover timeline stage nodes and see radial chart popovers showing composition, not just counts.
- Switching chapters changes all mini-visualizations uniformly — users can diagnose friction or focus on outcomes from the same timeline bar.
- Per-stop alternate toggles let users explore aging, mix, geography, and other dimensions at individual stops.
- Nudge cards, Activity, and My Tasks panels have the glass-card depth and soft hover glow from the design system.
- No regression in period synchronization, rail adaptation, or responsive behavior.

## Risks & Assumptions

- **Risk:** Radial chart popovers may feel crowded on stages with very low counts (0 or 1). **Mitigation:** Popovers degrade to a simple count tooltip when a stage has fewer than 2 items.
- **Risk:** Multiple interaction modes (per-stop toggles + chapter override) could confuse users. **Mitigation:** Chapter override hides per-stop toggles entirely, making modes mutually exclusive. Clear chapter label in the controls zone.
- **Assumption:** Existing backend endpoints provide sufficient data granularity to compute radial segments per chapter without new endpoints.
- **Assumption:** Radial/donut chart rendering can use a lightweight SVG approach without adding a heavy charting library.

## Dependencies

- F0012 backend changes (periodDays on KPIs, avgDwellDays + emphasis on flow nodes) — **Done (archived)**
- F0010 opportunities refactor baseline (Pipeline Board + aging/hierarchy endpoints) — **Done**
- Design tokens in `planning-mds/screens/design-tokens.md` — glass-card, glow utilities, editorial accent palette (coral/steel-blue)
- Existing ABAC policy coverage for `dashboard_kpi`, `dashboard_pipeline`, `dashboard_nudge`

## Related Stories

- F0013-S0000 — Editorial palette refresh — dark & light themes (foundational — blocks all other stories)
- F0013-S0001 — Restore framed canvas identity with three-layer visual hierarchy
- F0013-S0002 — Build vertical timeline with connected stage nodes and terminal outcome branches
- F0013-S0003 — Add contextual mini-visualizations at each timeline stage node
- F0013-S0004 — Connect chapter controls as uniform override for timeline visualizations
- F0013-S0005 — Ensure responsive, accessibility, and performance parity for framed storytelling canvas

## Rollout & Enablement

- Capture before/after screenshots showing the three-layer visual hierarchy (framed canvas with floating operational panels and timeline flow).
- Include review checklist for:
  - nudge cards have glass-card depth and glow
  - Activity and Tasks panels have glass-card depth and glow
  - timeline bar renders with connected stage nodes
  - radial popovers appear on stage hover/click
  - chapter switching changes radial segments
  - app chrome feels like a frame enclosing the canvas
