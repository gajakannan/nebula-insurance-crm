# G1 Runtime Preflight

## Scope

Establish the current backend and frontend runtime/build baseline before F0032 implementation edits.

## Environment

| Check | Result | Notes |
|-------|--------|-------|
| `.NET SDK` | PRESENT | `dotnet --info` reports SDK `10.0.203` and runtime `10.0.7`. |
| Frontend dependencies | PRESENT | `experience/node_modules` exists. |
| `pnpm` direct PATH | FAIL | `pnpm --dir experience --version` returned command not found (`127`). |
| Corepack pnpm | PASS | `corepack pnpm --dir experience --version` returned `11.5.1`. |

## Preflight Commands

| Command | Result | Notes |
|---------|--------|-------|
| `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore` | RESOLVED SANDBOX BLOCKER | Sandboxed attempt was silent until canceled after about 5 minutes; output reported `Build FAILED`, `0 Warning(s)`, `0 Error(s)`, `Time Elapsed 00:05:00.89`. |
| `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore` | RESOLVED SANDBOX BLOCKER | Sandboxed attempt was silent until canceled after about 5 minutes; output reported `Build FAILED`, `0 Warning(s)`, `0 Error(s)`, `Time Elapsed 00:05:00.96`. |
| `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore --disable-build-servers` | RESOLVED SANDBOX BLOCKER | Sandboxed attempt was silent until canceled after about 5 minutes; output reported `Build FAILED`, `0 Warning(s)`, `0 Error(s)`, `Time Elapsed 00:05:01.13`. |
| `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore --disable-build-servers` | RESOLVED SANDBOX BLOCKER | Sandboxed attempt was silent until canceled after about 5 minutes; output reported `Build FAILED`, `0 Warning(s)`, `0 Error(s)`, `Time Elapsed 00:05:01.19`. |
| `dotnet restore engine/src/Nebula.Api/Nebula.Api.csproj --disable-parallel -v:minimal` | PASS | Approved unsandboxed restore completed immediately: all projects up to date for restore. |
| `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore --disable-build-servers -v:minimal` | PASS | Approved unsandboxed API build passed with `0 Warning(s)`, `0 Error(s)`. |
| `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore --disable-build-servers -v:minimal` | PASS | Approved unsandboxed test-project build passed with seven existing nullable warnings and `0 Error(s)`. |
| `corepack pnpm --dir experience build` | PASS | TypeScript and Vite production build passed; Vite emitted the existing chunk-size warning for `index-B0VB6jcb.js` over 500 kB. |

## Verdict

PASS. Backend and frontend runtime preflight are established. The earlier backend blocker was sandbox-related and resolved through approved unsandboxed restore/build commands.

Frontend preflight is usable through `corepack pnpm`; direct `pnpm` remains unavailable on PATH in this shell.
