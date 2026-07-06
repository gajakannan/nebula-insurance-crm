# Test Plan — F0022

## Scope

- Backend compile and migration validation for queue/routing schema, services, endpoints, and policy embedding.
- Runtime smoke for API health, migration history, fallback seed rows, and authenticated queue listing.
- Frontend production build for the work-queues console and route/sidebar integration.
- Focused authorization regression coverage for queue read/manage/assign policy decisions.

## Test Cases

| ID | Area | Command / Check | Expected |
|----|------|-----------------|----------|
| T1 | Backend build | `docker compose build api` | API image builds successfully. |
| T2 | Migration startup | `docker compose up -d api` | API starts and migration applies. |
| T3 | Health | `curl -fsS http://localhost:8080/healthz` | `Healthy`. |
| T4 | Migration history | Query `__EFMigrationsHistory` | F0022 migration present. |
| T5 | Seed data | Query `WorkQueues` | Task, Submission, Renewal fallback queues active. |
| T6 | Runtime auth/API | Authenticated `GET /work-queues` | 200 with fallback queues. |
| T7 | Frontend build | `corepack pnpm --dir experience build` | TypeScript and Vite build pass. |
| T8 | Authorization regression | `dotnet test ... --filter WorkQueuePolicy_MatchesPolicyCsv` in SDK container | 13 cases pass. |

## Not Run

- Full end-to-end browser flow was not run at G2.
- Full backend suite was not run at G2; focused authorization coverage was added and executed.
