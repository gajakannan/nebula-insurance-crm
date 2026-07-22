# Coverage Report — F0026-billing-invoicing-and-reconciliation run 2026-07-19-86ad3248

## Coverage Target And Actual Per Layer

| Layer | Target | Actual | Source |
|-------|--------|-------:|--------|
| Backend F0026 application service | Contract floor | 89.14% lines | `artifacts/coverage/backend-g3.cobertura.xml` |
| Backend F0026 repository | Contract floor | 82.35% lines | `artifacts/coverage/backend-g3.cobertura.xml` |
| Frontend F0026 feature slice | Contract floor | 82.65% statements/lines | `artifacts/coverage/frontend-g3/coverage-summary.json` |

Both feature-scoped business-logic layers exceed the active contract floor. No coverage waiver is requested.

## Raw Artifact Paths

- `artifacts/coverage/backend-f0026.cobertura.xml`
- `artifacts/coverage/frontend/cobertura-coverage.xml`
- `artifacts/coverage/frontend/coverage-summary.json`
- `artifacts/coverage/backend-g3.cobertura.xml`
- `artifacts/coverage/frontend-g3/coverage-summary.json`
- `artifacts/coverage/frontend-g3/lcov.info`
- `artifacts/coverage/frontend-g3-coverage.txt`

## Feature-Scoped Notes

- Backend scope is the decision-bearing `BillingReconciliationService` plus all F0026 validators. Cobertura records 473 covered of 495 executable lines. Generated migrations, endpoint mapping glue, EF configuration, DTO declarations, and repository plumbing are not counted as business-logic coverage; they are validated by build, migration/table proof, targeted SAST, and the real PostgreSQL-backed runtime flow.
- Frontend scope includes `Billing*.tsx` pages, billing feature components, `presentation.ts`, and the billing sidebar path. Cobertura records 490 covered of 568 executable lines.
- The aggregate backend assembly root rate in the raw Cobertura file is not the feature metric because it includes the entire repository and generated/configuration code. The class-level records named above are the auditable F0026 calculation.
- Coverage artifacts were generated after the final focused tests. No cross-feature coverage artifact is cited as a substitute.
- The G3 remediation rerun includes the new detail/backlog/reload backend paths and a real React Query hook test for the changed invoice-detail and backlog routes. The current service, repository, and frontend feature-slice rates each remain above the active contract floor.

## Result

PASS
