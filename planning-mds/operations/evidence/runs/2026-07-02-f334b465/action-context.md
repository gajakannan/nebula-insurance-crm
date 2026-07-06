# Action Context

| Field | Value |
| --- | --- |
| Action | defect-bugfix |
| Product Root | `/Users/wallstreet62/Desktop/nebula3/nebula-insurance-crm` |
| Defect Run ID | `2026-07-02-f334b465` |
| Defect Run Folder | `planning-mds/operations/evidence/runs/2026-07-02-f334b465` |
| DEFECT_SUMMARY | Local CRM login route reports authentication is not configured. |
| OBSERVED_BEHAVIOR | `http://127.0.0.1:5174/login` renders `Authentication is not configured. Contact your administrator.` |
| EXPECTED_BEHAVIOR | Local Nebula CRM can be entered for F0021 validation using the approved development auth path or a correctly configured OIDC path. |
| REPRO_STEPS | Start Docker services and Vite frontend, then open `/login`. |
| AFFECTED_PATHS | `experience/src/features/auth/**`, `experience/.env*`, local Vite startup configuration. |
| AGENT_ROLES | architect, frontend-developer, quality-engineer |
| FEATURE_REFS | F0021, F0009 |
| ALLOW_FEATURE_PROPOSAL | false |
| Lifecycle Authority | none |

## Scope Boundary

This is a defect run, not a new feature run. It may change runtime code, tests, and defect evidence only. It does not create feature evidence, update `latest-run.json`, or alter feature closeout state.
