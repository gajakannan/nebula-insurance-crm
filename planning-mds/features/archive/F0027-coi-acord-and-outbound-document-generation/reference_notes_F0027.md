# Reference Notes — F0027 COI, ACORD & Outbound Document Generation

This note records the end-to-end F0027 path followed with the `nandini-nebula-agents` harness, from the initial dependency/readiness work through implementation, validation, runtime verification, and the sample examples.

## 1. Harness Identity And Scope

| Item | Value |
|------|-------|
| Product root | `/Users/wallstreet288/Nebula_pr/nebula-insurance-crm` |
| Harness root | `/Users/wallstreet288/Nebula_pr/nandini-nebula-agents` |
| Feature ID | `F0027` |
| Feature slug | `coi-acord-and-outbound-document-generation` |
| Final feature package | `planning-mds/features/archive/F0027-coi-acord-and-outbound-document-generation` |
| Plan run | `2026-07-02-e8a31f35` |
| Feature run | `2026-07-02-b9316621` |
| Feature review run | `2026-07-03-9d22c359` |
| Focused test run | `2026-07-03-f6b7fa89` |

The approved v1 scope was:

- COI generation.
- ACORD generation.
- Reusable proposal template rendering.
- Preview before explicit Issue.
- Admin users manage templates.
- Service/distribution users issue generated artifacts.

Explicitly out of scope:

- F0019 submission workflow ownership changes.
- Rating/pricing computation.
- E-signature.
- Outbound sending.
- OCR/extraction.

## 2. Dependencies And Requirements Installed Or Repaired

Initial runtime requirements used across the work:

- Python 3 for harness scripts, KG validation, tracker validation, and template validation.
- .NET SDK for API build/tests.
- Node/pnpm for frontend build/tests/audit.
- Docker Compose services for runtime preflight.
- `nandini-nebula-agents` harness actions and validators.

Dependency and readiness work recorded in harness evidence:

- Repaired stale KG coverage baseline before formal planning:
  - `python3 scripts/kg/validate.py --write-coverage-report`
  - `python3 scripts/kg/validate.py`
  - `python3 scripts/kg/validate.py --check-drift`
- Installed/refreshed frontend dependencies after dependency scan findings:
  - `pnpm --dir experience update --latest vitest @vitest/coverage-v8 vite @vitejs/plugin-react @tailwindcss/vite @playwright/test @lhci/cli @pact-foundation/pact eslint eslint-plugin-react-hooks eslint-plugin-react-refresh stylelint stylelint-config-standard stylelint-config-tailwindcss typescript-eslint jsdom msw`
  - `pnpm --dir experience update --latest react-router-dom ajv ajv-errors ajv-formats @lhci/cli`
  - `pnpm --dir experience install`
  - Final frontend audit passed: `pnpm --dir experience audit --audit-level high`
- Added backend OpenAPI dependencies to support API contract/openapi behavior:
  - `Microsoft.AspNetCore.OpenApi` version `10.0.9`
  - `Microsoft.OpenApi` version `2.3.12`
- Backend package vulnerability scan was rerun:
  - `dotnet list engine/tests/Nebula.Tests/Nebula.Tests.csproj package --vulnerable --include-transitive --format json`
- Security dependency scan was rerun through the harness:
  - `sh agents/security/scripts/scan-dependencies.sh --frontend-dir ... --backend-dir ... --skip-ai`

Accepted nonblocking dependency note:

- Backend OpenAPI advisory remained documented with waiver/follow-up in security evidence.
- Some unrelated full-repo frontend/test baseline issues were documented outside F0027 and were not treated as F0027 blockers.

## 3. Agents Involved

| Agent / Role | Responsibility In F0027 |
|--------------|--------------------------|
| Product Manager | Phase A requirements, story files, PRD refinement, tracker sync, closeout status. |
| Architect | Phase B architecture, schema/API design, KG ontology alignment, G0 assembly plan, G7 KG reconciliation. |
| Feature Orchestrator | Feature action gate sequencing, evidence package discipline, G5/G6 coordination. |
| Backend Developer | Outbound generation service, DTOs, endpoints, renderer/data assembly integration, backend tests. |
| Frontend Developer | Document panel generation controls, document detail provenance, frontend wiring/tests. |
| Quality Engineer | Focused regression plan, backend/frontend test execution, acceptance coverage notes. |
| Code Reviewer | G3 implementation review and remediation tracking. |
| Security Reviewer | Security scan review, dependency scan disposition, auth/storage/audit risk review. |
| DevOps | Runtime preflight, build/deployability checks, Docker service readiness. |

## 4. Harness Actions And Gates

### Plan Action: `2026-07-02-e8a31f35`

Action contract: `agents/actions/plan.md`

Gates completed:

- G1 Clarification: user selected COI + ACORD + reusable proposal template, Preview then explicit Issue, Admin template editing, service/distribution issuing.
- G2 Tracker Sync: stories, registry, roadmap, blueprint, KG mappings, and tracker alignment validated.
- G3 Phase A Approval: user approved proceeding to architecture.
- G4 Ontology Sync: API schemas, KG coverage, drift checks, tracker checks, and templates validated.
- G5 Phase B Approval: user approved starting feature action.

Plan artifacts generated or updated:

- Refined `PRD.md`.
- Created story files `F0027-S0001` through `F0027-S0005`.
- Updated `STATUS.md`.
- Updated `REGISTRY.md`, `ROADMAP.md`, `BLUEPRINT.md`, `STORY-INDEX.md`.
- Added/updated KG mapping coverage for F0027.
- Added generated-document request/preview/issue schemas.
- Produced plan evidence in `planning-mds/operations/evidence/runs/2026-07-02-e8a31f35/`.

### Feature Action: `2026-07-02-b9316621`

Action contract: `agents/actions/feature.md`

Gates completed:

- G0 PASS: Architect assembly plan validated.
- G1 PASS: Runtime preflight confirmed Docker/API/Auth/DB readiness.
- G2 PASS WITH RECOMMENDATIONS: implementation builds and targeted tests passed.
- G3 PASS WITH RECOMMENDATIONS: code/security review passed after remediation/waivers.
- G4 PASS: user approval recorded.
- G5 PASS WITH RECOMMENDATIONS: required signoffs present.
- G6 PASS: candidate evidence validated.
- G7 PASS: Architect KG reconciliation completed.
- G8 PASS WITH RECOMMENDATIONS: PM closeout, archive move, trackers, latest-run, and KG paths finalized.

Feature artifacts generated:

- `feature-assembly-plan.md`
- `ARCHITECTURE.md`
- `F0027-S0001-template-library-governance.md`
- `F0027-S0002-preview-generated-document.md`
- `F0027-S0003-issue-generated-artifact.md`
- `F0027-S0004-regenerate-and-retrieve-artifacts.md`
- `F0027-S0005-render-proposal-from-submission-packet.md`
- `README.md`
- `STATUS.md`
- `GETTING-STARTED.md`
- Evidence package under `planning-mds/operations/evidence/runs/2026-07-02-b9316621/`
- Latest-run pointer under `planning-mds/operations/evidence/features/F0027-coi-acord-and-outbound-document-generation/latest-run.json`

Implementation areas changed:

- API layer: outbound document endpoints.
- Application layer: generated document DTOs and outbound generation service.
- Infrastructure layer: merge data assembly and PDF rendering boundaries.
- Document service integration: generated artifacts persist as document sidecars/versions.
- Frontend document experience: Generate Document controls in the shared parent Documents panel.
- Document detail experience: generated artifact provenance display.
- Tests: backend outbound generation unit tests and frontend parent document panel test.
- KG/code index: F0027 capability bindings and archived source paths.

### Feature Review: `2026-07-03-9d22c359`

Review result:

- F0027 closeout evidence was reviewed.
- Latest-run was confirmed to point to the archived F0027 package and approved feature evidence.
- F0027-specific backend, frontend, KG, template, and evidence validations passed.
- No F0027 deployment blocker was found.
- Full repository validation still had unrelated historical/legacy issues and was not treated as an F0027 blocker.

### Focused Test Run: `2026-07-03-f6b7fa89`

Result:

- PASS WITH RECOMMENDATIONS.
- Focused F0027 regression passed with no blocking defects.

## 5. Validations Run From Start To End

Planning and tracker validations:

```bash
python3 agents/product-manager/scripts/validate-stories.py /Users/wallstreet288/Nebula_pr/nebula-insurance-crm/planning-mds/features/F0027-coi-acord-and-outbound-document-generation
python3 agents/product-manager/scripts/generate-story-index.py /Users/wallstreet288/Nebula_pr/nebula-insurance-crm/planning-mds/features/
python3 agents/product-manager/scripts/validate-trackers.py --skip-feature-evidence
python3 agents/scripts/validate_templates.py
```

KG and contract validations:

```bash
python3 scripts/kg/validate.py --write-coverage-report
python3 scripts/kg/validate.py
python3 scripts/kg/validate.py --check-drift
python3 scripts/kg/validate.py --check-symbols
python3 scripts/kg/lookup.py --untested feature:F0027
python3 planning-mds/testing/validate-nebula-api-contract.py
python3 -m json.tool planning-mds/schemas/generated-document-request.schema.json
python3 -m json.tool planning-mds/schemas/generated-document-preview-response.schema.json
python3 -m json.tool planning-mds/schemas/generated-document-issue-response.schema.json
```

Feature evidence validations:

```bash
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/wallstreet288/Nebula_pr/nebula-insurance-crm --feature F0027 --run-id 2026-07-02-b9316621 --stage G0
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/wallstreet288/Nebula_pr/nebula-insurance-crm --feature F0027 --run-id 2026-07-02-b9316621 --stage G3
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/wallstreet288/Nebula_pr/nebula-insurance-crm --feature F0027 --run-id 2026-07-02-b9316621 --stage G4
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/wallstreet288/Nebula_pr/nebula-insurance-crm --feature F0027 --run-id 2026-07-02-b9316621 --stage G5
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/wallstreet288/Nebula_pr/nebula-insurance-crm --feature F0027 --run-id 2026-07-02-b9316621 --stage G6
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/wallstreet288/Nebula_pr/nebula-insurance-crm --feature F0027 --run-id 2026-07-02-b9316621 --stage G7
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/wallstreet288/Nebula_pr/nebula-insurance-crm --feature F0027 --stage closeout
python3 agents/product-manager/scripts/validate-trackers.py --feature F0027 --run-id 2026-07-02-b9316621
python3 agents/product-manager/scripts/patch-prior-manifest.py --product-root /Users/wallstreet288/Nebula_pr/nebula-insurance-crm --feature F0027 --new-run-id 2026-07-02-b9316621
```

Build, test, and theme validations:

```bash
dotnet build engine/src/Nebula.Api/Nebula.Api.csproj
dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj
dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter OutboundDocumentGenerationServiceTests --no-build
pnpm --dir experience build
pnpm --dir experience test src/features/documents/tests/ParentDocumentsPanel.test.tsx
pnpm --dir experience exec vitest run src/features/documents/tests/ParentDocumentsPanel.test.tsx --reporter=json --outputFile=../planning-mds/operations/evidence/runs/2026-07-03-f6b7fa89/artifacts/test-results/f0027-frontend-parent-documents-panel.json
pnpm --dir experience lint:theme
```

Security and dependency validations:

```bash
pnpm --dir experience audit --audit-level high
dotnet list engine/tests/Nebula.Tests/Nebula.Tests.csproj package --vulnerable --include-transitive --format json
sh agents/security/scripts/scan-dependencies.sh --frontend-dir /Users/wallstreet288/Nebula_pr/nebula-insurance-crm/experience --backend-dir /Users/wallstreet288/Nebula_pr/nebula-insurance-crm/engine --skip-ai
```

Focused regression result:

- Backend F0027 service tests: `3 passed / 0 failed`.
- Frontend parent document panel test: `1 passed / 0 failed`.
- KG untested check: `untested_count = 0`.

## 6. Runtime And Manual Verification

Runtime used for manual verification:

- Docker dependency services were running and healthy.
- API ran at `http://127.0.0.1:5113`.
- API health endpoint returned `Healthy`.
- Frontend ran at `http://127.0.0.1:5174/`.

Manual UI behavior verified:

- The shared Documents panel renders F0027 generation controls on parent records.
- Empty-template state is correct when no published COI/ACORD/proposal template exists.
- Uploading and saving a regular document works and opens the document detail view.
- The document detail view shows preview, versions, metadata, replace binary, and download controls.
- The uploaded screenshot example is a valid regular document upload/save flow.

Important behavior distinction:

- Generic document upload/save is not the same as F0027 generated artifact issue.
- Upload/save writes `DocumentUploaded` and `DocumentMetadataEdited` timeline events keyed to the `Document` entity.
- F0027 issue/regenerate writes `OutboundDocumentIssued` or `OutboundDocumentRegenerated` timeline events.
- The current Dashboard Activity widget reads only `Broker` timeline events, so uploaded/saved/generated documents do not automatically appear in Dashboard Activity with current wiring.

## 7. Sample Example Created

Created:

- `sample_example_F0027.md`

Purpose:

- Capture the screenshot verification.
- Explain why the empty published-template state is correct.
- Document portability expectations for another contributor.
- Provide sample preview, issue, and regenerate requests/responses.
- Record focused validation commands and results.

Key sample note:

- A generated-document template needs both tags and backend metadata:
  - Tags such as `coi`, `acord`, `proposal`, or `outbound:<family>`.
  - Metadata fields `artifactFamily` and `outboundStatus = published`.
- The generic template upload UI sends classification and tags, but does not currently populate all F0027 outbound metadata fields.

## 8. Current Status

F0027 is implemented, archived, validated, and manually verified for the focused flow.

Current accepted follow-ups:

- Add live API integration tests for preview/issue/regenerate, including auth failures.
- Add browser E2E for preview -> issue -> document detail provenance once seeded published templates are stable.
- Add accessibility coverage for the Generate Document panel.
- Decide separately whether Dashboard Activity should become a general activity feed that includes document/generated-document events.
