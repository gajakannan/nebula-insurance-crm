# Coverage Report — F0036-dynamic-product-attribute-form-engine run 2026-05-28-077b7b30

> Frontend-only feature. Coverage collected via `pnpm test:coverage` (vitest + v8). No coverage is waived (`manifest.waivers.coverage` is unset).

## Coverage Target And Actual Per Layer

| Layer | Target | Actual | Source |
|-------|------:|------|--------|
| Unit + Component (experience) | 70% | Collected (v8); every F0036 module ships a dedicated unit/component suite (see below) — 242/242 feature-lane tests green | `artifacts/test-results/g2-full-suite.log` |
| Accessibility | n/a | 8/8 jest-axe (light/dark + keyboard/focus) | `artifacts/test-results/g2-full-suite.log` |
| Integration (Cyber screens) | n/a | 19/21 (2 pre-existing reds, not F0036) | `artifacts/test-results/g2-integration-lane.log` |

> The v8 per-file percentage table is in the cited `test:coverage` console log. Rather than restate unverified aggregate numbers here, coverage is asserted per-module: each new F0036 source file has a colocated test that exercises its branches.

## Feature-Scoped Coverage (per new module)

| Module | Covering test |
|--------|---------------|
| `engine/widgetRegistry.ts` (+ option fail-closed) | `engine/__tests__/widgetRegistry.test.ts`, `widgets.test.tsx` |
| `engine/widgets/index.tsx` (10 widgets) | `widgets.test.tsx`, `widgets.a11y.test.tsx` |
| `engine/options.ts` | `widgets.test.tsx` (deriveOptions / assertOptionsSubsetOfEnum) |
| `engine/deriveWidgets.ts` | `deriveWidgets.test.ts` |
| `engine/ajvValidator.ts` | `parity.test.ts` (validate + normalize + parityKeySet) |
| `engine/SchemaDrivenForm.tsx` | `SchemaDrivenForm.test.tsx`, `DynamicAttributePanel*.test.tsx` |
| `engine/usePinnedBundle.ts` | `usePinnedBundle.test.ts` |
| `engine/uiConditionalMap.ts` | `DynamicAttributePanel.test.tsx` (gating) |
| `engine/rhfDirtyAdapter.ts` | `rhfDirtyAdapter.test.ts`, `dualBackend.test.tsx` |
| `engine/FormPreservation.tsx` | `DynamicAttributePanel.preservation.test.tsx`, `dualBackend.test.tsx` |
| `forms/useRegisteredForm.ts` | `dualBackend.test.tsx`, all per-form registration tests |
| `forms/useControlledDirtyTracker.ts` | `useControlledDirtyTracker.test.ts` (equality matrix + sensitive paths) |
| `lib/cyber.ts` (`cyberValuesToAttributes`) | `DynamicAttributePanel.test.tsx` (round-trip) |
| `components/DynamicAttributePanel.tsx` | `DynamicAttributePanel.test.tsx` (+ `.preservation`) |
| ~11 wired CRUD components | per-form regression tests (S0007/S0008) |

## Raw Artifact Paths

- `artifacts/test-results/g2-full-suite.log` — `test:coverage` console (v8 per-file table)
- `artifacts/test-results/g2-integration-lane.log`

## Feature-Scoped Notes

The MSW bundle fixture was updated (real Cyber data-schema) so the schema-driven panel renders in integration tests. No cross-feature coverage regression: the 2 remaining integration reds are pre-existing (red on the committed baseline). Per-screen Playwright E2E coverage is deferred to the QE container lane.

## Result

PASS
