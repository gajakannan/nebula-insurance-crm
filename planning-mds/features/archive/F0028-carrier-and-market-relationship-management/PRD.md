---
template: feature
version: 1.1
applies_to: product-manager
---

# F0028: Carrier & Market Relationship Management

**Feature ID:** F0028
**Feature Name:** Carrier & Market Relationship Management
**Priority:** Medium
**Phase:** CRM Release MVP+
**Status:** Draft — Phase A refined, pending approval

## Feature Statement

**As a** distribution leader or underwriter
**I want** structured carrier, market, appetite, appointment, and underwriter relationship records
**So that** Nebula can support placement strategy with searchable, auditable market context before deeper carrier integrations are introduced

## Business Objective

- **Goal:** Extend CRM visibility beyond broker/account context to the carrier and market side of commercial P&C placement.
- **Metric:** Users can find market relationship context, review appetite and appointment status, and connect that context to submissions and policies without leaving Nebula.
- **Baseline:** Carrier appetite, market access, appointments, and underwriter contacts are tracked outside the CRM or as unstructured notes on individual workflows.
- **Target:** Carrier-side context is first-class, searchable, permission-safe, and reusable across placement and reporting workflows.

## Problem Statement

- **Current State:** Teams rely on local notes, email memory, and submission-specific carrier/market text when deciding where to place business.
- **Desired State:** Market relationships are structured CRM records with contacts, appetite notes, appointment context, activity history, and links to related submissions and policies.
- **Impact:** Distribution and underwriting teams can make faster placement decisions, preserve institutional market knowledge, and avoid stale or inaccessible carrier intelligence.

## Personas

- **Distribution Leader:** Owns carrier relationships, market access, and placement strategy across teams.
- **Underwriter:** Needs current appetite and relationship context when reviewing submissions or quote packets.
- **Relationship Manager:** Maintains market contacts and records relationship touchpoints.
- **Program Manager:** Reviews market access and appointment context for program-level planning.

## Scope & Boundaries

**In Scope:**
- Carrier and market directory records with status, admitted/non-admitted classification, market segments, geographic availability, and ownership.
- Underwriter and market contact relationships linked to carrier or market records.
- Appetite notes with line of business, geography, effective date, freshness/next-review information, source, confidence, and internal visibility.
- Appointment context with status, jurisdiction/LOB scope, effective dates, owner, and supporting notes.
- Market-side activity visibility and links to related submissions, policies, producers, and territories.
- Permission-safe search/global-discovery participation for carrier and market records.

**Out of Scope:**
- Carrier API integration or synchronization.
- Rating, pricing, eligibility scoring, quote comparison, or recommendation automation.
- Reinsurance workflows.
- External broker collaboration or broker-visible carrier records.
- Commission, billing, and carrier accounting workflows.

## Success Criteria

- Internal users can create and find carrier/market records and open a single relationship workspace for each record.
- Authorized users can manage contacts, appetite notes, appointment context, and market activities with clear audit/timeline evidence.
- Submission and policy users can link to carrier/market context without duplicating relationship data inside workflow-specific screens.
- Global search and reporting can discover carrier/market records without exposing unauthorized internal intelligence.
- Stale appetite and appointment records are visibly flagged by explicit freshness or next-review metadata.

## Screen Layouts (ASCII)

### Desktop — Market Directory

```text
┌────────────────────────────────────────────────────────────────────────────┐
│ Markets                                                                    │
│ Search [ carrier, market, contact, appetite ]  Status [ ] Segment [ ] LOB [ ]│
├───────────────────────────────┬────────────────────────────────────────────┤
│ Market list                   │ Selected market preview                    │
│ ┌───────────────────────────┐ │ Carrier / Market name                      │
│ │ Acme Specialty Markets    │ │ Status · Type · Segments · Regions         │
│ │ Active · E&S · Cyber      │ │ Appetite freshness · Appointment status    │
│ └───────────────────────────┘ │ Primary contacts · Owner                   │
│ ┌───────────────────────────┐ │ Recent activity                            │
│ │ Northstar Casualty        │ │ Related submissions / policies             │
│ └───────────────────────────┘ │ [Open relationship workspace]              │
└───────────────────────────────┴────────────────────────────────────────────┘
```

### Desktop — Market Relationship Workspace

```text
┌────────────────────────────────────────────────────────────────────────────┐
│ Acme Specialty Markets                 [Edit profile] [Add contact]        │
│ Active · E&S · Cyber / Property · Northeast / Midwest · Owner: Maya         │
├──────────────┬─────────────────────────────────────────────────────────────┤
│ Navigation   │ Overview                                                    │
│ Overview     │ Appetite Notes                                              │
│ Contacts     │ ┌────────────┬────────────┬────────────┬─────────────────┐ │
│ Appetite     │ │ LOB        │ Geography  │ Freshness  │ Source/Confidence│ │
│ Appointments │ └────────────┴────────────┴────────────┴─────────────────┘ │
│ Activity     │ Appointment Context                                         │
│ Related Work │ Activity Timeline                                           │
└──────────────┴─────────────────────────────────────────────────────────────┘
```

### Narrow — Market Relationship Workspace

```text
┌──────────────────────────────┐
│ Acme Specialty Markets       │
│ Active · E&S · Cyber         │
│ [Edit] [Add]                 │
├──────────────────────────────┤
│ Tabs: Overview | Contacts    │
│       Appetite | Activity    │
├──────────────────────────────┤
│ Appetite card                │
│ Appointment card             │
│ Related submissions/policies │
│ Recent activity              │
└──────────────────────────────┘
```

## Workflows

| Workflow | Entry Point | Terminal Evidence |
|----------|-------------|-------------------|
| Create or update market profile | Market Directory → New Market / Edit profile | Reload shows saved profile, owner, status, segments, regions, and timeline event |
| Manage underwriter contacts | Market Workspace → Contacts → Add/Edit | Reload shows contact row and contact activity/timeline entry |
| Capture appetite note | Market Workspace → Appetite → Add note | Reload shows note with LOB, geography, source, confidence, and next-review/freshness state |
| Maintain appointment context | Market Workspace → Appointments → Add/Edit | Reload shows appointment status/scope/effective dates and audit event |
| Link market context to placement work | Submission or Policy detail → Market context link/search | Related work panel shows linked record and market workspace shows reciprocal link |
| Discover market records | Global search / Market Directory filters | Only authorized records/counts render; source link opens market workspace |

## Related User Stories

| Story | Title | Status |
|-------|-------|--------|
| [F0028-S0001](./F0028-S0001-market-directory-search.md) | Market directory search and open | Not Started |
| [F0028-S0002](./F0028-S0002-carrier-market-profile-management.md) | Carrier and market profile management | Not Started |
| [F0028-S0003](./F0028-S0003-underwriter-contact-management.md) | Underwriter and market contact management | Not Started |
| [F0028-S0004](./F0028-S0004-appetite-note-capture.md) | Appetite note capture and freshness | Not Started |
| [F0028-S0005](./F0028-S0005-appointment-context-management.md) | Appointment context management | Not Started |
| [F0028-S0006](./F0028-S0006-market-activity-and-related-work.md) | Market activity and related work visibility | Not Started |

## Risks & Assumptions

- **Risk:** Market management scope expands into carrier integrations, rating, or quote comparison too early.
- **Assumption:** Relationship, appetite, and appointment visibility delivers value before systems integration.
- **Mitigation:** Start with CRM-side records, recorded facts, and links; defer data exchange and computation.
- **Risk:** Stale market intelligence may mislead users.
- **Mitigation:** Every appetite note and appointment record carries freshness or effective-date metadata plus owner/source.

## Dependencies

- F0019 Submission Quoting, Proposal & Approval Workflow — completed; provides submission/quote context and recorded carrier/market reference facts.
- F0023 Global Search, Saved Views & Operational Reporting — completed; provides search/reporting substrate to reuse in Phase B.
- F0017 Broker/MGA Hierarchy, Producer Ownership & Territory Management — planned/Now; producer and territory linkage may be read-only or deferred if F0017 is not delivered when F0028 builds.

## Architecture & Solution Design

Phase B must confirm the implementation contract. Phase A requirements expect the architecture to:

- Introduce carrier/market relationship records separate from submission-specific quote packet facts.
- Reuse existing search/reporting and timeline/audit patterns where possible.
- Preserve recorded-fact boundaries: Nebula records appetite/appointment/relationship context; it does not rate, score, price, compare, or recommend markets in F0028.
- Treat appetite notes, appointment context, and relationship intelligence as internal commercially sensitive data.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | Carrier/market relationship workspace, appetite notes, appointment context, and market activity history | Phase B to confirm |
| Reuses: Established Component/Pattern | Global search/reporting substrate, audit/timeline events, ABAC policy model | ADR-014 search architecture; existing workflow/timeline patterns |
| PRD-Only Traceability | Carrier API integration, rating, and reinsurance remain explicitly out of scope | None currently required |
