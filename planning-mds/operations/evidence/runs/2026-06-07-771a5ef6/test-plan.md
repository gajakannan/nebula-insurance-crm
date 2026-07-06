# Test Plan — F0017 run 2026-06-07-771a5ef6

## Objective

Validate that the F0017 hierarchy, ownership, territory, and distribution UI slices satisfy PRD/ADR-026 behavior without widening into deferred F0037 authorization enforcement or rollup reporting.

## Backend Coverage Plan

- Run targeted integration tests for `DistributionEndpointTests`, `ProducerOwnershipEndpointTests`, and `TerritoryEndpointTests`.
- Confirm hierarchy mutation guards: self-parent, cycle prevention, not found, unauthorized mutation, and precondition behavior.
- Confirm hierarchy reads: ancestors and descendants.
- Confirm producer ownership: assign, reassign, as-of lookup, invalid period/backdate rejection, not found, and unauthorized mutation.
- Confirm territory: create, duplicate active name conflict, member assignment, member list/as-of lookup, invalid period/backdate rejection, not found, and unauthorized mutation.
- Preserve historical June evidence for the incremental hierarchy/ownership/territory runs as reconstructed notes, but use the July 2 rerun as the current authoritative test result.

## Frontend Coverage Plan

- Run the feature-level distribution panel test path: `src/features/distribution/tests/HierarchyPanel.test.tsx`.
- Confirm the hierarchy panel renders breadcrumb/children and exposes the set-parent control.
- Run project lint and production build to catch integration/type regressions that the narrow component test may miss.

## Deployability Plan

- Confirm current Docker compose runtime status.
- Confirm frontend build output.
- Treat the EF migration as deployment-config-bearing under the harness path classes and require DevOps/deployability evidence at G2.
- Record dependency/build warnings as recommendations rather than silently passing them.

## Out-of-Scope Checks

- No hierarchy-aware read/access scoping tests are required for F0017 because enforcement is deferred to F0037.
- No production rollup/reporting tests are required for F0017 because rollups are deferred to F0037/F0023 consumers.
