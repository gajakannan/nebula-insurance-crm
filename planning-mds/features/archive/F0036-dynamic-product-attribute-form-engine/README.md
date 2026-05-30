# F0036: Form Engine and Form-State Preservation (RHF + AJV + Widget Registry)

**Status:** Plan complete (A1+B2 approved, `2026-05-25-51ff2a92`). Two plan-review rounds returned findings — `2026-05-26-aaa8bd7c` (NOT READY) and `2026-05-26-378ac7da` (CONDITIONALLY READY); **all findings from both rounds resolved by rework 2026-05-26/27** (validation-parity, Workstream B inventory, tracker/KG state, KG coverage, framework templates). Pending plan-review re-run to confirm READY before the feature action. See `STATUS.md` → Plan Review Findings
**Created:** 2026-05-25
**Priority:** High
**Phase:** Platform Foundation / CRM Release MVP Enabler

> Folder slug stays `F0036-dynamic-product-attribute-form-engine`; the title was broadened on 2026-05-25 when CRUD-form coverage was added.

## Purpose

Two workstreams under one feature:

- **A — Dynamic product-attribute engine (full ADR-021):** F0034 delivered the backend product schema-bundle registry but shipped the frontend `DynamicAttributePanel` as a hardcoded Cyber panel — no RHF, no AJV, no widget registry. F0036 builds the schema-driven engine (RHF field state + AJV client validation + shadcn-style widget registry) for LOB product attributes, Cyber first.
- **B — CRUD form preservation via controlled-form adapter:** the hand-rolled CRUD mutation forms — the exhaustive ~11-component create+edit inventory in S0007 (broker, account incl. account-contact, contact, task create + inline edit, submission native, policy create native, renewal create) — remain controlled components and register with F0035 through a small `useControlledDirtyTracker` adapter behind a library-agnostic shared registration helper. No CRUD form changes field-state library (scope refinement 2026-05-27 — RHF stays scoped to the dynamic engine; ADR-021 §6 reworked the same day).

Both workstreams register their forms with F0035 session-continuity preservation so unsaved input survives a forced re-auth — **fully closing F0035 finding #1** (preservation was wired to zero forms).

## Why This Feature Exists

A review of the archived F0035 found its form-state preservation registry connected to zero forms. The cause was ADR-021 drift: the ADR ("Accepted") mandated an RHF/AJV/widget-registry engine, but F0034 implemented a hardcoded panel and never added the dependencies, and the rest of the app's forms were hand-rolled. F0035 then planned its form preservation against that paper engine. F0036 closes the drift at its source (Workstream A) and connects every in-scope mutation form to preservation (Workstream B).

## Documents

- [PRD](./PRD.md)
- [Status](./STATUS.md)

## Proposed User Stories

_Workstream A — product-attribute engine:_

- F0036-S0001 — Adopt RHF + AJV dependencies and engine skeleton + widget-registry contract
- F0036-S0002 — MVP widget vocabulary with theme + a11y coverage
- F0036-S0003 — Schema-driven rendering + AJV client validation with backend parity (Cyber)
- F0036-S0004 — Pin-during-edit binding to `(productVersionId, stage)`
- F0036-S0005 — Replace hardcoded Cyber `DynamicAttributePanel` (five-screen regression)
- F0036-S0006 — Wire product-attribute form into F0035 dirty-form registry + restore

_Workstream B — CRUD form preservation via controlled-form adapter:_

- F0036-S0007 — Controlled-form dirty-tracker (`useControlledDirtyTracker`) + library-agnostic shared preservation registration helper; wire CRUD inventory through it (no CRUD field-state rewrite)
- F0036-S0008 — Register controlled CRUD forms with F0035 (via the adapter) + restore; close S0003 Contact Edit scenario

## Scope

- **In:** RHF + AJV + widget-registry engine for LOB product attributes (Cyber pilot); ADR-021 MVP widget vocabulary; pin-during-edit; controlled-form dirty-tracker adapter for the hand-rolled CRUD forms; library-agnostic shared registration helper; F0035 preservation integration for **all** in-scope mutation forms.
- **Out:** Changing CRUD forms' field-state library (RHF stays scoped to the dynamic engine; CRUD stays controlled); putting CRUD forms through the schema engine; heavy widgets; new LOBs; backend registry/schema changes; filter-only forms with no in-flight state.

## Dependencies

- F0034 Product Schema Registry and Dynamic LOB Attributes (backend bundle registry, Cyber `cyber/1.0.0`)
- ADR-021 Dynamic Form Engine With RHF, AJV, and shadcn Widget Registry (the decision being realized)
- F0035 Session Continuity & Token Refresh (`dirtyFormRegistry`, `useSessionRestorableForm`, `consumeFormSnapshot`)
- F0019 Submission Quoting, Proposal & Approval (downstream consumer)

## Architecture

Governed by **ADR-021** (amended 2026-05-25, plan run `2026-05-25-51ff2a92`; §6 reworked 2026-05-27). Phase B reconciled ADR-021's "Accepted-but-unimplemented" status with the shipped F0034 panel: the amendment records the `ui-schema.json` filename + layout-only shape, the data-schema→widget derivation table, the parity scope (client AJV over `data-schema.json` measured against the actual backend per ADR-022; cross-field rules backend-authoritative via `lobErrors[]`, **no ADR-023 dependency** — §3 reworked 2026-05-26), the conditional-gating convention, the library-agnostic F0035 preservation adapter, and Workstream B's controlled-form dirty-tracker adapter for CRUD forms (§6 reworked 2026-05-27 — supersedes the earlier CRUD rewrite framing; CRUD stays controlled). **No separate companion ADR** was created — the F0035 integration is governed by **ADR-024**. **No backend/schema/bundle change.** Ontology binding completed in `feature-mappings.yaml` (`feature:F0036`); no new canonical nodes. See PRD → *Architecture Traceability → Phase B Outcome*.

## Notes

This README is the lightweight index. Authoritative content lives in `PRD.md` and ADR-021. The `feature-assembly-plan.md` is produced by the feature action at Step 0, not at Phase A.
