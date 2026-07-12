# F0032 E2E Test Plan

## Harness

- Product root: `/Users/wallstreet123/Desktop/nebula-workspace-2/nebula-insurance-crm`
- Framework root: `/Users/wallstreet123/Desktop/nebula-workspace-2/nebula-agents`
- Evidence run: `2026-07-06-f0ef8526`
- Command log: `planning-mds/operations/evidence/runs/2026-07-06-f0ef8526/commands.log`

All execution commands must be recorded with `nebula-agents/agents/scripts/append-command-log.py`.

## Scope

Exercise F0032 end to end across API, Vite proxy, admin authorization, domain catalog, draft/update/validate/compare/publish/rollback, audit filters, and harness closeout validation.

## Test Matrix

| Area | Scenarios | Expected Result |
|------|-----------|-----------------|
| Runtime readiness | API health and Vite `/admin` proxy | API healthy; unauthenticated `/admin` returns API ProblemDetails, not Vite HTML. |
| Authorization | Admin token and non-Admin token | Admin can access catalog; non-Admin is blocked. |
| Catalog | List domains and detail | Four supported domains render/return: queue-routing, workflow-sla-thresholds, search-report-defaults, template-metadata. |
| Draft lifecycle | Create draft, update payload with reason, stale/blank reason guards | Draft is created/updated with row version and audit evidence; invalid requests are rejected. |
| Validation/compare | Validate current draft and compare against published state | Passed validation, compare summary returned, stale validation blocks publish after payload change. |
| Publish | Publish validated draft | New published version created, refresh status persisted, audit event appended. |
| Rollback | Roll back to previous published version | Rollback creates a new published version without mutating history. |
| Audit | Query audit by domain/action/outcome and inspect details | Events exist for draft, validation, publish, rollback with structured summaries. |
| Regression | Focused tests and builds | Backend/frontend builds and focused tests pass. |
| Harness closeout | Evidence and tracker validation | nebula-agents validators pass after E2E evidence updates. |

## Go Criteria

- No blocking E2E failures.
- Publish and rollback pass in a seeded local test runtime.
- Audit evidence proves the lifecycle.
- Harness evidence and tracker validators pass.
