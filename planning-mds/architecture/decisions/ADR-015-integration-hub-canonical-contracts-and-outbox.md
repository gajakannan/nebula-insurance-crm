# ADR-015: Establish Integration Hub with Canonical Contracts and Outbox Delivery

**Status:** Proposed
**Date:** 2026-03-23
**Owners:** Architect
**Related Features:** F0021, F0029, F0030

## Context

Nebula will need external integrations across communications, documents, carriers, portals, and finance. If each feature connects to external systems with bespoke payloads and direct transactional calls, the platform will accumulate brittle point-to-point coupling.

The CRM release plan calls for an integration hub and data-exchange capability that can evolve without destabilizing core transactional workflows.

## Decision

Establish an integration architecture based on:

- canonical internal integration contracts
- connector-specific adapters
- asynchronous delivery where appropriate
- an outbox or equivalent reliable publication pattern
- operational replay and monitoring controls

## Scope

This ADR governs:

- outbound event publication boundaries
- inbound integration handling patterns
- connector adapter responsibilities
- retry, replay, and dead-letter expectations

## Consequences

### Positive

- External-system coupling is reduced.
- Event delivery becomes more reliable and observable.
- New connectors can share common operational patterns.

### Negative

- Adds architectural abstraction and operational tooling requirements.
- Canonical contracts must be versioned carefully to avoid drift.

## Follow-up

- Reference this ADR from portal, communication, and integration-hub PRDs.
- Define first-wave canonical contracts and replay controls.
- Align secrets and connector-auth handling with security guidance.
