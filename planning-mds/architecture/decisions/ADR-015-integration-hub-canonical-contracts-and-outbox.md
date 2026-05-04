# ADR-015: Establish Integration Hub with Canonical Contracts and Outbox Delivery

**Status:** Proposed
**Date:** 2026-03-23
**Owners:** Architect
**Related Features:** F0021, F0029, F0030, F0031

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
- the inbound exchange record envelope and the landing zone record lifecycle

## Inbound Exchange Record Envelope

Every record entering the integration hub from an external source is represented as an exchange record envelope. The envelope carries source provenance, contract association, transport metadata, and the business payload as separate fields so that landing, validation, replay, and lineage operations do not require parsing the payload itself.

Required envelope fields:

- `exchangeRecordId` — landing zone identifier assigned at receipt
- `sourceSystem` — originating connector or system identifier
- `sourceBatchId` — batch or feed identifier from the source, where applicable
- `sourceRecordId` — record identifier in the source system
- `recordType` — domain record type (Account, Broker, Contact, Submission, Policy, Renewal, Activity, Document, etc.)
- `operation` — declared intent such as UPSERT or DELETE
- `contractId` and `contractVersion` — versioned contract the record claims to conform to
- `correlationId` — cross-system trace identifier
- `idempotencyKey` — deterministic key used to suppress duplicate processing
- `extractedAt` — timestamp from the source side
- `receivedAt` — timestamp at the integration hub boundary
- `payload` — the business record itself, treated as opaque until contract association and validation

The payload remains separate from envelope metadata so connector-specific source fields do not bleed into operational CRM aggregates, and so envelope-driven concerns (replay, quarantine, lineage) do not require touching business data.

## Landing Zone Record Lifecycle

Each landed exchange record progresses through an explicit lifecycle so that operators can distinguish "received but not yet processed" from "validated and waiting for promotion" from "failed and requiring intervention." States are persisted on the landing zone record and transitions are auditable.

Lifecycle states:

- `Received` — landed in the inbound zone, no contract association attempted yet
- `Validated` — associated with a known contract version and passed structural and semantic checks
- `ValidationFailed` — failed contract validation; remains visible for review and replay
- `DuplicateCandidate` — flagged for deduplication review (used by F0031 import scenarios)
- `ReadyForPromotion` — validated and, where applicable, deduplicated; awaiting promotion
- `Promoted` — mapped through canonical integration models and applied to a CRM domain entity
- `PromotionFailed` — promotion attempted and rejected by the domain layer
- `Quarantined` — held outside the normal pipeline pending operator action
- `Superseded` — replaced by a later record from the same source for the same key

Validation failures capture enough context to debug without resending: failed field path, failure reason, expected constraint, observed value where it is safe to record, contract identifier and version, source system, source batch and record identifiers, and timestamp. Failed records remain in the landing zone and are never silently dropped.

Promotion outcomes preserve source-to-target lineage linking the landing zone record to the resulting domain entity type and identifier, so operational CRM data can be traced back to source system, batch, record identifier, contract identifier, and contract version.

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
- When defining first-wave canonical contracts, evaluate Open Data Contract Standard (ODCS v3.x) for external import/export contracts. ODCS would govern dataset-shaped exchanges (imports, exports, batch feeds) — not OpenAPI/Pact, which remain authoritative for application APIs. See [F0030 NOTES.md](../../features/F0030-integration-hub-and-data-exchange/NOTES.md).
