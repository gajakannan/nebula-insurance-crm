# F0013-S0005: Ensure Responsive, Accessibility, and Performance Parity for Framed Storytelling Canvas

**Story ID:** F0013-S0005
**Feature:** F0013 — Dashboard Framed Storytelling Canvas
**Title:** Ensure responsive, accessibility, and performance parity for framed storytelling canvas
**Priority:** Medium
**Phase:** MVP

## User Story

**As a** dashboard user on any device or using assistive technology
**I want** the framed storytelling canvas to adapt gracefully across desktop, tablet, and phone, remain keyboard and screen-reader accessible, and avoid unnecessary request / render pressure
**So that** the dashboard experience remains usable and legible regardless of screen size, input mode, or motion preference

## Context & Background

By S0005, the implemented F0013 experience is no longer the old flat F0012 canvas or a horizontal timeline. The shipping shape is:

- framed dashboard shell with bordered inset container
- elevated operational panels (`Nudges`, `Activity`, `My Tasks`)
- flat story canvas with a vertical SVG spine
- same-side ghost-bordered story panels per stop
- stage / outcome drilldown details rendered through the shared `Popover` dialog path
- synchronized base story queries with lazy per-stage breakdown loading

This story validates that behavior across breakpoints and assistive interactions without reintroducing stale assumptions from earlier planning drafts.

## Acceptance Criteria

**Happy Path:**
- **Given** F0013-S0001 through F0013-S0004 are implemented
- **When** the user opens the dashboard on desktop, tablet, or phone
- **Then** the framed canvas remains readable, keyboard-accessible, and performant enough to support the story flow without eager-loading every alternate visualization

**Alternative Flows / Edge Cases:**
- Very slow network or delayed API response -> skeleton / empty states render without layout collapse
- User resizes from desktop to phone width -> timeline remains vertical but shifts from alternating side panels to stacked phone layout
- All stages have count `0` -> stage nodes remain visible for continuity and the canvas shows `No activity in period`
- No terminal outcomes in period -> outcome area shows `No exits in period` / `No outcomes in period`
- `prefers-reduced-motion: reduce` -> shell / panel / mini-visual transitions are disabled
- Keyboard-only user -> chapter tabs, stage nodes, dialogs, and dismiss flow remain operable

### Responsive Layout

**Desktop (1280px+):**
- [ ] Framed inset shell remains between the left nav and right rail with a visible outer gap
- [ ] Vertical timeline renders with alternating left/right story panels
- [ ] Stage drilldowns render as anchored popovers
- [ ] Activity and My Tasks remain elevated panels below the story canvas

**Tablet Landscape / Portrait (640px-1023px):**
- [ ] Vertical timeline remains the primary layout
- [ ] Stage spacing and panel widths compact without overlapping the outcome branch area
- [ ] Stage / outcome drilldowns render as centered overlay dialogs
- [ ] Chapter pills remain directly accessible; period controls remain visible

**Phone (<640px):**
- [ ] Period selector collapses to a compact `<select>`
- [ ] Chapter controls remain available as a horizontally scrollable pill group
- [ ] Stage nodes stay centered on the spine and story panels stack with the node instead of alternating sides
- [ ] Stage / outcome drilldowns render as bottom-sheet style dialogs
- [ ] Operational panels stack vertically at full width

### Accessibility

- [ ] Chapter controls expose `role="tablist"` and `aria-selected` on the active chapter
- [ ] Stage nodes are keyboard-navigable with Arrow keys, `Home`, and `End`
- [ ] Stage and outcome dialogs expose `role="dialog"` with descriptive `aria-label` text
- [ ] Dialogs dismiss on `Escape`
- [ ] Focus returns to the invoking trigger after dialog close
- [ ] Focus-visible treatment remains clear in dark and light themes
- [ ] KPI labels and values meet intended contrast requirements in both themes
- [ ] `prefers-reduced-motion: reduce` disables relevant transitions / animations

### Performance / Data-Loading Expectations

- [ ] Story canvas base queries stay synchronized by `periodDays`
- [ ] Per-stage breakdown queries are lazy-loaded on first activation instead of all fetching at mount
- [ ] Story canvas does not eagerly preload every alternate visualization dimension
- [ ] Timeline SVG and story panels render without overflow or clipping at supported widths
- [ ] Dialog open / close interaction remains responsive on stage and outcome triggers
- [ ] Loading, empty, and retry states are recoverable without breaking the surrounding dashboard shell

### Collapsible Rails

- [ ] Canvas width adapts to left-nav and right-rail collapse state changes
- [ ] Inset shell border / radius / shadow remain intact across rail states
- [ ] Story timeline recomputes available width without horizontal overflow
- [ ] Operational panels preserve their elevated treatment in all rail states

## Data Requirements

No new API contracts are introduced by S0005. Validation covers the implemented F0013 data flows:

- synchronized base queries for KPIs, dashboard opportunities, flow, outcomes, and aging
- lazy breakdown queries for stage alternates
- supporting dashboard queries for nudges, activity, and tasks

## Role-Based Visibility

All internal dashboard roles see the same responsive / accessibility behavior. Data remains subject to the existing ABAC-scoped dashboard endpoints.

## Non-Functional Expectations

- Readability first: preserve the three-layer hierarchy at every breakpoint
- Responsiveness first: adapt the layout without changing the core story model
- Efficiency first: load the base story state eagerly, but keep breakdown-backed alternates on demand

## Dependencies

**Depends On:**
- F0013-S0001 — framed hierarchy / inset shell
- F0013-S0002 — vertical timeline / terminal branches
- F0013-S0003 — inline story panels and alternates
- F0013-S0004 — Flow / Friction / Outcomes chapter model

**Related Stories:**
- All prior F0013 stories — this is the cross-cutting validation pass

## Out of Scope

- Native mobile app behavior
- Offline/PWA concerns
- New dashboard data contracts beyond the existing F0013 implementation

## UI/UX Notes

- The vertical timeline remains vertical at every breakpoint; only spacing and panel placement change.
- Desktop / tablet use alternating same-side story panels; phone uses stacked panels centered on the spine.
- Inline story panels are always visible; drilldown details use dialogs/popovers layered on top.
- `Popover.tsx` currently handles the responsive drilldown container shift:
  - desktop anchored popover
  - tablet centered overlay
  - phone bottom sheet

## Questions & Assumptions

**Open Questions:**
- [ ] Should StoryCanvas expose an explicit reduced-motion visual test once the environment/tooling blockers are cleared?

**Assumptions:**
- `VerticalTimeline` uses `ResizeObserver` to recompute width buckets for desktop / compact / phone layouts
- StoryCanvas remains submission-led for the main flow while using synchronized aggregate data to enrich stage narratives
- Breakdown views stay cached after first load through React Query

## Definition of Done

- [ ] Acceptance criteria verified across desktop, tablet, and phone
- [ ] Accessibility checks verified for chapter tabs, stage navigation, dialogs, and reduced motion
- [ ] Lazy breakdown behavior confirmed
- [ ] Rail collapse states verified
- [ ] Both dark and light themes reviewed
- [ ] Validation evidence recorded
- [ ] Story filename matches Story ID prefix
- [ ] Story index regenerated
