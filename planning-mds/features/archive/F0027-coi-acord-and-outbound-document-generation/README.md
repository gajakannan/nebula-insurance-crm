# F0027 — COI, ACORD & Outbound Document Generation

**Status:** Done; archived
**Priority:** Medium
**Phase:** CRM Release MVP+
**Archived:** 2026-07-03

## Overview

Generate reusable outbound insurance artifacts from CRM data: COIs, ACORD forms, and proposal outputs. F0027 owns template-governed preview, explicit issue, generated-artifact audit, and document-storage linkage while leaving submission quote/proposal workflow state in F0019.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Full product requirements, scope, workflows, and story map |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Phase B architecture, API, schema, storage, authorization, and KG design |
| [STATUS.md](./STATUS.md) | Completion checklist and progress tracking |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Setup and refinement notes |

## Stories

| ID | Title | Status |
|----|-------|--------|
| [F0027-S0001](./F0027-S0001-template-library-governance.md) | Template library governance for outbound artifacts | Done |
| [F0027-S0002](./F0027-S0002-preview-generated-document.md) | Preview generated document before issue | Done |
| [F0027-S0003](./F0027-S0003-issue-generated-artifact.md) | Issue final generated artifact with audit | Done |
| [F0027-S0004](./F0027-S0004-regenerate-and-retrieve-artifacts.md) | Regenerate and retrieve generated artifacts | Done |
| [F0027-S0005](./F0027-S0005-render-proposal-from-submission-packet.md) | Render proposal from submission packet context | Done |

**Total Stories:** 5
**Completed:** 5 / 5

## Architecture Review

**Phase B status:** Approved and implemented
**Execution Plan:** Completed by feature action G0 in run `2026-07-02-b9316621`.

### Key Findings

- Hard dependencies F0018, F0020, and F0019 are archived as done.
- F0027 is planning/refinement-ready but not build-ready until Phase B completes.
- Proposal rendering must consume F0019 packet context without taking ownership of packet workflow state.

### Architecture Artifacts

| Artifact | Status |
|----------|--------|
| Data model / ERD | Covered by `ARCHITECTURE.md`; no new relational document table |
| API contract (OpenAPI) | Added F0027 `OutboundDocumentGeneration` paths |
| Workflow state machine | Covered by `ARCHITECTURE.md`; preview/issue/regenerate, no F0019 state mutation |
| Casbin policy | Added F0027 outbound template/document actions |
| JSON schemas | Added generated-document request/preview/issue schemas |
| C4 diagrams | Covered textually in `ARCHITECTURE.md`; implementation G0 may add diagrams if needed |
| ADRs | No new ADR required; design extends ADR-012 and ADR-025 |
| Assembly plan | Produced by later feature action G0 |
