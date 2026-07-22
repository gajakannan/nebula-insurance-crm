# Action Context

> Seeded by init-run.py. Fill the judgment sections before G0.

## Run Identity

- **action:** feature
- **contract_effective_date:** 2026-07-11
- **contract_version:** 2026-07-11
- **feature_id:** F0026
- **feature_index_root:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/operations/evidence/features/F0026-billing-invoicing-and-reconciliation
- **feature_slug:** billing-invoicing-and-reconciliation
- **mode:** clean
- **product_root:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm
- **run_folder:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-07-19-86ad3248
- **run_id:** 2026-07-19-86ad3248
- **run_id_prior:** None

## Inputs

- `FEATURE_ID=F0026`
- `MODE=clean`
- `SLICE_ORDER_SOURCE=assembly-plan`
- `PRODUCT_ROOT=/home/gajap/uSandbox/repos/nebula/nebula-insurance-crm`
- Primary spec: `planning-mds/features/F0026-billing-invoicing-and-reconciliation/feature-assembly-plan.md`

## Assumptions

- The completed plan run `2026-07-19-79477865` is planning evidence only and is not an implementation approval package.
- Raw PRD, stories, ADR-034, OpenAPI, JSON schemas, security policy, and as-built source outrank KG projections when they conflict.
- F0018 and F0025 are implemented dependencies; F0030 is a deferred production integration seam, not a runtime dependency.
- The approved assembly plan remains the primary implementation spec; story conflicts would be recorded through `workstate.py decision --topic plan-story-reconcile` before proceeding.

## Scope Boundaries

- Implement only the six F0026 stories: source-authorized invoice visibility/creation, manual and bounded mock-CSV receipts, explicit exact application, controlled corrections, backlog/audit visibility, and the corresponding frontend surfaces.
- Preserve the agency-bill-only boundary. Direct bill, real bank/payment connectivity, partial or tolerance matching, write-offs, refunds, ledger, tax, settlement, producer payouts, and production F0030 transport are excluded.
- Planned runtime scope includes `engine/**`, `experience/**`, database migration/configuration, tests, Casbin policy parity, and feature evidence. No AI/Neuron work is in scope.

## Lifecycle Stage

- G0 Architect assembly-plan validation passed; manifest advanced to `in-progress`.
- G1 DevOps runtime preflight restored the API service, confirmed the `/healthz` readiness contract, and passed stage validation.
- G2 implementation, QE, coverage, deployability, security-scan handoff, accessibility, visual, and real PostgreSQL-backed runtime evidence is assembled for ordered gate validation.
