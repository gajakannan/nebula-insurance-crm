# F0002: Broker & MGA Relationship Management

**Feature ID:** F0002
**Feature Name:** Broker & MGA Relationship Management
**Priority:** Critical
**Phase:** MVP

## Feature Statement

**As a** Distribution and Relationship team member
**I want** a unified broker/MGA management workspace
**So that** I can manage records, contacts, and activity context without fragmented tools

## Business Objective

- **Goal:** Establish a reliable broker relationship source of truth for intake and collaboration workflows.
- **Metrics:**
  - API latency: broker create/read/update p95 < 500ms; broker search p95 < 300ms (per story-level targets)
  - Data completeness: 90% of broker records have LegalName, LicenseNumber, State, and at least one Contact within 30 days of creation
  - Auditability: 100% of broker mutations produce an immutable timeline event
- **Baseline:** Spreadsheet-based tracking; no latency SLA, no audit trail, estimated 40% of broker records missing contact information.
- **Target:** All three metric targets met within 60 days of internal go-live.

## Problem Statement

- **Current State:** Broker and MGA relationship information is spread across spreadsheets and messages.
- **Desired State:** Structured broker profiles, contact data, and timeline history in one system.
- **Impact:** Improves intake speed, handoff quality, and operational accountability.

## Scope & Boundaries

**In Scope:**
- Broker creation, retrieval, update, and delete lifecycle (soft delete where applicable).
- Contact association and broker search by name/license.
- Timeline events for all broker mutations.

**Out of Scope:**
- External broker portal access (broker self-service login or profile management)
- Advanced analytics, scoring models, or risk ratings for brokers
- Bulk broker import or CSV upload
- MGA/broker hierarchy management (parent-child relationships between brokers and MGAs) — deferred to a future MGA management feature
- Broker license verification via external regulatory API
- Duplicate broker detection beyond license number exact-match uniqueness
- Broker data enrichment from third-party data sources
- Multi-jurisdiction (non-US) license number formats or phone normalization
- Hard delete (permanent removal) of broker records

## Success Criteria

- Broker create/search workflows are available with role-aware access control.
- Every broker mutation produces immutable timeline records.
- Core broker data quality checks (required fields, uniqueness, format validation) are enforced.

## Risks & Assumptions

- **Risk:** Inconsistent legacy broker data quality during migration/adoption.
- **Assumption:** License number uniqueness is sufficient for MVP deduplication.
- **Mitigation:** Enforce strong validation and deterministic conflict responses.

## Dependencies

- authentik OIDC and Casbin ABAC enforcement (see ADR-006).
- Broker, Contact, and timeline data model baseline.

## Related User Stories

- F0002-S0001 - Create Broker
- F0002-S0002 - Search Brokers
- F0002-S0003 - Read Broker (Broker 360 View)
- F0002-S0004 - Update Broker
- F0002-S0005 - Deactivate Broker
- F0002-S0006 - Manage Broker Contacts
- F0002-S0007 - View Broker Activity Timeline
- F0002-S0008 - Reactivate Broker
- F0002-S0009 - Adopt Native Casbin Enforcer (authorization hardening)

## Rollout & Enablement

- Internal team onboarding for broker workflow usage.
- Runbook updates for support and access troubleshooting.
