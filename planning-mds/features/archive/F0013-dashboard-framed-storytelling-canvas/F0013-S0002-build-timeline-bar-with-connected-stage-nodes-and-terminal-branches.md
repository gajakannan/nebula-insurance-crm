# F0013-S0002: Build Vertical Timeline with Connected Stage Nodes and Terminal Outcome Branches

**Story ID:** F0013-S0002
**Feature:** F0013 — Dashboard Framed Storytelling Canvas
**Title:** Build vertical timeline with connected stage nodes and terminal outcome branches
**Priority:** High
**Phase:** MVP

## User Story

**As a** dashboard user
**I want** the opportunity flow to render as a vertical timeline spine with stage nodes alternating left and right, connected by flow ribbons, with terminal outcomes branching at the bottom
**So that** I can read the opportunity journey top-to-bottom like a narrative, with each stop naturally flowing into the next as I scroll

## Context & Background

The current opportunity flow (from F0012/F0010) renders workflow stages as flat rectangular cells in a horizontal row. The handdrawn wireframes (`planning-mds/screens/handdrawn.jpeg`, `planning-mds/screens/handdrawn-timeline.jpeg`) and pipeline inspiration images (`pipeline1.png` through `pipeline5.png`) envision a timeline/journey-map style where stages are connected nodes along a spine. The vertical orientation (inspired by `pipeline5.png`) works naturally with scrolling and gives each stop room for its mini-visualization (S0003). Terminal outcomes (Bound, Declined, Expired, Lost, No Quote) branch off the bottom of the timeline.

## Acceptance Criteria

**Happy Path:**
- **Given** the dashboard loads with opportunity flow data
- **When** the user views the story canvas zone
- **Then** a vertical timeline spine renders top-to-bottom
- **And** stage nodes are positioned alternating left-right of the spine
- **And** nodes are connected by flow ribbons along the spine whose thickness reflects transition count between stages
- **And** stage nodes are ordered top-to-bottom by `displayOrder`
- **And** stage nodes use `colorGroup` for warm-to-cool color progression (intake → triage → waiting → review → decision)
- **And** each stage node displays the stage label and current count
- **And** each stage node has a companion story panel on the same side of the spine containing both a mini-visualization and a narrative callout stacked vertically inside a single ghost-bordered card (both rendered in S0003)
- **And** the mini-visualization area scales proportionally with the stage's item count (larger count = larger visual)
- **And** the narrative callout (below the mini-visual, separated by a subtle divider) displays 2-3 data-driven bullet points that tell that stage's story in words (content defined in S0003)
- **And** terminal outcome branches fan out from the spine's bottom point (the root of the trunk), not from the last stage node
- **And** terminal outcome branches use line styles: solid (positive — Bound), dashed (negative — Declined, Lost), dotted (passive — Expired, No Quote)

**Alternative Flows / Edge Cases:**
- Stage with count = 0 → Node renders at reduced opacity with minimum-size visual area but remains on the timeline (no gaps)
- Single stage has all items → That node renders with the largest visual; other nodes scale down proportionally
- No terminal outcomes in window → Terminal branch area renders empty with "No exits in period" text
- Very long pipeline (10+ stages) → Timeline remains scrollable within the story canvas zone

**Checklist:**
- [ ] Vertical timeline spine renders top-to-bottom
- [ ] Stage nodes alternate left-right of the spine
- [ ] Flow ribbons along spine reflect transition count (ribbon thickness)
- [ ] Stage nodes show label + count
- [ ] Story panel on same side as stage node contains both mini-visualization and narrative callout in a single ghost-bordered card
- [ ] Mini-visualization area scales with item count (proportional sizing)
- [ ] Narrative callout stacked below mini-visual with subtle divider, accommodates 2-3 bullet lines of text (content from S0003)
- [ ] Ghost border uses `--callout-border` token (blue in dark mode, salmon in light mode)
- [ ] Color progression follows `colorGroup` (warm-to-cool top-to-bottom)
- [ ] Terminal outcome nodes branch off the bottom of the timeline
- [ ] Branch line styles: solid (positive), dashed (negative), dotted (passive)
- [ ] Terminal outcome nodes show outcome label, count, and percentage
- [ ] Timeline renders as SVG for crisp scaling
- [ ] Timeline sits within the flat story canvas zone (no card borders around it)

## Data Requirements

**Required Fields (from existing endpoints):**
- `OpportunityFlowNode`: status, label, isTerminal, displayOrder, colorGroup, currentCount, inflowCount, outflowCount
- `OpportunityFlowLink`: sourceStatus, targetStatus, count (used for ribbon thickness)
- Terminal outcomes: outcome_type, count, percent_of_exits

**Validation Rules:**
- Nodes ordered by `displayOrder` ascending (deterministic left-to-right)
- Ribbon thickness = `link.count / max(all link counts)` (normalized)
- Zero-count stages still render (don't skip them)

## Role-Based Visibility

All dashboard roles see the same timeline visualization. Data is ABAC-scoped at the endpoint level.

## Non-Functional Expectations

- Performance: Timeline SVG renders within 200ms of data arrival. No canvas/WebGL — pure SVG.
- Accessibility: Stage nodes are keyboard-navigable (tab order follows displayOrder top-to-bottom). Screen reader announces stage label + count.

## Dependencies

**Depends On:**
- F0013-S0001 — Story canvas zone visual hierarchy must be established first

**Related Stories:**
- F0013-S0003 — Radial popovers attach to the stage nodes built here
- F0013-S0004 — Chapter controls modify visual emphasis on the timeline built here

## Out of Scope

- Radial chart popovers on stage nodes (that's S0003)
- Chapter overlay behavior on the timeline (that's S0004)
- Responsive/mobile adaptation of the timeline (that's S0005)

## UI/UX Notes

- The vertical timeline replaces the current flat rectangular stage cells
- SVG rendering allows crisp lines, arcs, and ribbons at any zoom level
- Terminal outcome branches fan out from the spine's bottom point (the root of the trunk) — they represent exits from the entire pipeline, not just the last stage
- The spine line extends all the way down to the outcome branch origin point, so the terminal outcomes visually grow from the timeline like roots of a tree
- Alternating left-right placement (like `pipeline5.png` and `handdrawn-timeline.jpeg`) creates visual rhythm and gives each stop room for its story panel
- **Layout per stop (alternating):**
  - Odd stops: story panel on left of spine
  - Even stops: story panel on right of spine
  - Each story panel is a single ghost-bordered card containing the mini-visual on top and narrative callout bullets below, separated by a subtle divider
  - Ghost border color swaps per theme: blue (`--accent-secondary`) at 70% in dark mode, salmon (`--accent-primary`) at 70% in light mode — defined as the `--callout-border` CSS custom property
- The combination of a visual and a textual callout at each stop is what makes this a storytelling infographic — like a magazine data spread where each section has both a chart and a caption
- **Line thickness hierarchy:**
  - Spine trunk: 3.5px — the primary visual axis
  - Connector stubs (spine → node): 2.5px
  - Flow-volume segments: 3px base + up to 7px proportional to outflow
  - Outcome branches: 3.5–4px

## Questions & Assumptions

**Assumptions:**
- SVG is the right rendering approach (not canvas or a charting library)
- Existing `OpportunityFlowNode` and `OpportunityFlowLink` data provides all needed information for timeline rendering
- Ribbon thickness is proportional to transition count — no need for logarithmic scaling at MVP

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Vertical timeline renders as SVG within the story canvas zone
- [ ] Stage nodes alternate left-right with stacked story panels (mini-visual + narrative callout) on the same side
- [ ] Proportional flow ribbons connect nodes along spine
- [ ] Terminal outcomes branch at bottom with correct line styles
- [ ] Keyboard navigation works across stage nodes (top-to-bottom)
- [ ] Tests pass
- [ ] Story filename matches Story ID prefix
- [ ] Story index regenerated
