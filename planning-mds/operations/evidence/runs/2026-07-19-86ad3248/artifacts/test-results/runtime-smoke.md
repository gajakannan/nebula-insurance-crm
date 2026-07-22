# F0026 Runtime Smoke Evidence

- Date: 2026-07-19
- Runtime: product Docker Compose stack, API at `http://127.0.0.1:8080`, PostgreSQL compose service.
- Build/start: `docker compose up -d --build api` exited 0 after the final invalid-UTF-8 handling change.
- Health: `GET /healthz` returned HTTP 200 and body `Healthy`.
- Migration: `20260719212424_F0026_BillingInvoicingReconciliation` is present in `__EFMigrationsHistory`.
- Schema: PostgreSQL resolved `BillingInvoices`, `PaymentReceipts`, and `ReconciliationExceptions` as live tables.
- Authorized billing query: `GET /billing-invoices?pageSize=5` returned HTTP 200 with an empty bounded page for the runtime reviewer persona.
- Anonymous billing query: the same request without a bearer token returned HTTP 401.
- Validation: an empty invoice-create request returned HTTP 400 with validation details.
- Import boundary: a multipart CSV containing invalid UTF-8 returned HTTP 422 with `payment_receipt_import_invalid_encoding`.
- Persisted happy path: the API created an invoice and a matching manual receipt against seeded policy/version/account context, enforced the missing `If-Match` precondition with HTTP 428, accepted the retry with the current invoice row version, created one exact payment application, and returned the reloaded invoice as `Reconciled` with outstanding amount `0`.
- Persisted-flow artifacts: `runtime-persisted-flow.txt` records the expected HTTP 428 chronology; `runtime-persisted-flow-completion.txt` records the successful application and reload.
- Runtime result: PASS.

The exact commands and exit codes are in `commands.log`; no tokens or secret values are retained here.
