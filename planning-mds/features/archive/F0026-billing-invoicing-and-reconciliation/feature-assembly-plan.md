# F0026 — Billing, Invoicing & Reconciliation — Feature Assembly Plan

**Owner:** Architect  
**Status:** Approved — Phase B operator approval recorded 2026-07-19  
**Plan Run:** `2026-07-19-79477865`  
**Governing Decision:** [ADR-034](../../architecture/decisions/ADR-034-agency-bill-invoicing-and-exact-reconciliation.md)

## Outcome and Hard Boundary

Build an internal `BillingReconciliation` vertical slice for agency-bill invoices, manual/mock-CSV payment receipts, explicit exact applications, controlled exceptions/corrections, and management backlog visibility. The slice must not implement direct bill, real bank/payment connectivity, partial or tolerance matching, write-offs, refunds, ledger/tax/settlement, statements, producer payouts, or production F0030 transport.

## Dependency Assembly

| Dependency | Required Contract | Evidence / Status |
|------------|-------------------|-------------------|
| F0018 Policy Lifecycle | Existing policy, immutable policy-version, account/currency context, policy authorization, timeline conventions | Done/archived; feature-evidence audit pending, so implementation must verify raw ADR-018, contracts, and code paths before consuming them. |
| F0025 Commission Revenue | Expected-commission read context and explicit handoff of billing/payment/reconciliation ownership | Done/archived; approved run `2026-07-07-9859bad4`. |
| F0030 Integration Hub | Future production adapter/outbox/transport seam only | Planned; not a first-release blocker or runtime dependency. |
| ADR-008 / ADR-011 | Casbin enforcement and append-only workflow/audit conventions | Available. |

KG lookup informed navigation only. Raw PRDs, ADRs, API/schema contracts, authorization policy, and implementation paths remain authoritative.

## Target File Map

### Backend

- Domain entities: `engine/src/Nebula.Domain/Entities/{BillingInvoice,PaymentReceipt,PaymentReceiptImportBatch,PaymentReceiptImportRowOutcome,PaymentApplication,ReconciliationException,BillingCorrection}.cs`
- DTOs/validators: `engine/src/Nebula.Application/DTOs/BillingDtos.cs`, `engine/src/Nebula.Application/Validators/BillingValidators.cs`
- Interfaces/services: `engine/src/Nebula.Application/Interfaces/IBillingRepository.cs`, `engine/src/Nebula.Application/Services/BillingReconciliationService.cs`
- Persistence: `engine/src/Nebula.Infrastructure/Repositories/BillingRepository.cs`, `engine/src/Nebula.Infrastructure/Persistence/Configurations/BillingConfiguration.cs`, `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs`
- Migration: `engine/src/Nebula.Infrastructure/Persistence/Migrations/<timestamp>_F0026_BillingReconciliation.cs` plus snapshot
- Wiring/endpoints: `engine/src/Nebula.Infrastructure/DependencyInjection.cs`, `engine/src/Nebula.Api/Endpoints/BillingEndpoints.cs`, `engine/src/Nebula.Api/Program.cs`
- Policy: `engine/src/Nebula.Api/auth_model/policy.csv` kept byte-for-byte action-aligned with `planning-mds/security/policies/policy.csv`

### Frontend

- Feature: `experience/src/features/billing/{api,components,hooks,types,tests}/**`
- Pages: `experience/src/pages/{BillingWorkspacePage,BillingInvoiceDetailPage,ReconciliationWorkspacePage}.tsx`
- Shell: `experience/src/App.tsx`, `experience/src/components/layout/Sidebar.tsx`, and related route/navigation tests

### Tests

- Backend unit: validators, exact-application invariant, same-user denial, terminal decisions, CSV profile/idempotency
- Backend integration: endpoint auth/source scoping, persistence/reload, transaction rollback, no-leak counts, ETag/preconditions, migration
- Frontend: workspace/detail/reconciliation behavior, permission-shaped actions, CSV outcomes, error/empty/stale states, light/dark responsive smoke
- Contract: OpenAPI + JSON schema parity and ProblemDetails codes

## Domain Contract

| Aggregate / Record | Mutable State | Immutable / Unique Rules |
|--------------------|---------------|--------------------------|
| BillingInvoice | status, operational outstanding, row version | Invoice number unique; policy/version/account/currency/original amount immutable after create. |
| PaymentReceipt | application status, row version | Source evidence immutable; `(Source, ExternalReference)` unique. |
| PaymentReceiptImportBatch | none after completion | File name/hash/version/counts and actor/time retained; raw bytes discarded. |
| PaymentReceiptImportRowOutcome | none | One immutable result per submitted row. |
| PaymentApplication | none | One receipt/one invoice, full exact amount only; append-only. |
| ReconciliationException | link metadata, Open/Resolved, row version | Opening/resolution history emitted; balance never changed here. |
| BillingCorrection | Pending to Approved/Rejected, row version | Request/decision immutable; requester cannot decide; terminal once decided. |

`PaymentApplication` and approved `BillingCorrection` transactions lock/check all affected row versions and append timeline events before commit. They do not call an external service.

## Endpoint and Authorization Contract

| Method / Route | Action | Roles | Important Responses |
|----------------|--------|-------|---------------------|
| `GET /billing-invoices` | `billing:read` | Finance Operations Analyst, Finance Manager, Admin | 200; source-filtered rows/counts |
| `POST /billing-invoices` | `billing:invoice_create` | Finance Operations Analyst, Admin | 201; 409 duplicate; 422 invalid policy/version/context |
| `GET /billing-invoices/{invoiceId}` | `billing:read` | Finance Operations Analyst, Finance Manager, Admin | 200/404 |
| `GET /policies/{policyId}/billing-summary` | `billing:summary_read` + `policy:read` | Finance roles, DistributionUser/Manager, RelationshipManager, Admin | bounded 200/404; no finance detail |
| `GET /payment-receipts` | `billing:read` | Finance Operations Analyst, Finance Manager, Admin | source-filtered 200 |
| `POST /payment-receipts` | `billing:receipt_record` | Finance Operations Analyst, Admin | 201; 409 duplicate |
| `POST /payment-receipt-imports` | `billing:receipt_import` | Finance Operations Analyst, Admin | 201 with created/duplicate/rejected row outcomes; 413/422 |
| `GET /payment-receipt-imports/{batchId}` | `billing:read` | Finance Operations Analyst, Finance Manager, Admin | 200/404 |
| `POST /payment-applications` | `billing:application_apply` | Finance Operations Analyst, Admin | 201 exact; 409 mismatch/applied; 412/428 precondition |
| `GET /reconciliation-exceptions` | `billing:backlog_read` | Finance Operations Analyst, Finance Manager, Admin | source-filtered list/count |
| `PATCH /reconciliation-exceptions/{exceptionId}/reference` | `billing:exception_manage` | Finance Operations Analyst, Admin | 200; 409 ineligible; 412/428 |
| `POST /reconciliation-exceptions/{exceptionId}/corrections` | `billing:correction_request` | Finance Operations Analyst, Admin | 201 pending; 409 existing pending |
| `POST /billing-corrections/{correctionId}/decision` | `billing:correction_approve` | Finance Manager, Admin | 200; 403 self; 409 terminal; 412/428 |
| `GET /reconciliation-backlog` | `billing:backlog_summary_read` | Finance Manager, Admin | 200 management totals after filtering |

All finance endpoints authorize the named action and linked policy/account before materialization. Summary access never implies finance detail access. External and unlisted roles have no F0026 allow lines.

## Mock CSV Adapter

- Multipart field: `file`; media type `text/csv`; sanitized `.csv` file name.
- Limits: 1 MiB, 1,000 data rows, UTF-8 (BOM allowed), comma delimiter, RFC 4180 quoting.
- Version/header: `mock-payment-receipt-row-v1`; `externalReference,receivedDate,currency,amount,invoiceReference,memo`.
- Required: first four fields. Amount is positive, maximum two decimals; date ISO; currency uppercase three-letter code.
- Normalize only surrounding whitespace for unquoted values. Never evaluate spreadsheet formulas. Reject NUL/control characters and log no raw memo.
- A retry creates another batch/outcome set. Unique receipt source/reference turns prior successes into deterministic `Duplicate` outcomes.

## Assembly Order

1. Add domain enums/entities, EF configurations, indexes/constraints, migration, and DbContext sets.
2. Add DTOs, JSON schema parity, validators, repository projections, and mock CSV parser.
3. Implement the application service transaction boundaries, source-record authorization, ETag checks, timeline payloads, and named ProblemDetails.
4. Add Minimal API endpoints and keep API/policy/KG source contracts synchronized.
5. Build the React feature slice and shell routes/navigation using semantic tokens and accessible controls.
6. Add unit/integration/contract/UI tests, then apply/rollback migration and run maximum-size import/performance checks.
7. During feature-action G7, reconcile code-index bindings to the paths that actually land; do not pre-bind nonexistent implementation paths.

## Mutation Traceability

| Mutation | Atomic Writes | Timeline Event |
|----------|---------------|----------------|
| Invoice create | invoice | `BillingInvoiceCreated` |
| Manual receipt | receipt | `PaymentReceiptRecorded` |
| CSV import | batch, row outcomes, new receipts | `PaymentReceiptImportCompleted` |
| Exact application | application, receipt state, invoice balance/status | `ExactPaymentApplied` |
| Reference correction | exception link/state | `ReconciliationReferenceCorrected` |
| Correction request | correction pending | `BillingCorrectionRequested` |
| Correction approve/reject | correction decision; invoice balance only on approve | `BillingCorrectionApproved` / `BillingCorrectionRejected` |

Events contain IDs and bounded operational before/after facts, never raw CSV or unrestricted memo/note values.

## Security, Reliability, and Performance Checks

- Prove finance action and source-record authorization happen before rows, counts, totals, existence hints, and import/detail outcomes.
- Prove same-principal decision denial even when the caller also holds Admin.
- Prove duplicate retry, concurrent application, stale decision, and transaction rollback are deterministic.
- Prove no request invokes a bank/vendor network, outbox, ledger, tax, or F0030 service.
- Do not cache transactional finance data. Default page 25, maximum 100.
- Targets: list/detail/backlog p95 <= 500 ms; 1,000-row import p95 <= 5 s in representative local integration tests.
- Redact external reference/memo/reason values from structured logs; use IDs, outcome codes, counts, duration, and correlation ID.

## Frontend Behavior Contract

- `/billing` provides search/filter, invoice results, context, and receipt/import actions based on server permissions.
- `/billing/invoices/:invoiceId` shows operational balance, applications, exceptions, and permitted audit detail.
- `/billing/reconciliation` supports explicit selection and exact application; mismatch UI explains that no balance changed and links to the exception.
- States: initial loading, populated, empty, filtered empty, 404/hidden, validation, duplicate, mismatch, stale 412, missing 428, partial import outcomes, unexpected retry, and read-only summary.
- Keyboard navigation, focus restoration after dialogs/drawers, programmatic labels, error summaries, reduced motion, 320/768/1280 px checks, and light/dark theme smoke are required.

## Story-to-Slice Trace

| Story | Backend | Frontend | Primary Verification |
|-------|---------|----------|----------------------|
| S0001 | invoice search/detail + policy summary | workspace/context | auth-filtered rows/counts and bounded summary |
| S0002 | invoice create | create panel | policy/version/account invariant + persistence |
| S0003 | manual receipt + mock import | receipt drawer/import results | provenance, duplicate idempotency, row outcomes |
| S0004 | exact application transaction | reconciliation workspace | exact-only/no-mutation-on-mismatch |
| S0005 | exception reference + correction workflow | exception/decision panels | SoD, terminal/stale decisions, before/after audit |
| S0006 | backlog query + timeline | backlog/audit views | post-filter aggregates and immutable history |

## Required Feature-Action Signoffs

| Role | Required | Focus |
|------|----------|-------|
| Quality Engineer | Yes | Exact reconciliation, import outcomes, correction workflow, UI states |
| Code Reviewer | Yes | Transaction boundaries, persistence, API/UI composition |
| Security Reviewer | Yes | Finance data, source-scope no-leak behavior, separation of duties, CSV handling |
| DevOps | Yes | Migration, import resource limits, deploy/runtime health |
| Architect | Yes | Finance boundary, as-built contracts, G7 KG reconciliation |

## Exit Criteria for Implementation

All six stories meet their acceptance criteria; contracts and policies are parity-validated; migrations apply/rollback; exact/mismatch/duplicate/concurrency/security tests pass; light/dark responsive UI checks pass; feature-action evidence is indexed; and G7 binds only the implementation paths that actually exist.

## G3 Architect Reconciliation

The first code-review cycle found four plan-to-implementation gaps and routed them back to the Architect before shared contract edits. The reconciled as-built contract now:

- returns a `BillingInvoiceDetail` envelope with the invoice, exact applications, receipt provenance, linked exceptions, and permitted audit events;
- embeds the current pending correction in its source-authorized exception so a manager can reload and decide it without transient identifiers;
- reports exact applications, pending corrections, rejected import rows, and duplicate import rows alongside the existing exception backlog measures; and
- resolves the linked policy/version context before checking the global invoice-number uniqueness constraint, preventing unauthorized existence hints.

These changes clarify and implement the existing S0001, S0005, S0006, frontend-behavior, and authorization clauses; they do not widen F0026 scope.
