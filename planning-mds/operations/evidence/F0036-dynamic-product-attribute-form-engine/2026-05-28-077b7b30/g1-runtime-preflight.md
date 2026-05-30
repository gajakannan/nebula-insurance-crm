# Runtime Preflight — F0036-dynamic-product-attribute-form-engine run 2026-05-28-077b7b30

> `runtime_bearing = true` (the `experience/**/*.test.*` §7 path class is in the change set). This is a **frontend-only** feature; the "runtime" is the `experience/` Node/pnpm toolchain + the vitest/jsdom test runner — no application containers (operator-confirmed: frontend toolchain only; DevOps not required).

## Feature

- Feature ID: F0036
- Run ID: 2026-05-28-077b7b30
- Date: 2026-05-30
- Owner: Feature Orchestrator (frontend toolchain; DevOps not required)

## Runtime Services / Containers / Jobs

| Surface | Kind | Notes |
|---------|------|-------|
| `experience/` frontend toolchain | Node + pnpm | node v24.14.0, pnpm 10.33.0 |
| vitest + jsdom | test runner | unit / component / integration / a11y lanes |
| MSW (`src/mocks/`) | API mock | provides `/lob-schemas/active/...` Cyber bundle, etc. |
| Docker / engine API | N/A | not exercised — frontend-only; backend consumed as F0034/F0035 contracts |

## Command Evidence

Preflight commands (see `commands.log`):

```text
- node --version → v24.14.0
- pnpm --version → 10.33.0
- pnpm install --package-import-method=copy → Done (deps linked)
- pnpm build → exit 0 (tsc -b type-check + vite build)
```

**Runtime-blocked event + restoration (per Failure Triage):** the initial `pnpm install` failed on the WSL `/mnt/c` drvfs mount with `ERR_PNPM_EACCES … rename` in the `.pnpm` store. Classified `runtime-blocked`; restored by re-running with `--package-import-method=copy` (avoids the hardlink/rename path); preflight then re-passed and validation continued unchanged.

## Health Status

| Service | Status | Notes |
|---------|--------|-------|
| experience toolchain (node/pnpm) | healthy | deps linked via copy import method |
| vitest/jsdom runner | healthy | unit + a11y lanes green |
| build (`tsc -b && vite build`) | healthy | exit 0 |

## Restore Steps If Unavailable

On `/mnt/c` (WSL drvfs), if `pnpm install` fails with `ERR_PNPM_EACCES … rename`, re-run `pnpm install --package-import-method=copy`. No docker runtime is required for this feature.

## Result

PASS
