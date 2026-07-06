# Test Plan — F0024 Drift Reconciliation

## Backend

- Compile the backend through the focused F0024 unit-test target.
- Verify service-case creation, transition, update, follow-up task, and validator guardrails.
- Add/confirm coverage for required due date, future date-of-loss rejection, waiting reason requirement, and closure resolution requirement.

## Frontend

- Compile the frontend through `npm run build`.
- Run targeted service-case component tests.
- Confirm TypeScript coverage for expanded service-case DTO fields and new workspace/detail flows.

## Out Of Scope For This Gate

- Full browser automation across all service-case workflows.
- Full repository regression suite.
- Deployment rehearsal beyond compile/build-level deployability checks.
