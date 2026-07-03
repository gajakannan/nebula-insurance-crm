# F0036 Test Execution Report

## Result

PASS

## Commands Executed

| Command | Exit | Artifact |
|---------|-----:|----------|
| `pnpm --version` | 0 | `artifacts/test-results/pnpm-global-version.txt` |
| `pnpm --dir experience build` | 0 | `artifacts/test-results/frontend-build-pnpm.txt` |
| `pnpm --dir experience exec vitest run src/features/lob-attributes src/features/forms/__tests__/useControlledDirtyTracker.test.ts src/features/forms/__tests__/dualBackend.test.tsx --coverage ...` | 0 | `artifacts/test-results/frontend-lob-attributes-tests-pnpm.txt` |
| `pnpm --dir experience lint` | 0 | `artifacts/test-results/frontend-lint-pnpm.txt` |
| `pnpm --dir experience lint:theme` | 0 | `artifacts/test-results/frontend-lint-theme-pnpm.txt` |
| `pnpm --dir experience lint:effects` | 0 | `artifacts/test-results/frontend-lint-effects-pnpm.txt` |

## Pass/Fail Counts

Focused F0036 Vitest lane: 12 test files passed, 78 tests passed, 0 failed.

## Notes

The build artifact shows `tsc -b && vite build` passed. The Vitest artifact includes component, unit, and a11y coverage for the F0036 dynamic attribute form engine and the shared preservation adapter tests.

## Current-Code Boundary

This execution revalidates current code only. It does not prove the original 2026-05-28 closeout commit.
