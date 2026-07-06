# Artifact Trace — F0027 Feature Review

## Reviewed Feature Evidence

- `planning-mds/operations/evidence/features/F0027-coi-acord-and-outbound-document-generation/latest-run.json`
- `planning-mds/operations/evidence/runs/2026-07-02-b9316621/evidence-manifest.json`
- `planning-mds/operations/evidence/runs/2026-07-02-b9316621/pm-closeout.md`
- `planning-mds/operations/evidence/runs/2026-07-02-b9316621/test-execution-report.md`
- `planning-mds/operations/evidence/runs/2026-07-02-b9316621/code-review-report.md`
- `planning-mds/operations/evidence/runs/2026-07-02-b9316621/security-review-report.md`
- `planning-mds/operations/evidence/runs/2026-07-02-b9316621/deployability-check.md`

## Reviewed Feature Artifacts

- `planning-mds/features/archive/F0027-coi-acord-and-outbound-document-generation/PRD.md`
- `planning-mds/features/archive/F0027-coi-acord-and-outbound-document-generation/STATUS.md`
- `planning-mds/features/archive/F0027-coi-acord-and-outbound-document-generation/feature-assembly-plan.md`

## Review Outputs

- `planning-mds/operations/evidence/runs/2026-07-03-9d22c359/feature-review-report.md`
- `planning-mds/operations/evidence/runs/2026-07-03-9d22c359/artifacts/test-results/`
- `planning-mds/operations/evidence/runs/2026-07-03-9d22c359/artifacts/validation/`

## Validation Commands

- `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj`
- `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj`
- `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter OutboundDocumentGenerationServiceTests --no-build`
- `pnpm --dir experience build`
- `pnpm --dir experience test src/features/documents/tests/ParentDocumentsPanel.test.tsx`
- `pnpm --dir experience lint:theme`
- `python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/wallstreet288/Nebula_pr/nebula-insurance-crm --feature F0027 --stage closeout`
- `python3 agents/product-manager/scripts/validate-trackers.py --feature F0027 --run-id 2026-07-02-b9316621`
- `python3 scripts/kg/validate.py --check-symbols`
- `python3 scripts/kg/validate.py --check-drift`
- `python3 agents/scripts/validate_templates.py`
- `pnpm --dir experience test`
- `python3 agents/product-manager/scripts/validate-trackers.py`
