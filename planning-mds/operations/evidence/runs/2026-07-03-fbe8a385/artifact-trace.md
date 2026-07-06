# Artifact Trace - F0008 Standalone Test Run

## Artifacts Read

| Artifact | Purpose |
| --- | --- |
| `agents/actions/test.md` | Test action contract. |
| `agents/templates/prompts/evidence-contract/test-operator-friendly.md` | Test evidence prompt contract. |
| `agents/quality-engineer/SKILL.md` | QE role rules. |
| `planning-mds/features/F0008-broker-insights/*` | Story acceptance criteria and approved status. |
| `planning-mds/operations/evidence/runs/2026-07-03-fd732693/*` | Approved parent feature evidence and deferred runtime follow-ups. |
| `docker-compose.yml`, `engine/Dockerfile` | Runtime container shape. |
| `experience/package.json` | Frontend test/build commands. |

## Artifacts Created Or Updated

| Artifact | Status | Notes |
| --- | --- | --- |
| `evidence-manifest.json` | Created | Standalone test manifest. |
| `README.md` | Created | Run evidence index. |
| `action-context.md` | Created | Run identity and scope. |
| `artifact-trace.md` | Created | Evidence trace. |
| `gate-decisions.md` | Created | T0-T5 gate table. |
| `commands.log` | Created | JSONL command log. |
| `lifecycle-gates.log` | Created | Test lifecycle gates. |
| `test-plan.md` | Created | QE T0 plan. |
| `test-execution-report.md` | Created | T1 test and runtime results. |
| `coverage-report.md` | Created | T2 coverage and waiver notes. |
| `artifacts/test-results/f0008-postcloseout-backend-after-repair.trx` | Created | Backend focused test result after repair. |
| `artifacts/test-results/*/coverage.cobertura.xml` | Created | Backend coverage attachments from `dotnet test`. |
| `artifacts/test-results/f0008-postcloseout-frontend-vitest.json` | Created | Frontend broker insights component test result. |
| `artifacts/test-results/f0008-postcloseout-frontend-build.txt` | Created | Frontend production build output. |
| `artifacts/runtime/*-after-repair.http` | Created | Runtime API smoke responses after repair. |
| `artifacts/runtime/broker-insight-table-check.txt` | Created | Isolated DB verification that `BrokerInsightProjections` exists. |
| `artifacts/runtime/f0008-api-filtered.log` | Created | Compact API log lines relevant to migration/runtime smoke. |

## Generated Evidence

- Backend API image rebuild passed after repair.
- Backend focused tests passed after repair: 3 passed, 0 failed, 0 skipped.
- Frontend broker insights component tests passed: 2 passed, 0 failed, 0 skipped.
- Frontend production build passed with existing chunk-size warning.
- Isolated runtime health check returned 200.
- Runtime smoke after repair: scorecards no-auth 401, scorecards auth 200, nonexistent broker-specific routes 404.

## External Or Global Evidence References

- Parent approved feature run: `planning-mds/operations/evidence/runs/2026-07-03-fd732693`.
- Feature latest-run pointer: `planning-mds/operations/evidence/features/F0008-broker-insights/latest-run.json`.

## Omissions And Waivers

- Frontend container execution is omitted because no frontend Dockerfile or Compose service exists; frontend test/build commands use the product Node toolchain.
- Frontend coverage is waived for this focused run; backend Cobertura coverage was captured by `dotnet test`.
- Browser E2E/screenshots were not captured because no frontend runtime was started for this standalone post-closeout run.

## Run Environment

- Commands span sibling repos under `/Users/wallstreet48/nebula-feature-26`.
- Absolute cwd values are recorded for unambiguous provenance.
