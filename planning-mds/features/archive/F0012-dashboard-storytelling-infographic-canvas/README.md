# F0012 — Dashboard Storytelling Infographic Canvas (Flat Canvas + Collapsible Rails)

**Status:** Done (Archived)
**Priority:** High
**Phase:** MVP
**Supersedes:** F0011

## Overview

Refactors the Dashboard into a continuous flat infographic canvas. Nudge bar, KPI band, connected opportunity flow, chapter overlays, activity, and tasks all render as sections of one seamless narrative surface — differentiated by spacing and typography, not panel borders or card wrappers. Left navigation and right Neuron rail remain collapsible with adaptive canvas width. F0012 absorbs all F0011 scope (connected flow, terminal outcomes, visual system, secondary insight rebalancing).

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Full product requirements (why + what + how) |
| [STATUS.md](./STATUS.md) | Completion checklist and progress tracking |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Developer/agent setup guide |
| [TEST-PLAN.md](./TEST-PLAN.md) | Feature test plan (happy path + error scenarios) |
| [DEPLOYABILITY-CHECK.md](./DEPLOYABILITY-CHECK.md) | Feature deployability checklist and evidence log |
| [F0012-CODE-REVIEW-REPORT.md](./F0012-CODE-REVIEW-REPORT.md) | Feature code review output (Step 3a implementation review) |
| [F0012-SECURITY-REVIEW-REPORT.md](./F0012-SECURITY-REVIEW-REPORT.md) | Feature security review output (Step 3b implementation review) |
| [F0012-FEATURE-ACTION-EXECUTION.md](./F0012-FEATURE-ACTION-EXECUTION.md) | Step-by-step feature action execution log |

## Screen Specification

| Screen Spec | Location |
|-------------|----------|
| [S-DASH-001 — Infographic Canvas](../../../screens/S-DASH-001-infographic-canvas.md) | Formal screen spec for the dashboard infographic canvas |

## Stories

| ID | Title | Status |
|----|-------|--------|
| [F0012-S0001](./F0012-S0001-unify-kpi-and-opportunities-into-single-story-canvas.md) | Unify nudge bar, KPI band, and connected opportunity flow into one flat infographic canvas | Implemented |
| [F0012-S0002](./F0012-S0002-build-interactive-opportunities-story-chapters-and-overlays.md) | Add interactive story chapters and in-canvas analytical overlays | Implemented |
| [F0012-S0003](./F0012-S0003-reflow-dashboard-layout-with-activity-and-tasks-below-canvas.md) | Flow Activity and My Tasks as flat canvas sections below story content | Implemented |
| [F0012-S0004](./F0012-S0004-support-collapsible-nav-and-neuron-rails-with-adaptive-canvas-width.md) | Preserve collapsible left nav and right Neuron rail with adaptive canvas width | Implemented |
| [F0012-S0005](./F0012-S0005-ensure-responsive-accessibility-and-performance-parity-for-story-canvas.md) | Ensure responsive, accessibility, and performance parity for infographic canvas | Implemented |

**Total Stories:** 5
**Completed:** 5 / 5
