# Coverage Report

## Coverage Evidence

The backend smoke test run generated Coverlet coverage output. The raw `coverage.cobertura.xml` is written by the Coverlet collector under `engine/tests/Nebula.Tests/TestResults/<run-guid>/` (a git-ignored, per-run transient path per `.gitignore`) and is not retained in this evidence package; the coverage assessment below is the retained coverage evidence of record.

## Coverage Assessment

Smoke coverage proves existing baseline routes and application startup paths remain healthy after F0032 compile integration. It does not yet provide focused F0032 branch coverage.

## Follow-up Coverage Requirements

- Add unit tests for `AdminConfigurationService` publish, validation-hash mismatch, stale base version, rollback, and audit append behavior.
- Add endpoint tests for Admin allowed actions and non-admin 403 behavior.
- Add frontend tests for workspace rendering and mutation guardrails.
