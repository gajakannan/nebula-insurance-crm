# Coverage Report - F0025 run 2026-07-07-9859bad4

## Backend Coverage

- Focused backend test command passed 22 tests covering commission validators and Casbin commission policy rows.
- Coverage attachment from passing backend run: `engine/tests/Nebula.Tests/TestResults/67e8f260-23ef-4c67-99e8-72a554d9e5b4/coverage.cobertura.xml`.

## Frontend Coverage

- Feature-local Vitest file `experience/src/features/commissions/tests/CommissionsPage.test.tsx` passed 2 tests:
  - workspace record and rollup rendering
  - detail recalculation mutation wiring

## Known Gaps

- No full e2e browser journey yet for search -> schedule -> split -> calculate -> adjustment -> rollup. This remains a QE task for G3/G6.
- No dedicated unit test yet for revenue projection refresh internals; behavior is build-validated and should receive code-review/QE attention.
