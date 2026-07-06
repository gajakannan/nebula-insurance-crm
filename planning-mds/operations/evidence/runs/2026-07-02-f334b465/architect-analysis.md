# Architect Analysis

## Root Cause

The login page correctly fails closed when neither development auth mode nor OIDC configuration is present. The local Vite run had no loaded `VITE_AUTH_MODE`, because the repo only had `.env.development.local.example`; Vite does not load `.env.example`.

After adding the local dev auth file, the app entered the authenticated shell, but API calls still failed because `experience/vite.config.ts` read proxy target values from `process.env` only. Vite config files do not automatically populate `process.env` from `.env.development.local`, so `VITE_API_PROXY_TARGET` remained ignored and the proxy fell back to `http://localhost:5113` instead of the running Docker API at `http://127.0.0.1:8080`.

## Fix Strategy

Keep the F0009 login fail-closed behavior intact. Add local-only dev runtime values and patch Vite config to use `loadEnv(mode, process.cwd(), '')` so env files participate in proxy target resolution.

## Risk Assessment

Low. The code change affects only Vite development/proxy configuration. Production build behavior remains guarded by `nebula-auth-mode-guard`, and `.env.development.local` is ignored by `.gitignore`.

## Ownership Boundary

Frontend/runtime configuration only. No backend, F0021 business logic, authorization policy, or knowledge-graph semantic binding changed.
