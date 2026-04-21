# F0013-S0003: Add Contextual Mini-Visualizations at Each Timeline Stage Node

**Story ID:** F0013-S0003
**Feature:** F0013 — Dashboard Framed Storytelling Canvas
**Title:** Add contextual mini-visualizations at each timeline stage node
**Priority:** High
**Phase:** MVP

## User Story

**As a** dashboard user
**I want** each timeline stop to have a story panel containing a mini-visualization and a narrative callout with 2-3 data-driven bullet points stacked together in a single ghost-bordered card — so that I can read the entire submission journey by scrolling the timeline, with each stop telling its story both visually (chart) and textually (callout)
**So that** I understand the full operational narrative at a glance like a magazine data spread, and can explore alternate chart views per-stop when I want a different angle

## Context & Background

The pipeline inspiration images (`pipeline2.png`, `pipeline3.png`, `pipeline5.png`) show timeline infographics where each stop has a DIFFERENT type of mini-chart — one stop might be a donut, another a bar chart, another an area chart. The handdrawn timeline sketch (`planning-mds/screens/handdrawn-timeline.jpeg`) shows radial/donut charts at each stop with counts in the center. Combining both ideas: each stop gets the chart type that best answers the most meaningful question for that stage.

The chart types are NOT forced to be uniform. A donut at one stop, a bar chart at another, a gauge at another — whatever best fits the data story. This is what makes the timeline an infographic, not a dashboard widget grid.

Critically, each stop also has a **narrative callout** stacked directly below the mini-visual within the same ghost-bordered card, separated by a subtle divider line. This keeps the visual and its textual explanation tightly grouped — preventing the confusion of opposite-side placement where callouts visually blend with adjacent stages. These callouts are 2-3 data-driven bullet points that tell the stage's story in words — complementing the visual with specific facts, trends, and highlights. The ghost border uses the `--callout-border` CSS token (blue in dark mode, salmon in light mode at 70% opacity).

## Acceptance Criteria

**Happy Path:**
- **Given** the vertical timeline is rendered with stage nodes (S0002)
- **When** the user views the timeline
- **Then** each stage node displays a mini-visualization inline (not on hover — always visible)
- **And** the visualization type is contextually chosen per stage (see stage mapping below)
- **And** the visualization size scales with the stage's item count
- **And** each visualization shows a count (the stage's `currentCount`) prominently
- **And** each visualization includes a brief label describing what it shows (e.g., "Top Brokers", "SLA Health")
- **And** directly below the mini-visual (within the same ghost-bordered card, separated by a divider) a narrative callout shows 2-3 data-driven bullet points
- **And** the callout bullets are dynamically generated from the same data that powers the mini-visual
- **And** each callout highlights the most compelling insight for that stage (top contributor, trend, outlier, or threshold breach)

**Stage-to-Visualization Mapping (Default Context):**

| Stage | Chart Type | Question | Segments / Data |
|-------|-----------|----------|-----------------|
| Received | Icon grid / waffle chart | "What kind of work is coming in?" | Insurance type icons (property, casualty, marine, auto, professional liability, etc.) — each icon represents a submission, color-coded by line of business |
| Triaging | SLA gauge/arc | "Are we triaging on time?" | On-time / approaching / overdue bands |
| Waiting on Broker | Progress ring | "How long are we waiting?" | Avg wait days as arc against SLA threshold |
| UW Review | Donut chart | "Who is reviewing?" | Underwriter workload distribution |
| Quote Preparation | Stacked bar | "What kind of work?" | Submissions vs renewals |
| Quoted | Donut chart | "Conversion potential?" | Historical quote-to-bind rate |
| Bind Requested | Count badge | "Almost there" | Simple prominent count (low volume stage) |

**Narrative Callout Mapping (stacked below mini-visual in same ghost-bordered card):**

Each callout has 2-3 short, data-driven bullet points. The content adapts dynamically based on what's noteworthy in the data — these are templates, not static copy.

| Stage | Callout Bullets (examples) | Data Source |
|-------|---------------------------|-------------|
| Received | "**Property** dominates at 42%" / "Marine up 18% vs prior period" / "6 lines of business represented" | Line of business distribution, period-over-period delta, LOB count |
| Triaging | "78% triaged within SLA" / "3 submissions approaching deadline" / "Avg triage time: 1.2 days" | SLA band counts, approaching count, avgDwellDays |
| Waiting on Broker | "Avg wait: 4.3 days" / "**Ironwood** has 3 items waiting >7d" / "5 items nearing SLA breach" | avgDwellDays, longest-waiting broker, SLA threshold proximity |
| UW Review | "**Sarah** handles 34% of reviews" / "8 submissions waiting >7 days" / "Renewal mix: 28%" | Top UW name + share, aging outliers, entity type ratio |
| Quote Preparation | "210 submissions, 90 renewals" / "Renewals up 22% vs prior" / "3 programs account for 60%" | Entity type counts, period delta, top programs |
| Quoted | "Historical bind rate: 38%" / "Avg days to bind: 12.4" / "72 bound this period" | Conversion rate, avg days to terminal, bound count |
| Bind Requested | "8 awaiting final bind" / "Oldest: 3 days" / "All within SLA" | Count, max age, SLA status |

**Callout behavior:**
- Bullets are computed from the same data that drives the mini-visual — no separate API calls
- When the user toggles to an alternate mini-visual, the callout bullets update to match the alternate's data dimension
- When a chapter override is active (S0004), callout bullets update to reflect the chapter's uniform dimension (e.g., Friction chapter → all callouts show dwell time insights)
- Stage with count = 0 → Callout shows "No items in this stage" with muted styling
- Callout text uses `text-secondary` / `muted-foreground` styling — supportive, not dominant
- Key values within callout bullets are **bold** or use `text-primary` for emphasis (broker names, percentages, counts)

**Alternate Mini-Visualization Views:**

Each stage has a primary mini-visualization (shown by default) and zero or more alternate views the user can flip through. A small dot indicator or chevron toggle on the mini-visual lets the user cycle views per-stop. This is a per-stop toggle, not a global control (chapters handle global override via S0004).

| Stage | Primary | Alternate 1 | Alternate 2 |
|-------|---------|-------------|-------------|
| Received | Icon grid / waffle chart (insurance type icons) | Mini bar chart (top brokers by volume) | Geographic map (submission origins) |
| Triaging | SLA gauge/arc (on-time health) | Geographic map (regional SLA variance) | — |
| Waiting on Broker | Progress ring (avg wait) | Bar chart (wait time by broker) | — |
| UW Review | Donut (UW workload) | Bar chart (by line of business) | — |
| Quote Preparation | Stacked bar (subs vs renewals) | Donut (by program) | — |
| Quoted | Donut (conversion rate) | Bar chart (quote age distribution) | — |
| Bind Requested | Count badge | — | — |

- **Given** a stage has alternate views configured
- **When** the user clicks the toggle indicator on that stop's mini-visual
- **Then** the visualization crossfades (150ms) to the next alternate view
- **And** the dot indicator updates to show the active view
- **And** the count and label update to reflect the new view's data
- **And** cycling past the last alternate wraps back to the primary

- **Given** a stage has no alternates (e.g., Bind Requested)
- **When** the mini-visual renders
- **Then** no toggle indicator is shown

- **Given** a chapter override is active (S0004)
- **When** the user views a stage
- **Then** the per-stop toggle is hidden — chapter uniform view takes precedence
- **And** switching back to Flow chapter restores the last-selected per-stop view

**Alternative Flows / Edge Cases:**
- Stage with count = 0 → Mini-visualization renders as an empty state (hollow ring/empty bar) with "0" count and muted styling; callout shows "No items in this stage"
- Stage with count = 1 → Visualization still renders (single-segment donut, single-bar chart, etc.)
- Stage with count < 3 → Complex chart types (bar chart, donut with segments) degrade to a simple count badge (not enough data for meaningful segments)
- Data for a specific chart type is unavailable (e.g., no broker data for Received) → Falls back to entity type donut (submissions vs renewals)
- Workflow has stages not in the mapping table → Default to entity type donut
- Alternate view data unavailable (e.g., no geo data for geographic map) → That alternate is skipped in the cycle; toggle only shows views with available data
- User toggles to alternate, then switches chapter (S0004), then switches back to Flow → Alternate selection is preserved per-stop (React state)
- Geographic map at small mini-visual size → Renders as a simplified regional dot map (not full-detail map), or degrades to a bar chart by region if viewport is too small

**Checklist:**
- [ ] Each timeline stop displays a mini-visualization inline (always visible, not hover-triggered)
- [ ] Chart type varies per stage (not uniform across all stops)
- [ ] Each visualization shows the stage's `currentCount` prominently
- [ ] Each visualization includes a descriptive label
- [ ] Visualization size scales proportionally with item count
- [ ] Visualizations render within the alternating left-right layout of the timeline (S0002)
- [ ] SVG rendering for all chart types (consistent with timeline)
- [ ] Accent color palette (violet/fuchsia/purple/pink/orange) used for segments
- [ ] Graceful degradation for low-count stages (count badge fallback)
- [ ] Fallback to entity type donut when stage-specific data is unavailable
- [ ] Each visualization is self-contained (readable without interaction)
- [ ] Narrative callout renders below the mini-visual within the same ghost-bordered card, separated by a divider
- [ ] Each callout has 2-3 data-driven bullet points
- [ ] Callout bullets update when mini-visual alternate is toggled
- [ ] Callout bullets update when chapter override is active (S0004)
- [ ] Key values in callouts are bold / emphasized
- [ ] Empty stage callout shows "No items in this stage" with muted styling
- [ ] Stages with alternates show a dot/chevron toggle indicator
- [ ] Toggle cycles through primary → alternate(s) → back to primary with 150ms crossfade
- [ ] Stages without alternates show no toggle
- [ ] Chapter override (S0004) hides per-stop toggles
- [ ] Returning to Flow chapter restores last-selected per-stop view
- [ ] Alternates with unavailable data are skipped in the cycle
- [ ] Geographic map renders as simplified dot map at mini-visual scale

## Data Requirements

**Required Fields (from existing endpoints):**
- `OpportunityFlowNode`: status, label, currentCount, inflowCount, outflowCount, avgDwellDays, emphasis
- Entity type breakdown per stage: derived from existing opportunities data
- Broker distribution per stage: derived from existing opportunities data (may require additional grouping)
- Underwriter workload: derived from existing assignment data

**Data Derivation (frontend computation — no new endpoints):**
- **Insurance type distribution** (Received): group items in `Received` status by line of business (property, casualty, marine, auto, professional liability, etc.) — each icon in the grid represents one submission
- **Top brokers** (Received alternate): group items in `Received` status by broker, take top 3-5
- **SLA bands** (Triaging): compute from `avgDwellDays` and item ages against SLA thresholds
- **Wait time** (Waiting on Broker): `avgDwellDays` from flow node data
- **UW distribution** (UW Review): group items by assigned underwriter
- **Entity type** (Quote Prep): count submissions vs renewals in that status
- **Conversion rate** (Quoted): historical bound/total ratio from terminal outcomes data
- **Geographic distribution** (Received, Triaging alternates): group items by broker state/region — requires broker address data from existing broker endpoint
- **Wait by broker** (Waiting on Broker alternate): group dwell days by broker
- **Line of business** (UW Review alternate): group items by LOB/program type
- **Program breakdown** (Quote Prep alternate): group items by program
- **Quote age** (Quoted alternate): bucket quote ages (0-7d, 7-14d, 14-30d, 30d+)

**Validation Rules:**
- Chart segments must sum to the stage's currentCount (where applicable)
- Zero-value segments are hidden (not rendered as zero-width slices)
- Mini-visualizations must be legible at their minimum size (smallest count stage)

## Role-Based Visibility

All dashboard roles see the same mini-visualizations. Data is ABAC-scoped at the endpoint level.

## Non-Functional Expectations

- Performance: All mini-visualizations render within 200ms of timeline render (data is pre-fetched with flow data, not per-visualization). Total timeline + all visuals < 400ms.
- Accessibility: Each mini-visualization has `aria-label` describing the chart type, stage, count, and key takeaway. Narrative callout bullets are standard HTML text — screen readers read them naturally. Together, the mini-visual `aria-label` + callout text give screen reader users the complete stage story.

## Dependencies

**Depends On:**
- F0013-S0002 — Vertical timeline with stage nodes must exist for mini-visuals to attach to

**Related Stories:**
- F0013-S0004 — Chapter controls override these contextual defaults with uniform dimensions
- F0013-S0005 — Responsive behavior for mini-visualizations on mobile

## Out of Scope

- Chapter-driven uniform override (that's S0004 — this story implements the contextual defaults only)
- Drilldown navigation from mini-visualization to a detail view
- Hover/click popovers with additional detail beyond the inline mini-visual (may be added as enhancement)
- User-configurable alternate view ordering or custom views

## UI/UX Notes

- Mini-visualizations are INLINE on the timeline — always visible, not hover-triggered. This is key: the timeline tells its story just by scrolling, no interaction required.
- Each visualization sits in a ghost-bordered story panel on the same side as its stage node (alternating left-right per S0002). The narrative callout is stacked below the mini-visual within the same card, not on the opposite side.
- Compact size: roughly 100-140px wide, height proportional to count. Readable at a glance.
- Accent colors for segments: use the theme-independent accent palette from design tokens.
- The combination of different chart types at each stop is what makes this an infographic, not a dashboard. Like a magazine data spread where each section has its own visualization style.

## Questions & Assumptions

**Open Questions:**
- [ ] What SLA thresholds define on-time / approaching / overdue for the Triaging stage? (Assumption: configurable, default 1d/3d/7d)
- [ ] Should the "Top Brokers" bar chart at Received show broker names or anonymized labels? (Assumption: show broker short names)

**Assumptions:**
- All data for contextual mini-visualizations is derivable from existing endpoints without new backend work
- SVG mini-charts are lightweight enough that rendering 7-10 simultaneously is performant
- The stage-to-visualization mapping (primary + alternates) is hardcoded for MVP (not user-configurable)
- Geographic map data is derivable from broker address fields without a dedicated geo endpoint
- Per-stop alternate selection is React state only — not persisted across sessions

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Each timeline stop has a primary contextual mini-visualization (chart type varies per stage)
- [ ] Stages with alternates have a toggle indicator and cycle through views
- [ ] Narrative callouts render below mini-visual in same ghost-bordered card with 2-3 data-driven bullets per stop
- [ ] Visualizations and callouts are inline (always visible, not hover-triggered)
- [ ] Graceful degradation for low-count and missing-data scenarios
- [ ] Accessible (aria-labels on all visualizations)
- [ ] Tests pass
- [ ] Story filename matches Story ID prefix
- [ ] Story index regenerated
