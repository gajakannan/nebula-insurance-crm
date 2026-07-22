# F0026-S0003: Record manual and mock-vendor payment receipts

**Story ID:** F0026-S0003
**Feature:** F0026 — Billing, Invoicing & Reconciliation
**Title:** Record manual and mock-vendor payment receipts
**Priority:** High
**Phase:** Brokerage Platform Expansion

## User Story

**As a** Finance Operations Analyst
**I want** to record payment references manually or from a mock-vendor CSV
**So that** payment evidence is available for reconciliation without connecting Nebula to a real bank

## Context & Background

The operator approved manual input and a mock bank/payment-vendor integration. Both entry paths produce the same operational receipt contract and preserve source provenance. Receipt ingestion never changes an invoice balance by itself.

## Acceptance Criteria

**Happy Path — Manual:**
- **Given** I am authorized with receipt-entry permission
- **When** I enter external reference, received date, currency, positive amount, and optional invoice reference, then Save
- **Then** one unapplied manual receipt is persisted and remains visible after reload
- **And** an immutable receipt-recorded audit event identifies manual provenance, actor, time, reference, amount, and currency

**Happy Path — Mock Vendor CSV:**
- **Given** I select the mock-vendor import surface
- **When** I upload a file conforming to the approved fixture contract
- **Then** valid rows create unapplied receipts with batch/row provenance
- **And** the result reports created, duplicate, and rejected row counts with row-level reasons

**Alternative Flows / Edge Cases:**
- Missing reference/date/currency/amount or non-positive amount -> reject the row/entry without creating a receipt.
- Repeated source plus external reference -> identify it as duplicate and create no second receipt.
- File-level parse failure -> create no receipts and show actionable format feedback.
- Mixed valid/invalid rows -> persist valid rows once and report invalid rows without hiding failures.
- Import retry -> never duplicate a receipt that already succeeded.
- Unauthorized receipt entry or import -> reject the request without exposing receipt or import details and create no receipt.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Billing Workspace -> Record Receipt | Enter receipt fields and Save | Enabled for Finance Operations Analyst with receipt-entry permission | One unapplied manual receipt is saved | Reload receipt list/detail and observe source=`manual`, values, and audit event | Internal finance only |
| Billing Workspace -> Import Mock Vendor CSV | Select fixture file and Import | Enabled for Finance Operations Analyst; labeled mock/non-production | Valid rows save as unapplied mock-vendor receipts; invalid/duplicate rows receive outcomes | Reload import result and receipt list; successful row ids remain stable on retry | Internal finance only; no production credential or network call |

- [x] Render-only behavior cannot satisfy the story.
- [x] Validation and row-level error behavior are specified.
- [x] Successful receipt creation requires immutable audit evidence.
- [x] Reload/retry proves persistence and idempotent outcomes.

## Data Requirements

**Required Fields:** Receipt id; source; external reference; received date; currency; amount; application state; created actor/time.

**Optional Fields:** Invoice reference from source; memo; import batch id; source row number.

**Validation Rules:** Amount > 0; currency supported; external reference present; duplicate key uses source plus external reference; CSV conforms to the approved fixture schema.

## Role-Based Visibility

- Finance Operations Analyst: manual entry and mock import within finance scope.
- Finance Manager: read import/receipt outcomes; no entry requirement unless separately granted.
- Distribution/Relationship and external roles: no receipt or import detail access.

## Non-Functional Expectations

- Reliability: retries are idempotent and mixed-row outcomes are deterministic.
- Security: the mock adapter uses no production credential, secret, or outbound network connection.
- Auditability: batch and row provenance remain traceable after later application.

## Dependencies

**Depends On:** F0026-S0001 workspace access. F0030 is not required.

**Related Stories:** F0026-S0004 applies eligible receipts; F0026-S0006 reports import failures/backlog.

## Business Rules

1. Manual and mock-vendor paths produce one canonical receipt shape with explicit source provenance.
2. Recording/importing a receipt never changes invoice balance.
3. Duplicate external receipts are not recreated.

## Out of Scope

- Real bank, ACH, card, lockbox, or payment-gateway connection.
- Refund, chargeback, settlement, or bank-balance verification.

## Questions & Assumptions

**Open Questions:** None blocking for Phase A approval.

**Assumptions:** Phase B defines fixture columns, batch limits, retention, and idempotency implementation.

## Definition of Done

- [ ] Manual and mock-vendor happy paths pass
- [ ] Duplicate, mixed-row, parse, and retry cases pass
- [ ] Permissions enforced
- [ ] Receipt/import audit evidence persisted
- [ ] No real network call or production credential exists
- [ ] Tests pass
- [ ] Story filename matches `F0026-S0003`
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
