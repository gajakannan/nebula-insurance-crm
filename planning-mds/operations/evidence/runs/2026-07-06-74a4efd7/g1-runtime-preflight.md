# G1 Runtime Preflight

## Verdict

PASS

## Runtime

- Docker Postgres was running through the CRM compose stack.
- API health passed at `http://127.0.0.1:5113/healthz`.
- Fixed-code API health passed at `http://127.0.0.1:5114/healthz`.
- Vite dev frontend was started at `http://127.0.0.1:4173/` with `VITE_AUTH_MODE=dev` and `VITE_API_PROXY_TARGET=http://127.0.0.1:5114`.

## Evidence

- Health checks returned HTTP 200.
- Browser E2E used `F0037_E2E_BASE_URL=http://127.0.0.1:4173` and `F0037_API_BASE=http://127.0.0.1:5114`.
