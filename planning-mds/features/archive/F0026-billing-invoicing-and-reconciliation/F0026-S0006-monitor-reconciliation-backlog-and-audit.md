# F0026-S0006: Monitor reconciliation backlog and audit history

**Story ID:** F0026-S0006
**Feature:** F0026 — Billing, Invoicing & Reconciliation
**Title:** Monitor reconciliation backlog and audit history
**Priority:** High
**Phase:** Brokerage Platform Expansion

## User Story

**As a** Finance Manager
**I want** to monitor unresolved billing work and inspect immutable decision history
**So that** I can prioritize follow-up and explain operational finance changes without relying on a ledger replacement

## Context & Background

Invoice, receipt, import, application, exception, and correction records create a finance operations backlog. The management view must expose authorized work and days open while preventing hidden data from influencing totals or drilldowns.

## Acceptance Criteria

**Happy Path:**
- **Given** I have finance-manager and source-record access
- **When** I open Reconciliation Backlog and filter by exception type, source, state, policy/account, or days open
- **Then** I see authorized unresolved items with invoice/receipt context, reason, owner/requester when present, days open, next action, and source provenance
- **And** summary counts include authorized exact applications, unresolved exceptions, pending corrections, rejected import rows, and duplicate rows
- **And** opening an item shows chronological immutable events with actor, timestamp, action, source identifiers, and permitted before/after values

**Alternative Flows / Edge Cases:**
- No authorized work -> show zero/empty state without hidden-row count.
- User lacks access to linked source record -> omit item and all contribution to totals.
- Audit payload contains restricted finance detail -> return only fields permitted by finance and source policies.
- Query fails -> show retry feedback and preserve filters.
- Distribution/Relationship user opens a policy billing summary -> show bounded invoice state only, not backlog totals or finance audit detail.

**Checklist:**
- [ ] Days open is derived from persisted event/request timestamps without an invented SLA band.
- [ ] Filters and totals use the same authorization-filtered source set.
- [ ] Audit history is ordered and immutable.

## Interaction Contract

N/A — read-only story.

## Data Requirements

**Required Fields:** Backlog item id/type; invoice/receipt/exception/request references as applicable; reason/state; source; created/opened timestamp; days open; next action; authorized summary; audit event actor/time/action.

**Optional Fields:** Assigned analyst; mock-import batch/row; policy/account display context; decision note when authorized.

**Validation Rules:** Supported filters only; bounded pagination; days open is non-negative and computed from stored timestamps; counts and rows share identical authorization scope.

## Role-Based Visibility

- Finance Manager: backlog totals, items, and audit detail within source scope.
- Finance Operations Analyst: operational items/audit needed for assigned or accessible work; management-only totals may be restricted in Phase B.
- Distribution/Relationship user: bounded policy billing summary only.
- External roles: no F0026 access.

## Non-Functional Expectations

- Security: unauthorized records contribute nothing to totals, days-open summaries, facets, or drilldowns.
- Reliability: audit history is append-only and remains queryable after terminal decisions.
- Performance: Phase B defines measurable first-page and drilldown p95 budgets for bounded filters.

## Dependencies

**Depends On:** F0026-S0001 through F0026-S0005.

**Related Stories:** F0025 revenue rollups remain separate and read-only; F0030 production exchange remains deferred.

## Business Rules

1. Backlog and audit are operational finance views, not financial statements or ledger balances.
2. Counts, totals, facets, and drilldowns are calculated only from authorized source rows.
3. Days open is factual elapsed time, not an SLA breach classification in first release.

## Out of Scope

- Financial statements, tax reports, GL export, bank reconciliation statement, or producer payout report.
- Automated collections/dunning or exception assignment rules.
- Cross-currency or tolerance analytics.

## Questions & Assumptions

**Open Questions:** None blocking for Phase A approval.

**Assumptions:** Phase B defines query endpoints, pagination bounds, allowed audit fields, and measurable p95 budgets.

## Definition of Done

- [ ] Authorized backlog rows/counts/drilldowns pass
- [ ] Empty, error, and no-leak states pass
- [ ] Immutable audit order/fields pass
- [ ] Read-only behavior confirmed
- [ ] Tests pass
- [ ] Story filename matches `F0026-S0006`
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
