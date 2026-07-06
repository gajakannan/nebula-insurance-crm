# Artifact Trace — F0017-broker-mga-hierarchy-and-producer-ownership run 2026-06-07-771a5ef6

> Captures what was read, written, generated, referenced externally, and explicitly omitted/waived.

## Artifacts Read

- `nebula-agents/agents/product-manager/SKILL.md`
- `nebula-agents/agents/templates/prompts/evidence-contract/plan-operator-friendly.md`
- `nebula-agents/agents/product-manager/scripts/validate-feature-evidence.py`
- `planning-mds/features/archive/F0017-broker-mga-hierarchy-and-producer-ownership/PRD.md`
- `planning-mds/features/archive/F0017-broker-mga-hierarchy-and-producer-ownership/STATUS.md`
- `planning-mds/features/archive/F0017-broker-mga-hierarchy-and-producer-ownership/F0017-S0001..S0005-*.md`
- `planning-mds/features/ROADMAP.md`
- `planning-mds/features/REGISTRY.md`
- `planning-mds/architecture/feature-assembly-plan.md`
- `planning-mds/architecture/decisions/ADR-026-broker-mga-hierarchy-producer-ownership-and-territory.md`
- `experience/package.json`
- `experience/src/pages/BrokerDetailPage.tsx`
- `experience/src/pages/tests/BrokerDetailPage.integration.test.tsx`
- `experience/vite.config.ts`
- F0017 backend and frontend implementation paths listed in `evidence-manifest.json.changed_paths`

## Artifacts Created Or Updated

- `README.md`
- `action-context.md`
- `artifact-trace.md`
- `sample_example_F0017.md`
- `reference_notes_F0017.md`
- `gate-decisions.md`
- `commands.log`
- `lifecycle-gates.log`
- `evidence-manifest.json`
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
- `planning-mds/operations/evidence/features/F0017-broker-mga-hierarchy-and-producer-ownership/latest-run.json`
- `planning-mds/features/archive/F0017-broker-mga-hierarchy-and-producer-ownership/**`
- `planning-mds/features/STORY-INDEX.md`
- `planning-mds/features/ROADMAP.md`
- `planning-mds/features/REGISTRY.md`
- `planning-mds/BLUEPRINT.md`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/knowledge-graph/canonical-nodes.yaml`
- `planning-mds/knowledge-graph/coverage-report.yaml`
- `artifacts/test-results/g1-docker-ps.txt`
- `artifacts/test-results/step1-infrastructure-build.log`
- `artifacts/test-results/step1-hierarchy-integration-tests.txt`
- `artifacts/test-results/step1-ownership-integration-tests.txt`
- `artifacts/test-results/step1-territory-integration-tests.txt`
- `.kg-state/F0017-feature.yaml` (reconstructed identity artifact referenced by historical commands.log)
- F0017 G3 repairs in `engine/src/Nebula.Application/Services/DistributionNodeService.cs`, `engine/src/Nebula.Application/Services/TerritoryService.cs`, `engine/src/Nebula.Application/Interfaces/ITerritoryAssignmentRepository.cs`, `engine/src/Nebula.Infrastructure/Repositories/TerritoryAssignmentRepository.cs`, `engine/src/Nebula.Infrastructure/Persistence/Configurations/TerritoryAssignmentConfiguration.cs`, `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260608033854_F0017_DistributionHierarchyOwnershipTerritory.cs`, and `engine/tests/Nebula.Tests/Integration/TerritoryEndpointTests.cs`.
- F0017 same-run UI continuation in `experience/src/pages/BrokerDetailPage.tsx`, `experience/src/pages/tests/BrokerDetailPage.integration.test.tsx`, and `experience/vite.config.ts`.
- F0017 cleanup on 2026-07-03 in `engine/src/Nebula.Api/Nebula.Api.csproj`, `experience/src/pages/tests/BrokerDetailPage.integration.test.tsx`, and the ignored local `.env` used to make Authentik development containers healthy.
- F0017 hierarchy load cleanup on 2026-07-03 in `engine/src/Nebula.Application/Services/BrokerService.cs`, `engine/src/Nebula.Infrastructure/Persistence/DevSeedData.cs`, and `engine/tests/Nebula.Tests/Integration/BrokerEndpointTests.cs`.
- F0017 demo visibility cleanup on 2026-07-03 in `engine/src/Nebula.Application/Services/DistributionNodeService.cs`, `engine/src/Nebula.Application/Services/ProducerOwnershipService.cs`, `engine/src/Nebula.Application/Services/TerritoryService.cs`, `engine/src/Nebula.Infrastructure/Persistence/DevSeedData.cs`, and `experience/src/features/distribution/components/TerritoriesPanel.tsx`.

## Generated Evidence

- `artifacts/test-results/f0017-backend.trx` — current backend integration test TRX
- `artifacts/coverage/f0017-backend-cobertura.xml` — current Cobertura export
- `artifacts/test-results/f0017-frontend-vitest.txt` — current feature-level frontend test summary
- `artifacts/test-results/f0017-broker-detail-distribution-vitest.txt` — Broker Detail route integration for F0017 panels
- `artifacts/test-results/f0017-vite-proxy-smoke.txt` — live Vite proxy smoke for F0017 API prefixes
- `artifacts/test-results/f0017-frontend-runtime-fix-smoke.txt` — local dev-auth and browser-style SPA navigation verification after the F0017 frontend runtime fix
- `artifacts/test-results/f0017-hierarchy-load-fix-smoke.txt` — broker-backed hierarchy node repair, DB verification, and browser smoke for the reported Broker Detail Distribution failure
- `artifacts/test-results/f0017-demo-visibility-smoke.txt` — live UI verification for hierarchy children, territory reassignment history/as-of lookup, ownership sample, and broker Timeline events on `Anchor Advisors 015`
- `sample_example_F0017.md` — operator-facing sample data walkthrough for checking F0017 in the local app
- `reference_notes_F0017.md` — comprehensive internal developer reference covering the F0017 harness journey, implementation flow, validations, repairs, and maintenance guidance
- `artifacts/test-results/frontend-lint.txt` — current frontend lint summary
- `artifacts/test-results/frontend-build.txt` — current frontend build summary
- `/tmp/nebula-playwright-smoke.png` — local Playwright Chromium launch/screenshot smoke after installing the browser binary
- `/tmp/nebula-login-dev-mode-after-proxy.png` — local Playwright screenshot confirming `/login` enters the app through dev auth instead of showing the authentication-configuration error
- `/tmp/nebula-brokers-direct-route-after-proxy.png` — local Playwright screenshot confirming direct navigation to `/brokers` renders the SPA page
- `/tmp/nebula-f0017-hierarchy-distribution-fixed.png` — local Playwright screenshot confirming the reported broker's Distribution tab renders hierarchy instead of the error state
- `/tmp/nebula-f0017-demo-distribution.png` — local Playwright screenshot confirming F0017 demo examples render in the Distribution tab
- `/tmp/nebula-f0017-demo-timeline.png` — local Playwright screenshot confirming F0017 broker timeline events render in the Timeline tab
- `artifacts/test-results/dotnet-sandbox-msbuild-permission.txt` — sandbox failure note for the first .NET attempt
- `artifacts/test-results/f0017-backend-after-g3.trx` — backend integration test result after G3 repair
- `artifacts/coverage/f0017-backend-after-g3-cobertura.xml` — Cobertura export after G3 repair

## External Or Global Evidence References

- Plan-review readiness context: `planning-mds/operations/evidence/runs/2026-07-02-11251b53/plan-review-report.md` (READY).
- No global frontend lane is substituted for feature-level frontend evidence; current feature-level Vitest/lint/build evidence is local to this run.

## Omissions And Waivers

- No required G2 artifact is omitted.
- No coverage waiver is requested at G2. Repository-level coverage is reported as generated by the backend test assembly, while feature confidence is based on targeted integration/component tests.
- Security review is not required at G2/G3 because `security_sensitive_scope=false` and ADR-026 defers hierarchy-aware authorization enforcement to F0037.
- F0037 deferred hierarchy-aware access scoping and rollup reporting remain out of scope; this continuation only wires existing F0017 panels into Broker Detail and fixes dev proxy routing.
- Authentik production/OIDC setup remains out of scope for F0017; the runtime fix uses ignored local `VITE_AUTH_MODE=dev` configuration for development verification only.

## Run Environment

- `commands.log` `cwd` is recorded as repo-relative `{PRODUCT_ROOT}` where possible.
- Absolute cwd: `/Users/wallstreet290/Documents/WS-PR/nebula-insurance-crm` - current product root for the 2026-07-02 resume.
- Absolute cwd: `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm` - historical product root from the original June run.
