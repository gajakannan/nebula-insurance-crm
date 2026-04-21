---
template: feature
version: 1.1
applies_to: product-manager
---

# F0021: Communication Hub & Activity Capture

**Feature ID:** F0021
**Feature Name:** Communication Hub & Activity Capture
**Priority:** High
**Phase:** CRM Release MVP

## Feature Statement

**As a** distribution user, coordinator, or underwriter
**I want** communication history captured in Nebula
**So that** broker and customer interactions are visible, auditable, and actionable without relying on memory or email search

## Business Objective

- **Goal:** Move communication history into the CRM operating record.
- **Metric:** Captured activity volume, follow-up completion, and reduced time spent reconstructing conversations.
- **Baseline:** Important communication lives in Outlook threads and private notes.
- **Target:** Users can review meaningful communication history directly from Nebula records.

## Problem Statement

- **Current State:** Communication trails are fragmented and difficult to audit.
- **Desired State:** Calls, meetings, notes, and email-linked activity are visible in context.
- **Impact:** Faster follow-up, better broker service, and stronger institutional memory.

## Scope & Boundaries

**In Scope:**
- Notes, calls, meetings, and communication events
- Related activity capture on broker, account, submission, and policy records
- Follow-up creation and activity linkage
- Communication timeline visibility

**Out of Scope:**
- Full email-sending client
- Marketing automation
- External messaging integrations beyond the agreed MVP scope

## Success Criteria

- Users can see relevant communication history in context.
- Communication capture supports task follow-up and relationship continuity.
- Activity history reduces dependence on inbox archaeology.

## Risks & Assumptions

- **Risk:** Communication scope expands into a full messaging platform too early.
- **Assumption:** Structured activity capture is more important than full outbound email functionality in the first release.
- **Mitigation:** Focus on capture, visibility, and linkage before deeper integrations.

## Dependencies

- F0016 Account 360 & Insured Management
- F0004 Task Center UI + Manager Assignment

## Architecture & Solution Design

### Solution Components

- Introduce a communication or activity-capture service that owns notes, calls, meetings, and communication events as append-only business records.
- Add follow-up linkage between communication records and task or reminder capabilities instead of forcing communication state into the task schema itself.
- Provide timeline composition services that can render communication history consistently on broker, account, submission, and policy views.
- Keep full email-client behavior and broad messaging integrations out of the initial component set.

### Data & Workflow Design

- Model communication events with type, subject or summary, participants, occurred-at timestamp, linked entity references, and follow-up requirements.
- Preserve immutable timeline history for captured activity while allowing controlled correction or redaction workflows where necessary.
- Link communication records to multiple business objects where appropriate, but define a clear primary entity for ownership and authorization evaluation.
- Reuse task identifiers for follow-up tracking rather than duplicating a second workflow engine for basic reminders.

### API & Integration Design

- Expose endpoints for creating and retrieving communication events, linking them to entities, and creating associated follow-up tasks.
- Design the contract so later integrations with email, calendar, or telephony systems can map into the same communication event model.
- Reuse existing activity timeline patterns and descriptions so downstream reporting can treat communication as another auditable event source.
- Keep the API focused on structured capture and retrieval rather than trying to become a general-purpose messaging platform.

### Security & Operational Considerations

- Apply authorization from the linked business records and respect visibility constraints for sensitive communication notes.
- Support audit logging for creation, edit, redaction, and follow-up completion actions because communication history often becomes evidence in account handling.
- Guard against duplicate ingestion when later connectors sync the same meeting or email more than once.
- Index activity queries by primary entity, participant, event type, and occurred-at date because communication timelines can grow rapidly.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | Communication capture service, follow-up linkage, and communication timeline composition | PRD only |
| Extends: Cross-Cutting Component | Communication events become integration-friendly records for external exchange and replay | [ADR-015](../../architecture/decisions/ADR-015-integration-hub-canonical-contracts-and-outbox.md) (Proposed) |
| Reuses: Established Component/Pattern | Append-only activity timeline and follow-up linkage patterns | PRD only |

## Related User Stories

- To be defined during refinement
