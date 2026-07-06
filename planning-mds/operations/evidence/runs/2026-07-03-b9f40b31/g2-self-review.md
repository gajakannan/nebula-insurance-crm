# G2 Self Review — F0022 Work Queues, Assignment Rules, and Coverage

## Scope Review

- Implemented backend queue/routing foundation for WorkQueue, WorkQueueMember, AssignmentRule, CoverageWindow, QueueWorkItem, and RoutingDecisionLog.
- Added `/work-queues`, `/assignment-rules`, `/coverage-windows`, `/queue-work-items/{id}/assignment`, `/routing-events`, and `/routing-events/route` API surfaces.
- Added source adapters for Task, Submission, and Renewal routing summaries and assignment write-back.
- Added a compact frontend work-queues console reachable at `/work-queues`.
- Added runtime queue authorization policy rows and a focused Casbin regression test.

## Acceptance Criteria Review

- Queue setup, fallback queues, rules, coverage windows, queue worklists, reassignment, rebalance, and routing audit are implemented as durable backend concepts.
- Runtime startup applies the F0022 migration and seeds active fallback queues for Task, Submission, and Renewal.
- The UI exposes queue administration, rule creation, coverage creation, worklist visibility, reassignment, rebalance, and audit visibility.
- Source-record ABAC intersection remains a follow-up hardening area for queue worklist row detail expansion; current MVP returns queue item identifiers and source type/id only.

## Implementation Risks

- Local host `dotnet build` is unreliable in this workspace and had to be replaced with Docker SDK/build evidence.
- `dotnet ef` is unavailable, so the migration was hand-authored following existing manual migration patterns.
- The first migration seed attempt used EF `InsertData`; runtime rejected it without a designer model, so fallback seeds were converted to explicit SQL and revalidated.
- Full automatic routing hooks from every upstream lifecycle event remain candidates for hardening; the routing command endpoint, engine, and reassignment write-back foundation are present.

## Validation Evidence

- `docker compose build api` passed after backend implementation and migration updates.
- API startup applied `20260703133000_F0022_WorkQueuesRouting`.
- `/healthz` returned `Healthy`.
- Authenticated `GET /work-queues` returned the three seeded fallback queues.
- `corepack pnpm --dir experience build` passed.
- Focused .NET test `WorkQueuePolicy_MatchesPolicyCsv` passed 13 queue policy cases in the .NET SDK container.
