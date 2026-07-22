# Test Execution Report — F0026-billing-invoicing-and-reconciliation run 2026-07-19-86ad3248

## Commands Executed

All commands and retry exit codes are recorded in `commands.log`. Passing terminal lanes included:

```text
- dotnet test ... --filter FullyQualifiedName~Nebula.Tests.Unit.Billing --collect "XPlat Code Coverage" → 18 passed
- dotnet test ... --filter FullyQualifiedName!~Integration&FullyQualifiedName!~Contracts → 371 passed
- vitest run BillingPages.test.tsx Sidebar.test.tsx → 5 passed
- vitest run <accessibility lane including BillingPages.test.tsx> → 11 passed
- playwright test tests/visual/f0026-billing.spec.ts → 6 passed
- vitest focused F0026 coverage lane → 5 passed with coverage artifacts
- docker compose up -d --build api; /healthz; migration/table checks; HTTP probes → passed
- persisted invoice → receipt → exact application → reload API flow → passed after required If-Match retry
```

Targeted ESLint, `lint:theme`, `lint:css`, `lint:effects`, and the production frontend build also exited 0.

## Pass/Fail Counts

| Lane | Total | Pass | Fail | Skip | Retries |
|------|------:|-----:|-----:|-----:|--------:|
| Backend focused billing unit/service | 18 | 18 | 0 | 0 | 3 during implementation |
| Backend non-integration regression | 371 | 371 | 0 | 0 | 1 |
| Frontend focused billing/navigation | 5 | 5 | 0 | 0 | 1 after markup correction |
| Frontend accessibility regression | 11 | 11 | 0 | 0 | 0 |
| F0026 visual Playwright | 6 | 6 | 0 | 0 | 1 locator correction |
| Runtime persisted exact-reconciliation flow | 5 HTTP operations | 5 | 0 | 0 | 1 expected concurrency-precondition retry |

## Skipped Tests And Rationale

- The repository-wide Testcontainers integration lane was not used as passing F0026 evidence. The SDK-container harness first lacked a Docker socket, then Ryuk connectivity, and finally disposed/withdrew the ephemeral PostgreSQL endpoint before application migration. Owner: Quality Engineer / DevOps. Follow-up: `F0026-TESTCONTAINERS-HARNESS`. F0026’s real compose PostgreSQL persisted mutation/reload flow passed and is the integration evidence for this run.
- No real bank/vendor exchange is tested because F0030 production connectivity is explicitly out of F0026 scope; the bounded mock CSV adapter is covered.
- No AI lane exists because the assembly plan has no AI scope.

## Raw Test Artifact Paths

- `artifacts/test-results/backend-billing-tests.trx`
- `artifacts/test-results/backend-nonintegration-tests.trx`
- `artifacts/test-results/backend-full-tests.trx`
- `artifacts/test-results/backend-integration-preflight.trx`
- `artifacts/test-results/frontend-billing-tests.xml`
- `artifacts/test-results/frontend-accessibility-tests.xml`
- `artifacts/test-results/frontend-full-unit-tests.xml`
- `artifacts/test-results/frontend-stable-unit-tests.xml`
- `artifacts/test-results/runtime-smoke.md`
- `artifacts/test-results/runtime-persisted-flow.txt`
- `artifacts/test-results/runtime-persisted-flow-completion.txt`
- `artifacts/screenshots/billing-dark-desktop.png`
- `artifacts/screenshots/billing-dark-mobile.png`
- `artifacts/screenshots/billing-light-desktop.png`
- `artifacts/screenshots/billing-light-mobile.png`
- `artifacts/screenshots/billing-reconciliation-dark-desktop.png`
- `artifacts/screenshots/billing-reconciliation-light-desktop.png`

## Failed / Retried Command History

- Early focused backend runs exposed compile/test defects while implementation was active; fixes were applied and the final focused coverage run passed 18/18.
- The first focused frontend run exposed invalid definition-list markup and a Fast Refresh export warning; the components were corrected and the final lane passed 5/5.
- The first visual run used a strict locator that matched hidden responsive navigation as well as the visible heading; the assertion was narrowed and the final lane passed 6/6.
- The complete backend suite’s failures are isolated to the existing Testcontainers integration harness; 371/371 non-integration tests pass.
- The full frontend unit attempt passed 283/284 with one pre-existing cross-realm Blob assertion. Subsequent attempts excluding that file exposed one unrelated broker-contact modal flake; all F0026 tests remained green.
- The first persisted exact-application request omitted the required `If-Match` header and correctly returned HTTP 428. Retrying with the current invoice row version returned 201, and reload returned `Reconciled` with zero outstanding balance.

## AC Coverage Result

- F0026-S0001: covered — source-authorized workspace/detail, empty/auth boundaries, responsive UI.
- F0026-S0002: covered — validation, source context, persisted create/reload, audit path.
- F0026-S0003: covered — manual/duplicate receipt, bounded CSV outcomes, raw-byte disposal, invalid UTF-8.
- F0026-S0004: covered — exact application, mismatch exceptions, concurrency, persisted atomic reload.
- F0026-S0005: covered — reference-only correction and maker-checker decision including Admin self-decision denial.
- F0026-S0006: covered — authorized backlog/audit composition and responsive accessible surfaces.

## Result

PASS

## G3 Remediation Verification

After the first code-review cycle, QE reran the changed surface and retained the passing verdict:

- Backend focused billing suite: 20/20 passed, including invoice-detail aggregation, pending-correction reload, expanded backlog counts, audit context, and authorization-before-conflict regression coverage.
- Frontend billing pages and real query-hook wiring: 4/4 passed.
- Production builds: .NET solution and frontend Vite build passed.
- Targeted F0026 ESLint: passed.
- Visual regression: 8/8 passed in the supported Playwright container, including dark/light invoice evidence detail plus the existing workspace/reconciliation desktop/mobile coverage.
- Live Postgres-backed API: the remediated detail envelope returned application, receipt, and audit context; a fresh mismatch/correction flow reloaded the pending correction inline and updated backlog counts.
- API contract validation, strict security artifact audit, targeted Semgrep, and live ZAP reruns passed.

Current remediation artifacts:

- `artifacts/test-results/g3-remediation-backend-tests.txt`
- `artifacts/test-results/g3-remediation-frontend-tests.txt`
- `artifacts/test-results/g3-remediation-visual-tests.txt`
- `artifacts/test-results/runtime-remediation-probe.txt`
- `artifacts/test-results/runtime-correction-reload-flow.txt`

Result remains PASS.
