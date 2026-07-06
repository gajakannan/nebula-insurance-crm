# F0017 Reference Notes

Run: `2026-06-07-771a5ef6`
Feature: `F0017-broker-mga-hierarchy-and-producer-ownership`
Status: `approved` / `Archived`
Audience: maintainers, onboarding developers, QA, and future harness operators

## Purpose

F0017 adds Broker/MGA hierarchy management, producer ownership, territory assignment, and audit/timeline evidence to Nebula CRM. The feature is implemented across backend, frontend, database, test coverage, development seed repair, and harness evidence.

This document captures the end-to-end implementation journey so a future contributor can understand how the feature was planned, built, validated, repaired, demonstrated, and closed through the nebula-agents harness.

## Harness Identity

- Active harness run: `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6`
- Feature at run start: `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership`
- Feature at closeout: `planning-mds/features/archive/F0017-broker-mga-hierarchy-and-producer-ownership`
- Latest-run pointer: `planning-mds/operations/evidence/features/F0017-broker-mga-hierarchy-and-producer-ownership/latest-run.json`
- Manifest: `evidence-manifest.json`
- Command ledger: `commands.log`

Important process rule: the run was resumed from the first incomplete gate after G0/G1 and continued under the existing manifest. No duplicate F0017 run was created.

## Prerequisites

Local development assumes:

- Docker available for database and supporting services.
- .NET SDK compatible with the solution under `engine/Nebula.slnx`.
- `pnpm` installed for the Vite frontend.
- Node/Playwright available for browser smoke verification.
- Local dev auth configuration available for frontend verification.
- The API is started once after code changes so idempotent development seed repair runs.

Runtime ports used during verification:

- Backend API: `http://127.0.0.1:5113`
- Frontend: `http://127.0.0.1:5173`

## Dependencies And Runtime Services

Development runtime used:

- `db`
- `authentik-server`
- `authentik-worker`
- `nebula-api`
- Vite frontend dev server

Dependency/security note:

- `Microsoft.OpenApi` was pinned to `2.9.0`.
- `Microsoft.AspNetCore.OpenApi` was updated to `10.0.5`.
- Restore no longer reports the prior `Microsoft.OpenApi` 2.0.0 advisory in the verified environment.

Non-blocking runtime/build notes:

- Vite reports a large chunk warning.
- Lint passes with existing warnings outside F0017.
- Knowledge graph validation passes with existing repository-wide warnings unrelated to F0017.

## Agents And Roles

Required harness roles recorded in the manifest:

- Architect
- Quality Engineer
- Code Reviewer
- DevOps

Role outcomes:

- Architect: `PASS`
- Quality Engineer: `PASS WITH RECOMMENDATIONS`
- Code Reviewer: `APPROVED WITH RECOMMENDATIONS`
- DevOps: `PASS WITH RECOMMENDATIONS`

Security Reviewer was not forced because `security_sensitive_scope=false`. Hierarchy-aware authorization and rollup behavior were explicitly deferred to F0037 and were not implemented in this run.

## Planning Inputs

Primary planning artifacts:

- Archived PRD and stories under `planning-mds/features/archive/F0017-broker-mga-hierarchy-and-producer-ownership/`
- `planning-mds/features/archive/F0017-broker-mga-hierarchy-and-producer-ownership/feature-assembly-plan.md`
- `planning-mds/architecture/decisions/ADR-026-broker-mga-hierarchy-producer-ownership-and-territory.md`
- `planning-mds/architecture/feature-assembly-plan.md`
- Knowledge graph mappings in:
  - `planning-mds/knowledge-graph/feature-mappings.yaml`
  - `planning-mds/knowledge-graph/canonical-nodes.yaml`
  - `planning-mds/knowledge-graph/coverage-report.yaml`

Harness planning artifacts generated or updated:

- `action-context.md`
- `g0-assembly-plan-validation.md`
- `g1-runtime-preflight.md`
- `g2-self-review.md`
- `test-plan.md`
- `test-execution-report.md`
- `coverage-report.md`
- `deployability-check.md`
- `code-review-report.md`
- `signoff-ledger.md`
- `feature-action-execution.md`
- `kg-reconciliation.md`
- `pm-closeout.md`
- `sample_example_F0017.md`

## Gate Journey

Gate progression:

- G0: assembly plan validation passed.
- G1: runtime preflight passed.
- G2: self-review, test plan, test execution, coverage, and deployability artifacts created.
- G3: code review completed; repairs were performed for territory overlap and descendant/audit concerns.
- G4: operator approval recorded.
- G5: signoff ledger created.
- G6: candidate completion validation active after tracker promotion.
- G7: final validation passed.
- G8: PM closeout approved with recommendations.
- Archive correction: feature folder moved to `planning-mds/features/archive/`; registry, story index, roadmap, and KG coverage refreshed.

Same-run continuations after closeout:

- Broker Detail F0017 panels were wired into the app route.
- Vite proxy was corrected for F0017 API prefixes.
- Local dev-auth/browser navigation issues were cleaned up.
- Broker-backed hierarchy nodes were repaired for seeded databases.
- Demo examples were added for hierarchy children, ownership, territory history, and Timeline visibility.

## Scope Implemented

F0017 implemented these stories:

- `F0017-S0001`: broker/MGA hierarchy model.
- `F0017-S0002`: hierarchy navigation.
- `F0017-S0003`: producer ownership, effective-dated.
- `F0017-S0004`: territory management, effective-dated.
- `F0017-S0005`: hierarchy, ownership, and territory audit/timeline behavior.

Explicitly not implemented:

- F0037 hierarchy-aware authorization.
- F0037 hierarchy rollups.
- New external services or deployment topology.

## Backend Implementation

Backend areas touched:

- Domain entities:
  - `DistributionNode`
  - `ProducerOwnership`
  - `Territory`
  - `TerritoryAssignment`
  - `ActivityTimelineEvent`
- Application DTOs, validators, interfaces, and services.
- Infrastructure EF configurations, migrations, repositories, and dev seed repair.
- API endpoints for hierarchy, ownership, territories, and assignments.

Important behaviors:

- `DistributionNode` supports self-referencing hierarchy.
- Broker and MGA nodes are synchronized from existing Broker/MGA records.
- Broker nodes use `DistributionNode.Id = Broker.Id`.
- MGA nodes use `DistributionNode.Id = MGA.Id`.
- Brokers with `MgaId` become children of their MGA node.
- Hierarchy depth, ancestry path, and child count are maintained.
- Producer ownership supports current and historical as-of lookup.
- Territory assignments are effective-dated.
- Reassigning territory closes the prior open assignment.
- Audit/timeline events are emitted for F0017 mutations.
- Broker-scoped companion timeline events make relevant F0017 activity visible on Broker Detail.

Lifecycle sync:

- Broker create creates a matching root distribution node.
- Broker update syncs display name and active state.
- Broker delete marks the distribution node inactive.
- Broker reactivate marks the distribution node active.

Development seed repair:

- Idempotently creates broker/MGA-backed distribution nodes in already-seeded local databases.
- Adds visible demo examples for `Anchor Advisors 015`.
- Creates sample producer children, ownership, territory history, and timeline activity.

## Database Implementation

F0017 introduced or used these tables:

- `DistributionNodes`
- `ProducerOwnership`
- `Territories`
- `TerritoryAssignments`
- `ActivityTimelineEvents`

Migration:

- `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260608033854_F0017_DistributionHierarchyOwnershipTerritory.cs`

No new migration was required for the later hierarchy-load and demo-visibility repairs because existing F0017 tables already supported the needed data.

## API Surface

F0017 API prefixes:

- `/distribution-nodes`
- `/producer-ownership`
- `/territories`
- `/territory-assignments`

Representative endpoints:

- `GET /distribution-nodes/{nodeId}/ancestors`
- `GET /distribution-nodes/{nodeId}/descendants`
- `PUT /distribution-nodes/{nodeId}/parent`
- `POST /producer-ownership`
- `GET /producer-ownership?scopeType=...&scopeId=...&asOf=...`
- `POST /territories`
- `POST /territories/{territoryId}/members`
- `GET /territories/{territoryId}/members?asOf=...`
- `GET /territory-assignments?memberType=...&memberId=...&asOf=...`

Expected API semantics:

- Self-parent returns validation error.
- Cycle creation is rejected.
- Ancestors return ordered breadcrumb path.
- Descendants support hierarchy navigation.
- Producer reassignment closes previous current ownership.
- Territory reassignment closes previous open territory assignment.
- Audit/timeline payload includes actor context and old/new values where implemented.

## Frontend Implementation

Frontend areas touched:

- `experience/src/features/distribution`
- `experience/src/features/distribution/components/HierarchyPanel.tsx`
- `experience/src/features/distribution/components/OwnershipPanel.tsx`
- `experience/src/features/distribution/components/TerritoriesPanel.tsx`
- `experience/src/pages/BrokerDetailPage.tsx`
- `experience/src/pages/tests/BrokerDetailPage.integration.test.tsx`
- `experience/vite.config.ts`

UI behavior:

- Broker Detail includes a `Distribution` tab.
- Distribution tab renders:
  - hierarchy panel
  - producer ownership panel
  - territories panel
- Hierarchy panel shows breadcrumb and child nodes.
- Ownership panel shows current/as-of owner and assignment controls.
- Territories panel shows current/as-of territory and assignment controls.
- Timeline tab shows F0017 activity events for broker-scoped mutations.

Vite proxy:

- F0017 API prefixes are forwarded to the backend.
- Browser-style SPA navigation still resolves to frontend HTML.
- API calls no longer return the frontend shell.

## Fixes Performed During The Journey

Key same-run fixes:

- Wired F0017 panels into Broker Detail.
- Added Vite proxy entries for F0017 API prefixes.
- Repaired direct browser navigation for proxied SPA routes.
- Enabled local development auth configuration for app verification.
- Corrected backend test solution path from `Nebula.slnx` to `engine/Nebula.slnx` when running from product root.
- Added broker-backed `DistributionNode` sync so Broker Detail can use `broker.id` as `nodeId`.
- Added idempotent dev seed repair for already-seeded databases.
- Added sample producer children to make hierarchy navigation visible.
- Added territory as-of UI control.
- Added broker-scoped timeline events for ownership and territory activity.

## Testing And Validation

Backend focused command:

```bash
dotnet test engine/Nebula.slnx --filter "BrokerEndpointTests|DistributionEndpointTests|ProducerOwnershipEndpointTests|TerritoryEndpointTests"
```

Authoritative result:

```text
40 passed, 0 failed, 0 skipped
```

Frontend focused command:

```bash
pnpm exec vitest run src/pages/tests/BrokerDetailPage.integration.test.tsx src/features/distribution/tests/HierarchyPanel.test.tsx --reporter=verbose -t "renders F0017|HierarchyPanel"
```

Result:

```text
3 passed, 0 failed
```

Build and lint:

```bash
pnpm lint
pnpm build
```

Results:

- Lint: exit 0, with 5 existing warnings outside F0017.
- Build: exit 0, with existing Vite large chunk warning.

Knowledge graph:

```bash
.venv/bin/python scripts/kg/validate.py --check-drift
```

Result:

- PASS, with existing repository-wide warnings.

Harness validators:

```bash
.venv/bin/python agents/product-manager/scripts/validate-trackers.py --product-root ../nebula-insurance-crm --feature F0017
.venv/bin/python agents/product-manager/scripts/validate-feature-evidence.py --product-root ../nebula-insurance-crm --feature F0017 --stage closeout
```

Result:

- PASS.

## Sample Demonstration

Reference sample:

- `sample_example_F0017.md`

Route:

```text
http://127.0.0.1:5173/brokers/e2bb173c-ae3c-431b-bcd6-98f21f04448c
```

Broker:

```text
Anchor Advisors 015
```

Distribution tab should show:

```text
(root) > Acme MGA > Anchor Advisors 015
Anchor Advisors 015 Producer A
Anchor Advisors 015 Producer B
Owner: Anchor Advisors 015 Producer A
Assigned to F0017 Demo - Southeast
```

Territory as-of examples:

```text
2026-02-01 -> F0017 Demo - Northeast
2026-05-01 -> F0017 Demo - Southeast
```

Timeline tab should show:

```text
Territory reassigned from F0017 Demo - Northeast to F0017 Demo - Southeast effective 2026-04-01
Producer ownership assigned to e2bb173c-ae3c-431b-bcd6-98f21f041701 effective 2026-01-01
Dev Seed
```

Screenshots captured during verification:

- `/tmp/nebula-f0017-demo-distribution.png`
- `/tmp/nebula-f0017-demo-timeline.png`

## Portability For Another Contributor

Another contributor should see the same behavior if they:

1. Pull the feature implementation.
2. Restore backend/frontend dependencies.
3. Start required Docker services.
4. Apply migrations.
5. Start `Nebula.Api` once so idempotent dev seed repair runs.
6. Start the Vite frontend.
7. Use local dev auth/proxy configuration.
8. Open the sample broker route and inspect Distribution and Timeline tabs.

The sample data is intentionally idempotent and development-oriented. It should not require manual SQL edits.

## Maintenance Notes

When modifying F0017:

- Keep `Broker.Id` and broker-backed `DistributionNode.Id` aligned.
- Preserve effective-dated semantics for producer ownership and territory assignments.
- Ensure reassignments close prior open periods.
- Keep timeline events broker-visible when the user action is performed from Broker Detail.
- Do not add F0037 authorization/rollup behavior under F0017 maintenance.
- Re-run focused backend tests after service/repository changes.
- Re-run Broker Detail and distribution component tests after frontend changes.
- Re-run KG and harness closeout validators after evidence or tracker changes.

## Evidence Map

Primary evidence files:

- `evidence-manifest.json`
- `commands.log`
- `artifact-trace.md`
- `test-execution-report.md`
- `deployability-check.md`
- `sample_example_F0017.md`
- `artifacts/test-results/f0017-demo-visibility-smoke.txt`
- `artifacts/test-results/f0017-hierarchy-load-fix-smoke.txt`
- `artifacts/test-results/f0017-broker-detail-distribution-vitest.txt`
- `artifacts/test-results/f0017-vite-proxy-smoke.txt`

Primary implementation paths:

- `engine/src/Nebula.Domain/Entities`
- `engine/src/Nebula.Application/Services`
- `engine/src/Nebula.Application/Interfaces`
- `engine/src/Nebula.Infrastructure/Persistence`
- `engine/src/Nebula.Infrastructure/Repositories`
- `engine/src/Nebula.Api/Endpoints`
- `engine/tests/Nebula.Tests/Integration`
- `experience/src/features/distribution`
- `experience/src/pages/BrokerDetailPage.tsx`
- `experience/vite.config.ts`

## Current Verdict

F0017 is implemented, archived, validated, and demonstrable through the local app. The feature is good for continued QA and maintenance within its approved scope.
