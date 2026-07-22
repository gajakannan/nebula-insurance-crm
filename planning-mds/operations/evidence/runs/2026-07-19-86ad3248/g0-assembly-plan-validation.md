# G0 Assembly Plan Validation — F0026

**Role:** Architect  
**Run:** `2026-07-19-86ad3248`  
**Primary spec:** `planning-mds/features/F0026-billing-invoicing-and-reconciliation/feature-assembly-plan.md`  
**Result:** PASS

## Scope Split

- Backend ownership is confined to the in-process `BillingReconciliation` domain/application/infrastructure/API slice, migration, policy parity, and developer-owned tests.
- Frontend ownership is confined to `experience/src/features/billing/**`, three named pages, shell routing/navigation, accessibility, responsive behavior, and theme-safe tests.
- QE owns cross-tier acceptance, E2E, coverage, performance, migration, import-limit, and security-scan execution evidence.
- DevOps owns runtime preflight and deployability evidence; no new external service is introduced.
- AI/Neuron scope is explicitly absent. F0030 remains a deferred production integration seam.

The split covers all six stories and preserves the agency-bill-only, exact-application, no-ledger/no-bank boundary.

## Dependency And Assembly Review

- F0018 provides immutable policy/version/account context and source authorization; implementation must verify raw contracts and as-built paths before reuse.
- F0025 provides expected-commission read context and the explicit boundary excluding billing/payment ownership.
- ADR-034, the F0026 OpenAPI paths/components, JSON schemas, Casbin actions, and timeline events form the cross-agent contract.
- Assembly order is feasible: persistence → DTO/schema/parser → transactional services/authorization → endpoints/policy → UI → cross-tier validation → G7 as-built binding reconciliation.

## Integration Checkpoints

- Persistence: entities, constraints, migration apply/rollback, and optimistic concurrency are green before endpoint integration.
- Contract: OpenAPI, JSON schemas, policy actions, DTOs, and named ProblemDetails remain parity-aligned.
- Mutation: exact application and correction decisions commit state plus immutable timeline evidence atomically; mismatch paths preserve balances.
- Security: finance and linked-source authorization precede materialization, counts, totals, and existence signals; same-principal correction decisions are denied even for Admin.
- UI: permission-shaped actions, empty/error/stale/import states, semantic tokens, focus/keyboard behavior, and 320/768/1280 light/dark checks are verified.
- Release: runtime preflight, full tests, coverage, scans, migration, bounded import performance, and deployability evidence pass before review.

## Artifact Ownership

The plan and `STATUS.md` agree on strict ownership. Required signoff roles are initialized as Quality Engineer, Code Reviewer, Security Reviewer, DevOps, and Architect. Evidence reports are written only under this run folder; terminal reports do not belong in the feature folder.

## Contract Reconciliation

The approved plan matches the PRD, six story acceptance contracts, BLUEPRINT module/screens, solution patterns, ADR-034, and the F0026 OpenAPI/schema surface. No plan/story conflict or shared-semantics change was discovered at G0. The plan's G7 instruction correctly defers code-path binding until as-built source exists.

## Result

PASS
