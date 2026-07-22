# F0026-S0002: Create an agency-bill invoice

**Story ID:** F0026-S0002
**Feature:** F0026 — Billing, Invoicing & Reconciliation
**Title:** Create an agency-bill invoice
**Priority:** High
**Phase:** Brokerage Platform Expansion

## User Story

**As a** Finance Operations Analyst
**I want** to create an agency-bill invoice from policy/version context
**So that** the receivable item is traceable to the source policy without re-keying ownership facts

## Context & Background

The first release records agency-bill invoices only. Policy/account/version facts come from F0018 and remain authoritative; the F0026 invoice is an operational finance record, not a ledger entry or invoice-delivery service.

## Acceptance Criteria

**Happy Path:**
- **Given** I am authorized on Billing Workspace or an eligible Policy Detail and can read the selected policy/version/account
- **When** I enter invoice number, invoice date, due date, currency, positive invoice amount, and optional memo, then select Save
- **Then** one agency-bill invoice is persisted with stable policy, policy-version, and account links
- **And** Invoice Detail shows the saved values and full operational outstanding amount after reload
- **And** an immutable invoice-created audit event records actor, time, source identifiers, amount/currency, and invoice number

**Alternative Flows / Edge Cases:**
- Missing/invalid required field, non-positive amount, or due date before invoice date -> show field feedback and do not save.
- Duplicate invoice number in the declared uniqueness scope -> show conflict feedback and do not create a second record.
- Source policy/version/account is unauthorized or no longer eligible -> reject save and retain editable values.
- Concurrent duplicate submission -> return the existing idempotent result or a conflict; never create duplicate invoices.

**Checklist:**
- [ ] Account identity is derived from the selected policy, not independently editable.
- [ ] Policy-version link is immutable after creation.
- [ ] No email/delivery, tax, bank, or ledger action occurs.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Billing Workspace -> Create Invoice, or eligible Policy Detail -> Billing -> Create Invoice | Enter required invoice fields and Save | Enabled for Finance Operations Analyst with finance-create plus source policy/account access | One agency-bill invoice is created with full operational outstanding amount | Reload Invoice Detail and observe identical values, source links, and creation audit event | Internal finance only; source policy/version must be readable and eligible under Phase B contract |

- [x] Render-only behavior cannot satisfy the story.
- [x] Validation and conflict behavior are specified.
- [x] Successful save requires immutable audit evidence.
- [x] Persistence is proven after reload.

## Data Requirements

**Required Fields:** Invoice number; policy id; policy-version id; account id; invoice date; due date; currency; invoice amount; created actor/time.

**Optional Fields:** Memo; finance reference.

**Validation Rules:** Amount > 0; due date is not before invoice date; currency is a supported code; required source links resolve and are authorized; duplicate rule is deterministic.

## Role-Based Visibility

- Finance Operations Analyst: create within source scope.
- Finance Manager: read; creation permission only if separately granted in Phase B.
- Distribution/Relationship user: read-only summary; no create action.
- External roles: no access.

## Non-Functional Expectations

- Reliability: duplicate/retry behavior does not create multiple invoices.
- Security: server-side authorization validates finance action and source policy/account access.
- Auditability: rejected validation creates no invoice-created event.

## Dependencies

**Depends On:** F0018 Policy Lifecycle & Policy 360.

**Related Stories:** F0026-S0001 finds the invoice; F0026-S0004 reconciles it; F0026-S0005 corrects eligible operational data.

## Business Rules

1. Agency-bill invoices link to one policy, one immutable policy version, and that policy's account.
2. Invoice creation does not mutate policy premium/version facts.
3. New invoice outstanding amount equals its recorded invoice amount until an exact application or approved correction changes F0026 operational state.

## Out of Scope

- Direct-bill invoice/statement records.
- Invoice delivery, tax calculation, partial installment schedule, or accounting posting.

## Questions & Assumptions

**Open Questions:** None blocking for Phase A approval.

**Assumptions:** Phase B names technical lifecycle states and exact uniqueness/idempotency keys.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Validation, conflict, and retry cases handled
- [ ] Permissions enforced
- [ ] Invoice creation audit event persisted
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `F0026-S0002`
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
