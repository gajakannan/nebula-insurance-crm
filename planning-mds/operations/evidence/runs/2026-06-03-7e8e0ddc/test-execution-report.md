# Test Execution Report - F0019-submission-quoting-proposal-and-approval run 2026-06-03-7e8e0ddc

## Commands Executed

```text
- dotnet build Nebula.slnx -> exit 0
- dotnet test Nebula.slnx --no-build --filter 'FullyQualifiedName~WorkflowStateMachineTests|FullyQualifiedName~WorkflowServiceTests' -> exit 0
- dotnet build Nebula.slnx -> exit 0 after G3 archive/reactivate reason fix
- dotnet test Nebula.slnx --no-build --filter 'FullyQualifiedName~WorkflowStateMachineTests|FullyQualifiedName~WorkflowServiceTests' -> exit 0 after G3 archive/reactivate reason fix
- dotnet build Nebula.slnx -> exit 0 after G3 approvalPending filter fix
- dotnet test Nebula.slnx --no-build --filter 'FullyQualifiedName~WorkflowStateMachineTests|FullyQualifiedName~WorkflowServiceTests' -> exit 0 after G3 approvalPending filter fix
- env CI=true pnpm --dir experience build -> exit 0
- pnpm --dir experience exec vitest run src/pages/tests/SubmissionsPage.integration.test.tsx src/pages/tests/SubmissionDetailPage.integration.test.tsx -> exit 0
- pnpm --dir experience test -> exit 1 (out-of-scope fixed-date session-continuity fixture)
- /home/gajap/.dotnet/tools/dotnet-ef migrations script 20260507030000_F0034_ProductSchemaRegistryAndLobAttributes 20260603220000_F0019_SubmissionQuotingApproval -> exit 0
```

Each command is recorded in commands.log.

## Pass/Fail Counts

| Lane | Total | Pass | Fail | Skip | Retries |
|------|------:|-----:|-----:|-----:|--------:|
| Backend targeted workflow unit | 34 | 34 | 0 | 0 | 1 command retry for shell filter quoting |
| Frontend submissions integration | 6 | 6 | 0 | 0 | 0 |
| Frontend production build | 1 | 1 | 0 | 0 | 0 |
| Frontend broad unit suite | 239 | 238 | 1 | 0 | 0 |

The frontend broad unit suite failure is outside the F0019 changed path set:

- src/features/session-continuity/tests/sessionTelemetry.test.ts
- The fixture timestamp is 2026-05-24T12:00:00.000Z and deferred telemetry TTL is 7 days.
- On this run date, 2026-06-03, the implementation correctly drops the expired event and the fixed-date expectation fails.
- F0019 verdict relies on the scoped passing submissions integration lane and production build.

## Skipped Tests And Rationale

- No skipped tests in the executed targeted backend or submissions frontend lanes.
- Live HTTP endpoint integration tests were not added in this run; endpoint behavior is covered through service tests, endpoint compilation, OpenAPI contract update, and UI integration shell.
- E2E/browser visual tests were not run; F0019 did not add a new visual regression baseline.

## Raw Test Artifact Paths

- artifacts/test-results/backend-build-after-null-fix.txt
- artifacts/test-results/backend-workflow-tests.txt
- artifacts/test-results/backend-build-after-g3-fix.txt
- artifacts/test-results/backend-workflow-tests-after-g3-fix.txt
- artifacts/test-results/backend-build-after-g3-filter-fix.txt
- artifacts/test-results/backend-workflow-tests-after-g3-filter-fix.txt
- artifacts/test-results/frontend-build.txt
- artifacts/test-results/frontend-submissions-integration.txt
- artifacts/test-results/frontend-unit.txt

## Failed / Retried Command History

- Initial backend test filter command was entered without shell quoting around the `|` expression and exited 127. The correctly quoted rerun passed 33/33.
- Initial frontend build failed before retry because Rollup's optional native package was missing from node_modules. Reinstalling dependencies with a /tmp pnpm virtual store resolved the package layout; the subsequent production build passed.
- G3 code review found missing blank-reason validation for archive/reactivate audit actions. Service validation and a focused unit test were added; backend workflow tests then passed 34/34.
- G3 code review found approvalPending list filtering over-included quoted submissions without a ready packet or with an approval decision. Repository filtering was corrected; backend build and workflow tests remained green.
- Broad frontend unit suite failure remains out of F0019 scope and is documented above.

## AC Coverage Result

| Story | Outcome | Evidence |
|-------|---------|----------|
| F0019-S0001 | covered | backend workflow tests and build |
| F0019-S0002 | covered | backend workflow tests, frontend build, submissions integration |
| F0019-S0003 | covered | backend build/tests and activity schema |
| F0019-S0004 | covered | backend build/tests and migration SQL |
| F0019-S0005 | covered | backend workflow tests |
| F0019-S0006 | covered | backend build, frontend build, migration SQL |
| F0019-S0007 | covered | submissions integration |
| F0019-S0008 | covered | activity schema validation and backend build |

## Recommendations

- None.

## Result

Result: PASS
