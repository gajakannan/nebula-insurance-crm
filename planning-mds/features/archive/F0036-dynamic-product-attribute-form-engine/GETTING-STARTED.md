# F0036 — Form Engine and Form-State Preservation — Getting Started

> Plan-time skeleton (plan run `2026-05-25-51ff2a92`). Key paths below are the **targets** the feature action will create/modify, grounded in the current-state anchors verified 2026-05-25. Implementing agents fill in concrete details (commands, seed data, verification) as they build. F0036 is a **frontend-only** feature — no backend/migration/bundle changes.

## Prerequisites

- [ ] Frontend dev environment running (`pnpm --dir {PRODUCT_ROOT}/experience dev`)
- [ ] Backend running with the F0034 schema-bundle registry available (Cyber `cyber/1.0.0` bundle resolvable)
- [ ] F0035 session-continuity providers mounted (`SessionContinuityProvider`, `DirtyFormRegistryProvider`)
- [ ] No new migration or seed data required (consumes existing F0034/F0035 contracts)

## Services to Run

```bash
# Frontend (primary surface for this feature)
pnpm --dir {PRODUCT_ROOT}/experience dev
# Backend (for AJV/backend parity checks and bundle resolution)
# (project-standard backend run command — see repo root README)
```

## Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| _(none new)_ | F0036 introduces no new env vars; reuses F0034/F0035 frontend config | — |

## Seed Data

- No new seed data. The Cyber `cyber/1.0.0` bundle (`planning-mds/lob-schemas/cyber/1.0.0/`) supplies `data-schema.json`, `ui-schema.json`, and the published examples that drive the client-AJV/backend parity fixture matrix (S0003). The bundle also ships `rules.json`, whose cross-field rules are enforced backend-side (not a client-evaluated parity layer).

## Engine Module (S0001 — implemented)

The dynamic form engine lives at `experience/src/features/lob-attributes/engine/`:

- `types.ts` — engine contract types: `WidgetName`, `WidgetProps`, `WidgetRegistry`, `WidgetRegistryEntry`, `PinnedBundle`, `EngineFormProps`.
- `widgetRegistry.ts` — `createWidgetRegistry()` (single source of widget resolution; `resolve()` **throws `UnknownWidgetError`** on an unregistered name — no inline fallback) and `assertOptionAllowed()` (`UnknownWidgetOptionError`, fail-closed per ADR-021).
- `SchemaDrivenForm.tsx` — engine entry-component skeleton; accepts a `PinnedBundle` + `WidgetRegistry` and renders only mapped content (S0001 renders `ui-schema` section scaffold; field rendering + AJV land in S0002/S0003).
- Exported via `engine/index.ts` and re-exported from the feature barrel `lob-attributes/index.ts`.

Engine dependencies (exact, non-caret): `react-hook-form@7.76.1`, `ajv@8.20.0`, `ajv-formats@3.0.1`, `ajv-errors@3.0.0`.

**Registered MVP widgets (S0002 — `engine/widgets/`):** `text`, `textarea`, `number`, `money-minor`, `select`, `multi-select`, `checkbox`, `date`, `section`, `readonly-summary`. Register them into a registry with `registerMvpWidgets(createWidgetRegistry())`. Options for `select`/`multi-select` derive from the data-schema `enum` via `deriveOptions()`; a configured option outside the enum fails closed via `assertOptionsSubsetOfEnum()` (`UnknownWidgetOptionError`). `money-minor` stores integer minor units and displays the major unit. Widgets are theme-token-only (pass `lint:theme`/`lint:effects`) and accessibility-covered (jest-axe light/dark, keyboard focus).

**F0035 preservation (S0006):** the shared library-agnostic registration helper `useRegisteredForm` (`experience/src/features/forms/`) registers a form with the F0035 dirty-form registry and rehydrates a prior snapshot on mount. Workstream A supplies the source via `rhfDirtyAdapter` (`engine/rhfDirtyAdapter.ts`, RHF `formState.dirtyFields`→dotted paths); `engine/FormPreservation.tsx` mounts it for the engine form (only when a `preserve` config is given, so provider-less rendering is unaffected). `DynamicAttributePanel` auto-engages preservation for a logged-in user (`useCurrentUser`) on editable forms (`form_key = cyber-attributes:<route>`); on a forced re-auth F0035 snapshots the dirty values, and on return the form rehydrates and shows the "We saved your edits…" notice — **never auto-saving** (the user re-saves explicitly). `SchemaDrivenForm` RHF-owns its field state (so dirty tracking survives the controlled round-trip and actually snapshots). Follow-ups: store the pinned `(productVersionId, stage)` in the snapshot for multi-version LOBs; oversize/TTL/sign-out are inherited from F0035; the per-screen forced-re-auth E2E is QE-owned (G2).

**Workstream B — controlled CRUD form preservation (S0007/S0008):** `experience/src/features/forms/useControlledDirtyTracker.ts` produces the F0035 `DirtyFormRegistration` triple for a plain controlled form by deep-diffing current vs a stable `initialValues` ref (deep-equality; `sensitiveFieldPaths` default-deny; order-insensitive arrays by default). The same shared `useRegisteredForm` helper registers both backends (RHF adapter for the attribute form, controlled tracker for CRUD). **All ~11 in-scope CRUD components are wired** (broker create/edit, contact create/edit, account create + account/account-contact edit, task create + inline edit, submission create/edit native, policy create native, renewal create) — controlled forms unchanged, registration is render-side only. On a forced re-auth the dirty values snapshot; on return the form **restores on mount** (the helper consumes its `form_key` snapshot). For edit modals the registration is placed **after** the on-open reset so a restored snapshot wins over the server reset. **No auto-replay** — the user re-saves explicitly. **F0035 finding #1 is closed:** no in-scope mutation form loses unsaved input across a forced re-auth (product-attribute form via S0006, CRUD forms via S0007/S0008). Follow-up: CRUD forms restore values but do not yet render the F0035 "saved your edits" banner (the attribute panel does).

**Engine-backed panel (S0005 — `components/DynamicAttributePanel.tsx`):** the panel is reimplemented on the engine but keeps its exact public prop surface (`lineOfBusiness`/`value: CyberLobAttributeValues`/`onChange`/`errors`/`readOnly`/`actions`), so the five consuming screens — `CreateSubmissionPage`, `CreatePolicyPage`, `PolicyDetailPage`, `RenewalDetailPage`, `SubmissionDetailPage` — are unchanged (a green `pnpm build` of those screens is the drop-in proof). Internally it bridges flat↔nested via `cyberValuesToAttributes` / `normalizeCyberEnvelope`, pins the Cyber bundle (`usePinnedBundle`), and gates MFA-maturity through the declarative `CYBER_UI_CONDITIONAL_MAP` (`engine/uiConditionalMap.ts`, ADR-021 §4) — the validation half stays backend-authoritative via `lobErrors[]`. Bundle-load failure renders a controlled error, never a guessed form. **Per-screen Playwright create+edit E2E is QE-owned (G2/test phase).**

**Pin-during-edit (S0004 — `engine/usePinnedBundle.ts`):** `usePinnedBundle(productVersionId, stage, resolve)` captures the `(productVersionId, stage)` tuple at open and pins it for the session — a newer version activated elsewhere does **not** rebind an open form (no field/validation change under the user). A new form instance binds whatever is active at its open; an unresolvable pinned version returns a controlled `error` (no silent fallback). The pinned tuple travels with the host save (version recording wired in S0005).

**Schema-driven rendering + AJV parity (S0003):** `deriveWidgets.ts` (`deriveSections`) builds the field model from `data-schema.json` (widget per type/enum) with section/label from `ui-schema.json`; `ajvValidator.ts` (`createDataSchemaValidator`) validates the data-schema layer client-side and normalizes errors to the backend `(code, pointer)` contract (ADR-022); `SchemaDrivenForm.tsx` renders via RHF, blocks its own submit while data-schema-invalid, and binds backend cross-field `lobErrors[]` to fields by `pointer`. **Parity fixture matrix:** `experience/src/features/lob-attributes/engine/parity/cyber-examples.fixture.ts` (recorded backend `(code,pointer)` transcribed from `engine/src/Nebula.Application/Services/LobAttributeService.cs`); enforced by `engine/__tests__/parity.test.ts` (0 disagreements on the data-schema layer; cross-field codes are backend-authoritative and excluded). **Follow-up:** replace recorded backend values with live-endpoint captures once the .NET runtime is available (frontend-only runtime this run).

**Verify (S0001):**
```bash
# On /mnt/c (WSL drvfs), install with the copy import method to avoid pnpm EACCES rename errors:
pnpm --dir {PRODUCT_ROOT}/experience install --package-import-method=copy
pnpm --dir {PRODUCT_ROOT}/experience exec vitest run src/features/lob-attributes/engine/__tests__/widgetRegistry.test.ts
pnpm --dir {PRODUCT_ROOT}/experience build
```

## How to Verify (target — feature action fills exact steps)

1. Open a Cyber attribute form on a draft submission/policy → confirm fields render from the bundle (no hardcoded list).
2. Enter data-schema-invalid data (negative `recordsHeld`, out-of-enum `revenueBand`) → confirm inline AJV errors block submit. Submit a cross-field violation (e.g. sub-1% retention) → confirm the backend `lobErrors[]` message renders inline against its field.
3. With a dirty attribute form, trigger a forced re-auth (401 + silent-renewal-fail) → after sign-in, values are restored and Save is enabled (no auto-replay).
4. Run the canonical CRUD scenario: Contact Edit → type into "Notes" → forced re-auth → values restored → explicit Save.

## Key Files

| Layer | Path | Purpose |
|-------|------|---------|
| Frontend (engine) | `{PRODUCT_ROOT}/experience/src/features/lob-attributes/` (new engine module + `components/DynamicAttributePanel.tsx` rewrite) | Schema-driven form engine, widget registry, client AJV over `data-schema.json` (cross-field rules backend-authoritative via `lobErrors[]`), pin-during-edit |
| Frontend (deps) | `{PRODUCT_ROOT}/experience/package.json` | Add pinned `react-hook-form`, `ajv`, `ajv-formats`, `ajv-errors` (S0001) |
| Frontend (preservation) | `{PRODUCT_ROOT}/experience/src/features/session-continuity/` (consumed, not modified) | `useSessionRestorableForm`, `consumeFormSnapshot`, `DirtyFormRegistration` |
| Frontend (shared helper) | new library-agnostic helper built on `useSessionRestorableForm` (S0007) | Accepts a `DirtyFormRegistration`-shaped source from **either** the RHF adapter (Workstream A) **or** `useControlledDirtyTracker` (Workstream B); owns `form_key` shape; calls `consumeFormSnapshot` on mount |
| Frontend (controlled-form tracker) | `useControlledDirtyTracker(values, initialValues, options?)` (S0007) | Produces F0035's `isDirty`/`getValues`/`getDirtyFieldPaths` triple by deep-diffing current values against initial values — no field-state library required |
| Frontend (CRUD forms) | Exhaustive S0007 inventory (~11, create + edit): `CreateBrokerPage`, `EditBrokerModal`, `CreateAccountPage`, `AccountDetailPage` (account + account-contact edit), `ContactFormModal` (create+edit), `TaskCreateModal`, `TaskDetailPanel` (inline edit), `CreateSubmissionPage` + `SubmissionDetailPage` (native fields), `CreatePolicyPage` (native fields), `RenewalsPage` (create modal) | **Stay controlled.** Wire through the shared helper with `useControlledDirtyTracker` (S0007); register with F0035 preservation (S0008). No field-state library change. |
| Consuming screens | `CreateSubmissionPage`, `CreatePolicyPage`, `PolicyDetailPage`, `RenewalDetailPage`, `SubmissionDetailPage` | Host the engine-backed panel; five-screen regression surface (S0005) |
| Bundle | `{PRODUCT_ROOT}/planning-mds/lob-schemas/cyber/1.0.0/{data-schema,ui-schema,rules}.json` | Consumed as-is; source of fields, layout, and cross-field rules |

## Dev User Credentials (depends on F0035 auth flows)

F0036 exercises the F0035 forced-re-auth path. Use the existing F0009/F0035 OIDC dev credentials; no new credentials are introduced. Document the exact dev login during the feature action.

## Notes

- **No-auto-replay invariant (ABSOLUTE):** inherited from F0035 (operator mandate, plan run `2026-05-23-41109356`). A restored dirty form must require an explicit Save; never replay the interrupted mutation.
- **`ui-schema.json` is layout-only:** widgets are derived from `data-schema.json` types/enums, not from the ui-schema. The ui-schema gives sections + labels only (see amended ADR-021 §1–§2).
- **Parity scope is the data-schema layer (ADR-021 §3):** the client validates `data-schema.json` with AJV and parity is measured against the **actual backend** (ADR-022 `(code, pointer)` multiset equality). Cross-field rules are **backend-authoritative** and surfaced from `lobErrors[]` bound by `pointer` — the client does **not** evaluate `rules.json` as an authoritative layer (an optional UX pre-check is allowed behind the parity harness). F0036 does not depend on ADR-023.
- **Pin-during-edit:** a restored form rebinds to the snapshot's `(productVersionId, stage)`, not the currently-active version.
