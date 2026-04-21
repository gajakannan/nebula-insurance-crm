# ADR-014: Introduce Search Index and Saved View Architecture for Operational CRM Workloads

**Status:** Proposed
**Date:** 2026-03-23
**Owners:** Architect
**Related Features:** F0008, F0023, F0032

## Context

Nebula needs cross-object search, operational reporting, and reusable saved views over high-volume CRM data. Direct transactional queries from every screen will not scale cleanly across broker, account, policy, submission, renewal, and task workloads.

Saved views also require a stable query-definition model that can outlive any single UI implementation.

## Decision

Introduce a search and operational-reporting architecture based on:

- read-optimized search documents or index records
- persisted saved-view definitions
- reporting projections for workflow, backlog, and aging metrics

This decision keeps transactional writes separate from search and reporting concerns while preserving navigable links back to source records.

## Scope

This ADR governs:

- cross-object search architecture
- saved-view persistence and sharing semantics
- operational report projection boundaries
- refresh and indexing expectations

## Consequences

### Positive

- Search and reporting can scale independently from transactional workloads.
- Saved views become durable operational artifacts.
- Analytical and operational screens can share a consistent query model.

### Negative

- Adds indexing or projection lag as an explicit concern.
- Requires clear freshness and rebuild expectations.

## Follow-up

- Reference this ADR from search, reporting, broker-insight, and admin-config PRDs.
- Choose concrete indexing and refresh implementation details during implementation planning.
- Define authorization filtering strategy inside the search/report layer.
