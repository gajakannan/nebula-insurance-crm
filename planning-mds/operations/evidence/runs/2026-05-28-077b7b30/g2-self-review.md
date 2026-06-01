# Self Review — F0036-dynamic-product-attribute-form-engine run 2026-05-28-077b7b30

## Scope Review

Implemented scope matches the G0 `feature-assembly-plan.md` and the PRD: all 8 stories (S0001–S0008) across both workstreams, entirely within `experience/**` (plus the F0036 planning/tracker/KG artifacts). No `engine/**`, schema, bundle, or deployment change — backend F0034/F0035 contracts consumed as-is.

Reconciled drift from the plan:
- **Workstream A engine** realized per ADR-021 (registry, 10 widgets, schema-driven render + AJV, pin-during-edit, panel swap, F0035 preservation).
- **Workstream B** kept CRUD forms controlled (no field-state-library rewrite, per the 2026-05-27 refinement / ADR-021 §6); the shared `useRegisteredForm` helper is library-agnostic (RHF adapter + controlled tracker).
- **One test-infra change** outside `experience/src` feature code: `src/mocks/handlers.ts` Cyber bundle made realistic so the schema-driven panel renders in integration tests (documented in `test-execution-report.md`).

## Acceptance Criteria Review

- S0001 deps + registry fail-closed — `test-execution-report.md` (unit: `widgetRegistry.test.ts`) + build.
- S0002 widget vocabulary + theme/a11y — `widgets.test.tsx`, `widgets.a11y.test.tsx`.
- S0003 schema render + AJV + backend parity (0 disagreements) — `parity.test.ts`, `deriveWidgets.test.ts`, `SchemaDrivenForm.test.tsx`.
- S0004 pin-during-edit — `usePinnedBundle.test.ts`.
- S0005 panel swap + 5-screen parity + gating — `DynamicAttributePanel.test.tsx` + integration lane (PolicyDetail/RenewalDetail green).
- S0006 attr-form preservation + restore + no auto-replay — `DynamicAttributePanel.preservation.test.tsx`, `rhfDirtyAdapter.test.ts`.
- S0007 controlled tracker + dual-backend helper + ~11 wired CRUD forms — `useControlledDirtyTracker.test.ts`, `dualBackend.test.tsx`, per-form regression suites.
- S0008 CRUD restore-on-mount + canonical Contact-Edit + no auto-replay + per-form_key/per-user — `ContactFormModal.restore.test.tsx`.

## Implementation Risks

- **[low] Engine panel is now async (bundle load)** vs the old synchronous hardcoded panel — owner: Frontend; follow-up: deferred-no-followup. Mitigated by a controlled loading/error region; integration tests exercise the loaded panel.
- **[low] Parity backend side is recorded (transcribed from `LobAttributeService.cs`)**, not a live capture — owner: QE; follow-up: capture live when the .NET runtime is available.
- **[low] CRUD forms restore values but do not render the F0035 "saved your edits" banner** (the attribute panel does) — owner: Frontend; follow-up: deferred-no-followup (minor UX gap).
- **[low] SubmissionDetailPage + AccountDetailPage edit forms are build-verified only** (no dedicated full-page regression test) — owner: QE; follow-up: per-screen E2E at G2 container lane.

## Validation Evidence

- `g1-runtime-preflight.md` (frontend toolchain), `test-plan.md`, `test-execution-report.md` (PASS), `coverage-report.md`, `deployability-check.md`.
- `artifacts/test-results/g2-full-suite.log`, `g2-integration-lane.log`, `g2-integration-baseline.log` (pre-existing-failure attribution).
- `artifacts/diffs/changed-files.txt` (80-path change set).
- Build/eslint/lint:theme/lint:effects all green; a11y 8/8; F0036 feature lane 242/242.

## Result

PASS
