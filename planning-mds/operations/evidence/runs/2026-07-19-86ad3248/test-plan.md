---
template: test-plan
version: 2.0
applies_to: quality-engineer
---

# Test Plan — F0026-billing-invoicing-and-reconciliation run 2026-07-19-86ad3248

## Story-to-AC Mapping

The story files express acceptance criteria as happy-path, alternative-flow, and checklist groups rather than numbered ACs; the IDs below preserve that raw grouping.

| Story | AC | Lane | Test ID / Evidence | Owner |
|-------|----|------|--------------------|-------|
| F0026-S0001 | happy/read scope | unit + component + runtime | `Create_search_get_and_summarize_invoice_preserve_policy_context`; `BillingPages` source-authorized workspace; authenticated/anonymous billing query in `runtime-smoke.md` | Developer + QE |
| F0026-S0001 | empty/error/bounded UI | component + visual | `BillingPages` accessible empty/workflow state; billing desktop/mobile screenshots | Frontend Developer + QE |
| F0026-S0002 | create/persist/reload/audit | unit + runtime | `Create_search_get_and_summarize_invoice_preserve_policy_context`; persisted create/reload flow | Backend Developer + QE |
| F0026-S0002 | invalid/source mismatch/duplicate | unit | `Invoice_requires_due_date_on_or_after_invoice_date`; `Create_invoice_rejects_account_or_currency_outside_policy_version_context`; duplicate repository contract | Backend Developer |
| F0026-S0003 | manual receipt/duplicate | unit + runtime | `Manual_receipt_can_be_searched_and_duplicate_source_reference_is_rejected`; persisted receipt flow | Backend Developer + QE |
| F0026-S0003 | bounded CSV/row outcomes/invalid bytes | unit + runtime | `Mock_csv_import_records_created_duplicate_and_rejected_rows_without_retaining_bytes`; `Mock_csv_import_rejects_invalid_utf8_without_persisting_a_batch`; HTTP 422 runtime smoke | Backend Developer + QE |
| F0026-S0004 | exact apply/atomic reload | unit + runtime | `Exact_application_atomically_reconciles_invoice_and_receipt`; persisted exact-application flow | Backend Developer + QE |
| F0026-S0004 | amount/currency/reference conflict | unit | `Amount_mismatch_preserves_balances_and_opens_one_exception`; `Currency_mismatch_preserves_balances_and_opens_exception`; `Conflicting_source_reference_blocks_exact_application` | Backend Developer |
| F0026-S0004 | UI explicit selection | component | `wires explicit exact application with selected receipt context` | Frontend Developer |
| F0026-S0005 | reference-only correction | unit + component | `Reference_correction_updates_only_reference_and_resolves_exception`; reconciliation component flow | Developer + QE |
| F0026-S0005 | maker-checker decision | unit + component | `Different_principal_can_approve_consistent_correction_and_resolve_exception`; `Same_principal_cannot_decide_own_correction_even_as_admin`; separate request/decision UI flow | Developer + QE |
| F0026-S0006 | backlog/audit visibility | unit + component | exception search/backlog service paths exercised in focused suite; accessible reconciliation workspace | Developer + QE |
| F0026-S0006 | responsive/theme presentation | visual + accessibility | six F0026 Playwright screenshots; `BillingPages` jest-axe assertions | Frontend Developer + QE |

## Test Strategy

- Unit: xUnit tests in `engine/tests/Nebula.Tests/Unit/Billing/` cover validators and business invariants with deterministic fakes.
- Component: Vitest/Testing Library tests in `experience/src/features/billing/tests/` and the sidebar test cover rendered states, mutation wiring, routing, and jest-axe assertions.
- Integration/runtime: the real compose API and PostgreSQL database prove migration, auth, invoice/receipt persistence, concurrency precondition, exact application, and reload.
- E2E/visual: Playwright renders billing and reconciliation in light/dark themes and desktop/mobile workspace widths.
- API/error: direct HTTP probes cover 200, 201, 400, 401, 422, and 428 behavior.
- Accessibility: the focused component lane uses jest-axe and the direct accessibility regression lane includes F0026.
- Security: maintained dependency, Gitleaks, Semgrep, and ZAP scanners produce a raw G3 handoff.

## Developer-vs-QE Test Ownership

- Backend and frontend developers own feature-local unit/component tests and green focused reruns.
- QE owns acceptance mapping, regression selection, coverage computation, accessibility/visual execution, persisted runtime flow, skipped-layer triage, and final verdict.
- DevOps owns runtime/container/migration deployability interpretation.
- Security owns exploitability and severity judgments for scanner outputs.

## Test Data / Fixtures

- Unit tests use deterministic in-memory billing and activity repositories with fixed policy/version/account context.
- Frontend tests use MSW handlers and source-authorized finance fixtures; no production credentials or live external system is used.
- Runtime persistence uses the local compose database and one existing seeded policy/version/account. It creates uniquely timestamped F0026 invoice/receipt smoke rows and applies the full matching amount.
- Auth probes use locally constructed non-secret development bearer claims matching the existing Development runtime configuration.

## Happy / Edge / Error / Auth / Accessibility / Regression Cases

- Happy: create/search/reload invoice; record receipt; exact reconcile; reference correction; different-principal correction approval; backlog rendering.
- Edge: duplicate receipt, bounded paging, duplicate CSV row, invalid UTF-8, amount/currency/reference mismatch, missing/stale concurrency preconditions.
- Error: invalid date/fields, source-context mismatch, HTTP 400/422/428 responses, query failure states.
- Auth: authenticated finance read succeeds, anonymous read returns 401, source-scope filtering is applied before rows/counts, same-principal decision is denied even for Admin.
- Accessibility: jest-axe component assertions, semantic definition-list correction, responsive light/dark visual inspection.
- Regression: backend non-integration suite, focused frontend suite, accessibility lane, build, lint/theme/CSS/effects guards.

## Risks And Mitigations

- The repository-wide Testcontainers integration harness cannot keep its ephemeral PostgreSQL available from the SDK container. The feature’s real compose PostgreSQL flow covers the F0026 persistence and transaction path; owner for harness repair: Quality Engineer / DevOps; follow-up: `F0026-TESTCONTAINERS-HARNESS`.
- Repository-wide frontend tests contain unrelated pre-existing failures in `src/services/api.test.ts` and `ContactFormModal.test.tsx`. Focused F0026 suites pass repeatedly; owner: Frontend Platform; follow-up: `FRONTEND-REGRESSION-HARNESS`.
- The `test:accessibility` wrapper cannot find its nested `pnpm` executable in the isolated container. Direct Vitest execution of the same lane passed 11/11; owner: Frontend Platform; follow-up: `FRONTEND-A11Y-WRAPPER`.

## Result

PASS
