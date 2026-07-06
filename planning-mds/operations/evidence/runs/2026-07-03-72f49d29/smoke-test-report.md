# Smoke Test Report — F0024

## Scope

- Feature: F0024 Claims & Service Case Tracking.
- Harness run: `2026-07-03-72f49d29`.
- Local frontend: `http://127.0.0.1:5173`.
- API reachability check: `http://localhost:8080/service-cases?page=1&pageSize=5`.

## Smoke Checks

| Check | Result | Evidence |
|---|---|---|
| API listener reachable | PASS | Unauthenticated `GET /service-cases?page=1&pageSize=5` returned expected `401 invalid_token`, proving the service is listening and auth enforcement is active. |
| Frontend route serves React app | PASS | `GET /service-cases` with `Accept: text/html` returned Vite `index.html`. |
| Workspace smoke | PASS | Playwright rendered Service Cases, row data, filters, account/policy/owner/claim columns, and workspace create modal. |
| Detail smoke | PASS | Playwright rendered detail, work-management edit, communication link, status transition to Waiting, and history entry. |

## Command

```bash
npm exec playwright -- test tests/visual/f0024-smoke.spec.ts --config=playwright.f0024.config.ts
```

Result: 2 passed, 0 failed.

## Artifacts

- `experience/playwright-report/index.html`
- `experience/playwright-report/data/a461cba3cb2f84da1b467d8c0e2cb52187bbc99a.png`
- `experience/playwright-report/data/f25a1dbcd00c19ac150ea663da6cf34f481f116c.png`

## Notes

- The in-app browser bridge was attempted first but returned a sandbox metadata error before opening a tab, so the smoke used the repo's Playwright dependency.
- The status dropdown is currently selected in smoke by option text because the control does not expose a dedicated visible label. This is not blocking the smoke pass, but it is a useful accessibility hardening follow-up.
