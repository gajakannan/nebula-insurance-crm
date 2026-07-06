# Action Context

## Run Identity

- Feature ID: F0037
- Feature slug: hierarchy-aware-access-scoping-and-distribution-rollups
- Run ID: 2026-07-06-38152f5c
- Mode: drift-reconcile
- Product root: `/Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm`
- Prior run: 2026-07-06-76799554

## Inputs

- Operator-approved plan: Add F0037 Operational Reports entry to the sidebar.
- Existing F0037 UI route: `/operational-reports?report=rollups`
- Existing page: `experience/src/pages/OperationalReportsPage.tsx`

## Assumptions

- This is a discoverability follow-up for the completed F0037 feature.
- The sidebar link should land directly on the Distribution rollups tab.
- No backend, security, or deployment configuration changes are required.

## Scope Boundaries

- In scope: `experience/src/components/layout/Sidebar.tsx`
- Out of scope: F0037 API, authorization, reporting service, KG feature semantics, and backend behavior.

## Lifecycle Stage

Feature action follow-up, G0 through G8 evidence lifecycle.
