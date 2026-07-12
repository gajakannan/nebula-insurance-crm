# Artifact Trace — F0037 Plan Run 2026-07-06-6e3851ab

## Artifacts Read

- `planning-mds/features/ROADMAP.md`
- `planning-mds/features/REGISTRY.md`
- `planning-mds/features/TRACKER-GOVERNANCE.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/PRD.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/README.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/STATUS.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/GETTING-STARTED.md`
- `planning-mds/features/archive/F0017-broker-mga-hierarchy-and-producer-ownership/PRD.md`
- `planning-mds/features/archive/F0023-global-search-saved-views-and-operational-reporting/PRD.md`
- `planning-mds/architecture/decisions/ADR-008-casbin-enforcer-adoption.md`
- `planning-mds/architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md`
- `planning-mds/architecture/decisions/ADR-026-broker-mga-hierarchy-producer-ownership-and-territory.md`
- `planning-mds/architecture/decisions/ADR-031-broker-insights-read-models.md`
- `planning-mds/architecture/feature-assembly-plan.md`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/schemas/README.md`
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- `engine/src/Nebula.Application/Services/ProjectionVisibilityResolver.cs`
- `engine/src/Nebula.Application/Services/OperationalReportService.cs`
- `engine/src/Nebula.Application/Services/SearchService.cs`
- `engine/src/Nebula.Infrastructure/Repositories/OperationalReportProjectionRepository.cs`
- `engine/src/Nebula.Infrastructure/Repositories/SearchDocumentRepository.cs`
- `experience/src/features/reports/types.ts`
- `experience/src/features/reports/components/ReportControls.tsx`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/knowledge-graph/canonical-nodes.yaml`
- `agents/templates/story-template.md`
- `agents/product-manager/references/vertical-slicing-guide.md`
- `agents/product-manager/scripts/validate-trackers.py`
- `agents/product-manager/scripts/validate-stories.py`

## Artifacts Created Or Updated

- `planning-mds/operations/evidence/runs/2026-07-06-6e3851ab/README.md`
- `planning-mds/operations/evidence/runs/2026-07-06-6e3851ab/action-context.md`
- `planning-mds/operations/evidence/runs/2026-07-06-6e3851ab/artifact-trace.md`
- `planning-mds/operations/evidence/runs/2026-07-06-6e3851ab/gate-decisions.md`
- `planning-mds/operations/evidence/runs/2026-07-06-6e3851ab/commands.log`
- `planning-mds/operations/evidence/runs/2026-07-06-6e3851ab/lifecycle-gates.log`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/PRD.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/README.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/STATUS.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/GETTING-STARTED.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/F0037-S0001-resolve-current-user-distribution-scope.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/F0037-S0002-enforce-hierarchy-aware-read-scoping.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/F0037-S0003-apply-visibility-to-search-views-insights-reports.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/F0037-S0004-add-distribution-rollup-reporting.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/F0037-S0005-add-rollup-filters-panels-and-no-leak-states.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/F0037-S0006-add-security-evidence-and-reconciliation.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/feature-assembly-plan.md`
- `planning-mds/architecture/feature-assembly-plan.md`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/schemas/README.md`
- `planning-mds/schemas/distribution-rollup-report.schema.json`
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- `planning-mds/features/ROADMAP.md`
- `planning-mds/features/STORY-INDEX.md`
- `planning-mds/BLUEPRINT.md`
- `planning-mds/knowledge-graph/canonical-nodes.yaml`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/knowledge-graph/code-index.yaml`
- `planning-mds/knowledge-graph/coverage-report.yaml`
- `agents/product-manager/scripts/validate-trackers.py`
- `agents/product-manager/scripts/tests/test_validate_trackers_plan_mode.py`

## Generated Evidence

- `planning-mds/features/STORY-INDEX.md` regenerated after six F0037 stories were added.
- `planning-mds/knowledge-graph/coverage-report.yaml` regenerated after F0037 was removed from excluded coverage and mapped; regenerated again after Phase B KG nodes and bindings were added.
- Phase B validation confirmed story validation, tracker sync, KG integrity, KG drift, schema JSON syntax, and template validation.
- Validation evidence recorded in `commands.log` and `lifecycle-gates.log`.

## External Or Global Evidence References

- None.

## Omissions And Waivers

- Feature evidence package omitted by design; plan action produces only base run evidence.
- Feature evidence package omitted until G5 approval and the future `feature` action.
- Product implementation source files intentionally unchanged during plan.

## Run Environment

- Absolute cwd: `/Users/srinivasubezawada/Desktop/nebula3/nebula-agents` — sibling framework repo required by the nebula-agents harness.
- Absolute cwd: `/Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm` — product repo root required for product-owned planning and KG commands.
