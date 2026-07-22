---
template: feature
version: 1.1
applies_to: product-manager
---

# F0026: Billing, Invoicing & Reconciliation

**Feature ID:** F0026
**Feature Name:** Billing, Invoicing & Reconciliation
**Priority:** Medium
**Phase:** Brokerage Platform Expansion
**Planning Status:** Phase A and Phase B approved on 2026-07-19 under plan run `2026-07-19-79477865`; ready for the feature action.

## Feature Statement

**As a** finance operations user
**I want** to create agency-bill invoices, record mock-vendor payment receipts, and reconcile exact payments to policy-linked invoices
**So that** billing exceptions and outstanding operational work are traceable in Nebula without turning the CRM into an accounting system

## Business Objective

- **Goal:** Add a bounded agency-bill operations workflow beside policy and expected-commission context.
- **Baseline:** Nebula records policy premium and expected commission but has no invoice, payment-receipt, or reconciliation workflow.
- **Target:** Every first-release invoice is linked to a policy/account, every mock-vendor receipt has source provenance, and every exact match or exception is visible to authorized finance users.
- **Measures:** invoice-to-policy linkage completeness; receipt import/entry success and duplicate counts; exact-match and exception counts; unresolved exception age; audit-event completeness.

## Problem Statement

- **Current State:** Finance users reconcile policy-related agency-bill activity outside Nebula and cannot trace invoice/payment exceptions from policy context.
- **Desired State:** Authorized users can issue policy-linked agency-bill invoices, record manual or CSV/mock-vendor receipts, explicitly apply exact receipts, and manage exceptions with separation of duties.
- **Impact:** Finance-facing operational work becomes visible alongside policy and commission context while ledger, bank, tax, and settlement ownership remain external.

## Personas

- **Finance Operations Analyst (primary):** Creates invoices, records/imports mock-vendor receipts, performs exact reconciliation, and requests correction adjustments.
- **Finance Manager (primary approver):** Reviews balance-affecting correction requests, approves or rejects them, and monitors reconciliation backlog.
- **Distribution/Relationship user (secondary):** Reads permitted billing summaries from policy/account context but cannot mutate finance records.
- Detailed role archetypes: [`finance-operations-personas.md`](../../examples/personas/finance-operations-personas.md).

## G1 Clarification Decisions

1. First release is **agency bill only**; direct-bill carrier processing is deferred.
2. Payment inputs are manual entry and CSV through a **mock bank/payment-vendor adapter**; no real bank connectivity.
3. Reconciliation is exact currency/amount matching. Partial, under-, over-, and cross-currency items remain exceptions; no automatic tolerances or write-offs.
4. Finance Operations Analyst prepares and reconciles; Finance Manager approves or rejects balance-affecting corrections. A requester cannot approve the same request.
5. F0030 Integration Hub is a deferred integration seam, not a hard dependency.

## Scope & Boundaries

### In Scope — First Release

- Agency-bill invoice creation and authorized list/detail visibility.
- Stable links from invoices to policy, policy version, and account context.
- Manual payment-receipt entry and CSV/mock-vendor receipt ingestion with provenance.
- Explicit application of a same-currency receipt whose amount exactly equals the selected invoice's outstanding amount.
- Reconciliation exceptions for missing invoice context, amount/currency mismatch, duplicate external reference, or unusable source data.
- Analyst-requested, manager-approved correction adjustments that change Nebula's operational billing record without posting a ledger entry or write-off.
- Finance workspace, reconciliation backlog, immutable audit/timeline evidence, and read-only policy/account summaries.

### Out of Scope

- Direct-bill carrier statements or carrier sweep reconciliation.
- Real bank, lockbox, ACH, card, or payment-gateway connectivity.
- Automatic tolerances, partial payment application, overpayment allocation, cross-currency conversion, refunds, chargebacks, write-offs, collections, or dunning.
- General ledger, accounts payable, tax calculation/reporting, financial statements, bank settlement, producer payouts, or GL export.
- External broker/insured payment portal or invoice delivery/email generation.
- Production F0030 connector implementation.

## Success Criteria

- 100% of created invoices identify an authorized policy, immutable policy version, and account.
- 100% of recorded receipts identify manual or mock-vendor/CSV provenance and an external reference.
- Exact applications persist after reload and show invoice, receipt, actor, timestamp, and resulting operational balance state.
- Non-exact items never mutate invoice balance; they appear as actionable exceptions.
- Every invoice, receipt, application, exception disposition, and correction decision creates immutable audit evidence.
- Finance mutation data is unavailable to external roles and read-only for distribution/relationship users.
- No first-release path posts to a bank, ledger, tax system, or real payment vendor.

## Dependencies

| Dependency | State | Why It Matters | Evidence |
|------------|-------|----------------|----------|
| F0018 Policy Lifecycle & Policy 360 | Done and archived | Supplies policy/account identity, immutable policy-version premium context, and policy authorization. | Feature evidence audit pending; raw PRD, ADR-018, KG, and implemented paths are available. |
| F0025 Commission, Producer Splits & Revenue Tracking | Done and archived | Supplies expected-commission context and the explicit handoff of billing/payments/reconciliation scope to F0026. | Approved feature run `2026-07-07-9859bad4`. |
| F0030 Integration Hub & Data Exchange | Planned; deferred | Future production connector seam for payment/billing exchanges. | Not a first-release blocker. |

## Story Map

| Story | Title | Slice Type | User Value |
|-------|-------|------------|------------|
| F0026-S0001 | Billing workspace search and policy context | Read visibility | Finance users can find authorized invoices and open policy-linked context. |
| F0026-S0002 | Create an agency-bill invoice | Invoice mutation | Analysts can record an auditable policy-linked receivable item. |
| F0026-S0003 | Record manual and mock-vendor payment receipts | Receipt ingestion | Analysts can capture payment evidence without real bank connectivity. |
| F0026-S0004 | Apply an exact payment and reconcile an invoice | Exact reconciliation | Analysts can close exact items without tolerance or partial allocation. |
| F0026-S0005 | Review exceptions and approve correction adjustments | Controlled exception resolution | Analysts and managers can resolve data corrections with separation of duties. |
| F0026-S0006 | Monitor reconciliation backlog and audit history | Operational oversight | Managers can prioritize unresolved work and inspect immutable decisions. |

## Screen Responsibilities

| Screen / Surface | Responsibility |
|------------------|----------------|
| Billing Workspace | Search/filter authorized invoices and open policy/account context. |
| Invoice Create | Select policy/version, capture invoice fields, validate, and save an agency-bill invoice. |
| Invoice Detail | Show invoice, operational balance, linked receipts, exceptions, and audit history. |
| Payment Receipt Drawer | Record one manual receipt or upload a mock-vendor CSV and review row outcomes. |
| Reconciliation Workspace | Select invoice/receipt pairs, apply exact matches, and route mismatches to exceptions. |
| Exception Review Panel | Correct links/data, request a balance correction, and approve/reject under separation of duties. |
| Reconciliation Backlog | Filter unresolved items, view days open and source context, and drill to audit history. |

## Screen Layouts (ASCII)

### Desktop — Billing and Reconciliation Workspace

```text
+--------------------------------------------------------------------------------+
| Billing & Reconciliation                                                       |
| Search [invoice / policy / account / receipt]  State [v]  Exceptions [v]       |
+-------------------------+--------------------------+---------------------------+
| Invoice results         | Selected invoice         | Reconciliation            |
| Invoice | Policy        | Amount / outstanding     | Receipt [select]           |
| Account | Due date      | Policy version / account | [Apply Exact Match]        |
| Exception indicator     | Receipt links / audit    | Exception reason / action  |
+-------------------------+--------------------------+---------------------------+
| Backlog: unresolved count | oldest days open | mock-vendor import failures      |
+--------------------------------------------------------------------------------+
```

### Narrow — Billing Workspace

```text
+--------------------------------------+
| Billing & Reconciliation              |
| Search                                |
| State [v]  Exceptions [v]             |
+--------------------------------------+
| Invoice card                          |
| Invoice / policy / account            |
| Amount / outstanding / exception      |
+--------------------------------------+
| Detail tabs                           |
| Summary | Receipts | Exceptions | Audit|
+--------------------------------------+
| [Record Receipt] [Reconcile]           |
+--------------------------------------+
```

## Workflow Summary

1. Finance Operations Analyst opens Billing Workspace and selects an authorized policy/version.
2. Analyst creates an agency-bill invoice; the saved invoice appears with policy/account context and audit evidence.
3. Analyst records a receipt manually or imports mock-vendor CSV rows; ingestion alone does not change invoice balance.
4. Analyst selects a receipt and invoice for explicit application.
5. Same-currency/full-outstanding exact matches persist as reconciled; every mismatch remains unapplied and creates an exception.
6. Analyst corrects non-balance data or requests a balance correction; a different Finance Manager approves or rejects it.
7. Manager monitors unresolved backlog, days open, source provenance, and the immutable audit trail.

## Business Rules

1. **Agency-bill boundary:** Every invoice is an internal operational record tied to one policy, one immutable policy version, and the policy's account.
2. **Receipt provenance:** Every receipt records whether it came from manual entry or the mock-vendor CSV adapter and retains its external reference.
3. **No implicit cash application:** Recording/importing a receipt does not change any invoice until an analyst explicitly applies it.
4. **Exact match only:** Application succeeds only when receipt currency equals invoice currency and receipt amount equals the full outstanding amount. All other cases remain exceptions.
5. **No automatic tolerance or write-off:** The first release cannot auto-accept differences or classify them as write-offs.
6. **Separation of duties:** The analyst who requests a balance-affecting correction cannot approve or reject that request.
7. **Source preservation:** Corrections do not mutate F0018 policy premium/version facts or F0025 expected-commission source records.
8. **Audit:** Successful mutations record actor, time, affected identifiers, before/after operational values, and a human-readable summary.
9. **Internal finance scope:** External users receive no F0026 access. Distribution/relationship users are read-only where source-record authorization permits.

## Risks & Mitigations

- **Risk:** Scope expands into an AMS/accounting replacement. **Mitigation:** Explicit exclusions prohibit ledger, bank, tax, direct-bill, settlement, and write-off behavior.
- **Risk:** Mock integration is mistaken for production connectivity. **Mitigation:** UI/docs label the adapter as mock and no production credentials or network dependency exist.
- **Risk:** Users expect partial/overpayment handling. **Mitigation:** Non-exact items remain visible exceptions; the limitation is explicit in every affected story.
- **Risk:** Finance data leaks through counts or linked CRM views. **Mitigation:** Require finance-resource and source-record authorization before rows, totals, counts, or drilldowns.

## Questions & Assumptions

**Open Questions:** None blocking for Phase A approval.

**Phase B Decisions:** ADR-034 and the feature assembly plan define the `BillingReconciliation` persistence boundary, exact-application transaction, versioned mock CSV profile, source/reference idempotency, 1 MiB/1,000-row import limits, row-version preconditions, Finance Operations Analyst/Finance Manager policy actions, no-cache posture, and F0030 production-integration seam.

## Existing Architecture Notes To Reconcile In Phase B

The prior shell proposed invoice/billing services, payment references, reconciliation records, finance views, and batch exchange boundaries. Phase B must retain only the portions compatible with the approved agency-bill, mock-vendor, exact-match scope and record the reconciliation in `gate-decisions.md`; it must not silently restore direct-bill, real-bank, tolerance, write-off, or full-accounting behavior.

## Related User Stories

- [F0026-S0001](./F0026-S0001-billing-workspace-search-and-policy-context.md) — Billing workspace search and policy context
- [F0026-S0002](./F0026-S0002-create-agency-bill-invoice.md) — Create an agency-bill invoice
- [F0026-S0003](./F0026-S0003-record-payment-receipts.md) — Record manual and mock-vendor payment receipts
- [F0026-S0004](./F0026-S0004-apply-exact-payment-and-reconcile-invoice.md) — Apply an exact payment and reconcile an invoice
- [F0026-S0005](./F0026-S0005-review-exceptions-and-approve-corrections.md) — Review exceptions and approve correction adjustments
- [F0026-S0006](./F0026-S0006-monitor-reconciliation-backlog-and-audit.md) — Monitor reconciliation backlog and audit history
