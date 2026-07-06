# Test Plan

Scope: E2E standalone verification for F0021  
Date: 2026-07-02  
Result: Planned

## Story-to-AC Mapping

| Story | Acceptance Criteria | Test Type | Test Case |
| --- | --- | --- | --- |
| F0021-S0001 | Capture communication from contextual record and observe persistence | E2E + API evidence | Create Account communication through UI; verify API persisted event and panel shows it after route reload. |
| F0021-S0002 | Empty and populated contextual history, no load failure | E2E | Verify policy empty state, then populated history with type/summary/follow-up count. |
| F0021-S0003 | Primary link and participant persist | E2E + API evidence | Capture participant during UI save; verify API response includes primary Account link and participant details. |
| F0021-S0004 | Follow-up task linkage | E2E + API evidence | Create communication with follow-up via API-backed setup; verify panel follow-up count and `/my/tasks` contains linked task. |
| F0021-S0005 | Correction and redaction | E2E + API evidence | Correct event and verify corrected summary; redact event and verify redacted list indicator and masked summary. |
| Cross-cutting | Unauthorized/error behavior | API + E2E | Direct unauthenticated `/communications` returns auth error; host page remains usable when communication history fails. |

## Test Types In Scope

- E2E browser tests with Playwright against the running local frontend and API.
- Focused component regression for `CommunicationPanel`.
- Lint, theme guard, and production build as quality gates.

## Out Of Scope / Waivers

- Line/branch coverage is waived for this standalone E2E run because the request is end-to-end validation after feature closeout. Scenario coverage is required and recorded in `coverage-report.md`.
- Full cross-browser matrix is out of scope; local Chromium/Chrome is sufficient for this run.
- Security scanner execution is out of scope for this E2E-only run because F0021 feature closeout already recorded security review evidence.

## Test Infrastructure

- Running API: `http://127.0.0.1:8080`
- Running frontend: `http://127.0.0.1:5174`
- Dev auth mode token is accepted for local E2E.
- Raw test output must be written under `artifacts/test-results/`.
- Screenshots must be written under `artifacts/screenshots/`.
- E2E spec path: `experience/tests/e2e/f0021-communications.spec.ts`.

## Developer-vs-QE Ownership

- Existing developer-owned component coverage remains `CommunicationPanel.test.tsx`.
- QE owns the new E2E spec, scenario evidence, standalone reports, and quality gate decision.
