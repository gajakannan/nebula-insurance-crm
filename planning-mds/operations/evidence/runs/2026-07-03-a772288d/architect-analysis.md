# Architect Analysis - Defect Run 2026-07-03-a772288d

## Root Cause

The local Vite frontend was started without a local auth mode. `LoginPage` treats an unset `VITE_AUTH_MODE` as an invalid auth configuration and renders "Authentication is not configured. Contact your administrator."

A second local runtime mismatch was present: Vite defaults the API proxy target to `http://localhost:5113`, but this session started the Docker Compose API on `http://localhost:8080`.

## Ownership Boundary

- Frontend/runtime configuration owns `VITE_AUTH_MODE` and the Vite API proxy target.
- Backend/API implementation is healthy and unchanged.
- No product feature scope or auth architecture change is required.

## Fix Strategy

Use the existing local-dev mechanism: create ignored `experience/.env.development.local` with:

- `VITE_AUTH_MODE=dev`
- `VITE_API_PROXY_TARGET=http://localhost:8080`

This keeps production/staging OIDC behavior untouched and aligns with `experience/.env.development.local.example` plus the Docker backend port used by this run.

## Risk Assessment

Low. The file is git-ignored and local only. Production builds are still protected by the existing Vite auth-mode guard that blocks `VITE_AUTH_MODE=dev` in production.
