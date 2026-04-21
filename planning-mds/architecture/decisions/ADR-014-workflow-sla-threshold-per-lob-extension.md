# ADR-014: Extend WorkflowSlaThreshold with Per-LOB Dimension

**Status:** Accepted
**Date:** 2026-03-26
**Owners:** Architect
**Related Features:** F0007
**Amends:** ADR-009

## Context

ADR-009 introduced `WorkflowSlaThreshold` with a `(EntityType, Status)` unique constraint for per-status SLA thresholds. F0007 (Renewal Pipeline) requires per-LOB configurable renewal timing windows — different Lines of Business have different outreach target and warning thresholds (e.g., Workers' Compensation needs 120-day lead time vs. Cyber at 60 days).

Rather than creating a separate `RenewalWindowThreshold` entity, extending the existing `WorkflowSlaThreshold` with a LOB dimension is more consistent and avoids model fragmentation. The same entity can then serve per-LOB thresholds for any workflow entity type, not just renewals.

## Decision

Add a nullable `LineOfBusiness` column (varchar(50)) to `WorkflowSlaThreshold`. Change the unique constraint from `(EntityType, Status)` to `(EntityType, Status, COALESCE(LineOfBusiness, '__default__'))` to support one default entry (NULL LOB) and optional LOB-specific overrides.

### Schema

```
WorkflowSlaThreshold
├── Id: uuid (PK)
├── EntityType: string ("submission" | "renewal")
├── Status: string (workflow status)
├── LineOfBusiness: string (nullable; NULL = default for any LOB without a specific entry)
├── WarningDays: int
├── TargetDays: int
├── CreatedAt: datetime
└── UpdatedAt: datetime

Constraint: (EntityType, Status, COALESCE(LineOfBusiness, '__default__')) UNIQUE
Invariant: WarningDays < TargetDays (for submissions: dwell semantics; for renewals: both are days-before-expiry)
```

### Lookup Algorithm

```
1. Query WHERE EntityType = :entityType AND Status = :status AND LineOfBusiness = :lob
2. If no match, fallback: WHERE EntityType = :entityType AND Status = :status AND LineOfBusiness IS NULL
3. If still no match, use hardcoded application defaults
```

### Semantic Difference by EntityType

| EntityType | WarningDays means | TargetDays means |
|------------|-------------------|------------------|
| submission | Dwell time (days in status) before "approaching" | Dwell time (days in status) before "overdue" |
| renewal | Additional buffer days before overdue (approaching = TargetDays + WarningDays before expiry) | Outreach target (days before expiry to start outreach; overdue if past this and still Identified) |

The application layer interprets WarningDays/TargetDays based on EntityType. This semantic overload is documented and acceptable because the field names remain meaningful in both contexts (warning = when to warn, target = when it's a problem).

## Alternatives Considered

### Separate RenewalWindowThreshold entity
Rejected. Creates model fragmentation — two configuration tables for the same concept (threshold-based timing). Future entity types (e.g., servicing workflows) would face the same choice. A single extensible entity is more maintainable.

### LOB as a separate join table (many-to-many)
Rejected. Over-engineered for a simple nullable dimension. A threshold row is always specific to one LOB or is the default. No many-to-many relationship exists.

## Consequences

### Positive
- Single entity for all workflow timing thresholds, extensible to any entity type and LOB combination.
- Existing ADR-009 submission thresholds are unaffected (their LineOfBusiness remains NULL).
- Per-LOB renewal windows are seed-data configurable without code changes.

### Negative
- The unique constraint requires a COALESCE expression index for PostgreSQL NULL handling.
- Application layer must understand the EntityType-dependent semantics of WarningDays/TargetDays.
- Migration rebuilds the existing unique index.

### Neutral
- No CRUD API for thresholds in MVP (seed-only, consistent with ADR-009).
- ABAC policies unchanged — thresholds are read-only configuration data.
