# F0019 G1 Runtime Preflight

## Result

PASS

## Runtime Scope

Backend workflow services and frontend submission pages are in scope. EF migration evidence remains historical; current-code build/test validation was captured for runtime-bearing surfaces.

## Commands Identified

- `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj -v minimal`
- `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter FullyQualifiedName~WorkflowServiceTests|FullyQualifiedName~WorkflowStateMachineTests`
- direct `tsc -b && vite build`
- direct `vitest run` for submissions integration tests
