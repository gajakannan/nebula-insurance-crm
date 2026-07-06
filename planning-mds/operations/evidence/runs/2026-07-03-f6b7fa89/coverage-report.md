# Coverage Report — F0027 Focused Regression

Scope: `regression`
Date: `2026-07-03`
Result: `PASS WITH WAIVER`

## Coverage Target And Actual

| Layer | Target | Actual | Artifact |
|-------|--------|--------|----------|
| Backend F0027 service behavior | Focused `OutboundDocumentGenerationServiceTests` pass | 3/3 tests passed | `artifacts/test-results/f0027-backend-outbound-tests.trx` |
| Backend Cobertura | Artifact required, percentage not gated for filtered standalone run | Cobertura emitted; assembly-level line-rate `0.0135`, branch-rate `0.0057` | `artifacts/coverage/f0027-backend-coverage.cobertura.xml` |
| Frontend component behavior | Focused document-panel test pass | 1/1 test passed | `artifacts/test-results/f0027-frontend-parent-documents-panel.json` |
| KG public-surface untested check | No untriaged public symbols for F0027 | `untested_count=0` | `commands.log` |

## Waiver

Coverage percentage is not used as a quality gate for this standalone post-closeout focused regression run.

- Reason: `dotnet test --filter OutboundDocumentGenerationServiceTests` emits Cobertura with a full-assembly denominator, so the percentage is not a meaningful F0027-only coverage measure.
- Owner: Quality Engineer
- Approved on: 2026-07-03
- Scope: This standalone focused test run only.
- Follow-up: Add dedicated F0027 endpoint/browser/a11y suites and a feature-scoped coverage measurement if the team wants a numeric F0027 coverage gate.

## Result

PASS WITH WAIVER. Required coverage artifact exists, the executed F0027 tests passed, and the KG untested-surface check reports no untested public symbols for `feature:F0027`.
