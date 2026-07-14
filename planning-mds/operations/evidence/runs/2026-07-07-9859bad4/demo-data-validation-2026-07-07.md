# F0025 Demo Data Validation - 2026-07-07

## Scope

Seeded local Docker Postgres with deterministic F0025 demo data to validate the Commissions UI.

## Seed Artifact

- `f0025-demo-seed.sql`

The seed is idempotent and recreates only deterministic rows with `F0025-demo` source markers.

## Seeded Data

- Expected commissions: 3
- Commission schedules: 2
- Producer split assignments: 3
- Producer split participants: 3
- Revenue attribution projections: 3
- Commission adjustments: 2

## Expected UI Results

- `/commissions` should show 3 expected commission records.
- Status filter `Calculated` should show 2 records.
- Status filter `Exception` should show 1 record.
- Exception filter `MissingSchedule` should show 1 record.
- Revenue rollups grouped by `producer` should show 3 rows, including 1 exception row.
- Opening `25000000-0000-0000-0000-000000000501` should show schedule, split, and approved adjustment details.

## API Validation

- `GET /expected-commissions?page=1&pageSize=10` with dev token returned 3 records.
- `GET /expected-commissions?page=1&pageSize=10&status=Calculated` with dev token returned 2 records.
- `GET /revenue-attribution/rollups?startDate=2026-04-07&endDate=2026-07-07&groupBy=producer` with dev token returned 3 rollup rows.
- `GET /expected-commissions/25000000-0000-0000-0000-000000000501` with dev token returned commission detail with 1 schedule, 1 split, and 1 approved adjustment.
