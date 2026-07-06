# Artifact Trace

## Artifacts Read
- `/Users/msig2/Desktop/work_space/nebula-agents/agents/templates/prompts/evidence-contract/defect-bugfix-operator-friendly.md`
- `/Users/msig2/Desktop/work_space/nebula-agents/agents/ROUTER.md`
- `/Users/msig2/Desktop/work_space/nebula-agents/agents/agent-map.yaml`
- `/Users/msig2/Desktop/work_space/nebula-agents/agents/docs/AGENT-USE.md`
- `/Users/msig2/Desktop/work_space/nebula-agents/agents/architect/SKILL.md`
- `/Users/msig2/Desktop/work_space/nebula-agents/agents/frontend-developer/SKILL.md`
- `/Users/msig2/Desktop/work_space/nebula-agents/agents/frontend-developer/references/ux-audit-ruleset.md`
- `/Users/msig2/Desktop/work_space/nebula-insurance-crm/.agentignore`
- KG hints for `experience/src/pages/ServiceCasesPage.tsx` and `experience/src/features/service-cases/hooks.ts`
- `/Users/msig2/Desktop/work_space/nebula-insurance-crm/experience/src/features/service-cases/hooks.ts`
- `/Users/msig2/Desktop/work_space/nebula-insurance-crm/experience/vite.config.ts`
- `/Users/msig2/Desktop/work_space/nebula-insurance-crm/engine/src/Nebula.Api/Endpoints/ServiceCaseEndpoints.cs`
- `/Users/msig2/Desktop/work_space/nebula-insurance-crm/experience/src/pages/ServiceCasesPage.tsx`

## Artifacts Created Or Updated
- This defect evidence package.
- `/Users/msig2/Desktop/work_space/nebula-insurance-crm/experience/vite.config.ts`
- `/Users/msig2/Desktop/work_space/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-07-03-8ac36f46/architect-analysis.md`
- `/Users/msig2/Desktop/work_space/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-07-03-8ac36f46/frontend-fix-report.md`

## Generated Evidence
- `artifacts/screenshots/service-cases-after-fix.png`
- Focused curl checks recorded in `commands.log`.
- Focused Vitest, lint, theme lint, and build results recorded in `commands.log`.

## External Or Global Evidence References
- Running local frontend: `http://localhost:5173/`
- Running local backend health endpoint: `http://localhost:8080/healthz`

## Omissions Or Waivers
- Broader backend test suite was not rerun because the backend service-case endpoint returned `200 OK` with the frontend dev token and the fix was limited to Vite dev proxy routing.
