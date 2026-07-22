# F0026 Acceptance Criteria Checklist

## Story Coverage

| Story | Happy Path | Error/Edge Cases | Per-Role Authorization | Interaction Contract | Persistence Evidence | Audit Expectation |
|-------|------------|------------------|------------------------|----------------------|----------------------|-------------------|
| F0026-S0001 | Yes | Empty/error/no-leak states | Yes | Read-only N/A | Reload/query result | Read-only N/A |
| F0026-S0002 | Yes | Invalid source/fields/concurrency | Yes | Yes | Invoice visible after reload | Invoice creation event |
| F0026-S0003 | Yes | Row errors/duplicates/retry | Yes | Yes | Receipt/import outcome after reload | Receipt/import events |
| F0026-S0004 | Yes | Amount/currency mismatch | Yes | Yes | Applied links and balance state after reload | Application/exception events |
| F0026-S0005 | Yes | Self-approval/stale/terminal decision | Yes | Yes | Decision and corrected value after reload | Request/decision events |
| F0026-S0006 | Yes | Empty/error/no-leak states | Yes | Read-only N/A | Stable filtered view | Reads existing immutable history |

## Clarity And Testability

- [x] Every criterion uses observable pass/fail behavior.
- [x] No acceptance criterion uses unbounded words such as fast, intuitive, simple, or secure.
- [x] Agency bill, mock-vendor, exact-match, and no-write-off boundaries are explicit.
- [x] Non-exact receipt cases never mutate invoice balance.

## Mutation Contracts

- [x] Every mutation names its entry point, action, editable state, save result, reload evidence, and role/status constraint.
- [x] Render-only behavior cannot close a mutation story.
- [x] Validation failure preserves data for correction and creates no success audit event.
- [x] Correction request and approval enforce different actors.

## Authorization And Data Safety

- [x] External users have no F0026 access.
- [x] Finance mutations require finance authorization plus source policy/account access.
- [x] Distribution/Relationship access is read-only and bounded.
- [x] Counts, totals, exception indicators, and drilldowns are authorization-filtered.

## Explicit Non-Goals

- [x] No direct bill, real bank/payment connection, partial/overpayment application, automatic tolerance, write-off, ledger, tax, settlement, producer payout, or external portal.
