# Test Plan - F0025 run 2026-07-07-9859bad4

## Scope

Validate the F0025 commission revenue slice across backend validators/policy coverage, API build integrity, frontend workspace/detail behavior, route wiring, semantic theme guard, and knowledge-graph bindings.

## Backend

- Build `Nebula.Api` to compile domain, application, infrastructure, and API endpoint registrations.
- Run focused commission validator tests for schedule, split, and rollup request validation.
- Run Casbin policy unit coverage for `commission` resource actions.

## Frontend

- Build Vite/TypeScript output.
- Run ESLint and theme semantic guard.
- Run feature-local Vitest coverage for commission workspace search/rollup rendering and detail recalculation mutation wiring.

## Harness

- Validate knowledge graph bindings after adding `capability:commission-revenue-tracking`.
- Validate G2 evidence package with the product-manager evidence validator.

## Deferred

- Full browser/e2e coverage and security scans move to G3/G6 after code review and security review.
