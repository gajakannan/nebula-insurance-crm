# Feature Evidence README — F0017-broker-mga-hierarchy-and-producer-ownership run 2026-06-07-771a5ef6

> `{PRODUCT_ROOT}/planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/README.md` (§8).

## Run Summary

`feature` action for **F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management**, a clean first run (no prior approved run). The run was resumed on 2026-07-02 from the first incomplete gate after G0/G1, without creating a duplicate feature run. The July 2 plan-review rerun (`2026-07-02-11251b53`) returned READY and is referenced as readiness context only; this manifest remains the active implementation evidence run.

The built slice spans `engine/` (self-referencing hierarchy + cached ancestry, effective-dated producer ownership, effective-dated territory management with overlap prevention, audit/timeline, EF Core migration) and `experience/` (distribution hierarchy/ownership/territory panels).

## Status

`approved` / `Archived` — agrees with `evidence-manifest.json` `status` and `feature_state`. Transitions: `draft`@G0 → `in-progress`@G1-G7 → `approved`@G8; archive correction completed 2026-07-03 under the same run. A same-run continuation on 2026-07-03 resolved the post-closeout F0017 UI integration gaps by wiring the existing distribution panels into Broker Detail and adding the missing Vite proxy prefixes.

## Evidence Index

- `evidence-manifest.json` — schema v1 (§11)
- `action-context.md` — Run Identity, Inputs, Assumptions, Scope Boundaries, Lifecycle Stage
- `artifact-trace.md` — read/written artifacts
- `gate-decisions.md` — pass/fail/skip per gate row (§17 stage matrix)
- `commands.log` — JSON Lines per §13
- `lifecycle-gates.log` — lifecycle gate run summary
- `g0-assembly-plan-validation.md` — G0 Architect assembly-plan validation
- `g1-runtime-preflight.md` — G1 runtime preflight
- `g2-self-review.md` — G2 self-review
- `test-plan.md` — G2 QE plan
- `test-execution-report.md` — G2 test execution report
- `coverage-report.md` — G2 coverage report
- `deployability-check.md` — G2 deployability report
- `artifacts/test-results/f0017-backend.trx` — current backend integration test result
- `artifacts/coverage/f0017-backend-cobertura.xml` — current backend Cobertura output
- `artifacts/test-results/f0017-frontend-vitest.txt` — current feature-level frontend test result
- `artifacts/test-results/f0017-broker-detail-distribution-vitest.txt` — Broker Detail route integration test for F0017 panels
- `artifacts/test-results/f0017-vite-proxy-smoke.txt` — live Vite proxy smoke for F0017 API prefixes
- `artifacts/test-results/frontend-lint.txt` — current frontend lint result
- `artifacts/test-results/frontend-build.txt` — current frontend build result

## Validation Summary

- **G0** `validate-feature-evidence.py --stage G0` → exit 0 (PASS). Assembly plan authored + validated.
- **G1** `validate-feature-evidence.py --stage G1` → exit 0 (PASS). Runtime preflight green.
- **Current backend validation** `dotnet test Nebula.slnx --filter "DistributionEndpointTests|ProducerOwnershipEndpointTests|TerritoryEndpointTests"` → exit 0; **24 passed, 0 failed, 0 skipped** after G3 repair.
- **Current frontend feature validation** `pnpm exec vitest run src/features/distribution/tests/HierarchyPanel.test.tsx --reporter=verbose` → exit 0; **2 passed, 0 failed**.
- **Current Broker Detail F0017 UI validation** `pnpm exec vitest run src/pages/tests/BrokerDetailPage.integration.test.tsx src/features/distribution/tests/HierarchyPanel.test.tsx --reporter=verbose -t "renders F0017|HierarchyPanel"` → exit 0; **3 passed, 0 failed** for the F0017-filtered route/component coverage.
- **Current Vite proxy smoke** F0017 route prefixes through `http://127.0.0.1:5173` → backend responses observed; authenticated `/distribution-nodes/{nodeId}/ancestors` returned `200 application/json`.
- **Current frontend lint** `pnpm lint` → exit 0; 0 errors, 5 warnings outside the F0017 distribution slice.
- **Current frontend build** `pnpm build` → exit 0; production build completed with a large chunk warning.
- **G2** `validate-feature-evidence.py --stage G2` → exit 0 (PASS; completion validation skipped because registry still marks F0017 Planned).
- **G3** `validate-feature-evidence.py --stage G3` → exit 0 (PASS; completion validation skipped because registry still marks F0017 Planned). Code review completed with territory overlap and descendant audit repairs.
- **G4** `validate-feature-evidence.py --stage G4` → exit 0 (PASS; completion validation skipped because registry still marks F0017 Planned). Operator approval received in chat (`approve`) and recorded in `gate-decisions.md`.
- **G5** `validate-feature-evidence.py --stage G5` → exit 0 (PASS; completion validation skipped because registry still marks F0017 Planned). Signoff ledger created and recommendations accepted as non-blocking follow-ups.
- **G6** `validate-feature-evidence.py --stage G6` → exit 0 (PASS; full completion-evidence validation active after tracker promotion).
- **G7** `validate-feature-evidence.py --stage G7` → exit 0 (PASS).
- **G8** `validate-feature-evidence.py --stage G8` → exit 0 (PASS). PM closeout approved with recommendations.
- **Final tracker validation** `validate-trackers.py --feature F0017` → exit 0 (PASS).
- **G8 archive correction** moved F0017 to `planning-mds/features/archive/F0017-broker-mga-hierarchy-and-producer-ownership/`, regenerated `STORY-INDEX.md` and `coverage-report.yaml`, ran KG drift check, patched prior manifests idempotently, refreshed `latest-run.json`, and re-ran final closeout validation.
- **Same-run UI continuation validation** `validate-trackers.py --feature F0017` and `validate-feature-evidence.py --feature F0017 --stage closeout` → exit 0 (PASS).

## Open Follow-ups

- **Dependency advisory.** .NET restore reports `Microsoft.OpenApi` 2.0.0 high severity advisory GHSA-v5pm-xwqc-g5wc. This is not introduced uniquely by F0017 but should be remediated before final release signoff.
- **Frontend bundle size.** Vite reports the main JS chunk is larger than 500 kB after minification. Not F0017-blocking at G2, but should remain a release-hardening follow-up.
- **Branch migration-snapshot drift (pre-existing).** Prior run notes state the F0017 migration was hand-scoped to its four tables and the broader snapshot drift belongs to separate branch reconciliation.
- **Signoff carry-forward.** Accepted recommendations for the OpenAPI advisory, frontend chunk warning, broader frontend panel coverage, and migration snapshot follow-up remain non-blocking post-closeout follow-ups.
- **Pre-existing Broker Detail test gap.** A broad `BrokerDetailPage.integration.test.tsx` run still exposes an unrelated contact-edit assertion failure; the F0017-specific Broker Detail distribution test passes and this issue is outside the approved F0017 continuation scope.
