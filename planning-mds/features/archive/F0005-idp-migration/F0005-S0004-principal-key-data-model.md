# F0005-S0004 — Data Model Principal Key Rename

**Feature:** F0005 — IdP Migration
**Story ID:** F0005-S0004
**Owner:** Backend Developer
**Depends on:** F0005-S0002 (NebulaPrincipal in place)
**Priority:** Must-complete before F0001/F0002 entity implementation

---

## Story

As a backend developer, I need all entity fields that previously stored Keycloak `sub` strings to store the internal `UserId (uuid)` instead, so that ownership and audit fields are stable across IdP changes.

---

## Acceptance Criteria

1. `UserProfile` table has `UserId (uuid, PK)` + `(IdpIssuer, IdpSubject)` unique pair. The old `Subject (string, PK)` column does not exist.
2. `BaseEntity` audit fields are `CreatedByUserId (uuid, NOT NULL)` and `UpdatedByUserId (uuid?)`. No `string` subject fields remain.
3. `BaseEntity` soft-delete field is `DeletedByUserId (uuid?)`. No `string` subject field.
4. All entity-specific subject fields are renamed to UUID references:
   - `Broker.ManagedBySubject` → `Broker.ManagedByUserId (uuid?)`
   - `Program.ManagedBySubject` → `Program.ManagedByUserId (uuid?)`
   - `Submission.AssignedTo (string)` → `Submission.AssignedToUserId (uuid)`
   - `Renewal.AssignedTo (string)` → `Renewal.AssignedToUserId (uuid)`
   - `Task.AssignedTo (string)` → `Task.AssignedToUserId (uuid)`
   - `ActivityTimelineEvent.ActorSubject (string)` → `ActivityTimelineEvent.ActorUserId (uuid)`
   - `WorkflowTransition.ActorSubject (string)` → `WorkflowTransition.ActorUserId (uuid)`
5. All indexes that reference old field names are recreated with new names (e.g., `IX_Tasks_AssignedTo_Status_DueDate` → `IX_Tasks_AssignedToUserId_Status_DueDate`).
6. EF Core configuration uses `HasForeignKey` from `*UserId` to `UserProfile.UserId` for all mutable entities. Append-only tables (`ActivityTimelineEvent`, `WorkflowTransition`) use a logical reference without a hard FK.
7. Casbin enforcement condition `r.obj.assignee == r.sub.id` correctly compares `AssignedToUserId.ToString()` against `NebulaPrincipal.UserId.ToString()` — verified by integration test.

---

## Field Rename Reference Table

| Entity | Old name | Old type | New name | New type | FK to UserProfile? |
|--------|----------|----------|----------|----------|-------------------|
| UserProfile | `Subject` (PK) | string | removed | — | — |
| UserProfile | — | — | `UserId` (PK) | uuid | self |
| UserProfile | — | — | `IdpIssuer` | varchar(255) | — |
| UserProfile | — | — | `IdpSubject` | varchar(255) | — |
| BaseEntity | `CreatedBy` | string | `CreatedByUserId` | uuid | Yes |
| BaseEntity | `UpdatedBy` | string? | `UpdatedByUserId` | uuid? | Yes |
| BaseEntity | `DeletedBy` | string? | `DeletedByUserId` | uuid? | Yes |
| Broker | `ManagedBySubject` | string? | `ManagedByUserId` | uuid? | Yes |
| Program | `ManagedBySubject` | string? | `ManagedByUserId` | uuid? | Yes |
| Submission | `AssignedTo` | string | `AssignedToUserId` | uuid | Yes |
| Renewal | `AssignedTo` | string | `AssignedToUserId` | uuid | Yes |
| Task | `AssignedTo` | string | `AssignedToUserId` | uuid | Yes |
| ActivityTimelineEvent | `ActorSubject` | string | `ActorUserId` | uuid | Logical only |
| WorkflowTransition | `ActorSubject` | string | `ActorUserId` | uuid | Logical only |

---

## EF Core Migration Notes

- This story generates EF Core **Migration 000** (the base schema). Since no prior migrations exist in production, this is the initial schema — not a rename migration.
- `UserProfile` is the anchor entity. All migrations must seed `UserProfile` rows for any test/dev data users before inserting entities that reference `CreatedByUserId`.
- `ActivityTimelineEvent` and `WorkflowTransition` omit EF `HasForeignKey` for `ActorUserId` to preserve append-only immutability.

---

## Dashboard Index Renames

| Old index | New index |
|-----------|-----------|
| `IX_Tasks_AssignedTo_Status_DueDate` | `IX_Tasks_AssignedToUserId_Status_DueDate` |
| `IX_Tasks_DueDate_Status` | unchanged |
| `IX_Submissions_AssignedTo_CurrentStatus` | `IX_Submissions_AssignedToUserId_CurrentStatus` |
| `IX_Renewals_AssignedTo_CurrentStatus` | `IX_Renewals_AssignedToUserId_CurrentStatus` |
| `IX_Brokers_ManagedBySubject` | `IX_Brokers_ManagedByUserId` |
| `IX_Programs_ManagedBySubject` | `IX_Programs_ManagedByUserId` |

---

## Audit / Timeline Requirements

None — this story defines the schema; it does not mutate application data. Audit requirements for the entities themselves remain unchanged (create/update/delete of Broker, Task, etc. all generate `ActivityTimelineEvent` records with `ActorUserId`).
