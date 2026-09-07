# Frontend Quality Evidence Lane

This folder stores the solution-owned manifest consumed by Nebula's lifecycle gate for frontend quality validation.

## Required File

- `latest-run.json`
  - machine-readable manifest for the latest approved frontend validation run
  - must reference the feature evidence package, command logs, lifecycle gate log, story-to-suite mapping, and layer-specific artifacts

## Required Layers

- `component`
- `integration`
- `accessibility`
- `coverage`
- `visual`

The lifecycle gate fails when the manifest is missing, when a required layer is absent, or when the referenced artifacts do not exist.

The 2026-09-07 one-off repository review is recorded under
[`2026-09-07/`](2026-09-07/). Its manifest is revision-bound and reports the
measured coverage floor and explicit sandbox/test-order limitations instead of
carrying forward the stale March record.
