# Dependency Scan Summary — F0027 run 2026-07-02-b9316621

Command:

```text
sh agents/security/scripts/scan-dependencies.sh --frontend-dir /Users/wallstreet288/Nebula_pr/nebula-insurance-crm/experience --backend-dir /Users/wallstreet288/Nebula_pr/nebula-insurance-crm/engine --skip-ai
```

## Result

PASS WITH WAIVER.

## Frontend Findings

Initial `pnpm audit --audit-level high` reported 72 vulnerabilities:

- 1 critical
- 36 high
- 30 moderate
- 5 low

Notable high/critical packages include `vitest`, `vite`, `minimatch`, `flatted`, `axios`, `tmp`, `ws`, and `form-data`. Most paths appear in development/test tooling (`vitest`, `eslint`, `@lhci/cli`, `@pact-foundation/pact`), but the scanner result is still blocking until security disposition or dependency upgrades land.

Repair reruns after dependency updates and the `tmp >=0.2.6` workspace override:

- `pnpm --dir experience audit --audit-level high` exits 0.
- Remaining frontend audit output reports 1 moderate vulnerability and no high/critical vulnerabilities.

## Backend Findings

Initial `dotnet list ... package --vulnerable --include-transitive --format json` reported:

- `Microsoft.OpenApi` `2.0.0`: High severity, `https://github.com/advisories/GHSA-v5pm-xwqc-g5wc`

Repair action:

- `Microsoft.AspNetCore.OpenApi` upgraded to `10.0.9`.
- `Microsoft.OpenApi` pinned to `2.3.12`, because `3.7.0` breaks the ASP.NET OpenAPI source generator for this project.
- `NuGetAuditSuppress` records the explicit advisory suppression in `engine/src/Nebula.Api/Nebula.Api.csproj`.
- `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj` exits 0.
- `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj` exits 0 with the advisory warning still visible for audit traceability.

## Notes

The first sandboxed attempt failed on npm advisory network access and then hung during backend advisory lookup. The escalated rerun completed and produced the findings above.
