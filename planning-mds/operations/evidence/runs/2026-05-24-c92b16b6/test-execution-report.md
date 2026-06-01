# Test Execution Report - F0035

**Owner:** Quality Engineer  
**Date:** 2026-05-24  
**Verdict:** PASS

## Results

| Command | Result | Evidence |
| --- | --- | --- |
| Backend focused integration tests in .NET SDK container | PASS, 8 passed | artifacts/test-results/backend-session-continuity.trx |
| Backend focused closeout integration tests in .NET SDK container | PASS, 8 passed | artifacts/test-results/backend-session-continuity-closeout.trx |
| Frontend focused F0035 Vitest suite | PASS, 58 passed | artifacts/test-results/frontend-session-continuity-g3-fixes.xml |
| Frontend focused closeout Vitest suite | PASS, 58 passed | artifacts/test-results/frontend-session-continuity-closeout.xml |
| Frontend focused F0035 Vitest coverage | PASS, 56 passed | artifacts/coverage/frontend-session-continuity/coverage-summary.json |
| Frontend lint | PASS, 0 errors, 3 warnings | commands.log |
| Frontend production build | PASS | commands.log |

## Notes

- The first frontend test attempt failed before startup because Rollup's Linux optional dependency was missing in `experience/node_modules`. The dependency tree was restored with `CI=true pnpm --dir experience install --frozen-lockfile --virtual-store-dir /home/gajap/.pnpm-virtual-store/nebula-crm-experience`, then tests passed.
- The first direct reinstall attempt failed with a Windows-mounted filesystem rename error. The Linux-side pnpm virtual store resolved it.
- Frontend lint warnings are pre-existing in `DocumentUploadDialog.tsx` and `PolicyDetailPage.tsx`; the command exits 0.
- No failing focused backend or frontend F0035 test remains.

## Residual Risk

- End-to-end browser smoke for a real authentik refresh-token issuance path remains a G4.6 lifecycle candidate, not a G2 blocker.
