# Code Review Report — F0036-dynamic-product-attribute-form-engine run 2026-05-28-077b7b30

> Code Reviewer-owned. Reviews the `experience/**` change set (canonical set: `scm.diff_artifact` = `artifacts/diffs/changed-files.txt`).

## Reviewed Files

The full F0036 change set per `artifacts/diffs/changed-files.txt`:
- **Engine** (`experience/src/features/lob-attributes/engine/**`): `widgetRegistry.ts`, `widgets/index.tsx`, `options.ts`, `deriveWidgets.ts`, `ajvValidator.ts`, `SchemaDrivenForm.tsx`, `usePinnedBundle.ts`, `uiConditionalMap.ts`, `rhfDirtyAdapter.ts`, `FormPreservation.tsx`, `parity/cyber-examples.fixture.ts`, `types.ts`, `index.ts`.
- **Shared forms** (`experience/src/features/forms/**`): `useControlledDirtyTracker.ts`, `useRegisteredForm.ts`, `index.ts`.
- **Panel + bridge**: `components/DynamicAttributePanel.tsx`, `lib/cyber.ts`.
- **F0035 consumption**: `features/session-continuity/index.ts` (additive context export only).
- **~11 wired CRUD components**: brokers (`ContactFormModal`, `EditBrokerModal`), tasks (`TaskCreateModal`, `TaskDetailPanel`), pages (`CreateBrokerPage`, `CreateAccountPage`, `CreatePolicyPage`, `CreateSubmissionPage`, `SubmissionDetailPage`, `AccountDetailPage`, `RenewalsPage`).
- **Deps**: `package.json` (+4 exact-pinned), `pnpm-lock.yaml`.
- **Test infra**: `src/mocks/handlers.ts` (realistic Cyber bundle); colocated `*.test.*` suites.

## Validation Artifacts

`test-execution-report.md` (PASS; 242/242 feature lane, a11y 8/8, integration 19/21 with 2 pre-existing reds attributed via baseline), `coverage-report.md`, `g2-full-suite.log`, `g2-integration-{lane,baseline}.log`, build (exit 0), eslint (0 errors).

## Severity-Ranked Findings

No `critical`, `high`, or `medium` blocking findings. No layer violations, no scope drift outside F0036, no shared-semantics edits (no new canonical nodes — confirmed by `lookup.py`). The engine fails closed on unknown widget/option/malformed bundle (verified by tests).

## Non-Blocking Recommendations With Owner/Follow-up

- [low] CRUD forms restore snapshot values but do not render the F0035 "saved your edits" banner (the attribute panel does) — owner: Frontend; follow-up: deferred-no-followup (minor UX consistency).
- [low] `SubmissionDetailPage` + `AccountDetailPage` edit forms are build-verified only (no dedicated full-page regression test) — owner: QE; follow-up: per-screen E2E in the QE container lane.
- [low] Parity matrix backend side is recorded (transcribed from `LobAttributeService.cs`), not a live HTTP capture — owner: QE; follow-up: capture live when the .NET runtime is available.
- [low] `experience/src/features/lob-attributes/engine/widgets/index.tsx` triggers a `react-refresh/only-export-components` warning (exports `registerMvpWidgets`/`MVP_WIDGET_NAMES` alongside components) — owner: Frontend; follow-up: deferred-no-followup (optional: move constants to a separate module).

These are non-blocking and do not change the APPROVED verdict; they are also recorded in `g2-self-review.md`.

## Vertical-Slice Completeness

Complete end-to-end for a frontend slice: bundle (F0034) → engine derivation/validation → schema-driven panel + CRUD wiring → F0035 preservation/restore → tests. No backend/API change (consumed as contracts). The five Cyber consuming screens are exercised by the integration lane.

## AC / Test Adequacy

Every story AC maps to a test (see `test-plan.md` Story-to-AC table). No AC-without-test in the feature lane. The forced-re-auth restore journey is covered at the component level (S0006 attr-form, S0008 CRUD); per-screen Playwright E2E is the documented deferred addition.

## Architecture Compliance

Conforms to the G0 assembly plan and boundary policy: ADR-021 (RHF + AJV + widget registry; conditional gating §4; controlled-form adapter §6), ADR-022 (validator equivalence — client AJV vs backend `(code, pointer)` parity), ADR-024 (session-continuity preservation, consumed as-is — only an additive context export was added). All changes under `experience/**`; CRUD forms remain controlled (no field-state-library change). Correctness fixes during the run (RHF-owned state for dirty tracking S0006; restore-after-open-reset ordering S0008) are sound.

## Coverage Verification

`coverage-report.md` asserts per-module coverage (each new file has a colocated test); consistent with the `test:coverage` run. No coverage waiver. No drift between the report and the cited run.

## Result

APPROVED
