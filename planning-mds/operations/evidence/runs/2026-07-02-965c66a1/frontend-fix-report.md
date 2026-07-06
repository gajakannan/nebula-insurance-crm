# Frontend Fix Report

## Changes

- Updated `experience/vite.config.ts` to include `/communications` in the development API proxy path list.

## Route-Level Behavior

- Before: `/communications?...` was handled by Vite as an SPA fallback and returned `index.html`.
- After: `/communications?...` is proxied to the API and returns JSON for authenticated app requests.

## UX Notes

No component UX was changed. The F0021 empty state now appears because the existing `CommunicationPanel` receives the expected API response.

## Evidence

- `artifacts/communications-panel-empty-state.png`
- Browser smoke confirmed `hasLoadError=false`, `hasEmptyState=true`, and `contentType=application/json; charset=utf-8`.
