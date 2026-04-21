# Feature Architecture Traceability Taxonomy

Use this taxonomy in feature PRDs to distinguish feature-local design from cross-cutting architectural decisions and to make ADR coverage explicit.

## Classification Labels

| Classification | Meaning | ADR Expectation |
|----------------|---------|-----------------|
| Introduces: Feature-Local Component | New component or service whose contract and lifecycle are owned by a single feature | ADR usually not required unless it later becomes shared or infrastructure-bearing |
| Introduces: Cross-Cutting Component | New shared module, runtime service, data subsystem, or platform capability used by multiple features | ADR required |
| Introduces/Standardizes: Cross-Cutting Pattern | New reusable design pattern, workflow model, contract style, or governance rule expected beyond one feature | ADR required |
| Extends: Cross-Cutting Component | A feature adds capabilities or configuration surfaces to a component already governed elsewhere | Reference the governing ADR |
| Reuses: Established Component/Pattern | The feature depends on an accepted or proposed shared component or pattern without introducing it | Reference the governing ADR when one exists |
| PRD-Only Traceability | The design choice is intentionally local to the feature and remains governed by the PRD | State `PRD only` or `None currently required` |

## ADR Rules

- Write an ADR when a feature introduces a new shared runtime dependency, shared subsystem, cross-cutting pattern, or hard-to-reverse module boundary.
- Reference an existing ADR when the feature reuses or extends a shared component, runtime, or pattern already governed elsewhere.
- Keep feature-local design choices in the PRD unless they later become reusable platform standards.
- Use `Proposed` ADRs when the architectural direction is needed for traceability but has not yet been fully accepted.

## PRD Usage

Each feature PRD should include an `Architecture Traceability` section with short rows that answer:

1. What feature-local components are introduced?
2. What cross-cutting components or patterns are introduced?
3. What existing shared components or ADR-governed patterns are reused?
4. Which ADRs govern the architectural choices?
