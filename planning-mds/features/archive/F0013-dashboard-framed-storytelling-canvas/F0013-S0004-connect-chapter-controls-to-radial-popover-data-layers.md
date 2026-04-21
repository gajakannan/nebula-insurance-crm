# F0013-S0004: Connect Chapter Controls as Uniform Override for Timeline Visualizations

**Story ID:** F0013-S0004
**Feature:** F0013 — Dashboard Framed Storytelling Canvas
**Title:** Connect chapter controls as uniform override for timeline visualizations
**Priority:** High
**Phase:** MVP

## User Story

**As a** dashboard user
**I want** the chapter controls (Flow, Friction, Outcomes) to override the contextual mini-visualizations and force all timeline stops into a uniform comparison mode
**So that** I can compare dwell time across all stages or focus on terminal outcomes, then return to the contextual defaults for the full narrative

## Context & Background

By default (Flow chapter), each timeline stop shows its own contextual mini-visualization — different chart types answering different questions per stage (S0003). When the user needs to compare a single dimension across all stages (e.g., "where is work stuck the longest?"), they switch chapters. This overrides ALL stops to show the same dimension with the same chart type, enabling uniform cross-stage comparison.

This is a two-mode mental model:
1. **Flow (default):** Narrative mode — each stop tells its own story with the best-fit chart (plus per-stop alternate toggles from S0003)
2. **Friction/Outcomes:** Comparison mode — all stops show the same dimension uniformly (per-stop toggles hidden)

## Acceptance Criteria

**Chapter: Flow (default — narrative mode)**
- **Given** Flow chapter is active
- **When** the user views the timeline
- **Then** each stop shows its contextual mini-visualization (per S0003 stage mapping)
- **And** no uniform override is applied
- **And** timeline nodes have no special emphasis (default rendering)
- **And** per-stop alternate toggles (S0003) are visible and functional

**Chapter: Friction (uniform override — dwell time)**
- **Given** Friction chapter is active
- **When** the timeline renders
- **Then** ALL stage mini-visualizations switch to uniform donut charts showing dwell time bands (<1d, 1-3d, 3-7d, 7d+)
- **And** stage nodes gain emphasis rings based on the `emphasis` field: `bottleneck` (fuchsia ring), `blocked` (orange ring), `active` (violet ring), `normal` (no ring)
- **And** each donut includes `avgDwellDays` as a label

**Chapter: Outcomes (uniform override — terminal branch focus)**
- **Given** Outcomes chapter is active
- **When** the timeline renders
- **Then** terminal outcome branch paths glow/highlight with their respective line styles
- **And** terminal outcome nodes show emphasized count + percentage
- **And** stage mini-visualizations dim (reduced opacity) to draw focus to terminal branches

**Alternative Flows / Edge Cases:**
- Rapid chapter switching (user clicks multiple chapters quickly) → Only the final chapter's data renders; intermediate transitions are cancelled
- Stage has count = 0 in a chapter-specific dimension (e.g., zero renewals in Friction) → Donut shows full circle for the single non-zero segment
- User switches chapter while a per-stop alternate is selected → Per-stop selection is preserved in state; restoring Flow chapter shows the previously selected alternate
- All terminal outcomes have count = 0 in Outcomes chapter → Terminal branches render with muted styling and "No outcomes in period" label

**Checklist:**
- [ ] Chapter controls render as a pill/button group: [Flow] [Friction] [Outcomes]
- [ ] Default chapter is Flow (narrative mode — contextual mini-visuals per stage with per-stop toggles)
- [ ] Switching to Friction or Outcomes overrides ALL stage mini-visuals and hides per-stop toggles
- [ ] Switching back to Flow restores contextual defaults and per-stop toggle state
- [ ] Switching chapters does NOT unmount the timeline — only the mini-visualizations crossfade
- [ ] Friction chapter: uniform donuts (dwell bands) + emphasis rings on nodes
- [ ] Outcomes chapter: terminal branches glow + stage visuals dim
- [ ] Chapter switching has a smooth visual transition (150ms CSS crossfade on mini-visuals)
- [ ] Period selector synchronization is maintained — chapter switch does not reset the period

## Data Requirements

**Eager-loaded (mount):**
- Flow nodes + links (base timeline data)
- KPIs with periodDays
- Nudge items
- Terminal outcomes

**Friction data:** Loaded eagerly as part of flow nodes (avgDwellDays, emphasis are fields on OpportunityFlowNode). No lazy loading needed — all chapter data is available at mount.

**No lazy-loaded chapter data.** Aging and Mix were the only chapters that required lazy loading. With those chapters removed, all data needed for Flow, Friction, and Outcomes is eagerly loaded.

## Role-Based Visibility

All dashboard roles see the same chapter controls. Data is ABAC-scoped at the endpoint level.

## Non-Functional Expectations

- Performance: Chapter switch visual update < 150ms (CSS transition). No lazy data loads — all chapter data is eagerly loaded at mount.
- Accessibility: Chapter controls are keyboard-navigable (arrow keys within the group). Active chapter is announced by screen reader. Emphasis changes on timeline nodes have sufficient contrast.

## Dependencies

**Depends On:**
- F0013-S0002 — Timeline bar must exist for emphasis overlays
- F0013-S0003 — Radial popovers must exist for segment switching

**Related Stories:**
- F0013-S0005 — Responsive behavior for chapter controls on mobile

## Out of Scope

- Adding new chapters beyond Flow/Friction/Outcomes
- Persisting chapter selection across sessions (React state only)
- Backend changes to existing endpoints

## UI/UX Notes

- Chapter controls render as a horizontal pill group within the story controls zone (alongside period selector)
- Active chapter pill has filled/highlighted state
- The timeline bar does NOT unmount on chapter switch — only the emphasis overlay and popover segment data change
- This is overlay composition, not tab switching

## Questions & Assumptions

**Assumptions:**
- Dwell time band breakdown (<1d, 1-3d, 3-7d, 7d+) can be computed frontend-side from individual item ages, or is derivable from existing flow node data

## Definition of Done

- [ ] Acceptance criteria met for all 3 chapters (Flow, Friction, Outcomes)
- [ ] Chapter controls render and switch correctly
- [ ] Timeline emphasis changes smoothly on chapter switch
- [ ] Radial popover segments update per active chapter
- [ ] Period synchronization maintained across chapter switches
- [ ] Tests pass
- [ ] Story filename matches Story ID prefix
- [ ] Story index regenerated
