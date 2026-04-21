# ADR-009: Line of Business Classification and SLA Configuration

**Status:** Accepted
**Date:** 2026-03-14
**Owners:** Architect
**Related Features:** F0013

## Context

F0013 (Dashboard Framed Storytelling Canvas) introduces contextual mini-visualizations at each timeline stage node. Two visualizations require data not currently present in the system:

1. **Icon grid / waffle chart at Received stage** — shows insurance type distribution (property, casualty, marine, etc.). Requires a Line of Business (LOB) classification on opportunity entities. No LOB field exists on Submission, Renewal, Account, or Program.

2. **SLA gauge at Triaging stage** — shows on-time/approaching/overdue health. Requires defined SLA thresholds per workflow status. No SLA configuration exists; the aging endpoint returns fixed day-range buckets (0-2, 3-5, 6-10, 11-20, 21+) with no SLA context.

Additionally, F0013 introduces a per-stage breakdown endpoint (`groupBy=lineOfBusiness|broker|assignedUser|program|brokerState`) to power alternate mini-visualization views. This endpoint needs the LOB field to support the `lineOfBusiness` dimension.

## Decision

### 1. Add `LineOfBusiness` to Submission and Renewal entities

Add a nullable string field `LineOfBusiness` to both `Submission` and `Renewal` entities.

**Known values (commercial P&C NAIC/ISO standard):**

```
Property, GeneralLiability, CommercialAuto, WorkersCompensation,
ProfessionalLiability, Marine, Umbrella, Surety, Cyber, DirectorsOfficers
```

Design choices:
- **String, not DB enum** — allows adding new LOB values without schema migration. Validated at the API layer against the known set.
- **Nullable** — backward compatible with existing records. Frontend treats null as "Unknown" in visualizations.
- **On both entities** — Submission and Renewal share the same attribute shape. Avoids cross-entity joins for breakdown queries.
- **maxLength: 50** — sufficient for all known values with room for future additions.

### 2. Create `WorkflowSlaThreshold` configuration entity

New entity storing per-entity-type, per-status SLA thresholds.

```
WorkflowSlaThreshold
├── Id: uuid (PK)
├── EntityType: string ("submission" | "renewal")
├── Status: string (workflow status)
├── WarningDays: int (threshold for "approaching")
├── TargetDays: int (SLA breach threshold)
├── CreatedAt: datetime
└── UpdatedAt: datetime

Constraint: (EntityType, Status) UNIQUE
Invariant: WarningDays < TargetDays
```

SLA band computation:
- **On time:** item dwell days ≤ WarningDays
- **Approaching:** WarningDays < dwell ≤ TargetDays
- **Overdue:** dwell > TargetDays

Design choices:
- **Seed-only at MVP** — thresholds are inserted via EF Core migration seed data. No CRUD API. Values are operational defaults, not frequently changed.
- **Per-entity-type** — submission and renewal workflows may have different SLA expectations for the same status.
- **Server-side band computation** — the aging endpoint pre-computes onTime/approaching/overdue counts per status and returns them alongside the existing day-range buckets. Frontend consumes pre-computed bands rather than computing from raw data.

### 3. Extend aging endpoint with SLA bands

The existing `GET /dashboard/opportunities/aging` response is extended with an `sla` object per status:

```
+-----+-------- sla (new) --------+
|     | warningDays: int           |
|     | targetDays: int            |
|     | onTimeCount: int           |
|     | approachingCount: int      |
|     | overdueCount: int          |
+-----+----------------------------+
```

This co-locates SLA data with aging data, avoiding a separate endpoint. The existing `buckets` array is preserved for the aging heatmap visualization.

## Alternatives Considered

### LOB as enum migration
Rejected. Requires migration to add new LOB values. String with API validation provides the same safety with better extensibility.

### LOB on Submission only, derived via join for Renewal
Rejected. Renewals share the same attribute shape as submissions in this domain. Direct field avoids join complexity in breakdown queries and is consistent with the entity model.

### SLA thresholds as frontend constants
Rejected. SLA values are operational configuration that may vary by deployment. Server-side storage allows per-environment tuning without frontend redeployment. Server-side band computation keeps the frontend thin.

### Separate SLA endpoint
Rejected. SLA bands are consumed exclusively alongside aging data. Embedding in the aging response avoids an extra API call and keeps the data co-located.

## Consequences

### Positive
- Icon grid visualization at Received stage can show LOB distribution — the most meaningful view for intake.
- SLA gauge at any stage shows real health metrics based on configurable thresholds.
- Breakdown endpoint supports 5 groupBy dimensions including LOB and broker state.
- Aging endpoint becomes richer (SLA context) without breaking existing consumers (additive change).

### Negative
- EF Core migration required — two new columns (Submission.LineOfBusiness, Renewal.LineOfBusiness) and one new table (WorkflowSlaThreshold).
- Existing test submissions/renewals have null LOB — seed data must be updated for realistic dev testing.
- SLA threshold changes require a migration at MVP (no admin UI). Acceptable for initial deployment; admin UI can be added later.

### Neutral
- ABAC policies unchanged — breakdown endpoint reuses `dashboard_pipeline` authorization.
- No new infrastructure dependencies.
