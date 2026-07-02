# Frontend Fix Report

## Changes

- Updated `experience/vite.config.ts` to import `loadEnv` and resolve `NEBULA_API_PROXY_TARGET` / `VITE_API_PROXY_TARGET` from both shell environment and Vite env files.
- Added ignored local file `experience/.env.development.local`:
  - `VITE_AUTH_MODE=dev`
  - `VITE_API_PROXY_TARGET=http://127.0.0.1:8080`

## Route-Level Behavior

- Before: `/login` rendered `Authentication is not configured. Contact your administrator.`
- After: `/login` uses dev auth mode and redirects to `/` for local testing.

## UX Notes

No visible UI component was changed. The login page's configured and misconfigured states remain unchanged; only local runtime configuration was corrected.

## Evidence

- `artifacts/login-dev-mode-smoke.png`
- `curl -fsS http://127.0.0.1:5174/healthz` returned `Healthy`.
- Focused auth tests passed.
