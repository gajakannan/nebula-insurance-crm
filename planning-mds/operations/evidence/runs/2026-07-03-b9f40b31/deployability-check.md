# Deployability Check — F0022

## Summary

Result: PASS

## Runtime And Migration

- API image builds successfully with `docker compose build api`.
- Startup migration path applies pending migrations through `db.Database.MigrateAsync()`.
- F0022 migration was applied in local Postgres and recorded in `__EFMigrationsHistory`.
- Seed data created three active fallback queues for Task, Submission, and Renewal.

## Configuration

- No new required environment variables were introduced by F0022.
- Runtime authorization policy is embedded from `planning-mds/security/policies/policy.csv`; queue policy rows were added and verified through Casbin tests and authenticated API smoke.

## Rollback Notes

- Migration `Down` drops RoutingDecisionLogs, WorkQueueMembers, QueueWorkItems, CoverageWindows, AssignmentRules, and WorkQueues.
- Because routing tables are operational state, production rollback would require data retention/export approval before applying `Down`.

## Open Deployment Risks

- Manual migration was used because `dotnet ef` is unavailable in the workspace.
- Full pre-production smoke should include an authenticated browser pass through `/work-queues`.
