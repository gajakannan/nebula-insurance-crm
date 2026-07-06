# Self Review — F0027-coi-acord-and-outbound-document-generation run 2026-07-02-b9316621

## Scope Review

Implemented the F0027 vertical slice described by `feature-assembly-plan.md`: backend DTOs, repository write path, renderer/merge boundaries, outbound generation service, `/outbound-documents` endpoints, runtime generated-document metadata schema, document-panel preview/issue controls, generated artifact provenance display, and focused unit/component validation.

No F0019 submission workflow state is mutated. F0019 remains a source-context boundary through `IOutboundMergeDataAssembler`.

## Acceptance Criteria Review

- S0001 template governance: backend validates outbound template family/status metadata and requires published template evidence before preview/issue. UI filters template choices by outbound family tags.
- S0002 preview: `OutboundDocumentGenerationServiceTests.PreviewAsync_ReturnsReadyPreviewForPublishedTemplate` covers ready preview diagnostics.
- S0003 issue: `OutboundDocumentGenerationServiceTests.IssueAsync_CreatesAvailableGeneratedDocument` covers explicit issue, available generated document write, template use increment, and timeline event.
- S0004 regeneration: endpoint/service path is implemented via `RegenerateAsync`; focused test remains recommended before G6.
- S0005 reusable proposal rendering: family enum and service path include `proposal`; first-release renderer is shared across COI, ACORD, and proposal.

## Implementation Risks

- Renderer is deterministic MVP PDF output, not a production-grade DOCX/PDF merge engine. Mitigation: renderer is isolated behind `IDocumentRenderer` for swap-out.
- Template governance currently reads `artifactFamily`/`outboundStatus` from template metadata, while the UI filters by tags for selection. Mitigation: backend remains authoritative.
- Regenerate path is implemented but not yet covered by a dedicated unit assertion. Mitigation: add targeted test before G6 candidate validation.

## Validation Evidence

- `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj` passed.
- `pnpm --dir experience build` passed.
- `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj` passed after test double update.
- `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter OutboundDocumentGenerationServiceTests --no-build` passed after sandbox-denied attempt was rerun outside sandbox.
- `pnpm --dir experience test src/features/documents/tests/ParentDocumentsPanel.test.tsx` passed after a narrow test setup localStorage guard.
- `pnpm --dir experience lint:theme` passed.

## Recommendations

- [medium] Add a dedicated regenerate unit test and API integration test before G6 — owner: Quality Engineer; follow-up: F0027-G6-test-tightening
- [medium] Replace `SimplePdfDocumentRenderer` with the production renderer adapter before enabling carrier-facing document output — owner: Backend Developer; follow-up: F0027-renderer-hardening

## Result

PASS WITH RECOMMENDATIONS
