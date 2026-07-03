# F0028 — Carrier & Market Relationship Management

**Status:** Done — archived 2026-07-03 after feature action run 2026-07-02-736e7854
**Archived:** 2026-07-03
**Priority:** Medium
**Phase:** CRM Release MVP+

## Overview

Track carrier and market relationship context, underwriter contacts, appetite notes, appointment status, and market activity so Nebula supports commercial P&C placement strategy without introducing carrier integrations or rating logic in this slice.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Product scope, workflows, screen layouts, and story map |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Phase B service, data, API, authorization, audit, and KG contract |
| [STATUS.md](./STATUS.md) | Planning and implementation tracker |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Setup and refinement notes |

## Stories

| ID | Title | Status |
|----|-------|--------|
| [F0028-S0001](./F0028-S0001-market-directory-search.md) | Market directory search and open | Complete |
| [F0028-S0002](./F0028-S0002-carrier-market-profile-management.md) | Carrier and market profile management | Complete |
| [F0028-S0003](./F0028-S0003-underwriter-contact-management.md) | Underwriter and market contact management | Complete |
| [F0028-S0004](./F0028-S0004-appetite-note-capture.md) | Appetite note capture and freshness | Complete |
| [F0028-S0005](./F0028-S0005-appointment-context-management.md) | Appointment context management | Complete |
| [F0028-S0006](./F0028-S0006-market-activity-and-related-work.md) | Market activity and related work visibility | Complete |

**Total Stories:** 6
**Completed:** 6 / 6

## Architecture Review

**Phase B status:** Approved for feature action
**Execution Plan:** Completed in feature action run `2026-07-02-736e7854`.

### Key Findings

- F0019 and F0023 are complete, so submission context and search/reporting substrate are available dependencies.
- F0028 remains CRM-side recorded market intelligence only; carrier API sync, rating, quote comparison, and reinsurance stay out of scope.
- Security Reviewer is expected to be required because appetite, appointment, and underwriter relationship context are commercially sensitive.
- No new ADR is required; F0028 applies existing clean architecture, Casbin ABAC, timeline, search, and ProblemDetails patterns.

### Architecture Artifacts

| Artifact | Status |
|----------|--------|
| Data model / ERD | Defined in `ARCHITECTURE.md` |
| API contract (OpenAPI) | Updated in `planning-mds/api/nebula-api.yaml` |
| Workflow state machine | Not required; F0028 uses CRUD-style relationship records plus timeline events |
| Casbin policy | Updated in authorization matrix and policy CSV |
| JSON schemas | Added under `planning-mds/schemas/carrier-*.schema.json` |
| C4 diagrams | Covered by existing CRM/API container boundary; no new runtime container |
| ADRs | Not required for this slice |
| Assembly plan | Completed in feature action G0 |
