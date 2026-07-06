# Artifact Trace — F0028 run 2026-07-02-736e7854

## Artifacts Read

- `agents/actions/feature.md`
- `agents/templates/feature-assembly-plan-template.md`
- `agents/templates/*evidence*`
- `planning-mds/operations/evidence/runs/2026-07-02-0e5b0cce/gate-decisions.md`
- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/PRD.md`
- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/ARCHITECTURE.md`
- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/F0028-S*.md`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/api/nebula-api.yaml`
- Existing backend endpoint/service/repository/EF patterns under `engine/src/**`
- Existing frontend routing/API/page patterns under `experience/src/**`

## Artifacts Created Or Updated

- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/feature-assembly-plan.md`
- `planning-mds/architecture/feature-assembly-plan.md`
- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/STATUS.md`
- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/README.md`
- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/ARCHITECTURE.md`
- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/F0028-S*.md`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/schemas/carrier-*.schema.json`
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- `planning-mds/knowledge-graph/canonical-nodes.yaml`
- `planning-mds/knowledge-graph/code-index.yaml`
- `planning-mds/knowledge-graph/coverage-report.yaml`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `engine/src/Nebula.Domain/Entities/Carrier*.cs`
- `engine/src/Nebula.Application/DTOs/CarrierMarketDtos.cs`
- `engine/src/Nebula.Application/Interfaces/ICarrierMarketRepository.cs`
- `engine/src/Nebula.Application/Services/CarrierMarketService.cs`
- `engine/src/Nebula.Application/Validators/CarrierMarketValidators.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Configurations/CarrierMarketConfiguration.cs`
- `engine/src/Nebula.Infrastructure/Repositories/CarrierMarketRepository.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260702151138_F0028_CarrierMarketRelationshipManagement*.cs`
- `engine/src/Nebula.Api/Endpoints/CarrierMarketEndpoints.cs`
- `engine/tests/Nebula.Tests/Integration/CarrierMarketEndpointTests.cs`
- `engine/tests/Nebula.Tests/Unit/CasbinAuthorizationServiceTests.cs`
- `experience/src/features/carrier-markets/**`
- `experience/src/pages/CarrierMarketsPage.tsx`
- `experience/src/App.tsx`
- `experience/src/components/layout/Sidebar.tsx`
- `experience/src/App.test.tsx`
- `planning-mds/operations/evidence/runs/2026-07-02-736e7854/*`

## Generated Evidence

- EF migration: `20260702151138_F0028_CarrierMarketRelationshipManagement`
- Coverage attachment from focused backend test run under `engine/tests/Nebula.Tests/TestResults/**/coverage.cobertura.xml`
- Refreshed KG coverage report at `planning-mds/knowledge-graph/coverage-report.yaml`
- Archive correction moved feature planning artifacts to `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/`.

## Post-Closeout Defect Fix — 2026-07-03

- Fixed local development auth reproducibility for F0028 Markets by centralizing auth-mode resolution in `experience/src/features/auth/authMode.ts`.
- Local Vite development now falls back to dev auth when `VITE_AUTH_MODE` is unset, preventing stale OIDC browser sessions from sending invalid bearer tokens to `/carrier-markets`.
- Added idempotent F0028 carrier-market demo fixtures to `engine/src/Nebula.Infrastructure/Persistence/DevSeedData.cs` so fresh and existing development databases converge on visible sample markets.
- Verified `/carrier-markets` through the local frontend proxy with a synthetic dev bearer token returned `200` and three demo markets.
- Added a local-development retry guard in `experience/src/services/api.ts` so `/carrier-markets` invalid-token responses retry once with the deterministic dev token before forced reauth.
- Restarted the stale Vite dev server on port `5173`; the restarted proxy returned `200` with the three F0028 demo markets.

## Reference Notes — 2026-07-03

- Added `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/reference_notes_F0028.md` as a read-only handoff summary from initial F0028 harness setup through sample verification scenarios.

## External Or Global Evidence References

- Runtime smoke checks used local Docker Compose services for db, Authentik, and API.
- No external carrier API, rating engine, or third-party market sync evidence is in scope.

## Omissions And Waivers

- Full backend/frontend regression suites were not run; focused F0028 and authorization validation passed.
- Dedicated CI scanner automation for dependency/secrets/SAST/DAST is not configured locally; scanner limitations are recorded in `security-review-report.md`.
- Existing `Microsoft.OpenApi 2.0.0` NU1903 advisory is deferred outside F0028.

## Run Environment (conditional)

- Absolute cwd: `/Users/wallstreet289/Documents/workspace/nebula-insurance-crm-sagar` — local product repository.
- Absolute cwd: `/Users/wallstreet289/Documents/workspace/nebula-agents-sagar` — local harness repository.
