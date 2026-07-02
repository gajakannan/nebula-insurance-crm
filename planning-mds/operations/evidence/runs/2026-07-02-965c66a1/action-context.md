# Action Context

| Field | Value |
| --- | --- |
| Action | defect-bugfix |
| Product Root | `/Users/wallstreet62/Desktop/nebula3/nebula-insurance-crm` |
| Defect Run ID | `2026-07-02-965c66a1` |
| Defect Run Folder | `planning-mds/operations/evidence/runs/2026-07-02-965c66a1` |
| DEFECT_SUMMARY | F0021 contextual communication panels show `Unable to load communications.` |
| OBSERVED_BEHAVIOR | Account, Broker, Policy, and Renewal detail pages render a communication panel error instead of history/empty state. |
| EXPECTED_BEHAVIOR | `/communications` requests are proxied to the API and empty communication history renders the F0021 empty state. |
| REPRO_STEPS | Run the app locally, navigate in-app to a detail page with a `CommunicationPanel`, inspect `/communications?...` network responses. |
| AFFECTED_PATHS | `experience/vite.config.ts`, `experience/src/features/communications/**` |
| AGENT_ROLES | architect, frontend-developer, quality-engineer |
| FEATURE_REFS | F0021, F0009 |
| ALLOW_FEATURE_PROPOSAL | false |
| Lifecycle Authority | none |

## Scope Boundary

This is a defect run linked to F0021. It may change the local runtime proxy and defect evidence only. It must not create feature evidence, update `latest-run.json`, or reopen F0021 feature closeout.
