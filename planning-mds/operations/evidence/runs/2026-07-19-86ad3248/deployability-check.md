# Deployability Check — F0026-billing-invoicing-and-reconciliation run 2026-07-19-86ad3248

## Runtime / Deployment Config Changes

- Added EF Core migration `20260719212424_F0026_BillingInvoicingReconciliation` and updated the model snapshot.
- Registered the billing repository/service/validators and mapped F0026 endpoints during API startup.
- No Dockerfile, compose topology, CI workflow, environment variable, secret, port, or external-service contract changed.
- Synchronous mock-CSV import is bounded to 1 MiB and 1,000 rows; raw uploaded bytes are not retained.

## Migrations / Rollback

- Migration creates F0026 invoice, receipt/import, application, exception, and correction tables with indexes and foreign keys. Review confirmed it does not recreate F0025 tables.
- Compose startup applied `20260719212424_F0026_BillingInvoicingReconciliation`; PostgreSQL resolved the core F0026 tables.
- Rollback: stop API writers, preserve/export F0026 operational rows if required, run the EF migration down to the prior migration, then deploy the prior API image. The generated `Down` path drops only F0026 tables in dependency-safe order.
- Evidence: `artifacts/test-results/runtime-smoke.md` and exact commands in `commands.log`.

## Env / Config Contract

- No new environment variables or configuration keys.
- Existing `ConnectionStrings__DefaultConnection`, authentication authority/audience/roles claim, and Casbin policy paths remain unchanged.
- No production bank/vendor credential or secret is introduced; mock CSV input remains local synchronous test transport.

## Manifest Boolean Cross-Check

- `runtime_bearing=true`: API, application/domain/infrastructure, tests, and real runtime behavior changed.
- `deployment_config_changed=true`: the migration and startup/DI registration are deployment-bearing.
- `frontend_in_scope=true`: billing routes, pages, components, navigation, tests, and visuals changed.
- `security_sensitive_scope=true`: source-scoped finance data, CSV input, authorization, maker-checker separation, and audit behavior changed; Security Reviewer is required.

## Build / Start / Smoke Results

- API build/start: `docker compose up -d --build api` exited 0 after final changes.
- Health: `/healthz` returned HTTP 200 `Healthy`.
- Database: migration-history and table checks passed against compose PostgreSQL.
- Auth: finance query returned 200; anonymous query returned 401.
- Validation/import: invalid invoice returned 400; invalid UTF-8 CSV returned 422 with the declared error code.
- Persisted transaction: invoice and receipt creates returned 201; missing `If-Match` returned 428; exact application with current versions returned 201; reload returned `Reconciled` and outstanding `0`.
- Frontend production build passed; existing chunk-size warning is unchanged.
- Evidence: `artifacts/test-results/runtime-smoke.md`, `artifacts/test-results/runtime-persisted-flow.txt`, and `artifacts/test-results/runtime-persisted-flow-completion.txt`.

## Runtime Warnings

- Existing ASP.NET data-protection key storage warnings remain in the local development container; F0026 does not alter key management.
- Existing verbose projection SQL logging remains noisy but does not block readiness.
- The frontend build retains the repository’s existing large-chunk warning; F0026 introduces no new deployment split or runtime configuration.

## Result

PASS
