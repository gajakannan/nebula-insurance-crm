# G1 Runtime Preflight — F0024 Drift Reconciliation

## Scope

- Runtime-bearing feature: service-case backend API, service-case frontend workspace, detail workflows, and account/policy embedded panels.
- Deployment config changed: no new deployment target was added in this drift pass; existing F0024 API/frontend registrations remain in scope from the active feature baseline.

## Preflight Checks

| Check | Result | Evidence |
|---|---|---|
| Backend compile path | PASS | `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter ServiceCaseServiceTests --no-restore` compiled API/Application/Infrastructure/Test assemblies. |
| Frontend compile path | PASS | `npm run build` completed `tsc -b` and `vite build`. |
| Frontend unit path | PASS | `npm test -- ServiceCaseListPanel.test.tsx` passed. |
| Sandbox note | INFO | First `dotnet test` attempt failed under managed sandbox due MSBuild named-pipe permission; rerun with approved escalation passed. |

## Result

PASS for G1 implementation preflight. Browser/runtime smoke and final deployment rehearsal remain later-gate work.
