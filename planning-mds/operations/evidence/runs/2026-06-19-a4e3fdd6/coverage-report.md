# Coverage Report — F0023 run 2026-06-19-a4e3fdd6

**Role:** Quality Engineer · **Date:** 2026-06-20 · **Tool:** coverlet (XPlat Code Coverage), cobertura

## Coverage Target And Actual Per Layer

| Layer | Target | Actual (line) | Source |
|-------|--------|---------------|--------|
| Application services (business logic) | ≥80% | **~86%** — SavedViewService ~86%, OperationalReportService ~98%, SearchService ~90%, ProjectionVisibilityResolver 100% | 17 unit tests |
| Validators | ≥80% | exercised via service/endpoint paths; FluentValidation rules unit-reachable | unit |
| Infrastructure repositories (EF/SQL) | ≥80% | **0% via unit tests** (stub-repo unit strategy) — real SQL paths exercised by container route smoke | smoke + deferred integration |
| `SearchProjectionService` (backfill) | ≥80% | **0% via unit tests** — deferred integration coverage | deferred |
| Frontend (search/reports) | n/a (component) | 5 component tests over results list + search trigger | Vitest |

## Raw Artifact Paths

- artifacts/coverage/searchreporting.cobertura.xml
- artifacts/test-results/backend-unit-tests.txt
- artifacts/test-results/frontend-tests.txt

## Feature-Scoped Notes

Feature **logic** (services + visibility resolution) meets the ≥80% target. The data-access layer is intentionally not unit-tested (the codebase's unit strategy stubs repositories); its behavior is verified by the container route smoke (endpoints live + auth-enforced) and is the target of the deferred Testcontainers integration suite.

## Waiver Block

- **Layer waived:** Infrastructure repositories + `SearchProjectionService` automated line coverage.
- **Reason:** Unit tests stub repositories per codebase convention; real EF/SQL coverage requires Testcontainers-Postgres integration tests, which were deferred when the operator chose to proceed to frontend before adding them.
- **Compensating control:** container route smoke (build + migrate + 401/404 route checks); manual review of repository queries at G3.
- **Owner:** Quality Engineer · **Follow-up:** add `tests/Nebula.Tests/Integration/SearchReporting*` (permission-matrix, saved-view CRUD, real visibility predicate) — deferred follow-up, non-blocking.
- **Approved on:** 2026-06-20.

## Result

`PASS` — feature logic coverage ≥80%; data-access coverage waived with compensating smoke control and a tracked integration-test follow-up.
