# F0023 Test Plan

## Objective

Revalidate the F0023 search/reporting evidence needed to replace missing historical ignored artifacts and `.kg-state` evidence references.

## Backend

- Build `Nebula.Api`.
- Run SearchReporting-focused unit tests.

## Frontend

- Build with direct `tsc` and `vite` binaries.
- Run focused search component tests with coverage.

## Acceptance

All focused commands must produce run-local artifacts. Product dependency advisories are documented separately in security evidence.
