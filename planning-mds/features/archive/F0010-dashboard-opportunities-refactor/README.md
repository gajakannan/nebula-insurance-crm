# F0010 — Dashboard Opportunities Refactor (Pipeline Board + Insight Views)

**Status:** Abandoned — Superseded by F0013
**Superseded By:** [F0013 — Dashboard Framed Storytelling Canvas](../F0013-dashboard-framed-storytelling-canvas/README.md)
**Abandoned Date:** 2026-03-14
**Previously:** Done
**Phase:** MVP

## Overview

~~Refactors the dashboard Opportunities experience to make Pipeline Board the default view and provide optional Heatmap, Treemap, and Sunburst insight modes.~~

**ABANDONED:** F0010 established the Pipeline Board and insight views (Heatmap, Treemap, Sunburst). F0011 proposed replacing the Pipeline Board with a connected flow. F0012 went further with a flat infographic canvas. F0013 corrects the over-flattening and replaces the entire approach with a framed storytelling canvas featuring a vertical timeline with contextual mini-visualizations. The Pipeline Board, Heatmap, Treemap, and Sunburst views from F0010 are no longer part of the dashboard direction.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Full product requirements (why + what + how) |
| [STATUS.md](./STATUS.md) | Completion checklist and progress tracking |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Developer/agent setup guide |
| [TEST-PLAN.md](./TEST-PLAN.md) | Feature test plan (happy path + error scenarios) |
| [DEPLOYABILITY-CHECK.md](./DEPLOYABILITY-CHECK.md) | Feature deployability checklist and evidence log |
| [F0010-CODE-REVIEW-REPORT.md](./F0010-CODE-REVIEW-REPORT.md) | Feature code review output (Step 3a) |
| [F0010-SECURITY-REVIEW-REPORT.md](./F0010-SECURITY-REVIEW-REPORT.md) | Feature security review output (Step 3b) |
| [F0010-FEATURE-ACTION-EXECUTION.md](./F0010-FEATURE-ACTION-EXECUTION.md) | Step-by-step feature action execution log |

## Stories

| ID | Title | Status |
|----|-------|--------|
| [F0010-S0001](./F0010-S0001-replace-sankey-with-pipeline-board-default.md) | Replace Sankey default with Pipeline Board | Done |
| [F0010-S0002](./F0010-S0002-add-opportunity-aging-heatmap-view.md) | Add Opportunities Aging Heatmap view | Done |
| [F0010-S0003](./F0010-S0003-add-opportunity-composition-treemap-view.md) | Add Opportunities Composition Treemap view | Done |
| [F0010-S0004](./F0010-S0004-add-opportunity-hierarchy-sunburst-view.md) | Add Opportunities Hierarchy Sunburst view | Done |
| [F0010-S0005](./F0010-S0005-unify-drilldown-responsive-and-accessibility.md) | Unify drilldown, responsive layout, and accessibility | Done |

**Total Stories:** 5
**Completed:** 5 / 5
