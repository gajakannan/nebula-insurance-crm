# Test Execution Report — F0022

## Summary

Result: PASS

## Executed Checks

| Check | Result | Notes |
|-------|--------|-------|
| API container build | PASS | `docker compose build api` completed. Only pre-existing dashboard nullable warnings remained. |
| API restart and migration | PASS | `docker compose up -d api` restarted API and applied F0022 migration after seed SQL correction. |
| API health | PASS | `/healthz` returned `Healthy` after the latest API restart. |
| Migration history | PASS | `20260703133000_F0022_WorkQueuesRouting` present in `__EFMigrationsHistory`. |
| Fallback queue seed | PASS | Task, Submission, and Renewal fallback queues present and Active. |
| Authenticated queue listing | PASS | Dev JWT `GET /work-queues` returned the three fallback queues after the latest API restart. |
| Frontend production build | PASS | `corepack pnpm --dir experience build` completed. Vite reported existing chunk-size advisory. |
| Queue policy unit test | PASS | SDK container `dotnet test ... --filter WorkQueuePolicy_MatchesPolicyCsv` passed 15/15 cases after G7 policy/matrix reconciliation. |

## Residual Warnings

- Pre-existing nullable warnings remain in `DashboardRepository.cs`.
- Frontend production build reports the existing large bundle advisory.
