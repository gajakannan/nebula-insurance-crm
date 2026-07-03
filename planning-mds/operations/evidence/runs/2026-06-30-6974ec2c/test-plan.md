# F0036 Test Plan

## Scope

Validate the current F0036 frontend implementation paths:

- `experience/src/features/lob-attributes`
- `experience/src/features/forms`

## Commands

| Command | Purpose | Artifact |
|---------|---------|----------|
| `pnpm --dir experience build` | TypeScript and production build preflight | `artifacts/test-results/frontend-build-pnpm.txt` |
| `pnpm --dir experience exec vitest run src/features/lob-attributes src/features/forms/__tests__/useControlledDirtyTracker.test.ts src/features/forms/__tests__/dualBackend.test.tsx --coverage --coverage.reportsDirectory=../planning-mds/operations/evidence/runs/2026-06-30-6974ec2c/artifacts/coverage/frontend-lob-attributes` | Focused F0036 unit/component/a11y/preservation coverage | `artifacts/test-results/frontend-lob-attributes-tests-pnpm.txt` |
| `pnpm --dir experience lint` | Frontend lint | `artifacts/test-results/frontend-lint-pnpm.txt` |
| `pnpm --dir experience lint:theme` | Theme guard | `artifacts/test-results/frontend-lint-theme-pnpm.txt` |
| `pnpm --dir experience lint:effects` | Hardcoded effects guard | `artifacts/test-results/frontend-lint-effects-pnpm.txt` |

## Story Mapping

Stories S0001-S0008 are covered by the focused LOB engine/component tests plus shared preservation adapter tests. Historical full-lane and integration detail remains in prior run `2026-05-28-077b7b30`; this remediation captures missing durable artifacts for the current code state.
