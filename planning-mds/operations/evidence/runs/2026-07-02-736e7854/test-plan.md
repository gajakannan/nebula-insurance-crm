# Test Plan — F0028

## Scope

Validate F0028 carrier and market relationship management across API, persistence, authorization, frontend routing, runtime health, and KG/harness evidence.

## Backend Tests

- Build API project after adding entities, repository, service, endpoint registration, and EF migration.
- Build test project after adding focused F0028 integration tests.
- Run focused integration tests for carrier market list/detail/create/update/archive and child collections.
- Run Casbin authorization tests to confirm F0028 policy rows are available to runtime authorization.

## Frontend Tests

- Build the `experience` application after adding the `/carrier-markets` route, navigation, page, hooks, and types.
- Run focused `App.test.tsx` route smoke coverage including the new Markets route.

## Runtime Tests

- Rebuild/restart API through Docker Compose.
- Confirm `/healthz` returns healthy.
- Confirm unauthenticated access to `/carrier-markets` returns `401`, proving endpoint is mounted and protected.

## Harness Tests

- Refresh KG coverage report.
- Run KG validation and drift check.
- Validate stories, trackers, templates, and feature evidence through the harness scripts.
