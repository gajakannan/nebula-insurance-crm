# ADR-034: Agency-Bill Invoicing and Exact Reconciliation

**Status:** Accepted  
**Date:** 2026-07-19  
**Owners:** Architect  
**Feature:** F0026 — Billing, Invoicing & Reconciliation

## Context

Nebula has authoritative policy/version records (F0018) and expected-commission context (F0025), but it has no operational invoice, receipt, or reconciliation record. The approved F0026 scope is intentionally narrower than an accounting system: agency bill only, manual entry and a mock-vendor CSV adapter, explicit exact application, no tolerances or write-offs, and a different Finance Manager for balance-affecting correction decisions.

The previous architecture inventory named invoice services, billing events, reconciliation workflows, finance views, and batch-friendly exchange boundaries. It did not decide persistence, source provenance, authorization, idempotency, or the boundary between a mock adapter and F0030. Those gaps must be closed without restoring direct bill, real-bank connectivity, settlement, ledger, or general accounting behavior.

## Decision

### 1. Module and source-of-truth boundary

Add an in-process `BillingReconciliation` vertical slice to the existing modular monolith. It owns `BillingInvoice`, `PaymentReceipt`, `PaymentReceiptImportBatch`, `PaymentReceiptImportRowOutcome`, `PaymentApplication`, `ReconciliationException`, and `BillingCorrection` records. F0018 remains authoritative for policy, policy version, account, currency, and premium context; F0025 remains authoritative for expected commission. F0026 stores identifiers and display/audit snapshots but never mutates those upstream facts.

No ledger entry, cash account, settlement state, tax result, producer payout, or financial statement is created. `OutstandingAmount` is an operational reconciliation balance, not a general-ledger receivable.

### 2. Invoice and receipt invariants

- An invoice is agency-bill, belongs to one visible policy, immutable policy version, and matching account, and uses the policy-version currency.
- An invoice starts `Outstanding`. It becomes `Reconciled` only after an explicit exact payment application or an approved correction produces a zero operational outstanding amount.
- A receipt is immutable financial-source evidence after creation. It records `Manual` or `MockVendorCsv`, an external reference, currency, amount, received date, optional invoice reference and memo, and import provenance where applicable.
- `(Source, ExternalReference)` is unique. Retrying an import creates a new batch and immutable row outcomes but never a second receipt for an existing source/reference pair.
- Recording or importing a receipt never changes an invoice.

### 3. Mock CSV adapter contract

The adapter is synchronous, in-process, and explicitly labeled mock. It accepts UTF-8 CSV up to 1 MiB and 1,000 data rows with the versioned `mock-payment-receipt-row-v1` profile:

`externalReference,receivedDate,currency,amount,invoiceReference,memo`

The first four columns are required. Dates are ISO `YYYY-MM-DD`, currency is an uppercase three-letter code, and amount is a positive decimal with at most two fractional digits. Unknown/missing headers, malformed quoting, invalid rows, and control characters are rejected with row outcomes; duplicate source/reference pairs are reported as duplicates. Raw CSV bytes are not retained: the system stores the sanitized file name, SHA-256, contract version, counts, and row outcomes.

An external ODCS dataset contract is deferred. The first release has one in-process mock producer and one consumer, no external service-level agreement, and no production exchange. F0030 must introduce the production exchange contract, credentials, transport, retry, outbox, and operational ownership before a real connector is enabled.

### 4. Exact application and exceptions

`POST /payment-applications` is the only cash-application mutation. The request names one unapplied receipt and one outstanding invoice, carries the receipt row version, and uses `If-Match` for the invoice. Application succeeds atomically only when currency matches and receipt amount equals the full invoice outstanding amount. It creates an immutable `PaymentApplication`, marks the receipt `Applied`, sets invoice outstanding to zero/status to `Reconciled`, and emits timeline evidence in one transaction.

Missing/ambiguous invoice reference, amount mismatch, currency mismatch, duplicate reference, or unusable input creates or preserves an actionable `ReconciliationException`; it never mutates the invoice balance. There is no partial allocation, tolerance, automatic write-off, or cross-currency path.

### 5. Correction workflow and separation of duties

An authorized Finance Operations Analyst may correct eligible non-balance reference metadata through the exception endpoint. A balance-affecting change requires a `BillingCorrection` request containing before outstanding, signed correction amount, proposed outstanding, reason, and evidence note. The state machine is `Pending -> Approved | Rejected`.

A caller with `billing:correction_approve` may decide a current pending request only when their principal differs from the requester. Approval atomically applies the proposed operational outstanding amount and records before/after values; rejection leaves the invoice unchanged. Decisions are terminal and append-only. Neither path changes policy premium, expected commission, or a ledger.

### 6. Authorization and information disclosure

Endpoint handlers enforce the F0026 Casbin resource/action and source-policy/account visibility before repository reads return rows, counts, totals, facets, or drilldowns.

- Finance Operations Analyst: finance detail/read, invoice creation, receipt entry/import, exact application, exception management, correction request, and backlog detail.
- Finance Manager: finance detail/read, correction approval, backlog detail, and management summary. The manager has no first-release invoice/receipt/application mutation grant.
- DistributionUser, DistributionManager, and RelationshipManager: bounded `summary_read` only for already-visible policy records.
- Admin: all named actions, while still subject to validation, concurrency, immutable audit, and same-user decision denial.
- BrokerUser, MgaUser, ExternalUser, and all unlisted roles: deny.

Policy/account summary responses exclude receipt provenance, import outcomes, exception notes, correction values, backlog totals, and finance audit detail. Unauthorized resources return the same not-found shape as absent resources.

### 7. API, concurrency, audit, and performance

The REST contract uses plural shallow routes, camelCase JSON, RFC 7807 ProblemDetails, bounded pagination (default 25, maximum 100), and row-version ETags. Mutations of existing aggregates require `If-Match`; missing preconditions return 428 and stale versions return 412. Transactional billing data is not cached.

Every successful invoice, receipt, import, application, exception correction, correction request, and correction decision writes an immutable `ActivityTimelineEvent` with identifiers, actor/time, provenance, and relevant before/after operational values. Sensitive memo/note text is length-bounded and excluded from logs.

List/detail/backlog requests target p95 <= 500 ms under the MVP workload. A maximum-size mock import targets p95 <= 5 seconds. Repository queries must filter before projection/aggregation and use indexes described in the data model.

### 8. Frontend boundary

Add the `experience/src/features/billing/` vertical slice and `/billing`, `/billing/invoices/:invoiceId`, and `/billing/reconciliation` routes. Server authorization remains authoritative. The UI must distinguish empty, filtered-empty, forbidden/not-found, stale, conflict, validation, import-partial-result, and retry states; use semantic theme tokens; preserve keyboard/focus behavior; and pass light/dark responsive smoke checks.

## Consequences

### Positive

- Provides auditable operational billing without creating an accounting subsystem.
- Makes mock-vendor behavior deterministic and testable without production credentials or network dependencies.
- Enforces exact application and manager separation of duties in the service and database transaction boundary.
- Preserves a clean F0030 production-integration seam.

### Trade-offs

- Synchronous mock imports are capped at 1,000 rows and are not a production batch platform.
- One receipt applies to one invoice in the first release; partial allocation and multi-invoice distribution require a later decision.
- Operational outstanding amounts must be described carefully so users do not mistake them for ledger balances.

## Alternatives Considered

1. **Use ADR-015 Integration Hub/outbox immediately.** Rejected for the first release because there is no production external transport, credential, retry, or delivery requirement. F0030 owns that activation.
2. **Auto-match by invoice reference.** Rejected because the approved workflow requires explicit user application and exact verification.
3. **Store only receipt fields on the invoice.** Rejected because it destroys independent source provenance, idempotency, and exception history.
4. **Allow tolerances or write-offs.** Rejected by the approved PRD and because those behaviors imply accounting policy not owned by this CRM slice.

## Validation

- Contract tests prove request/response/ProblemDetails parity with `nebula-api.yaml` and JSON schemas.
- Unit and integration tests prove uniqueness, exact-match invariants, same-user denial, terminal decisions, transactional rollback, authorization-before-count behavior, and timeline completeness.
- UI tests prove permission-shaped controls and all required empty/error/stale/import states.
- Migration apply/rollback and maximum-size mock-import checks are required during the feature action.

## References

- F0026 PRD and stories
- ADR-008 — Casbin Enforcer Adoption
- ADR-011 — CRM Workflow State Machines and Transition History
- ADR-015 — Integration Hub Canonical Contracts and Outbox
- ADR-018 — Policy Aggregate Versioning and Reinstatement Window
- ADR-033 — Commission, Producer Splits, and Revenue Tracking
