# ADR-016: Govern Runtime Configuration Through Published Operational Configuration Sets

**Status:** Proposed
**Date:** 2026-03-23
**Owners:** Architect
**Related Features:** F0022, F0027, F0032

## Context

Nebula is introducing queues, rules, templates, and operational settings that should become configurable without turning the product into an uncontrolled admin surface. Shared runtime configuration needs validation, versioning, publish semantics, and downstream refresh expectations.

Without a governing architecture decision, each module could implement its own ad hoc configuration storage and rollout logic.

## Decision

Govern runtime business configuration through published configuration sets with:

- versioned configuration artifacts
- draft, validated, and published states where needed
- explicit validation before publish
- predictable downstream consumption and refresh behavior
- audit trails for publish and rollback actions

## Scope

This ADR governs:

- runtime business configuration, not infrastructure configuration
- configuration publication and rollback behavior
- consuming-module contract expectations
- audit and privilege expectations for configuration changes

## Consequences

### Positive

- Shared operational settings become governable and auditable.
- Modules can consume stable published configuration instead of mutable raw tables.
- Admin tooling and execution engines can evolve independently.

### Negative

- Adds versioning and publish-state complexity.
- Requires deliberate cache invalidation or refresh semantics.

## Follow-up

- Reference this ADR from queue, template, reporting, and admin-config PRDs.
- Define publishing workflow and consuming-module refresh rules.
- Extend admin screens to expose validation and publish status explicitly.
