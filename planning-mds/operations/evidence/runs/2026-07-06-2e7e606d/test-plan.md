# Test Plan

## Scope

Validate that the Distribution rollups page matches the F0037 PRD filter surface and that existing F0037 E2E behavior remains intact.

## Tests

- Build the frontend.
- Run focused DistributionRollupReportView component tests.
- Run F0037 Playwright E2E against the local CRM.
- Confirm the rollups page includes `As of`, `Root node`, `Territory`, `Producer`, `Group by`, and `Metric family`.
- Confirm the rollups page does not show `Region`, `Line of business`, or `Workflow type`.
