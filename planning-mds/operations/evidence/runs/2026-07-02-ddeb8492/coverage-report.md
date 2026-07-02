# Coverage Report

## Scope

Standalone E2E scenario coverage for F0021. Line and branch coverage are intentionally waived for this run because the requested scope is post-closeout browser/API validation.

## Scenario Coverage

| Story / Area | Scenario | Result | Evidence |
| --- | --- | --- | --- |
| F0021-S0001 Capture | Account UI Add Communication with participant and follow-up, then API persistence check | PASS | `artifacts/test-results/f0021-communications-results.json`, `artifacts/screenshots/f0021-created-with-follow-up.png` |
| F0021-S0002 View History | Empty state, populated history, no `Unable to load communications` | PASS | `artifacts/screenshots/f0021-empty-state.png`, `artifacts/screenshots/f0021-contextual-history.png` |
| F0021-S0003 Links & Participants | Primary record link and participant persisted after save | PASS | `artifacts/test-results/f0021-communications-results.json` |
| F0021-S0004 Follow-Up | Follow-up task created from communication and visible through `/my/tasks` API | PASS | `artifacts/screenshots/f0021-created-with-follow-up.png` |
| F0021-S0005 Correction/Redaction | Corrected summary persists; Admin redaction masks summary/body and shows redacted indicator | PASS | `artifacts/screenshots/f0021-redacted-state.png` |
| Authorization/Error | Unauthenticated `/communications` returns `401`; host record pages remain usable | PASS | `artifacts/test-results/f0021-communications-results.json` |

## Waiver

- Code coverage: waived for this standalone E2E run. Component regression and build/lint gates were executed separately and passed.

## Defects Found During Coverage

- `2026-07-02-09589802`: local dev frontend proxy did not route `/users`, blocking follow-up assignee search.
- `2026-07-02-314d22e1`: correction persistence returned HTTP 500 due correction audit row tracking.
