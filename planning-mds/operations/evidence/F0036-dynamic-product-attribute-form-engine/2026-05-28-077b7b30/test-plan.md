---
template: test-plan
version: 2.0
applies_to: quality-engineer
---

# Test Plan — F0036-dynamic-product-attribute-form-engine run 2026-05-28-077b7b30

> Frontend-only feature. All test lanes run in the `experience/` vitest/jsdom toolchain. Developers own unit/component; QE owns integration + a11y + the parity matrix + the forced-re-auth restore journeys.

## Story-to-AC Mapping

| Story | AC (summary) | Lane | Test ID (file) | Owner |
|-------|----|------|---------|-------|
| S0001 | deps pinned; registry fail-closed | Unit + Build | `engine/__tests__/widgetRegistry.test.ts`; `pnpm build` | Dev |
| S0002 | 10 widgets; theme/a11y; money round-trip; unknown option fail-closed | Unit + a11y | `engine/__tests__/widgets.test.tsx`, `widgets.a11y.test.tsx` | Dev/QE |
| S0003 | schema-driven render; AJV; backend parity; submit-block; lobErrors bind | Unit + Parity | `engine/__tests__/deriveWidgets.test.ts`, `parity.test.ts`, `SchemaDrivenForm.test.tsx` | Dev/QE |
| S0004 | pin-during-edit; no rebind on activation; unresolvable→error | Unit | `engine/__tests__/usePinnedBundle.test.ts` | Dev |
| S0005 | panel swap; 5-screen parity; read-only; conditional gating | Component + Integration | `components/__tests__/DynamicAttributePanel.test.tsx`; `pages/tests/{PolicyDetail,RenewalDetail}.integration.test.tsx` | Dev/QE |
| S0006 | attr-form preservation; restore; no auto-replay; dirty-path flatten | Component | `components/__tests__/DynamicAttributePanel.preservation.test.tsx`, `engine/__tests__/rhfDirtyAdapter.test.ts` | Dev/QE |
| S0007 | controlled tracker; shared helper (dual backend); wire ~11 CRUD forms | Unit + Component | `forms/__tests__/useControlledDirtyTracker.test.ts`, `dualBackend.test.tsx`; per-form: `ContactFormModal`, `EditBrokerModal`, `TaskCreateModal`, `TaskDetailPanel`, `CreateBrokerPage`, `CreateAccountPage`, `CreatePolicyPage`, `CreateSubmissionPage`, `RenewalsPage.create` | Dev/QE |
| S0008 | CRUD restore-on-mount; canonical Contact-Edit; no auto-replay; per-form_key; per-user | Component | `brokers/tests/ContactFormModal.restore.test.tsx` | QE |

## Test Strategy

- **Unit** (dev) — engine (registry, widgets, derivation, AJV, pin), `useControlledDirtyTracker`, `rhfDirtyAdapter`, `flattenDirtyFields`.
- **Component** (dev/QE) — `SchemaDrivenForm`, `DynamicAttributePanel` (regression + preservation), per-CRUD-form regression (payload/validation/error/registration unchanged).
- **Parity** (QE) — `parity.test.ts`: client AJV vs recorded backend `(code, pointer)` over the Cyber examples, 0 disagreements (ADR-022).
- **Integration** (QE) — the five Cyber consuming screens via the `pages/tests/*.integration.test.tsx` lane (MSW-backed).
- **Accessibility** (QE) — `widgets.a11y.test.tsx` (jest-axe light/dark + keyboard/focus); full a11y lane.
- **E2E** (QE, G2/container) — per-screen forced-re-auth restore (Playwright) is QE-owned and deferred to the container lane (host browser deps).

## Developer-vs-QE Test Ownership

- Developer: all unit + component tests (engine, forms, per-form wiring regression).
- QE: parity matrix, integration lane, a11y lane, restore journeys, the MSW bundle fixture, and the per-screen Playwright E2E (container).

## Test Data / Fixtures

- Personas: DistributionManager (integration auth), Underwriter, Broker Relationship Manager.
- Cyber bundle: `planning-mds/lob-schemas/cyber/1.0.0/{data-schema,ui-schema}.json` (parity); MSW `src/mocks/handlers.ts` `/lob-schemas/active/...` updated this run to return the **real** Cyber data-schema (with `properties`) so the schema-driven engine panel derives fields.
- F0035 snapshots: `FormSnapshotRecord` in jsdom `sessionStorage` (cleared per-test by `src/test-setup.ts`).
- Mutation hooks mocked per-form; data-query hooks partial-mocked via `importActual`.

## Happy / Edge / Error / Auth / Accessibility / Regression Cases

- Happy: schema-driven render, widget edit, valid submit, dirty snapshot + restore, explicit re-save.
- Edge: unknown widget/option fail-closed; pin activation race; oversize/skip; cross-user/TTL; per-form_key targeting; conditional gating; malformed bundle controlled error.
- Error: AJV inline + backend `lobErrors`; server 400/409 surfaces unchanged on CRUD.
- Auth: forced re-auth on `401-auth-failed` only; per-user snapshot isolation.
- A11y: jest-axe light/dark, keyboard/focus, programmatic labels, `aria-required`/`role=alert`.
- Regression: 5-screen panel parity; per-CRUD-form create/edit parity vs pre-wiring.

## Risks And Mitigations

- **Risk:** engine panel is now async (bundle load) vs the old synchronous hardcoded panel. **Mitigation:** controlled loading/error region; MSW fixture returns the real bundle; integration tests exercise the loaded panel.
- **Risk:** parity backend side is recorded (transcribed from `LobAttributeService.cs`), not a live capture. **Mitigation:** documented follow-up to capture live when the .NET runtime is available.
- **Residual:** per-screen forced-re-auth E2E and live-backend parity are deferred to QE/G2 container lane.

## Result

PASS
