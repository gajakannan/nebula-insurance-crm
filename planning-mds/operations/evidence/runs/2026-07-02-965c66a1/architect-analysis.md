# Architect Analysis

## Root Cause

F0021 communication panels call the same-origin API path `/communications?entityType=...&entityId=...`. The Vite development proxy did not include `/communications`, so Vite handled the request as an SPA fallback and returned `index.html` with HTTP 200. The frontend then failed JSON parsing and rendered `Unable to load communications.`

## Fix Strategy

Add `/communications` to the existing `apiProxyPaths` list in `experience/vite.config.ts`. Do not alter backend contracts, authorization policy, data model, or communication UI behavior.

## Risk Assessment

Low. This is a development proxy route fix for an already implemented F0021 API group. It makes the local dev server route match the backend endpoint already mapped by `app.MapCommunicationEndpoints()`.

## Ownership Boundary

Frontend runtime configuration only. No feature closeout evidence, `latest-run.json`, or knowledge-graph semantic binding changes are required.
