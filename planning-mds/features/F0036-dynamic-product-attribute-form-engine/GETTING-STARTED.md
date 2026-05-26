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

- No new seed data. The Cyber `cyber/1.0.0` bundle (`planning-mds/lob-schemas/cyber/1.0.0/`) supplies `data-schema.json`, `ui-schema.json`, `rules.json`, and published examples used for the AJV/backend parity fixture matrix (S0003).

## How to Verify (target — feature action fills exact steps)

1. Open a Cyber attribute form on a draft submission/policy → confirm fields render from the bundle (no hardcoded list).
2. Enter invalid data (negative `recordsHeld`, out-of-enum `revenueBand`, sub-1% retention) → confirm inline AJV/`rules.json` errors block submit.
3. With a dirty attribute form, trigger a forced re-auth (401 + silent-renewal-fail) → after sign-in, values are restored and Save is enabled (no auto-replay).
4. Run the canonical CRUD scenario: Contact Edit → type into "Notes" → forced re-auth → values restored → explicit Save.

## Key Files

| Layer | Path | Purpose |
|-------|------|---------|
| Frontend (engine) | `{PRODUCT_ROOT}/experience/src/features/lob-attributes/` (new engine module + `components/DynamicAttributePanel.tsx` rewrite) | Schema-driven form engine, widget registry, AJV + `rules.json` validation, pin-during-edit |
| Frontend (deps) | `{PRODUCT_ROOT}/experience/package.json` | Add pinned `react-hook-form`, `ajv`, `ajv-formats`, `ajv-errors` (S0001) |
| Frontend (preservation) | `{PRODUCT_ROOT}/experience/src/features/session-continuity/` (consumed, not modified) | `useSessionRestorableForm`, `consumeFormSnapshot`, `DirtyFormRegistration` |
| Frontend (shared helper) | new helper built on `useSessionRestorableForm` (S0007) | Adapts RHF → `DirtyFormRegistration` (`isDirty`/`getValues`/`getDirtyFieldPaths`) for all in-scope forms |
| Frontend (CRUD forms) | `EditBrokerModal`, `CreateBrokerPage`, `CreateAccountPage`, `ContactFormModal`, `TaskCreateModal`, `CreateSubmissionPage` (native fields) | RHF migration + F0035 registration (S0007/S0008) |
| Consuming screens | `CreateSubmissionPage`, `CreatePolicyPage`, `PolicyDetailPage`, `RenewalDetailPage`, `SubmissionDetailPage` | Host the engine-backed panel; five-screen regression surface (S0005) |
| Bundle | `{PRODUCT_ROOT}/planning-mds/lob-schemas/cyber/1.0.0/{data-schema,ui-schema,rules}.json` | Consumed as-is; source of fields, layout, and cross-field rules |

## Dev User Credentials (depends on F0035 auth flows)

F0036 exercises the F0035 forced-re-auth path. Use the existing F0009/F0035 OIDC dev credentials; no new credentials are introduced. Document the exact dev login during the feature action.

## Notes

- **No-auto-replay invariant (ABSOLUTE):** inherited from F0035 (operator mandate, plan run `2026-05-23-41109356`). A restored dirty form must require an explicit Save; never replay the interrupted mutation.
- **`ui-schema.json` is layout-only:** widgets are derived from `data-schema.json` types/enums, not from the ui-schema. The ui-schema gives sections + labels only (see amended ADR-021 §1–§2).
- **Parity includes `rules.json`:** plain AJV over the data-schema is insufficient for 0-disagreement backend parity; the client must also evaluate the bundle's `rules.json` (ADR-022/023).
- **Pin-during-edit:** a restored form rebinds to the snapshot's `(productVersionId, stage)`, not the currently-active version.
