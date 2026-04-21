# Screen Specification: Dashboard Framed Storytelling Canvas

**Screen ID:** S-DASH-002
**Name:** Dashboard Framed Storytelling Canvas
**Type:** Dashboard
**Route/URL:** `/`
**Feature:** F0013 — Dashboard Framed Storytelling Canvas

## Purpose & Context

- **Primary Users:** DistributionUser, DistributionManager, Underwriter, RelationshipManager, ProgramManager, Admin
- **Goal:** restore the framed dashboard identity while keeping the opportunity story readable as a vertical editorial timeline with inline stage narratives and on-demand drilldown details

## Design Philosophy

> **Framed canvas, not flat poster.** App chrome creates the frame. The content sits inside a bordered inset shell. Operational panels float. The story canvas stays flat and editorial.

The implemented visual stack is:

1. **Frame:** left nav, top bar, right rail
2. **Inset shell:** bordered / rounded main content container
3. **Operational panels:** elevated nudges, activity, tasks
4. **Story canvas:** flat controls, KPIs, timeline, and terminal branches

## Content Zones (Top to Bottom)

### Zone 0: Framed Shell
- **Position:** Between the left nav and right rail on desktop; fills the protected route body
- **Visual:** `--shell-inset-bg`, `1px` border, `0.75rem` radius, inset shadow, `0.75rem` outer gap on desktop
- **Behavior:** Width adapts to rail collapse state

### Zone 1: Intro Line
- **Position:** Top of dashboard content
- **Content:** "Your opportunities at a glance"
- **Visual:** Small muted lead-in copy before the panels and story controls

### Zone 2: Nudge Cards
- **Position:** Above the story canvas
- **Content:** Overdue or high-urgency nudges
- **Visual:** Elevated glass cards with hover/focus glow
- **Behavior:** Collapses naturally when empty

### Zone 3: Story Controls
- **Position:** Top of the flat story canvas
- **Content:**
  - period selector (`30d`, `90d`, `180d`, `365d`)
  - chapter selector (`Flow`, `Friction`, `Outcomes`)
- **Behavior:**
  - period updates KPIs, dashboard opportunities, flow, outcomes, and aging in sync
  - chapter changes the interpretation of stage panels and branch emphasis
- **Responsive:**
  - phone uses a `<select>` for period
  - chapter pills remain horizontally scrollable

### Zone 4: KPI Band
- **Position:** Immediately below story controls
- **Content:** Active Brokers, Open Submissions, Renewal Rate, Avg Turnaround
- **Visual:** Flat, inline KPI presentation without card wrappers
- **Behavior:** Bound to the same `periodDays` selection as the story canvas

### Zone 5: Vertical Story Timeline
- **Position:** Primary narrative zone
- **Content:**
  - vertical SVG spine
  - non-terminal stage nodes along the spine
  - same-side story panel per stage
  - terminal outcome branches rooted from the bottom of the spine
- **Visual:**
  - story panels use ghost borders via `--callout-border`
  - stage node card shows label + count
  - flow-volume spine segments vary thickness by outflow
- **Behavior:**
  - desktop / tablet: story panels alternate left and right
  - phone: node and panel stack on the centerline
  - zero-count states preserve continuity with empty-state messages

### Zone 6: Stage Story Panels
- **Position:** Inline with each stage node
- **Content:**
  - contextual mini-visual on top
  - label / summary
  - `Next view` affordance when alternates exist in `Flow`
  - stacked 2-3 bullet narrative callout
- **Behavior:**
  - `Flow`: contextual stage view with optional alternates
  - `Friction`: uniform dwell-band storytelling
  - `Outcomes`: stage panels dim while terminal branches take focus
  - breakdown-backed alternates load lazily on first activation

### Zone 7: Stage and Outcome Drilldowns
- **Position:** Layered over the screen from node / outcome triggers
- **Content:** Detailed items / composition for the selected stage or outcome
- **Behavior:** Uses the shared `Popover` dialog path
- **Responsive:**
  - desktop: anchored popover
  - tablet: centered overlay dialog
  - phone: bottom-sheet dialog

### Zone 8: Activity
- **Position:** Below the story canvas
- **Content:** Recent timeline events
- **Visual:** Elevated glass-card panel; `canvas-zone-break` remains transparent around it

### Zone 9: My Tasks
- **Position:** Below Activity
- **Content:** User tasks with due dates / status
- **Visual:** Elevated glass-card panel matching Activity

## Primary Data

- KPI aggregates by period
- Dashboard opportunities aggregate by period
- Submission opportunity flow by period
- Opportunity outcomes by period
- Opportunity aging / SLA data by period
- Lazy per-stage breakdowns:
  - `lineOfBusiness`
  - `broker`
  - `brokerState`
  - `assignedUser`
  - `program`
- Nudges
- Activity events
- My tasks

## User Actions

| Action | UI Element | Result | Permission |
|--------|------------|--------|------------|
| Switch period | Period control | Refreshes KPI + story queries in sync | Read (internal roles) |
| Switch chapter | Chapter pills | Changes stage/branch emphasis model | Read (internal roles) |
| Cycle stage alternate | `Next view` button in a story panel | Advances to another contextual view and lazily loads breakdown data if needed | Read (ABAC-scoped data) |
| Open stage drilldown | Stage node trigger | Opens stage detail dialog/popover | Read (ABAC scoped) |
| Open outcome drilldown | Outcome trigger | Opens outcome detail dialog/popover | Read (ABAC scoped) |
| Collapse left nav | Nav toggle | Expands available content width | Layout control |
| Collapse right rail | Rail toggle | Expands available content width | Layout control |
| Open task | Task row | Navigates to linked item | Read (role scoped) |
| Open activity item | Activity entry | Navigates to linked item / detail | Read (role scoped) |

## Navigation & Flow

**Entry Points:**
- Direct URL: `/`
- Post-login landing route
- Left nav: Dashboard

**Exit Points:**
- Broker routes
- Linked workflow item from tasks / activity
- Stage and outcome detail flows

## Responsive Layout

| Breakpoint | Layout Behavior |
|------------|----------------|
| **Desktop** (1280px+) | Full framed shell between rails; alternating story panels; anchored drilldown popovers |
| **Compact desktop / tablet** (640px-1279px) | Timeline remains vertical; panel widths and spacing compact; drilldowns shift to centered overlay dialogs |
| **Phone** (<640px) | Period control becomes `<select>`; stage panels stack on the centerline; chapter pills scroll horizontally; drilldowns use bottom-sheet dialogs |

## States & Empty Cases

| State | Behavior |
|-------|----------|
| **Loading** | Story flow zone shows skeleton; surrounding dashboard shell remains stable |
| **Flow error** | Inline recoverable error for opportunity flow; other zones can still render |
| **No stage activity** | Stage nodes remain visible; message shows `No activity in period` |
| **No exits** | Outcome area shows `No exits in period` / `No outcomes in period` |
| **Zero-count stage** | Story panel stays visible in muted form for continuity |
| **Breakdown unavailable** | Fallback / simplified visual remains available without collapsing the entire stage card |

## Validation & Errors

- Supported period values: `30`, `90`, `180`, `365`
- Supported chapters: `flow`, `friction`, `outcomes`
- Stage and outcome dialogs must remain dismissible and focus-safe
- Breakdown requests must remain period-scoped and status-scoped
- Missing breakdown data must degrade gracefully without removing the stage story

## Accessibility Notes

- Chapter selectors use `role="tablist"` and `aria-selected`
- Stage nodes support Arrow keys, `Home`, and `End`
- Stage / outcome dialogs use `role="dialog"` and descriptive `aria-label`
- `Escape` closes dialogs
- Focus returns to the invoking trigger
- Reduced motion disables the relevant transitions / animations
- Focus-visible treatment must remain visible in both themes

## Visual References

| Reference | Path | Purpose |
|-----------|------|---------|
| Original dashboard framing inspiration | `planning-mds/screens/handdrawn.jpeg` | Framed-canvas direction |
| Editorial palette / shell tokens | `planning-mds/screens/design-tokens.md` | Color, shell, glass-card, and glow tokens |
| F0013 planning PRD | `planning-mds/features/archive/F0013-dashboard-framed-storytelling-canvas/PRD.md` | Product intent and acceptance framing |
