# Frontend Fix Report

## Change
Updated `experience/vite.config.ts`:
- Added `/service-cases` to the local API proxy prefix list.
- Replaced inline proxy config with `createApiProxyOptions`.
- Added a bypass so direct browser navigation requesting HTML loads the React app instead of being proxied to the backend API.

## Route Behavior
- Direct navigation to `http://127.0.0.1:5173/service-cases` returns the React HTML shell.
- The Service Cases page fetch request to `/service-cases?page=1&pageSize=50` returns `200 OK` through the dev proxy.
- The page renders the empty state instead of "Unable to load service cases."

## Frontend Validation
- Focused component test: passed 2 tests.
- Playwright browser check: `hasLoadError=false`, `hasEmptyState=true`.
- Screenshot: `artifacts/screenshots/service-cases-after-fix.png`.
- `lint`: passed with unrelated existing warnings.
- `lint:theme`: passed.
- `build`: passed.
