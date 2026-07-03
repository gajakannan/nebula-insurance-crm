# F0035 Test Plan

## Objective

Revalidate the F0035 session continuity evidence needed to replace missing historical ignored artifacts and remove old absolute/scratch artifact references.

## Backend

- Build `Nebula.Api`.
- Attempt focused Testcontainers-backed session telemetry/auth ProblemDetails integration tests with coverage.

## Frontend

- Build with direct `tsc` and `vite` binaries.
- Run focused session-continuity/authentication tests with coverage.

## Acceptance

All focused commands must produce run-local artifacts. Docker prerequisite limitations and product dependency advisories are documented separately.
