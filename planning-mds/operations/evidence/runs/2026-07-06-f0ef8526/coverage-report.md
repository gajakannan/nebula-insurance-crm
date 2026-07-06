# Coverage Report

## Coverage Evidence

The backend smoke test run generated coverage output:

- `engine/tests/Nebula.Tests/TestResults/ad9e2d29-aac2-4825-aa25-0fb8f425c759/coverage.cobertura.xml`

## Coverage Assessment

Smoke coverage proves existing baseline routes and application startup paths remain healthy after F0032 compile integration. It does not yet provide focused F0032 branch coverage.

## Follow-up Coverage Requirements

- Add unit tests for `AdminConfigurationService` publish, validation-hash mismatch, stale base version, rollback, and audit append behavior.
- Add endpoint tests for Admin allowed actions and non-admin 403 behavior.
- Add frontend tests for workspace rendering and mutation guardrails.
