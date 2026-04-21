# F0002 — Broker & MGA Relationship Management — Getting Started

## Prerequisites

- [ ] Backend API running (`engine/src/Nebula.Api`)
- [ ] Frontend app running (`experience`)

## Services to Run

```bash
docker compose up -d db authentik-server authentik-worker
dotnet run --project engine/src/Nebula.Api
cd experience && pnpm dev
```

## How to Verify

1. Open `/brokers`.
2. Create, search, view, edit, and delete/deactivate broker records.
3. Verify contacts and timeline actions in Broker 360.
