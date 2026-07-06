# Action Context - Defect Run 2026-07-03-a772288d

## Scope

- DEFECT_SUMMARY: Frontend local URL shows "Authentication is not configured. Contact your administrator."
- OBSERVED_BEHAVIOR: Opening `http://localhost:5173/` renders the login splash with an auth configuration error.
- EXPECTED_BEHAVIOR: Local frontend should use development auth bypass and render the protected CRM shell against the running local backend.
- REPRO_STEPS: Start CRM backend with Docker Compose, start frontend with `corepack pnpm --dir experience dev --host 0.0.0.0`, open `http://localhost:5173/`.
- AFFECTED_PATHS: `experience/.env.development.local`, `experience/vite.config.ts`, `experience/src/pages/LoginPage.tsx`, `experience/src/features/auth/*`, frontend dev server runtime.
- AGENT_ROLES: architect, frontend-developer
- FEATURE_REFS: none
- ALLOW_FEATURE_PROPOSAL: false
- Lifecycle Authority: none

## Resolved Paths

- PRODUCT_ROOT: `/Users/msig2/Desktop/work_space/nebula-insurance-crm`
- DEFECT_RUN_FOLDER: `planning-mds/operations/evidence/runs/2026-07-03-a772288d`
