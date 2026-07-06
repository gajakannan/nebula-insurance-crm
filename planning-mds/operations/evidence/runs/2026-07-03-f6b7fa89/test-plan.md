# Test Plan — F0027 Focused Regression

Scope: `regression`
Date: `2026-07-03`
Mode: `standalone`
Feature: `F0027 — COI, ACORD & Outbound Document Generation`

## Story-to-Test Mapping

| Story | Acceptance Criteria | Test Type | Test Case |
|-------|---------------------|-----------|-----------|
| F0027-S0001 | Admin-governed outbound template families/status are enforced. | Backend unit / policy review | `OutboundDocumentGenerationServiceTests` validates unpublished/invalid template handling and policy gates where covered; policy/config artifacts reviewed through build and evidence validation. |
| F0027-S0002 | User previews generated document before issue. | Backend unit | `OutboundDocumentGenerationServiceTests` preview path validates merge diagnostics and source snapshot hash behavior. |
| F0027-S0002 | Preview controls appear in document panel. | Frontend component | `ParentDocumentsPanel.test.tsx` verifies the Generate Document panel mounting path and user-facing controls. |
| F0027-S0003 | Explicit Issue creates an available generated artifact with provenance. | Backend unit | `OutboundDocumentGenerationServiceTests` issue path validates generated write command and response fields. |
| F0027-S0004 | Regenerate/retrieve preserves prior artifact and creates a new generated artifact. | Backend unit | `OutboundDocumentGenerationServiceTests` regenerate scenario validates prior-link behavior where covered. |
| F0027-S0005 | Proposal rendering uses submission/quote packet context through reusable renderer boundary. | Backend unit / build | Service tests and backend build validate renderer and merge assembler wiring. |
| F0027-S0001-S0005 | API, schema, and authorization wiring stay buildable. | Backend build / KG validation | API build, test build, KG drift/symbol validation, and F0027 evidence validation. |

## Test Types in Scope

- [x] Unit tests: backend generation service.
- [x] Component tests: document panel generation entry point.
- [x] API/build validation: endpoint compilation and OpenAPI wiring.
- [x] Regression validation: F0027 evidence, tracker, KG, template contracts.
- [ ] Browser E2E tests: not available as an implemented F0027 suite in this repository.
- [ ] Live API integration tests: deferred follow-up from F0027 closeout; no endpoint integration test file exists yet.
- [ ] Accessibility tests: deferred to existing frontend quality lanes; no F0027-specific axe/Playwright test exists yet.

## Coverage Targets

- Backend service behavior: existing focused service tests must pass.
- Frontend component behavior: existing focused document-panel component test must pass.
- Build/regression: API build, test build, frontend build, KG validation, and harness validators must pass.
- Coverage percentage: no percentage threshold claimed in this standalone post-closeout run; coverage is artifact-backed where the tool emits it.

## Evidence Artifacts

- Backend test result: `artifacts/test-results/f0027-backend-outbound-tests.trx`
- Backend coverage artifact: `artifacts/coverage/f0027-backend-coverage.cobertura.xml`
- Frontend component test result: `artifacts/test-results/f0027-frontend-parent-documents-panel.json`
- Build and validator results: `commands.log`

## Test Infrastructure

- Backend: .NET/xUnit test project.
- Frontend: Vitest/React Testing Library.
- Runtime limitation: VSTest may require escalation outside the sandbox to bind sockets; this is recorded when it occurs.
- Runtime limitation: full frontend suite has unrelated localStorage/session failures and is not used as the F0027 pass/fail signal.

## Result

T0 PASS. Proceed to execution.
