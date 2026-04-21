# F0001 — Dashboard — Getting Started

## Prerequisites

- [ ] Backend API running (`engine/src/Nebula.Api`)
- [ ] Frontend app running (`experience`)
- [ ] Seed data loaded

## Services to Run

```bash
docker compose up -d db authentik-server authentik-worker
dotnet run --project engine/src/Nebula.Api
cd experience && pnpm dev
```

## How to Verify

1. Open dashboard `/`.
2. Verify KPI, pipeline, activity, nudge, and tasks widgets load.
3. Confirm empty/error states degrade gracefully.
