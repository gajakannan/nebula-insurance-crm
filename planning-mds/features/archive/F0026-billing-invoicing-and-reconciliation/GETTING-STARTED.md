# F0026 — Billing, Invoicing & Reconciliation — Getting Started

## Planning State

Phase A and Phase B were approved on 2026-07-19. Implementation may begin only through the F0026 feature action, using ADR-034 and the feature assembly plan as its G0 contract.

## Prerequisites

- Review F0018 Policy Lifecycle & Policy 360 for policy/version/account identity and premium source facts.
- Review F0025 Commission, Producer Splits & Revenue Tracking for expected-commission context and the explicit billing/reconciliation boundary.
- Use F0030 only as a future production connector seam; it is not a first-release dependency.

## First-Release Boundary

- Agency bill only.
- Manual receipt entry plus CSV through a mock bank/payment-vendor adapter.
- Explicit exact currency/full-outstanding application only.
- No partial/overpayment allocation, automatic tolerance, write-off, direct bill, real bank connection, ledger, tax, or settlement.
- Finance Operations Analyst prepares/reconciles; a different Finance Manager approves balance corrections.

## Verification Entry Points

After Phase B is approved, use the feature assembly plan as the implementation contract. Verification must prove policy-linked invoices, provenance-preserving receipt ingestion, no balance mutation on mismatch, separation of duties, immutable audit evidence, and permission-safe counts/drilldowns.

## Mock CSV Fixture Contract

Use UTF-8 CSV with the exact header below. This is the in-process `mock-payment-receipt-row-v1` profile, not a production bank format.

```csv
externalReference,receivedDate,currency,amount,invoiceReference,memo
MOCK-000001,2026-07-19,USD,1250.00,AGB-2026-000001,Exact test receipt
```

The first four values are required. Imports are capped at 1 MiB and 1,000 data rows. A repeated external reference for the mock-vendor source produces a duplicate row outcome and never a second receipt. Raw CSV bytes are discarded after sanitized metadata, SHA-256, counts, and row outcomes are persisted.
