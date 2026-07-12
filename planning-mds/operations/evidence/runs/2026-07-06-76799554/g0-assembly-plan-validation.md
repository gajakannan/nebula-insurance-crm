# G0 Assembly Plan Validation - F0037

## Scope Review

The feature assembly plan for F0037 is present and aligned with the approved plan run `2026-07-06-6e3851ab`. It keeps the build inside the existing CRM modular monolith, extends the F0023/F0008 projection visibility path, introduces a distribution-scope resolver, and adds the `GET /operational-reports/distribution-rollups` contract.

## Story Coverage

- F0037-S0001 maps to distribution-scope resolution.
- F0037-S0002 maps to hierarchy-aware read scoping.
- F0037-S0003 maps to search, saved-view, broker-insight, and report visibility.
- F0037-S0004 maps to distribution rollup reporting.
- F0037-S0005 maps to UI filters, panels, drilldowns, and no-leak states.
- F0037-S0006 maps to security evidence, permission parity, and reconciliation checks.

## Integration Checkpoints

- Backend scope resolver lands before predicate expansion.
- Visibility predicates apply before rows, counts, facets, suggestions, drilldowns, and rollups.
- Rollup API and schema reuse existing projection/reporting substrate.
- Frontend work follows backend contracts and preserves existing report/search patterns.
- Security, QE, code review, KG reconciliation, and PM closeout are required before completion.

## Signoff Roles

- Quality Engineer: required.
- Code Reviewer: required.
- Security Reviewer: required because F0037 introduces access-control enforcement and no-leak behavior.
- Architect: required for G0/G7 semantics and as-built KG reconciliation.
- DevOps: conditional; not required unless migrations, runtime topology, materialized jobs, or deployment configuration changes are introduced.

## Result

PASS
