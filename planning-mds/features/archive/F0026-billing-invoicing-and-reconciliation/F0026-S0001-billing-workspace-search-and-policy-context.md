# F0026-S0001: Billing workspace search and policy context

**Story ID:** F0026-S0001
**Feature:** F0026 — Billing, Invoicing & Reconciliation
**Title:** Billing workspace search and policy context
**Priority:** High
**Phase:** Brokerage Platform Expansion

## User Story

**As a** Finance Operations Analyst or Finance Manager
**I want** to find authorized invoices and open their policy/account context
**So that** I can understand billing work before recording or reconciling a payment

## Context & Background

F0018 supplies policy, immutable policy-version, premium, and account context. F0025 supplies expected-commission context. F0026 needs a finance-only workspace that joins these sources without changing them or leaking finance totals through results, counts, or filters.

## Acceptance Criteria

**Happy Path:**
- **Given** I have finance read access and source-record access
- **When** I search by invoice number, policy number, account name, or payment reference and apply state/exception filters
- **Then** I see only authorized invoice rows with invoice number, policy/account, invoice amount/currency, operational outstanding amount, due date, receipt/application state, and exception indicator
- **And** opening a row shows linked policy/version facts, read-only expected-commission context when authorized, receipts, exceptions, and audit history

**Alternative Flows / Edge Cases:**
- No authorized matches -> show an empty state with no hidden-row count.
- A linked policy/account is unavailable under source authorization -> omit the row rather than exposing a redacted finance amount.
- Query fails -> show retry feedback and retain search/filter input.
- Distribution/Relationship user with bounded summary access -> show only the approved policy billing summary, not the finance workspace or receipt/exception detail.

**Checklist:**
- [ ] Counts, totals, facets, and drilldowns are computed after authorization filtering.
- [ ] Results link to Invoice Detail and the authorized Policy/Account source.
- [ ] Search input is bounded and paginated.

## Interaction Contract

N/A — read-only story.

## Data Requirements

**Required Fields:** Invoice identifier/number; policy and policy-version identifiers; account identifier/display name; amount; currency; due date; operational outstanding amount; receipt/application state; exception indicator.

**Optional Fields:** Expected-commission reference/summary when separately authorized; source provenance summary; days open.

**Validation Rules:** Search/filter values use declared supported fields; page size is bounded by the Phase B contract.

## Role-Based Visibility

- Finance Operations Analyst and Finance Manager: finance workspace rows/detail within source scope.
- Distribution/Relationship user: bounded read-only policy billing summary only when authorized.
- Admin: diagnostic access only if granted the same finance and source-record policies.
- External roles: no F0026 access.

## Non-Functional Expectations

- Security: hidden rows do not contribute to totals, counts, facets, or existence signals.
- Reliability: source-link failures produce an actionable error without displaying stale finance totals as current.
- Performance: Phase B assigns a measurable p95 budget for bounded first-page queries.

## Dependencies

**Depends On:** F0018 policy/version/account context; F0025 expected-commission context.

**Related Stories:** F0026-S0002 through F0026-S0006 use the selected invoice/detail surfaces.

## Business Rules

1. Finance results require both F0026 finance authorization and authorization to the linked source policy/account.
2. Read-only expected commission remains owned by F0025 and is never mutated from F0026.

## Out of Scope

- External payment portal.
- General ledger or bank-balance views.
- Direct-bill carrier statement search.

## Questions & Assumptions

**Open Questions:** None blocking for Phase A approval.

**Assumptions:** Phase B defines endpoint/query shapes and the bounded pagination limit.

## Definition of Done

- [ ] Acceptance criteria and no-leak states pass
- [ ] Permissions enforced for rows, counts, totals, and drilldowns
- [ ] Read-only behavior confirmed
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `F0026-S0001`
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
