# F0020 Runtime Validation

Run ID: `7e15d9c8-c2a0-4442-99a7-082bc0b560f5`
Date: 2026-05-04

## Runtime Preflight After Rebuild

Commands:

- `docker compose build api`
- `docker compose up -d api`
- `docker compose ps`
- `curl -L -s http://localhost:8080/healthz`
- `docker compose logs --tail=80 api`

Result:

- API image rebuilt successfully from the current source tree.
- Initial rebuilt runtime failed because hosted services consumed scoped document services directly. Fixed in `QuarantinePromotionWorker` and `DocumentRetentionHostedService` by resolving scoped services inside `IServiceScopeFactory` scopes.
- Rebuilt API container started successfully after the fix.
- `docker compose ps` showed `nebula-api` Up on port 8080 and required dependencies healthy/running.
- `/healthz` returned `Healthy`.

## Backend Commands

Metadata schema registry rerun after G4 design gap repair:

- `docker compose build api`
  - Result: PASS
  - Notes: Existing nullable warnings remain in unrelated dashboard/submission files during publish.
- `docker compose up -d api`
  - Result: PASS
- `docker compose ps`
  - Result: PASS; `nebula-api` Up on port 8080 and required dependencies healthy/running.
- `curl -L -s http://localhost:8080/healthz`
  - Result: PASS; returned `Healthy`.
- `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj`
  - Result: PASS
  - Notes: Existing nullable warnings remain in unrelated dashboard/submission files.
- `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter DocumentServiceTests`
  - Result: PASS, 3 passed.
  - Coverage artifact: `engine/tests/Nebula.Tests/TestResults/6508a573-b2b2-47a0-a9d3-d702aade4162/coverage.cobertura.xml`
- `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-build`
  - Result: PASS, 398 passed, 1 skipped.
  - Coverage artifact: `engine/tests/Nebula.Tests/TestResults/ab8e5966-927e-4b3e-be5c-2c45d6895bab/coverage.cobertura.xml`

- `dotnet build`
  - Result: PASS
  - Notes: Existing nullable warnings remain in unrelated dashboard/submission/task/workflow files.
- `dotnet test --filter DocumentServiceTests`
  - Result: PASS, 2 passed.
- `dotnet test --no-build`
  - Result: PASS, 397 passed, 1 skipped.
  - Coverage artifact: `engine/tests/Nebula.Tests/TestResults/48e3ff64-d412-4d32-bfb9-2715c3b48990/coverage.cobertura.xml`

## Frontend Commands

Metadata schema registry rerun after G4 design gap repair:

- `CI=true pnpm --dir experience build`
  - Result: PASS
  - Notes: Vite reported only the existing chunk-size warning.
- `CI=true pnpm --dir experience exec vitest run src/features/documents/tests/ParentDocumentsPanel.test.tsx src/services/api.test.ts`
  - Result: PASS, 6 passed.
- `CI=true pnpm --dir experience test`
  - Result: PASS, 94 passed.
  - Notes: Existing Popover `act(...)` warnings appeared; tests passed.

- `CI=true pnpm --dir experience install --frozen-lockfile --virtual-store-dir /home/gajap/.pnpm-virtual-store/nebula-crm-experience`
  - Result: PASS
  - Notes: Required after Rollup optional dependency was missing and default Windows-mount relink hit `EACCES`.
- `CI=true pnpm --dir experience build`
  - Result: PASS
  - Notes: Vite reported only the existing chunk-size warning.
- `CI=true pnpm --dir experience exec vitest run src/features/documents/tests/ParentDocumentsPanel.test.tsx src/services/api.test.ts --exclude 'src/**/*.integration.test.ts' --exclude 'src/**/*.integration.test.tsx' --exclude 'src/**/*.a11y.test.ts' --exclude 'src/**/*.a11y.test.tsx'`
  - Result: PASS, 6 passed.
- `CI=true pnpm --dir experience test`
  - Result: PASS, 94 passed.
  - Notes: Existing Popover `act(...)` warnings appeared; tests passed.

## Diff Hygiene

Metadata schema registry rerun after G4 design gap repair:

- `git diff --check`
  - Result: PASS

- `git diff --check`
  - Result: PASS

## Repository Trackers And KG

Metadata schema registry rerun after G4 design gap repair:

- `python3 scripts/kg/validate.py --write-coverage-report`
  - Result: PASS; regenerated `planning-mds/knowledge-graph/coverage-report.yaml`.
- `python3 scripts/kg/validate.py`
  - Result: PASS.
- `python3 scripts/kg/validate.py --check-drift`
  - Result: PASS.
- `python3 agents/product-manager/scripts/validate-trackers.py`
  - Result: PASS.
- `python3 agents/scripts/validate_templates.py`
  - Result: PASS.

- `python3 agents/product-manager/scripts/validate-trackers.py`
  - Result: PASS
- `python3 agents/scripts/validate_templates.py`
  - Result: PASS
- `python3 scripts/kg/validate.py`
  - Initial result: FAIL because `coverage-report.yaml` was stale.
- `python3 scripts/kg/validate.py --write-coverage-report`
  - Result: PASS; regenerated `planning-mds/knowledge-graph/coverage-report.yaml`.
- `python3 scripts/kg/validate.py`
  - Result: PASS.
- `python3 scripts/kg/validate.py --check-drift`
  - Result: PASS.
