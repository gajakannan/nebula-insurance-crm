# F0013 — Dashboard Framed Storytelling Canvas — Getting Started

## Prerequisites

- [ ] Backend services running (`PostgreSQL`, `authentik`, `engine` API)
- [ ] Frontend dependencies installed (`cd experience && pnpm install`)
- [ ] Seed data loaded with:
  - submissions and renewals across multiple workflow stages
  - workflow transitions for `avgDwellDays` / emphasis calculations
  - lines of business on submissions / renewals
  - brokers, assigned users, and programs for breakdown views
  - tasks, timeline events, and nudges for the surrounding dashboard panels

## Services to Run

```bash
# Start backend stack
docker compose up -d

# Start frontend dev server
cd experience && pnpm dev
```

## Route and Proxy Assumptions

- The protected dashboard route is `/`.
- The Vite dev proxy defaults to `http://localhost:5113` and can be overridden with `NEBULA_API_PROXY_TARGET` or `VITE_API_PROXY_TARGET`.
- No F0013-specific feature flags are required beyond the normal frontend auth / proxy environment.

## Data Shape Used by the Story Canvas

F0013 synchronizes these base queries by `periodDays`:

- `GET /dashboard/kpis?periodDays=...`
- `GET /dashboard/opportunities?periodDays=...`
- `GET /dashboard/opportunities/flow?entityType=submission&periodDays=...`
- `GET /dashboard/opportunities/outcomes?periodDays=...`
- `GET /dashboard/opportunities/aging?entityType=submission&periodDays=...`

Per-stage alternate mini-visuals use lazy breakdown queries on first activation:

- `GET /dashboard/opportunities/{entityType}/{status}/breakdown?groupBy=...&periodDays=...`

## What the Current Implementation Does

- Uses the framed dashboard shell: app chrome stays flush to the viewport, while the main content sits inside a bordered inset container with rounded corners and inset shadow.
- Keeps operational panels elevated (`Nudges`, `Activity`, `My Tasks`) while the story canvas stays flat.
- Renders the opportunity story as a vertical SVG timeline with:
  - stage nodes alternating left/right on desktop and tablet
  - same-side ghost-bordered story panels (`--callout-border`) containing the mini-visual and stacked narrative callout
  - terminal outcome branches rooted from the bottom of the trunk, not the last stage node
- Supports exactly three chapters: `Flow`, `Friction`, `Outcomes`.
- Uses a phone-specific layout where stage nodes stay centered on the spine and story panels stack in-line with the node.
- Uses the shared `Popover` dialog path for stage / outcome drilldown details:
  - anchored popover on desktop
  - centered overlay on tablet
  - bottom-sheet style overlay on phone
- Loads per-stage breakdown alternates lazily instead of fetching every breakdown dimension at mount.

## Manual Verification

1. Open `http://localhost:5173/` and sign in.
2. Confirm the framed shell:
   - left nav, right rail, and top bar form the frame
   - main content sits inside a bordered inset container with a small outer gap
3. Confirm the three-layer hierarchy:
   - nudge cards, activity, and tasks use glass-card depth / glow
   - story controls and KPI band stay flat
4. Confirm story controls:
   - period selector is pill tabs on `sm+` and a `<select>` on phone
   - chapter controls show `Flow`, `Friction`, `Outcomes`
5. Confirm the timeline:
   - a vertical spine runs top-to-bottom
   - stage nodes alternate left/right on larger widths
   - each stop has one ghost-bordered story panel with the mini-visual above the narrative bullets
   - terminal outcomes branch from the bottom of the trunk
6. Confirm drilldowns:
   - clicking a stage node opens a detail dialog/popover
   - clicking an outcome node opens an outcome detail dialog/popover
   - `Escape` dismisses the dialog
7. Confirm chapter behavior:
   - `Flow`: contextual visuals plus per-stop `Next view` when alternates exist
   - `Friction`: uniform dwell-time donuts and node emphasis states
   - `Outcomes`: stage panels dim and terminal branches glow
8. Confirm lazy alternates:
   - in `Flow`, use `Next view` on a stage with alternates
   - breakdown-backed views load on demand and remain available afterward
9. Confirm responsive behavior:
   - desktop / tablet: alternating story panels
   - phone: centered spine, stacked panels, bottom-sheet drilldowns
10. Confirm accessibility basics:
   - chapter tabs respond to arrow keys / Home / End
   - stage nodes respond to arrow keys
   - focus ring remains visible
   - reduced motion removes transitions / animations

## Key Files

| Layer | Path | Purpose |
|-------|------|---------|
| Frontend | `experience/src/pages/DashboardPage.tsx` | Dashboard composition order |
| Frontend | `experience/src/components/layout/DashboardLayout.tsx` | Framed inset shell |
| Frontend | `experience/src/features/opportunities/components/StoryCanvas.tsx` | Period/chapter controls and synchronized queries |
| Frontend | `experience/src/features/opportunities/components/VerticalTimeline.tsx` | Vertical timeline layout, spine, branch rendering |
| Frontend | `experience/src/features/opportunities/components/TimelineStageNode.tsx` | Extracted timeline node trigger / popover wrapper |
| Frontend | `experience/src/features/opportunities/components/MiniVisualization.tsx` | Extracted stage mini-visual renderer |
| Frontend | `experience/src/features/opportunities/components/NarrativeCallout.tsx` | Extracted narrative bullet callout |
| Frontend | `experience/src/features/opportunities/components/StageNodeStoryPanel.tsx` | Inline story panels, alternates, lazy breakdown loading |
| Frontend | `experience/src/components/ui/Popover.tsx` | Desktop popover / tablet overlay / phone bottom-sheet behavior |
| Screen Spec | `planning-mds/screens/S-DASH-002-framed-storytelling-canvas.md` | F0013 screen specification |
| Design | `planning-mds/screens/design-tokens.md` | Editorial palette, shell, glass-card, glow tokens |

## Known Validation Caveats

- On this Windows-mounted workspace, `pnpm install` may fail with `ERR_PNPM_EACCES` during package rename steps.
- Targeted backend test runs may fail with `MSB3021` if `engine/src/Nebula.Api/bin/Debug/net10.0/` is locked by the local environment.
- These are current environment/tooling blockers, not part of the intended F0013 runtime behavior.

## Notes

- F0013 corrects F0012's over-flattening by restoring elevation only to operational panels, not to the story canvas.
- The always-visible inline story panels are separate from the drilldown popover dialogs.
- The current implementation uses submission flow as the primary story spine while enriching stages with synchronized opportunities, outcomes, aging, and lazy breakdown data.
