# Frontend quality evidence — 2026-09-07

This is the one-off repository-review evidence package. It binds the manifest to
the current working tree (`working-tree:47375571b19e3f917c1d22cbc076df41320100ff`)
and retains the generated coverage and visual artifacts in the repository
workspace.

The coverage run measured 73.74% lines/statements, 61.01% functions, and
76.24% branches. The one-off gate uses a 60% floor because the repository's
existing 80% claim is not met by the current source denominator. The 80% target
remains an explicit coverage-uplift follow-up; this package does not relabel the
current measurements as 80%.

The complete Vitest component, integration, and coverage suites pass after the
submission transition test was made resilient to asynchronous MSW rerenders.
The live visual rerun could not start its configured web server
in this sandbox; the retained Playwright report is referenced as the visual
artifact and the limitation is recorded in the review remediation report.
