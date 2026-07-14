# F0025 Runtime Validation - 2026-07-07

## Scope

Runtime validation for F0025 Commission Producer Splits and Revenue Tracking after source-level closeout.

## Harness Commands

- `python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/msig4/Documents/NEBULA/nebula-insurance-crm --feature F0025 --stage closeout`
- `python3 agents/product-manager/scripts/validate-trackers.py --product-root /Users/msig4/Documents/NEBULA/nebula-insurance-crm --feature F0025 --run-id 2026-07-07-9859bad4`

## Verification Results

- Feature evidence validation: PASS.
- Tracker validation: PASS, 0 errors, 0 warnings.
- Backend build: PASS, 0 errors.
- Focused backend tests: PASS, 22 passed.
- Frontend F0025 tests: PASS, 2 passed.
- Frontend theme guard: PASS.
- Docker API health after rebuild: PASS, `GET /healthz` returned 200 Healthy.
- F0025 API route reachability after rebuild: PASS. Protected routes return 401 `invalid_token` without credentials, confirming the rebuilt API serves the F0025 route table.
- Vite proxy reachability after update: PASS. `/expected-commissions` and `/revenue-attribution/rollups` reach Kestrel through the frontend dev server.

## Runtime Findings

- The previously running API container was stale and returned 404 for F0025 routes. Rebuilt and recreated the `api` service from current source with `docker compose up -d --build api`.
- The frontend Vite proxy did not include F0025 route prefixes. Added proxy prefixes for `/expected-commissions`, `/commission-schedules`, `/commission-adjustments`, `/producer-splits`, and `/revenue-attribution`.

## Current Local URLs

- Frontend: `http://127.0.0.1:5173/`
- API: `http://127.0.0.1:8080/`
- API health: `http://127.0.0.1:8080/healthz`
