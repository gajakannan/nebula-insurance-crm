# G1 Runtime Preflight - F0037

## Runtime Scope

F0037 is runtime-bearing because it will change backend services/API behavior and frontend report/search surfaces. Preflight checked the documented local runtime stack and developer tooling before implementation work.

## Commands

| Command | Result | Notes |
|---------|--------|-------|
| `docker compose ps` | PASS after approved unsandboxed execution | Sandbox could not access the Docker socket. Approved execution showed `nebula-api`, `nebula-db`, `nebula-authentik-server`, and `nebula-neuron` running; DB and authentik reported healthy. Compose warned that authentik bootstrap variables are unset. |
| `dotnet --info` | PASS | .NET SDK `10.0.203`, ASP.NET/Core runtime `10.0.7`, macOS arm64. |
| `pnpm --dir experience --version` | FAIL | `pnpm` is not directly on PATH. |
| `node --version` | PASS | Node `v24.15.0`. |
| `npm --version` | PASS | npm `11.12.1`. |
| `corepack --version` | PASS | Corepack `0.34.6`. |
| `corepack pnpm --dir experience --version` | PASS | pnpm `11.5.1` is available through Corepack. |

## Runtime Decision

PASS. Runtime and toolchain are usable for F0037 if frontend commands are invoked as `corepack pnpm --dir experience ...` in this environment.

## Follow-Ups

- Use Corepack for frontend validation commands unless `pnpm` becomes available directly on PATH.
- Do not treat the initial Docker socket denial as an application failure; approved Docker preflight was healthy.
