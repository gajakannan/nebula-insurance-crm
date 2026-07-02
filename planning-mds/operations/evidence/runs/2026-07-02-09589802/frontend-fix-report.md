# Frontend Fix Report

## Changed Paths

- `experience/vite.config.ts`

## Fix

Added `/users` to the Vite dev proxy paths so `AssigneePicker` can load user suggestions during F0021 follow-up creation.

## Validation

- `curl -fsS -H 'Authorization: Bearer <dev-token>' 'http://127.0.0.1:5174/users?q=Sarah'` returned `Sarah Chen`.
- Final focused F0021 E2E passed 5/5 after this fix and the correction persistence fix.
