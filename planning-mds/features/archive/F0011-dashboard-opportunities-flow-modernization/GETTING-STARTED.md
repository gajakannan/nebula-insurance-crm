# F0011 — Dashboard Opportunities Flow-First Modernization (Connected Pipeline + Terminal Outcomes) — Getting Started

## Prerequisites

- [ ] Local Nebula backend is running
- [ ] Local Nebula frontend is running
- [ ] Dashboard seed data includes open and terminal opportunities

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
- Submission opportunities across non-terminal and terminal statuses
- Renewal opportunities across non-terminal and terminal statuses
- Workflow transitions with enough history to calculate aging/outcome summaries

## How to Verify

1. Open Dashboard as an internal role user.
2. Confirm opportunities default view is the connected flow canvas.
3. Change period selector (30d/90d/180d/365d) and confirm stage/outcome counts refresh.
4. Open stage drilldowns and terminal outcome drilldowns.
5. Verify desktop, iPad, and iPhone breakpoint behavior.

## Key Files

| Layer | Path | Purpose |
|-------|------|---------|
| Backend | `engine/src/Nebula.Api/Endpoints/DashboardEndpoints.cs` | Opportunities endpoint contract updates |
| Backend | `engine/src/Nebula.Infrastructure/Repositories/DashboardRepository.cs` | Stage/outcome aggregate queries |
| Frontend | `experience/src/features/opportunities/components/` | Flow canvas, outcomes rail, mini-views |
| Frontend | `experience/src/features/opportunities/hooks/` | Opportunities data hooks and period sync |

## Notes

- This feature is a UI/data-contract refactor of existing opportunities surfaces; no domain status taxonomy changes are expected.
