# F0013-S0001: Restore Framed Canvas Identity with Three-Layer Visual Hierarchy

**Story ID:** F0013-S0001
**Feature:** F0013 — Dashboard Framed Storytelling Canvas
**Title:** Restore framed canvas identity with three-layer visual hierarchy
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** dashboard user
**I want** nudge cards, Activity, and My Tasks to render as raised panels with glass-card depth and soft hover glow, while the story canvas zone (KPIs, flow, chapters) remains flat and borderless
**So that** the dashboard feels like a framed canvas with depth and purpose — not a flat poster that lost its identity

## Context & Background

F0012 stripped borders, depth, and glow from ALL dashboard components in pursuit of a "flat infographic" philosophy. This lost the visual identity established in the original design (see `planning-mds/screens/handdrawn.jpeg`). The app chrome (left nav, right Neuron rail, top bar) should act as a frame, and operational panels should float with depth within that frame. Only the story canvas zone (opportunity flow area) should be the flat infographic surface.

## Acceptance Criteria

**Happy Path:**
- **Given** the dashboard page loads
- **When** the user views the page
- **Then** nudge cards render with `glass-card` treatment, visible depth/elevation, and soft hover glow (violet/fuchsia from design tokens)
- **And** Activity panel renders as a raised card with `glass-card`, depth, and hover glow
- **And** My Tasks panel renders as a raised card with `glass-card`, depth, and hover glow
- **And** the story canvas zone (story controls, KPI band, timeline flow, chapter overlays) renders flat without card borders
- **And** the app chrome (left nav rail, right Neuron rail, top bar) visually frames the dashboard content
- **And** the main content area renders inside a bordered, rounded-corner inset container (sidebar-08 pattern) with a visible border, rounded corners (0.75rem), inset shadow, and a small gap (0.75rem) between the sidebars and the content border on all sides

**Alternative Flows / Edge Cases:**
- No nudge items to display → Nudge zone collapses gracefully; glass-card container is not rendered (same as current behavior)
- Activity feed is empty → Activity panel renders with glass-card depth and "No recent activity" empty state
- My Tasks list is empty → Tasks panel renders with glass-card depth and "No tasks assigned" empty state
- Theme switch mid-session → Glass-card depth, glow intensity, and background adjust smoothly (200ms transition per design tokens)

**Checklist:**
- [ ] Nudge cards use `glass-card` class with depth/elevation
- [ ] Nudge cards have soft hover/focus glow (`glow-violet-hover` or equivalent)
- [ ] Nudge dismiss (X) interaction preserved
- [ ] Activity panel uses `glass-card` class with depth/elevation
- [ ] Activity panel has soft hover/focus glow
- [ ] My Tasks panel uses `glass-card` class with depth/elevation
- [ ] My Tasks panel has soft hover/focus glow
- [ ] KPI band renders flat (no card borders) within the story canvas zone
- [ ] Story controls (period + chapter selectors) render flat within the story canvas zone
- [ ] `.canvas-zone-break` section wrappers (around Activity, My Tasks) have transparent backgrounds — no tinted background panels behind glass-card components (the glass-card elevation provides sufficient visual separation)
- [ ] Main content area uses inset container with border (`1px solid var(--sidebar-border)`), rounded corners (`0.75rem`), inset shadow, and `--shell-inset-bg` background
- [ ] Gap of `0.75rem` between left nav / right rail and the content border (`.lg-sidebar-offset` padding)
- [ ] Visual hierarchy is clear: frame > inset content container > floating panels > flat story canvas
- [ ] Both dark and light themes maintain the three-layer visual distinction
- [ ] Existing `glass-card`, `glow-*-hover` tokens from `design-tokens.md` are used (no new tokens needed)

## Data Requirements

No new data requirements. This story is a visual/CSS-only change to restore existing design token usage on dashboard components.

## Role-Based Visibility

**Roles that can view dashboard:**
- DistributionUser, DistributionManager, Underwriter, RelationshipManager, ProgramManager, Admin — Read access, ABAC-scoped

**Data Visibility:**
- InternalOnly: All dashboard content (nudges, KPIs, flow, activity, tasks)

## Non-Functional Expectations

- Performance: No additional render cost — this is CSS class restoration, not new computation.
- Accessibility: Focus rings must remain visible on raised panels. Glass-card treatment must not reduce text contrast below WCAG AA.

## Dependencies

**Depends On:**
- None (this is the foundational visual correction; other stories build on top)

**Related Stories:**
- F0013-S0002 — Timeline bar renders within the flat story canvas zone established here
- F0013-S0003 — Radial popovers render within the flat story canvas zone established here

## Out of Scope

- Changing the opportunity flow visualization (that's S0002)
- Adding radial popovers (that's S0003)
- Chapter control behavior changes (that's S0004)
- Responsive behavior changes (that's S0005)

## UI/UX Notes

- Screens involved: Dashboard page
- The three-layer visual hierarchy is the core UX change:
  - **Layer 1 (Frame):** Left nav rail, right Neuron rail, top bar — flush to viewport edges, unchanged app chrome
  - **Layer 1.5 (Inset container):** The main content area sits in a bordered, rounded-corner container with `--shell-inset-bg` background and `var(--sidebar-border)` border — inspired by shadcn sidebar-08 "inset" pattern. A `0.75rem` gap separates the container from both sidebars on all sides.
  - **Layer 2 (Operational panels):** Nudge cards, Activity, My Tasks — `glass-card` + depth + glow, rendered inside the inset container
  - **Layer 3 (Story canvas):** Story controls, KPI band, timeline flow, chapter overlays — flat, no card borders, rendered inside the inset container

## Questions & Assumptions

**Assumptions:**
- The existing `glass-card` and `glow-*-hover` CSS utilities from `design-tokens.md` are sufficient — no new design tokens needed.
- The F0012 implementation removed these classes from dashboard components; this story adds them back.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Three-layer visual hierarchy visible in both dark and light themes
- [ ] Glass-card depth and glow present on nudge cards, Activity, and My Tasks
- [ ] Story canvas zone (KPIs, flow, chapters) remains flat and borderless
- [ ] Tests pass
- [ ] Story filename matches Story ID prefix
- [ ] Story index regenerated
