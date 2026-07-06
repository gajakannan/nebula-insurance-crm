# F0027 Focused Test Run

Run ID: `2026-07-03-f6b7fa89`
Action: `nandini-nebula-agents/agents/actions/test.md`
Mode: `standalone`
Test scope: `regression`
Feature under test: `F0027`
Reviewed feature run: `2026-07-02-b9316621`

## Status

Complete — PASS WITH RECOMMENDATIONS.

## Scope

Focused post-closeout QA for F0027 outbound document generation:

- COI, ACORD, and reusable proposal template generation
- Preview before explicit issue
- Generated artifact provenance
- Regenerate/retrieve generated artifacts
- Admin template governance and issue permissions
- Denial paths for unauthorized users

## Result

Focused F0027 regression testing passed. Backend service tests, frontend document panel component test, builds, KG checks, harness template validation, F0027 evidence validation, F0027 tracker validation, and untested-symbol routing all passed.

Recommended follow-ups remain nonblocking: add dedicated F0027 live API integration, browser E2E, and accessibility suites.
