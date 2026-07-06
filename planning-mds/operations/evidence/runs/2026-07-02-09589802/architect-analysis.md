# Architect Analysis

## Root Cause

`AssigneePicker` uses `GET /users?q=...` through the frontend origin. The local Vite dev proxy did not include `/users`, so browser requests stayed on the frontend server instead of reaching the API. The API endpoint itself was healthy and returned `Sarah Chen` when called directly with a dev token.

## Fix Strategy

Add `/users` to the existing Vite API proxy path list. This is a local runtime wiring fix for an already-approved F0021 dependency on the user search endpoint.

## Risk Assessment

Low. The change only affects development proxy routing and does not alter production code paths, authorization, or API behavior.
