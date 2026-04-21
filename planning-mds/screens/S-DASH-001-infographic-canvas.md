# Screen Specification: Dashboard Infographic Canvas

**Screen ID:** S-DASH-001
**Name:** Dashboard Infographic Canvas
**Type:** Dashboard
**Route/URL:** `/dashboard`
**Feature:** F0012 — Dashboard Storytelling Infographic Canvas

## Purpose & Context

- **Primary Users:** DistributionUser, DistributionManager, Underwriter, RelationshipManager, ProgramManager, Admin
- **Goal:** Present the complete operational story — alerts, metrics, opportunity flow, analytical overlays, activity, and tasks — as one continuous flat infographic canvas that users read top-to-bottom without encountering panel borders or mode switches

## Design Philosophy

> **Infographic, not dashboard.** One continuous flat canvas. No panel borders, card wrappers, elevated surfaces, or divider lines. Content zones are differentiated by spacing, typography weight/size, and color emphasis. The entire page reads like an editorial infographic poster.

## Content Zones (Top to Bottom)

The canvas flows vertically through these zones. Each zone uses spacing (not borders) to separate from adjacent zones.

### Zone 1: Nudge Bar
- **Position:** Top of infographic canvas, below top bar
- **Content:** Overdue/attention action items (e.g., "Follow renewal Lost — 6 days overdue")
- **Behavior:** Dismissible items; zone collapses when empty; no border or separator below
- **Visual:** Compact badges with status color (overdue = warm amber/red)
- **Flow:** Seamlessly merges into story controls below — no divider line

### Zone 2: Story Controls
- **Position:** Below nudge bar (no separator)
- **Content:** Period selector (`30d`, `90d`, `180d`, `365d`) + Chapter selector (`Flow`, `Friction`, `Outcomes`, `Aging`, `Mix`)
- **Behavior:** Period changes update all canvas data; chapter changes update overlays in place
- **Visual:** Inline button group or pill selector; selected state visually clear; keyboard navigable
- **Default:** Period = `180d`, Chapter = `Flow`

### Zone 3: KPI Band
- **Position:** Below story controls
- **Content:** Active Brokers, Open Submissions, Renewal Rate, Avg Turnaround
- **Behavior:** Values update with period selection; inline with canvas (no card wrappers)
- **Visual:** Large numeric typography with muted labels; horizontal layout on desktop, 2x2 grid on tablet, stacked on phone

### Zone 4: Connected Opportunity Flow
- **Position:** Primary narrative zone
- **Content:**
  - Connected left-to-right stage flow (Received → Triaging → UW Review → Quote Prep → Quoted → Bind Req → Bound)
  - Ribbon-style connections between stages (thickness = volume)
  - Terminal outcome branches with semantic path styles:
    - Solid = positive/successful conversions
    - Red dashed = negative outcomes (Declined, Lost)
    - Gray dotted = passive/time-based (Expired)
  - Terminal outcome nodes: count, % of exits, avg days to exit
- **Behavior:** Click stage/outcome node for drilldown; chapter overlays layer on top
- **Visual:** Warm-to-cool color progression across stages (left to right); selective glow for active/blocked stages

### Zone 5: Chapter Overlays (In-Canvas)
- **Position:** Layered on top of Zone 4 (same canvas area)
- **Content varies by chapter:**
  - **Flow** — no overlay (baseline view)
  - **Friction** — annotation badges on stages with highest dwell time / count concentration
  - **Outcomes** — terminal outcome rail emphasis with branch path highlighting
  - **Aging** — heat intensity blocks overlaid on stage nodes (5 buckets: 0-2, 3-5, 6-10, 11-20, 21+ days)
  - **Mix** — composition blocks (treemap-style) and radial mini inset as compact overlay elements
- **Behavior:** Chapter switch is instant (<250ms); stale overlay doesn't flash; compact mode at narrow widths

### Zone 6: Activity
- **Position:** Below story content, separated by vertical spacing (no border)
- **Content:** Timeline list of recent activity events (type, actor, description, timestamp)
- **Behavior:** Scroll within section; click event for details
- **Visual:** Section header ("Activity") + muted timeline with colored event dots; flat background (no card elevation)

### Zone 7: My Tasks
- **Position:** Below Activity, separated by vertical spacing (no border)
- **Content:** Task list with title, due date, status badge, linked entity
- **Behavior:** Click task to navigate to linked workflow item; sort/filter available
- **Visual:** Section header ("My Tasks") + flat list rows; overdue tasks highlighted with warm color; flat background (no card elevation)

## Primary Data

- Nudge items: title, overdue_days, linked_entity, dismiss_action
- KPI aggregates: activeBrokers, openSubmissions, renewalRate, avgTurnaroundDays
- Stage sequence: ordered stages with label, count, connection_strength
- Terminal outcomes: outcome_type, count, percent_of_exits, avg_days_to_exit, branch_style
- Chapter overlay data: aging buckets, hierarchy composition, friction emphasis hints
- Activity events: event_type, timestamp, actor_name, description
- Tasks: title, due_date, status, linked_entity_type, linked_entity_id

## User Actions

| Action | UI Element | Result | Permission |
|--------|------------|--------|------------|
| Dismiss nudge | Nudge item close button | Removes item from nudge zone; zone collapses if empty | Scoped to user's items |
| Switch period | Period selector buttons | Updates KPI + flow + outcomes + overlays in sync | Read (all internal roles) |
| Switch chapter | Chapter selector buttons | Updates overlay layer in-canvas; base flow persists | Read (all internal roles) |
| Stage drilldown | Click stage node | Opens drilldown popover/overlay with scoped details | Read (ABAC scoped) |
| Outcome drilldown | Click terminal outcome node | Opens outcome detail with mini-cards | Read (ABAC scoped) |
| View activity detail | Click activity event | Expands event detail or navigates to linked entity | Read (role scoped) |
| Open task | Click task row | Navigates to linked workflow item | Read (role scoped) |
| Collapse left nav | Rail toggle | Canvas width expands; nav collapses to icon rail | Layout control (all) |
| Collapse right Neuron rail | Rail toggle | Canvas width expands; AI rail collapses | Layout control (all) |

## Navigation & Flow

**Entry Points:**
- Direct URL: `/dashboard`
- Left nav: Dashboard icon (primary)
- Post-login default landing page

**Exit Points:**
- Click task → linked workflow item (Submission, Renewal, Broker detail)
- Click activity event → linked entity detail
- Click stage drilldown → filtered submission/renewal list
- Left nav → Brokers page or other navigation items

## Responsive Layout

| Breakpoint | Layout Behavior |
|------------|----------------|
| **Desktop** (1280px+) | Full-width canvas between collapsible left/right rails; horizontal stage flow; KPI band horizontal |
| **Tablet landscape** (1024px) | Rails auto-collapsed or narrow; stage flow horizontal with compact labels; KPI 2x2 grid |
| **Tablet portrait** (768px) | No side rails; stage flow may wrap or use compact horizontal scroll; KPI 2x2 grid |
| **Phone** (375px) | Stacked layout; stage flow as vertical cards with bottleneck/outcome list; KPI stacked; chapters in horizontal scroll pills |

## States & Empty Cases

| State | Behavior |
|-------|----------|
| **Loading** | Skeleton placeholders per zone; zones render independently as data arrives |
| **Empty nudges** | Nudge zone collapses; story controls shift up; no gap |
| **Empty opportunities** | Canvas shows "No opportunities data for selected period" with controls active |
| **Empty activity** | Section header + "No recent activity" message; spacing preserved |
| **Empty tasks** | Section header + "No tasks assigned" message; spacing preserved |
| **Partial failure** | Failed zone shows inline recoverable error; other zones unaffected |
| **Full error** | Dashboard shell renders; canvas shows recoverable error message |

## Validation & Errors

- All numeric values non-negative
- Period filter value must be one of supported values (30, 90, 180, 365)
- Chapter key must be one of supported keys (flow, friction, outcomes, aging, mix); fallback to flow
- Drilldown data scoped by ABAC before rendering
- Loading, empty, and error states must be visually and semantically distinct (not just color)

## Accessibility Notes

- **Keyboard navigation:** Tab order follows visual flow (nudge zone → period selector → chapter selector → KPI band → stage nodes → outcome nodes → activity → tasks)
- **Screen reader:** Stage/outcome nodes include accessible names with count context (e.g., "Triaging stage, 4 submissions")
- **Reduced motion:** `prefers-reduced-motion` suppresses flow animations and glow effects; interaction clarity preserved
- **Focus management:** Focus remains visible and restorable after rail toggle or chapter switch
- **Color contrast:** All text meets WCAG AA against dark/light theme backgrounds per design-tokens.md

## Visual References

| Reference | Path | Purpose |
|-----------|------|---------|
| Current baseline (F0010) — MacBook | `planning-mds/screens/temp/MacBook Pro-*.jpeg` (4 files) | Before state: panelized dashboard with bordered cards |
| Current baseline (F0010) — iPad | `planning-mds/screens/temp/iPad-*.jpeg` (4 files) | Before state: tablet responsive layout |
| Infographic inspiration | `planning-mds/screens/pipeline1-5.png` | Timeline infographic layouts with chapter nodes and embedded data |
| Design tokens | `planning-mds/screens/design-tokens.md` | Color, spacing, typography, glass/glow specifications |
