# Defect Run 2026-07-02-f334b465

## Run Summary

- Defect summary: Local CRM login route reports authentication is not configured.
- Observed behavior: `http://127.0.0.1:5174/login` renders `Authentication is not configured. Contact your administrator.`
- Expected behavior: Local Nebula CRM can be entered for F0021 validation using the approved development auth path or a correctly configured OIDC path.
- Feature references: F0021, F0009.

## Status

Fixed and validated.

## Evidence Index

- `action-context.md`
- `artifact-trace.md`
- `gate-decisions.md`
- `commands.log`
- `lifecycle-gates.log`
- `architect-analysis.md`
- `frontend-fix-report.md`
- `quality-report.md`
- `artifacts/`

## Validation Summary

- `curl -fsS http://127.0.0.1:5174/healthz` returned `Healthy`.
- Focused auth tests passed: 3 files, 20 tests.
- Browser smoke opened `/login`, redirected to `/`, and did not show the authentication misconfiguration alert.
- `pnpm --dir experience lint` passed with 6 pre-existing warnings.
- `pnpm --dir experience lint:theme` passed.
- `pnpm --dir experience build` passed with the existing large chunk warning.

## Open Follow-ups

- `experience/.env.development.local` is intentionally ignored and local-only. New local clones still need this file or equivalent shell environment values.
- Existing lint warnings in F0021/document/LOB/detail-page files remain outside this defect scope.
