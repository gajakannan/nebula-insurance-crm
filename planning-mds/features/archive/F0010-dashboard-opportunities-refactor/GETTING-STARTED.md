# F0010 — Dashboard Opportunities Refactor (Pipeline Board + Insight Views) — Getting Started

## Prerequisites

- [ ] Backend API running (`engine/src/Nebula.Api`)
- [ ] Frontend app running (`experience`)
- [ ] Dashboard seed data available for submissions and renewals

## Services to Run

```bash
docker compose up -d db authentik-server authentik-worker
dotnet run --project engine/src/Nebula.Api
cd experience && pnpm dev
```

## Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `VITE_AUTH_MODE` | Frontend auth mode for dashboard access | `oidc` |

## Seed Data

Required seed expectations for useful Opportunities visualization checks:
- Submission and renewal records across multiple non-terminal statuses
- Workflow transitions over 30/90/180/365 day windows

## How to Verify

1. Open `/` and authenticate as an internal role.
2. Confirm Opportunities defaults to Pipeline Board view.
3. Switch period (30d/90d/180d/365d) and verify counts refresh.
4. Switch to Heatmap, Treemap, and Sunburst views; verify each renders scoped data.
5. Open status drilldown mini-cards from each supported view.
6. Verify responsive behavior at MacBook, iPad, and iPhone breakpoints.

## Key Files

| Layer | Path | Purpose |
|-------|------|---------|
| Product Plan | `planning-mds/features/archive/F0010-dashboard-opportunities-refactor/PRD.md` | Feature goals and scope |
| Stories | `planning-mds/features/archive/F0010-dashboard-opportunities-refactor/F0010-S*.md` | Testable story slices |
| Assembly Plan | `planning-mds/architecture/feature-assembly-plan.md` | Cross-agent implementation order |

## Notes

- This feature refactors the Opportunities widget only; it does not redesign other dashboard widgets.
- Sankey is treated as non-default (optional/future) in this feature scope.
