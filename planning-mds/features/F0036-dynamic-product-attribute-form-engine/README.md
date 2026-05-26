# F0036: Form Engine and Form-State Preservation (RHF + AJV + Widget Registry)

**Status:** Phase A draft pending approval
**Created:** 2026-05-25
**Priority:** High
**Phase:** Platform Foundation / CRM Release MVP Enabler

> Folder slug stays `F0036-dynamic-product-attribute-form-engine`; the title was broadened on 2026-05-25 when CRUD-form coverage was added.

## Purpose

Two workstreams under one feature:

- **A — Dynamic product-attribute engine (full ADR-021):** F0034 delivered the backend product schema-bundle registry but shipped the frontend `DynamicAttributePanel` as a hardcoded Cyber panel — no RHF, no AJV, no widget registry. F0036 builds the schema-driven engine (RHF field state + AJV client validation + shadcn-style widget registry) for LOB product attributes, Cyber first.
- **B — CRUD form RHF migration + preservation:** migrate the hand-rolled CRUD forms (broker, account, submission, contact, task) to React Hook Form (fixed-shape; no schema engine) and register them with F0035.

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

_Workstream B — CRUD form migration + preservation:_

- F0036-S0007 — Shared preservation registration helper + migrate CRUD forms to RHF
- F0036-S0008 — Register migrated CRUD forms with F0035 + restore; close S0003 Contact Edit scenario

## Scope

- **In:** RHF + AJV + widget-registry engine for LOB product attributes (Cyber pilot); ADR-021 MVP widget vocabulary; pin-during-edit; RHF migration of the hand-rolled CRUD forms; F0035 preservation integration for **all** in-scope mutation forms.
- **Out:** Putting CRUD forms through the schema engine (RHF field state only); heavy widgets; new LOBs; backend registry/schema changes; filter-only forms with no in-flight state.

## Dependencies

- F0034 Product Schema Registry and Dynamic LOB Attributes (backend bundle registry, Cyber `cyber/1.0.0`)
- ADR-021 Dynamic Form Engine With RHF, AJV, and shadcn Widget Registry (the decision being realized)
- F0035 Session Continuity & Token Refresh (`dirtyFormRegistry`, `useSessionRestorableForm`, `consumeFormSnapshot`)
- F0019 Submission Quoting, Proposal & Approval (downstream consumer)

## Architecture

Governed by **ADR-021** (amended 2026-05-25, plan run `2026-05-25-51ff2a92`). Phase B reconciled ADR-021's "Accepted-but-unimplemented" status with the shipped F0034 panel: the amendment records the `ui-schema.json` filename + layout-only shape, the data-schema→widget derivation table, the parity scope (AJV + `rules.json` per ADR-022/023), the conditional-gating convention, the F0035 preservation adapter, and Workstream B RHF-for-CRUD. **No separate companion ADR** was created — the F0035 integration is governed by **ADR-024**. **No backend/schema/bundle change.** Ontology binding completed in `feature-mappings.yaml` (`feature:F0036`); no new canonical nodes. See PRD → *Architecture Traceability → Phase B Outcome*.

## Notes

This README is the lightweight index. Authoritative content lives in `PRD.md` and ADR-021. The `feature-assembly-plan.md` is produced by the feature action at Step 0, not at Phase A.
