# F0026-S0004: Apply an exact payment and reconcile an invoice

**Story ID:** F0026-S0004
**Feature:** F0026 — Billing, Invoicing & Reconciliation
**Title:** Apply an exact payment and reconcile an invoice
**Priority:** Critical
**Phase:** Brokerage Platform Expansion

## User Story

**As a** Finance Operations Analyst
**I want** to explicitly apply a same-currency receipt that equals an invoice's full outstanding amount
**So that** exact agency-bill items clear without automatic tolerance, partial allocation, or hidden balance changes

## Context & Background

Receipt ingestion and invoice creation are separate. This story is the only first-release cash-application path: an analyst selects both records and the system validates an exact match before mutating F0026 operational state.

## Acceptance Criteria

**Happy Path:**
- **Given** I can access one unapplied receipt and one outstanding invoice in the same currency, and receipt amount equals the invoice's full outstanding amount
- **When** I select Apply Exact Match and confirm the pair
- **Then** one application links the receipt and invoice
- **And** the receipt is no longer available for another application
- **And** Invoice Detail shows zero operational outstanding amount and reconciled state after reload
- **And** an immutable application audit event records actor, time, invoice id, receipt id, amount, currency, and before/after operational balance

**Alternative Flows / Edge Cases:**
- Currency differs -> create/show a currency-mismatch exception and change neither record's balance/application state.
- Amount is less or greater than full outstanding -> create/show an amount-mismatch exception and change neither record.
- Receipt or invoice is already applied/reconciled -> reject as stale conflict and show current state.
- Receipt source invoice reference conflicts with selected invoice -> require exception review; do not apply.
- Concurrent application attempt -> exactly one attempt succeeds; the other receives conflict feedback.

**Checklist:**
- [ ] Application is explicit; no import-time or background auto-application occurs.
- [ ] Failed validation creates no success application event.
- [ ] Mismatch exception remains visible for F0026-S0005 review.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Invoice Detail or Reconciliation Workspace -> Select Receipt -> Apply Exact Match -> Confirm | Confirm selected invoice/receipt pair | Enabled only for Finance Operations Analyst when both records are authorized, open/unapplied, same currency, and exact full-outstanding amount | One application persists; receipt becomes applied; invoice operational outstanding becomes zero | Reload both Invoice Detail and Receipt Detail; observe reciprocal link, terminal application state, zero outstanding, and audit event | Internal finance only; no partial/over/under/cross-currency application |

- [x] Render-only behavior cannot satisfy the story.
- [x] Validation/conflict behavior is specified.
- [x] Successful mutation has immutable audit evidence.
- [x] Reload and concurrency tests prove persisted single application.

## Data Requirements

**Required Fields:** Application id; invoice id; receipt id; amount; currency; applied actor/time; before/after operational outstanding amount.

**Optional Fields:** Analyst confirmation note.

**Validation Rules:** Receipt unapplied; invoice outstanding; same currency; receipt amount equals full outstanding amount; source-reference conflict blocks application; pair remains current under concurrency control.

## Role-Based Visibility

- Finance Operations Analyst: apply exact match within finance and source scope.
- Finance Manager: read applications and audit history; no separate application permission required for Phase A.
- Distribution/Relationship user: bounded invoice summary only; no receipt/application details or mutation.
- External roles: no access.

## Non-Functional Expectations

- Reliability: operation is atomic and concurrency-safe; one receipt cannot be applied twice.
- Security: both finance-resource and source-record authorization are evaluated server-side.
- Auditability: before/after state is captured without mutating F0018 or F0025 source facts.

## Dependencies

**Depends On:** F0026-S0002 invoice; F0026-S0003 receipt.

**Related Stories:** F0026-S0005 resolves mismatches; F0026-S0006 reports unresolved/reconciled work.

## Business Rules

1. First release permits only same-currency/full-outstanding exact application.
2. Partial, under-, over-, and cross-currency cases remain unapplied exceptions.
3. No automatic tolerance or write-off exists.
4. One receipt can participate in at most one successful first-release application.

## Out of Scope

- Split receipt across invoices or multiple receipts against one invoice.
- Tolerance matching, write-off, refund, or overpayment credit.
- Real settlement confirmation.

## Questions & Assumptions

**Open Questions:** None blocking for Phase A approval.

**Assumptions:** Phase B defines state names, atomicity mechanism, and conflict response contract.

## Definition of Done

- [ ] Exact application and persisted reciprocal links pass
- [ ] All mismatch/stale/concurrency cases preserve balances
- [ ] Permissions enforced
- [ ] Application/exception audit evidence persisted
- [ ] Tests pass
- [ ] Story filename matches `F0026-S0004`
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
