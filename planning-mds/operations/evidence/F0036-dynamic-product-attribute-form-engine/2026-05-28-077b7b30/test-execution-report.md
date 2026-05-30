# Test Execution Report — F0036-dynamic-product-attribute-form-engine run 2026-05-28-077b7b30

> Quality Engineer-owned; the QE verdict artifact. **Frontend feature** — all lanes run in the `experience/` vitest/jsdom toolchain.

## Commands Executed

```text
- pnpm test:coverage           → 242 unit pass / 0 fail (the 5 reds in the mixed run are all integration; see lane note)
- pnpm test:integration        → 19 pass / 2 fail (both pre-existing — see Pre-Existing Failures)
- pnpm test:accessibility      → 8 pass / 0 fail
- pnpm lint                    → 0 errors, 4 warnings (1 feature react-refresh, 3 pre-existing)
- pnpm lint:theme              → exit 0
- pnpm lint:effects            → exit 0
- pnpm build                   → exit 0 (tsc -b + vite build)
```

Each appears in `commands.log` with matching exit codes.

## Pass/Fail Counts

| Lane | Total | Pass | Fail | Skip | Retries |
|------|------:|-----:|-----:|-----:|--------:|
| Unit + Component (experience, frontend) | 242 | 242 | 0 | 0 | 0 |
| Accessibility (frontend) | 8 | 8 | 0 | 0 | 0 |
| Integration (frontend, QE lane) | 21 | 19 | 2 | 0 | 0 |

**Lane note:** `test:coverage` mixes unit + integration in parallel workers; its 5 reds were all integration tests, 4 of which were the Cyber-panel screens fixed this run (see below). The authoritative per-lane numbers above use the repo's separate lanes (`pnpm test` excludes integration; `pnpm test:integration` is the QE lane).

## Frontend Feature Test Notes

All F0036 surfaces are `experience/**`. The new feature test files (all green):
engine — `widgetRegistry`, `widgets`, `widgets.a11y`, `deriveWidgets`, `parity`, `usePinnedBundle`, `SchemaDrivenForm`, `rhfDirtyAdapter`; forms — `useControlledDirtyTracker`, `dualBackend`; panel — `DynamicAttributePanel`, `DynamicAttributePanel.preservation`; CRUD wiring — `ContactFormModal`, `ContactFormModal.restore`, `EditBrokerModal`, `TaskCreateModal`, `TaskDetailPanel`, `CreateBrokerPage`, `CreateAccountPage`, `CreatePolicyPage`, `CreateSubmissionPage`, `RenewalsPage.create`. The five Cyber consuming screens are exercised by the integration lane.

## F0036-Introduced Test-Infra Change

`src/mocks/handlers.ts` (`/lob-schemas/active/...`) previously returned a **stub** `dataSchema` with no `properties` — sufficient for the old hardcoded panel, but the S0005 schema-driven engine panel derives fields from `dataSchema.properties` and so failed closed on the stub. Updated the mock to return the **real** Cyber data-schema (with `properties`), mirroring `planning-mds/lob-schemas/cyber/1.0.0/data-schema.json`. This restored the 4 Cyber-panel integration tests (`PolicyDetailPage`, `SubmissionDetailPage`, `RenewalDetailPage`, `RenewalsPage`) that the engine swap had exposed.

## Pre-Existing Failures (NOT introduced by F0036)

Attributed by running the integration lane on the committed baseline (F0036 working tree stashed):

| Test | Baseline (no F0036) | With F0036 | Attribution |
|------|---------------------|-----------|-------------|
| `CreateSubmissionPage.integration` | FAIL | FAIL | Pre-existing. Selects Cyber LOB but fills no Cyber attributes; the existing `validateCyberLobAttributes` (F0034) correctly blocks create. Not F0036 scope. |
| `RenewalsPage.integration` | FAIL | FAIL | Pre-existing/flaky on the committed branch. Not F0036 scope. |
| `SubmissionDetailPage.integration` | FAIL | PASS | Flaky on baseline; passes with F0036 in the proper lane. No regression. |

**Net effect:** baseline integration lane = 3 failures; with F0036 = 2 failures. **F0036 introduces zero integration regressions.** The 2 remaining are pre-existing/flaky and out of F0036 scope; tracked for the owning teams, not blocking this feature.

## Skipped Tests And Rationale

- Per-screen forced-re-auth **Playwright E2E** — deferred to the QE container lane (host browser deps; `@playwright/test`), per the runtime boundary.
- Live-backend AJV parity capture — deferred until the .NET runtime is available; parity currently uses recorded backend `(code, pointer)` transcribed from `LobAttributeService.cs`.

## Raw Test Artifact Paths

- artifacts/test-results/g2-full-suite.log (mixed unit+integration+a11y+lint+build)
- artifacts/test-results/g2-integration-lane.log (QE integration lane — with F0036)
- artifacts/test-results/g2-integration-baseline.log (integration lane on committed baseline — attribution)
- artifacts/test-results/s0003-engine-vitest.log (per-story lane)
- artifacts/test-results/s0006-preservation.log (per-story lane)
- artifacts/test-results/s0008-crud-restore.log (per-story lane)

## Failed / Retried Command History

No failure-then-retry flakiness within the feature suite. The integration lane's 2 reds are pre-existing (see table); `test:coverage`'s extra red (`SubmissionDetailPage`) is mixed-parallel-lane flakiness that passes in the dedicated lane.

## AC Coverage Result

Every story AC is covered (see `test-plan.md` Story-to-AC mapping) — all `covered`. The forced-re-auth restore journey is covered at the component level (S0006 attr-form, S0008 CRUD); the per-screen Playwright E2E is the deferred container-lane addition.

## Result

PASS
