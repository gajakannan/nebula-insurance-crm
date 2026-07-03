# F0019 Test Plan

## Objective

Revalidate the F0019 workflow and submission UI evidence needed to replace missing historical ignored artifacts.

## Backend

- Build `Nebula.Api`.
- Run workflow-focused unit tests for `WorkflowServiceTests` and `WorkflowStateMachineTests`.

## Frontend

- Install frontend dependencies with global pnpm.
- Build with direct `tsc` and `vite` binaries.
- Run focused submissions integration tests with coverage.

## Acceptance

All focused commands must produce run-local artifacts. Product dependency advisories are documented separately in security evidence.
