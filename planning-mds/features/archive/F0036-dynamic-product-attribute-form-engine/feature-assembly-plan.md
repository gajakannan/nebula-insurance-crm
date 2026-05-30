# Feature Assembly Plan — F0036: Form Engine and Form-State Preservation (RHF + AJV + Widget Registry)

**Created:** 2026-05-28
**Author:** Architect Agent
**Status:** Draft
**Run:** `2026-05-28-077b7b30` (feature action G0 Step 0)

> **Purpose:** Implementation execution plan for F0036. This is a **frontend-only** feature (`experience/**`); per the template note, the entity/DTO/endpoint sections are replaced with component/hook/contract specs. No `engine/**`, schema, bundle, or deployment change.
>
> **Source precedence:** raw stories `F0036-S0001..S0008`, `PRD.md`, ADR-021 (amended), ADR-022/024, and the shipped F0035 contract win over any KG mapping. Where this plan conflicts with raw story text, this plan wins and the reconciliation is logged via `workstate.py decision --topic plan-story-reconcile`.

## Overview

F0034 shipped `DynamicAttributePanel` as a hardcoded Cyber panel (controlled `value`/`onChange`/`errors`, lifted parent state) with no RHF, AJV, or widget registry; `react-hook-form`/`ajv` are not dependencies. F0035 shipped a form-state-preservation registry wired to **zero** forms. F0036 (A) realizes the ADR-021 engine for LOB product attributes (Cyber pilot) and (B) registers every in-scope mutation form — the new RHF product-attribute form **and** the existing controlled CRUD forms (via a controlled-form dirty-tracker adapter) — with the F0035 registry, fully closing F0035 finding #1 **without rewriting any CRUD field-state mechanism**.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Deps + engine skeleton + widget-registry contract | S0001 | Everything downstream imports the registry contract + RHF/AJV. Fail-closed registry first. |
| 2 | MVP widget vocabulary (10 widgets) + theme/a11y | S0002 | Widgets are the registry's leaf nodes; needed before schema-driven rendering. |
| 3 | Schema-driven rendering + AJV client validation + backend parity | S0003 | Composes widgets from `data-schema.json`; parity harness vs actual backend. |
| 4 | Pin-during-edit binding to `(productVersionId, stage)` | S0004 | Edit-session invariant; small, isolatable once rendering exists. |
| 5 | Replace hardcoded Cyber `DynamicAttributePanel` (5-screen regression) | S0005 | Swap panel internals to the engine behind the same props; regression-gated. |
| 6 | Wire product-attribute form into F0035 registry + restore (RHF adapter) | S0006 | First real consumer of the shared registration helper. |
| 7 | `useControlledDirtyTracker` + library-agnostic shared registration helper; wire ~11 CRUD forms | S0007 | Workstream B mechanism; controlled forms stay controlled. |
| 8 | Register controlled CRUD forms + restore on mount; close F0035 S0003 Contact-Edit | S0008 | End-to-end closure of F0035 finding #1. |

## Existing Code (Must Be Modified)

> Paths verified against the working tree on 2026-05-28 (`hint.py experience/src/features/lob-attributes`, `…/session-continuity`).

| File | Current State | F0036 Change |
|------|---------------|--------------|
| `experience/package.json` | no `react-hook-form`/`ajv`/`ajv-formats`/`ajv-errors` | **Add** 4 exact (non-caret) deps (S0001) |
| `experience/src/features/lob-attributes/components/DynamicAttributePanel.tsx` | hardcoded Cyber panel; 9 fixed fields; controlled props; `useCyberSchemaBundle` used only for a status string; MFA-maturity gated by `value.mfaEnabled` (lines 135-136) | **Rewrite** internals onto the engine; keep the public prop surface (`lineOfBusiness`/`value`/`onChange`/`errors`/`readOnly`/`actions`) for the 5 consumers (S0005) |
| `experience/src/features/lob-attributes/index.ts` | re-exports panel + hook + types | **Expand** — export engine + widget registry + registration helper (S0001/S0006) |
| `experience/src/features/lob-attributes/types.ts` | `CyberLobAttributeValues`, `LobSchemaBundleDto`, `LobValidationIssueDto`, `LobAttributeEnvelopeDto` | **Expand** — engine field-model + widget-spec + `UiConditionalMap` types (S0001/S0003) |
| 5 consuming screens: `CreateSubmissionPage`, `CreatePolicyPage`, `PolicyDetailPage`, `RenewalDetailPage`, `SubmissionDetailPage` | render `<DynamicAttributePanel>` with controlled state | **No prop change**; behavior parity verified by per-screen regression (S0005); native CRUD fields on Create Submission/Policy join Workstream B (S0007) |
| ~11 CRUD components (S0007 inventory) | plain controlled forms | **Add** `useControlledDirtyTracker` + shared registration helper call only — no field-state/validation/submit change (S0007/S0008) |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `experience/src/features/lob-attributes/engine/widgetRegistry.ts` | engine | Registry map + fail-closed `resolveWidget(name)`; throws on unknown widget/option/layout |
| `experience/src/features/lob-attributes/engine/widgets/*.tsx` | engine | 10 MVP widgets (text, textarea, number, money-minor, select, multi-select, checkbox, date, section, read-only summary) |
| `experience/src/features/lob-attributes/engine/deriveWidgets.ts` | engine | `data-schema.json` (type/enum/format) → widget derivation; `ui-schema.json` for layout/labels only |
| `experience/src/features/lob-attributes/engine/SchemaDrivenForm.tsx` | engine | RHF-managed renderer composing sections + widgets from the pinned bundle |
| `experience/src/features/lob-attributes/engine/ajvValidator.ts` | engine | AJV instance (ajv-formats + ajv-errors) compiled against `data-schema.json` |
| `experience/src/features/lob-attributes/engine/uiConditionalMap.ts` | engine | Declarative presentational gating (e.g. MFA-maturity-enabled-when-MFA-enabled); frontend config, not the bundle (ADR-021 §4) |
| `experience/src/features/lob-attributes/engine/usePinnedBundle.ts` | engine | Pins `(productVersionId, stage)` at open; no rebind on activation (S0004) |
| `experience/src/features/lob-attributes/engine/useRegisteredForm.ts` | engine | **Shared, library-agnostic** F0035 registration helper: owns `form_key` shape, dirty-path contract, `consumeFormSnapshot` rehydration on mount; accepts a `DirtyFormRegistration`-shaped source (S0006/S0007) |
| `experience/src/features/lob-attributes/engine/rhfDirtyAdapter.ts` | engine | RHF → `DirtyFormRegistration` source (`getValues`, `formState.isDirty`, `dirtyFields` flattened to paths) (S0006) |
| `experience/src/features/forms/useControlledDirtyTracker.ts` | shared | Controlled-form → `DirtyFormRegistration` source via deep-equality diff vs `initialValues` (S0007) |
| `experience/src/features/lob-attributes/engine/parity/*.fixture.json` | test | Cyber parity fixture matrix (client AJV vs backend `(code, pointer)` multiset) (S0003) |
| `experience/src/features/lob-attributes/**/__tests__/*.test.tsx` + `experience/tests/e2e/**` | test | Component/integration/Playwright per ADR-021 + §9 |

---

## Step 1 — Engine skeleton + widget-registry contract (S0001)

### New Files
| File | Layer |
|------|-------|
| `engine/widgetRegistry.ts` | engine |
| `engine/types.ts` (or extend `../types.ts`) | engine |

### Contract
```ts
// experience/src/features/lob-attributes/engine/widgetRegistry.ts
export type WidgetName =
  | 'text' | 'textarea' | 'number' | 'money-minor' | 'select'
  | 'multi-select' | 'checkbox' | 'date' | 'section' | 'readonly-summary'

export interface WidgetProps<TValue = unknown> {
  fieldPath: string
  label: string
  value: TValue
  onChange: (next: TValue) => void
  error?: string
  required?: boolean
  disabled?: boolean
  options?: ReadonlyArray<{ value: string; label: string }>
}

export type WidgetComponent = (props: WidgetProps) => JSX.Element

// Fail-closed: throws on an unregistered widget name (ADR-021 governance).
export function resolveWidget(name: string): WidgetComponent
export function registerWidget(name: WidgetName, component: WidgetComponent): void
```

### Logic Flow
`resolveWidget(name)` → looks up the frozen registry map → if absent, `throw new Error(\`unknown widget: ${name}\`)` (NOT a silent fallback render). Bundle activation already fails closed server-side; the frontend must mirror that, never render around an unknown widget/option/layout primitive.

### Mutation Traceability
N/A — read-only infrastructure (no entity mutation; registry + deps only). Confirmed read-only by S0001 (`Required checks` state N/A audit/timeline for infra).

### Acceptance (S0001)
- 4 exact (non-caret) deps added; `pnpm install` clean; lockfile updated.
- Registry contract compiles; unknown-widget lookup **throws** (unit test).

---

## Step 2 — MVP widget vocabulary (S0002)

### New Files
`engine/widgets/{Text,Textarea,Number,MoneyMinor,Select,MultiSelect,Checkbox,DateField,Section,ReadonlySummary}.tsx` registered into `widgetRegistry`.

### Contract notes
- Each widget reuses existing `@/components/ui/*` (`TextInput`, `Select`) where possible to preserve theme tokens.
- `money-minor`: stores minor units (integer); displays major; round-trips losslessly (unit test).
- `select`/`multi-select`: options derive from the data-schema `enum`; an unknown option **fails closed**.
- Each widget exposes an inline error slot + required/disabled affordance.

### Mutation Traceability
N/A — presentational widgets; no mutation (S0002 states N/A audit/timeline for presentational).

### Acceptance (S0002)
- 10 registry entries; light/dark smoke + keyboard/focus coverage per widget; `lint:theme` clean.

---

## Step 3 — Schema-driven rendering + AJV client validation + backend parity (S0003)

### New Files
`engine/deriveWidgets.ts`, `engine/ajvValidator.ts`, `engine/SchemaDrivenForm.tsx`, `engine/parity/*.fixture.json`.

### Logic Flow
`SchemaDrivenForm({ bundle, value, onChange, readOnly })`:
1. `deriveWidgets(bundle.dataSchema)` → ordered field model (type/enum/format → `WidgetName`); `ui-schema.json` supplies section grouping + labels **only**.
2. RHF `useForm` holds field state; widgets are RHF-controlled.
3. AJV (`ajv` + `ajv-formats` + `ajv-errors`) compiled from `data-schema.json` validates client-side; inline messages render per field.
4. **Parity contract (ADR-021 §3 / ADR-022):** client AJV is measured against the *actual backend* by `(code, pointer)` multiset equality over the Cyber published examples. Cross-field rules (`mfa_required_for_high_record_count`, `minimum_retention_not_met`) stay **backend-authoritative**, surfaced via `LobValidationProblemDetails.lobErrors[]` bound to fields by `pointer`. The client keeps **no** authoritative rule duplicate and does **not** evaluate `rules.json` (no ADR-023 dependency).

### Mutation Traceability
| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|---|---|---|---|---|---|---|---|---|---|
| Host screen (Submission/Policy) embedding the panel | Edit attributes → host Save | existing F0034/F0019 host endpoints (unchanged) | unchanged backend service | `LobAttributeEnvelope.attributes` | host-screen auth (401/403 upstream; unchanged) | host If-Match/rowVersion (unchanged) | inline AJV (client) + backend `lobErrors[]` (authoritative) on submit | existing backend entity event on host save (no new event) | parity fixture matrix = 0 disagreements; blocked submit on invalid; backend rule surfaced inline |

### Acceptance (S0003)
- Parity fixture matrix: 0 disagreements on Cyber examples.
- Required-empty + cross-field-rule cases produce correct inline messages (client AJV + backend `lobErrors[]`).

---

## Step 4 — Pin-during-edit (S0004)

### New Files
`engine/usePinnedBundle.ts`.

### Logic Flow
`usePinnedBundle(productVersionId, stage)` captures the `(productVersionId, stage)` tuple at first open into a ref; subsequent product-version activations do **not** rebind an open form. Unresolvable version → controlled error (no silent fallback). Pinned tuple is immutable for the session and recorded with the host save.

### Mutation Traceability
N/A for the hook itself (read/binding); the pinned version is recorded by the host save path (unchanged backend event).

### Acceptance (S0004)
- Activation race: opening a form, activating a new version, the open form keeps the pinned bundle (unit + integration).

---

## Step 5 — Replace hardcoded Cyber panel, 5-screen regression (S0005)

### Modified Files
`components/DynamicAttributePanel.tsx` (internals → `SchemaDrivenForm`; **public prop surface unchanged**), `engine/uiConditionalMap.ts`.

### Logic Flow
- `DynamicAttributePanel` keeps its props; internally pins the Cyber bundle, derives widgets, renders `SchemaDrivenForm`, maps the engine field model to/from `CyberLobAttributeValues` for the existing controlled consumers (adapter layer so the 5 screens are untouched).
- Presentational MFA-maturity gating moves from hardcoded `value.mfaEnabled` (today lines 135-136) to the declarative `uiConditionalMap` (ADR-021 §4) — same observable behavior, no bundle change, no ad-hoc JSX.

### Mutation Traceability
| Screen | User Action | Endpoint | Service | Carrier | Authorization | Concurrency | Validation Failure | Audit/Timeline | Test Expectation |
|---|---|---|---|---|---|---|---|---|---|
| CreateSubmission / CreatePolicy / PolicyDetail / RenewalDetail / SubmissionDetail | create/edit/read-only attribute entry → host save | existing host endpoints (unchanged) | unchanged | `LobAttributeEnvelope.attributes` | per-screen auth unchanged | host rowVersion unchanged | controlled bundle-load failure; inline validation unchanged semantics | existing host event still fires | per-screen baseline-parity regression (create + edit + read-only) green before and after swap |

### Acceptance (S0005)
- All 5 screens: create/edit/read-only behavior unchanged; conditional gating preserved; bundle-load failure controlled.

---

## Step 6 — Product-attribute form preservation via F0035 (S0006)

### New Files
`engine/useRegisteredForm.ts` (shared helper), `engine/rhfDirtyAdapter.ts`.

### Logic Flow — shared helper (F0035 contract, verified shipped):
`DirtyFormRegistration = { formKey, route, isDirty, getValues, getDirtyFieldPaths }` (`dirtyFormRegistryContext.ts`).
`useRegisteredForm(source: DirtyFormRegistration)`:
1. `useSessionRestorableForm(source)` (registers; returns unregister on unmount).
2. On mount, `consumeFormSnapshot(userId, formKey)` → if a non-expired record exists (TTL 1h, ≤ 256 KB per `sessionRestore.ts`), rehydrate values; **no auto-replay** — Save returns enabled, user explicitly re-saves.
3. `form_key` shape owned here (stable per form/route); oversize/skip follows F0035 `SnapshotResult.skippedCause` (`oversize`/`storage_unavailable`) → route-only fallback + inline "unable to preserve" message.
`rhfDirtyAdapter(form)` → `{ isDirty: () => form.formState.isDirty, getValues: form.getValues, getDirtyFieldPaths: () => flatten(form.formState.dirtyFields) }`.

### Mutation Traceability
| Screen | User Action | Endpoint | Service | Carrier | Authorization | Concurrency | Validation Failure | Audit/Timeline | Test Expectation |
|---|---|---|---|---|---|---|---|---|---|
| Cyber attribute form (dirty) → forced re-auth → return | edit → 401-auth-failed → restore → explicit re-save | host save endpoint (unchanged) | unchanged | sessionStorage snapshot (`FormSnapshotRecord`) + `LobAttributeEnvelope` | per-user snapshot isolation; restore only on `401-auth-failed` (not 403) | pinned bundle preserved across restore | oversize/skip → route-only fallback | only explicit re-save emits the existing entity event (no `mutation-auto-replayed`) | E2E forced-re-auth restore journey green (the test F0035 could not provide) |

### Acceptance (S0006)
- Forced-re-auth on dirty Cyber form restores in-flight values; pinned `(productVersionId, stage)` survives; no auto-replay; oversize path handled.

---

## Step 7 — `useControlledDirtyTracker` + shared helper; wire ~11 CRUD forms (S0007, Workstream B)

### New Files
`experience/src/features/forms/useControlledDirtyTracker.ts`.

### Logic Flow
`useControlledDirtyTracker(values, initialValues, opts?: { excludePaths?: string[] })` → `DirtyFormRegistration` source:
- `isDirty()` = deep value-equality diff vs `initialValues` (NOT reference equality).
- `getValues()` = current `values` (with `excludePaths` redacted for sensitive fields).
- `getDirtyFieldPaths()` = flattened changed leaf paths.
- Equality matrix is the unit-test contract: typed-and-cleared, edited-then-reset (clean), array-reorder, identical-object-replacement (clean), nested change (dirty).

### Workstream B inventory (~11 components, operator-confirmed no deferrals 2026-05-27)
broker (`CreateBrokerPage`, `EditBrokerModal`), account (`CreateAccountPage`, `AccountDetailPage` incl. account-contact edit), contact (`ContactFormModal` create+edit), task (`TaskCreateModal`, `TaskDetailPanel` inline edit), submission native (`CreateSubmissionPage`, `SubmissionDetailPage`), policy native (`CreatePolicyPage`), renewal create (`RenewalsPage` modal). Excluded with reason: filters, action-dialogs, bulk-import, upload (no in-flight state worth preserving).

### Mutation Traceability
| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|---|---|---|---|---|---|---|---|---|---|
| Each of the ~11 CRUD forms | create/edit → existing submit | existing per-form endpoints (unchanged) | unchanged | existing entity + sessionStorage snapshot | per-form auth unchanged | existing per-form rowVersion/If-Match unchanged | existing server 400/409 surfaces the **same** message (no reshape) | existing create/update event (no new event) | per-form create+edit smoke regression green before & after registration; equality matrix unit tests |

### Acceptance (S0007)
- Tracker equality matrix green; each form's create+edit regression unchanged; registration is render-side only (no submit/network change).

---

## Step 8 — Register controlled CRUD forms + restore; close F0035 S0003 (S0008)

### Logic Flow
Each in-scope CRUD form calls `useRegisteredForm(useControlledDirtyTracker(values, initialValues, opts))` — same shared helper path as Workstream A. On mount, `consumeFormSnapshot` rehydrates; stable `initialValues` ref prevents re-marking dirty after restore.

### Mutation Traceability
| Screen | User Action | Endpoint | Service | Carrier | Authorization | Concurrency | Validation Failure | Audit/Timeline | Test Expectation |
|---|---|---|---|---|---|---|---|---|---|
| Contact Edit → "Notes" dirty → forced re-auth → restore → explicit re-save (F0035 S0003 canonical) | edit → 401 → restore → re-save | existing contact update endpoint | unchanged | snapshot + Contact entity | per-user isolation; 401-only | n/a | route-only fallback on skip | only explicit re-save emits existing Contact update event | E2E: canonical Contact-Edit scenario passes; concurrent dirty forms each restore by `form_key` |

### Acceptance (S0008)
- F0035 S0003 canonical scenario passes end-to-end; cross-user/TTL/sign-out/oversize handled; concurrent forms restore independently; F0035 finding #1 fully closed.

---

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`engine/`) | **None** — consumes F0034/F0035 contracts as-is; backend validation authoritative | — | N/A |
| Frontend (`experience/`) | Engine (registry, 10 widgets, schema-driven renderer, AJV, pin, UI-conditional map), panel swap, shared registration helper + RHF adapter, `useControlledDirtyTracker`, wire ~11 CRUD forms | Frontend Developer | Pending (Step 1) |
| AI (`neuron/`) | None | — | N/A |
| Quality | Component/integration tests, parity fixture matrix, 5-screen regression, equality matrix, 2 E2E forced-re-auth restore journeys (attr + CRUD) | Quality Engineer | Pending |
| DevOps/Runtime | None — new deps are bundled; no deploy/runtime/env change (DevOps `Required = No`) | — | N/A |

## Dependency Order

```
Step 0 (Architect):   this assembly plan + STATUS signoff matrix          [G0]
Step 1 (Frontend):    deps + widgetRegistry contract (fail-closed)
Step 2 (Frontend):    10 MVP widgets
  ──── Frontend checkpoint: registry resolves all 10; unknown throws ────
Step 3 (Frontend):    deriveWidgets + AJV + SchemaDrivenForm + parity fixtures
Step 4 (Frontend):    usePinnedBundle
  ──── Frontend checkpoint: parity 0 disagreements; pin holds on activation ────
Step 5 (Frontend):    DynamicAttributePanel swap + uiConditionalMap
  ──── Regression checkpoint: 5 screens create/edit/read-only parity ────
Step 6 (Frontend):    useRegisteredForm + rhfDirtyAdapter (attr form preservation)
Step 7 (Frontend):    useControlledDirtyTracker + wire ~11 CRUD forms
Step 8 (Frontend/QE): CRUD restore-on-mount + F0035 S0003 E2E closure
  ──── QE checkpoint: 2 E2E restore journeys + per-form regression ────
```

## Integration Checkpoints

### After Step 2 (Widgets)
- [ ] All 10 MVP widgets resolve from the registry; unknown widget/option throws (fail-closed).
- [ ] light/dark + keyboard/focus smoke per widget.

### After Step 4 (Rendering + Pin)
- [ ] Cyber form renders entirely from the bundle (no Cyber-specific JSX in the render path).
- [ ] Client AJV vs actual backend: 0 disagreements on the Cyber example matrix.
- [ ] Pin-during-edit holds across a mid-session activation.

### After Step 5 (Panel swap)
- [ ] 5 consuming screens: create + edit + read-only behavior unchanged (per-screen regression).
- [ ] MFA-maturity gating preserved via `uiConditionalMap`.

### Cross-Story Verification
- [ ] Full lifecycle: dirty attribute form → forced re-auth (`401-auth-failed`) → values restored → explicit re-save emits the existing host event (no auto-replay).
- [ ] Full lifecycle: F0035 S0003 Contact-Edit canonical scenario passes end-to-end.
- [ ] 100% of the ~11-component CRUD inventory registers with the F0035 registry; each survives forced re-auth.
- [ ] No new timeline event classes introduced; no `mutation-auto-replayed`.
- [ ] No backend/schema/bundle change; backend validation remains authoritative.

## Integration Checklist

- [ ] API contract compatibility validated (F0034 bundle, F0035 registry consumed as-is)
- [ ] Frontend contract compatibility validated (`DynamicAttributePanel` prop surface unchanged for 5 consumers)
- [ ] AI contract compatibility validated — N/A
- [ ] Test cases mapped to acceptance criteria (per-story; QE `test-plan.md`)
- [ ] Developer-owned fast-test responsibilities identified by layer (component/unit dev-owned; E2E QE-owned)
- [ ] Required runtime evidence artifacts identified (pnpm lint/lint:theme/build/test logs; coverage; Playwright in container if host libs missing)
- [ ] Framework vs solution boundary reviewed (all changes under `experience/**`; no `agents/**` drift)
- [ ] Mutation traceability tables completed for every mutating path (above)
- [ ] Render-only tests not used to close mutation stories (CRUD forms get create+edit regression)
- [ ] Run/deploy instructions updated in `GETTING-STARTED.md`

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Schema-driven rendering subtly regresses 5 Cyber screens | High | Express current Cyber semantics exactly; per-screen regression before swap | Frontend/QE |
| Client AJV vs backend divergence | High | Parity fixture matrix; backend authoritative on submit; cross-field rules via `lobErrors[]` | Frontend/QE |
| `useControlledDirtyTracker` over/under-snapshots (array reorder, typed-then-cleared) | Medium | Deep value equality + explicit equality matrix as the unit-test contract | Frontend |
| Registration accidentally changes CRUD render/submit (stale `initialValues` re-marks dirty) | Medium | Render-side-only helper; stable `initialValues` ref; per-form smoke regression | Frontend |
| Snapshot may transiently include `InternalOnly` fields (ADR-024 boundary) | Medium → **Security required** | `excludePaths` for sensitive fields; per-user sessionStorage isolation; Security review confirms boundary + no-auto-replay + auth-error semantics | Security |
| Playwright host deps (`libnspr4`/`libnss3`) missing | Low | Run Playwright in the project runtime container; record container command in evidence | QE |

## JSON Serialization Convention

camelCase throughout (existing convention). `money-minor` widgets store integer minor units; snapshot records follow `FormSnapshotRecord` (`user_id`/`form_key`/`dirty_field_paths` snake_case per shipped `sessionRestore.ts`).

## DI Registration Changes

None (frontend feature). `DirtyFormRegistryProvider` already mounted by F0035's `SessionContinuityProvider`.

## Casbin Policy Sync

None — no authorization artifact change. F0036 introduces no endpoint, resource, or policy. (`policy_rule:renewal-update` surfaced by `hint.py` belongs to F0034/renewal, not F0036.)
