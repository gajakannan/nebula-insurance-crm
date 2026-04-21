# F0012 — Dashboard Storytelling Infographic Canvas (Flat Canvas + Collapsible Rails) — Getting Started

## Prerequisites

- [ ] Local Nebula backend is running
- [ ] Local Nebula frontend is running
- [ ] Dashboard seed data includes open and terminal opportunities for submissions/renewals
- [ ] Activity and task seed data is present for canvas section validation
- [ ] At least one overdue/attention task exists for nudge bar validation

## Services to Run

```bash
# Backend API
dotnet run --project engine/src/Nebula.Api

# Frontend app
pnpm --dir experience dev
```

## Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `ASPNETCORE_ENVIRONMENT` | Backend runtime profile | `Development` |
| `VITE_API_BASE_URL` | Frontend API base URL | `http://localhost:5000` |

## Seed Data

Feature verification needs seeded data for:
- Overdue/attention task items for nudge bar (at least 2-3 items)
- Stage flow and terminal outcomes across multiple periods
- KPI metrics with non-zero values
- Activity timeline events with varied event types
- My Tasks records with varied status and due dates

## How to Verify

1. Open Dashboard as an internal role user.
2. Confirm the entire page renders as one continuous flat infographic canvas — **no panel borders, card wrappers, or divider lines visible**.
3. Confirm nudge bar appears at top and flows seamlessly into story controls below (no separator line).
4. Confirm KPI band is embedded inline (not separate card components).
5. Confirm opportunities render as connected left-to-right flow with terminal outcome branches.
6. Toggle chapter controls and confirm in-canvas overlays update in place without mode switching.
7. Collapse/expand left nav and right Neuron rail and confirm canvas width adapts.
8. Scroll down and verify Activity and My Tasks render as flat canvas sections (no borders).
9. Validate behavior on desktop, iPad, and iPhone breakpoints.

## Key Files

| Layer | Path | Purpose |
|-------|------|---------|
| Screen Spec | `planning-mds/screens/S-DASH-001-infographic-canvas.md` | Formal screen specification |
| Backend | `engine/src/Nebula.Api/Endpoints/DashboardEndpoints.cs` | Canvas data contracts and aggregate endpoints |
| Backend | `engine/src/Nebula.Infrastructure/Repositories/DashboardRepository.cs` | Flow, chapter overlay, and terminal outcome aggregates |
| Frontend | `experience/src/pages/DashboardPage.tsx` | Infographic canvas layout composition with collapsible rails |
| Frontend | `experience/src/features/opportunities/components/` | Connected flow, chapter overlays, terminal outcomes |
| Frontend | `experience/src/features/dashboard/components/` | Nudge bar, KPI band, activity section, tasks section |

## Notes

- This feature is a dashboard layout and interaction refactor; no workflow taxonomy changes are expected.
- F0012 supersedes F0011; all connected flow and terminal outcome scope is self-contained.
- Neuron rail behavior remains product-consistent (collapsible, not removed, not fixed-width-only).
- Design philosophy: infographic flat canvas — spacing and typography differentiate content zones, not borders.
