# Architect Analysis

## Root Cause
The Service Cases page calls the API through same-origin paths (`/service-cases?...`). The local Vite dev proxy forwards known API prefixes to the backend, but `/service-cases` was missing from that list. As a result, the service-case React Query call was handled by the frontend dev server instead of the backend and surfaced as "Unable to load service cases."

## Ownership Boundary
- Backend service-case domain/API behavior was not the defect source. The backend returned `200 OK` for `/service-cases` when called with the frontend dev token.
- Frontend local-dev runtime wiring owns the fix because Vite proxy registration controls whether same-origin API paths reach the backend.

## Fix Strategy
Add `/service-cases` to `apiProxyPaths` in `experience/vite.config.ts`. Because `/service-cases` is also a React route, centralize proxy options and bypass HTML navigation requests without an Authorization header to `/index.html`; authenticated API fetches continue to proxy to the backend.

## Risk Assessment
- Low production risk: this is Vite dev-server configuration, not production API/domain logic.
- Medium local-dev route risk if implemented as a plain prefix proxy; mitigated by the HTML navigation bypass.
- No security boundary change: the frontend still sends the existing dev token, and the backend still enforces authorization.
