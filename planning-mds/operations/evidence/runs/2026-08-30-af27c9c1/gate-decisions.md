# Gate Decisions — F####-{slug} run {run-id}

> Required per §8. One row per gate evaluated. §17 stage matrix dictates which rows must be present at each validation stage.

## Gate Decisions

| Gate | Decision | Decider | Timestamp | Rationale | Blocking | Follow-up |
|------|----------|---------|-----------|-----------|----------|-----------|
| G0   | PASS     | Architect | 2026-05-19T10:15:00-04:00 | Plan reconciles with PRD | No | - |
| G1   | PASS     | DevOps    | 2026-05-19T10:30:00-04:00 | Runtime preflight green | No | - |
| G2   | PASS     | QE        | 2026-05-19T11:00:00-04:00 | All AC covered, coverage ≥ target | No | - |
| G3   | PASS     | Code Reviewer | 2026-05-19T11:30:00-04:00 | No blocking findings | No | - |
| G5 | PASS     | PM        | 2026-05-19T12:00:00-04:00 | Signoff matrix complete | No | - |
| G6 | PASS     | PM        | 2026-05-19T12:15:00-04:00 | Candidate evidence validated | No | - |
| G8 | PASS     | PM        | 2026-05-19T12:30:00-04:00 | Closeout sealed | No | - |

Decisions: `PASS`, `PASS WITH RECOMMENDATIONS`, `FAIL`, `SKIP`. Blocking values: `Yes` / `No`.
