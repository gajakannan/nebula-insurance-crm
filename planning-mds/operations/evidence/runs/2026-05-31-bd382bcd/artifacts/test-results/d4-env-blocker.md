# D4 Test-Execution Environment Blocker

The frontend test/lint/build toolchain **cannot run** in this session's
environment. This is unrelated to the defect or the fix.

## Symptom

`vitest` startup fails:

```
Error: Cannot find module @rollup/rollup-linux-x64-gnu
```

The product repo's `experience/node_modules` was installed on Windows; under WSL
(Linux) the platform-native binaries are absent.

## Repair cycle attempted (one cycle, per contract)

`pnpm --dir experience install --frozen-lockfile --prefer-offline`

The install imports packages from the store successfully (`reused 785`) but
**aborts before the top-level linking phase** with a permission error on pnpm's
atomic temp-dir rename, which fails on the `/mnt/c` Windows DrvFs mount:

```
ERR_PNPM_EACCES  [importPackage .../lighthouse@12.1.0/.../lighthouse]
  EACCES: permission denied, rename '.../lighthouse_tmp_38965_7' -> '.../lighthouse'
```

A second run (after clearing stale `*_tmp_*` dirs) failed identically on a
different package:

```
ERR_PNPM_EACCES  [importPackage .../pino@8.21.0/.../pino]
  EACCES: permission denied, rename '.../pino_tmp_39771_6' -> '.../pino'
```

Because the install aborts before linking, **all** top-level dependency
symlinks are missing (`node_modules/vite`, `node_modules/vitest`,
`node_modules/typescript`, `node_modules/.bin/*` …), so `vitest`, `vite`,
`tsc`, and `eslint` are all unresolvable. The `@rollup/rollup-linux-x64-gnu`
binary IS present in the `.pnpm` store; only linking is blocked.

## Classification

- Root cause of blocker: pnpm cannot complete `node_modules` linking on the
  `/mnt/c` Windows mount under WSL (DrvFs rename permission semantics).
- Relation to defect/fix: **none**. The fix is a self-contained TypeScript
  change to `sessionRenewal.ts`.
- Stop condition: "a required validator or test fails for reasons unrelated to
  the fix after one repair cycle" → test execution is **escalated** to CI /
  a Linux-native install.

## Escalation / how to validate the fix

Run in an environment where `node_modules` is installed natively on Linux (CI,
a non-`/mnt/c` checkout, or `pnpm install` from a Linux-native clone):

```
pnpm --dir experience install
pnpm --dir experience exec vitest run \
  src/features/session-continuity/tests/sessionRenewal.test.ts
pnpm --dir experience lint
pnpm --dir experience build
```

The added regression tests in `sessionRenewal.test.ts` encode the expected
post-fix behavior (transient retry succeeds; terminal causes fail fast). They
are written to fail on the pre-fix code and pass on the fixed code.
