# ADR-003: Task Entity and Nudge Engine Design

**Status:** Accepted

**Date:** 2026-02-14

**Deciders:** Architecture Team

**Technical Story:** Phase B — Dashboard F0001 (F0001-S0003: My Tasks, F0001-S0005: Nudge Cards)

---

## Context and Problem Statement

Two dashboard widgets — **My Tasks & Reminders (F0001-S0003)** and **Nudge Cards (F0001-S0005)** — depend on a Task entity that is not yet defined in the data model. Additionally, nudge cards aggregate time-sensitive items from three different sources (overdue tasks, stale submissions, upcoming renewals) into a prioritized list of up to 3 cards.

**Key questions:**
1. What is the minimal Task entity shape needed for Dashboard MVP?
2. Where does the nudge computation logic live — frontend or backend?
3. How is nudge priority ordering enforced?

---

## Decision Drivers

- **Dashboard MVP:** Tasks widget and nudge cards are both High priority stories in F0001
- **Feature F0003 Alignment:** Task entity must support the future Task Center feature without rework
- **ABAC:** Task visibility must respect ownership (AssignedTo = current user) plus general ABAC scope
- **Performance:** Nudge computation must complete within 500 ms (p95) as part of the dashboard budget
- **Auditability:** Task mutations (create, update, complete) must generate timeline events

---

## Decision: Task Entity Design

### Task Table

**Table Name:** `Tasks`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, NOT NULL | Unique identifier |
| Title | string(255) | NOT NULL | Task title |
| Description | string(2000) | NULL | Optional detail |
| Status | string(20) | NOT NULL, DEFAULT 'Open' | Open, InProgress, Done |
| Priority | string(20) | NOT NULL, DEFAULT 'Normal' | Low, Normal, High, Urgent |
| DueDate | Date | NULL | Optional due date |
| AssignedTo | string(255) | NOT NULL | Keycloak subject (user) |
| LinkedEntityType | string(50) | NULL | e.g., "Broker", "Submission", "Renewal" |
| LinkedEntityId | Guid | NULL | FK to linked entity (polymorphic) |
| CreatedAt | DateTime | NOT NULL | UTC timestamp |
| CreatedBy | string(255) | NOT NULL | Keycloak subject |
| UpdatedAt | DateTime | NOT NULL | UTC timestamp |
| UpdatedBy | string(255) | NOT NULL | Keycloak subject |
| CompletedAt | DateTime | NULL | When status changed to Done |
| IsDeleted | bool | NOT NULL, DEFAULT false | Soft delete flag |

### Indexes

- `IX_Tasks_AssignedTo_Status_DueDate` — Composite index for My Tasks widget query
- `IX_Tasks_DueDate_Status` — For nudge computation (overdue tasks)
- `IX_Tasks_LinkedEntityType_LinkedEntityId` — For entity-linked task lookups

### Relationships

- **Many-to-One (polymorphic):** Task → Broker | Submission | Renewal | Account (via LinkedEntityType + LinkedEntityId)
- No hard FK constraint on LinkedEntityId (polymorphic reference); application-level validation ensures entity exists

### Audit Requirements

All Task mutations generate ActivityTimelineEvent:
- `TaskCreated` — on creation
- `TaskUpdated` — on field changes
- `TaskCompleted` — when Status changes to Done
- `TaskReopened` — when Status changes from Done back to Open/InProgress
- `TaskDeleted` — on soft delete

---

## Decision: Nudge Engine Design

### Server-Side Computation

Nudge logic runs **server-side** in a single endpoint (`GET /dashboard/nudges`). The backend executes three scoped queries in parallel, merges results by priority, and returns the top 3.

**Why server-side (not frontend):**
- ABAC scope filtering must happen before data leaves the server
- Cross-module query (tasks + submissions + renewals) is simpler to parallelise on the backend
- Avoids sending potentially large candidate lists to the client just to pick 3

### Nudge Computation Algorithm

#### Constants

| Name | Value | Notes |
|------|-------|-------|
| MAX_NUDGES | 3 | Maximum cards returned |
| STALE_THRESHOLD_DAYS | 6 | Submissions with DaysInCurrentStatus >= 6 are stale (i.e. > 5 full days) |
| RENEWAL_WINDOW_DAYS | 14 | Inclusive: today <= PolicyExpirationDate <= today + 14 |
| CANDIDATE_LIMIT | 3 | Max candidates fetched per nudge type |

#### Step 1: Execute three queries in parallel (all ABAC-scoped)

**a. Overdue tasks:**
```sql
SELECT t.Id, t.Title, t.DueDate,
       (@today - t.DueDate) AS DaysOverdue,
       t.LinkedEntityType, t.LinkedEntityId,
       COALESCE(b.LegalName, a.Name, sub.Id::text, ren.Id::text) AS LinkedEntityName
FROM Tasks t
  LEFT JOIN Brokers b      ON t.LinkedEntityType = 'Broker'     AND t.LinkedEntityId = b.Id AND b.IsDeleted = false
  LEFT JOIN Accounts a     ON t.LinkedEntityType = 'Account'    AND t.LinkedEntityId = a.Id AND a.IsDeleted = false
  LEFT JOIN Submissions sub ON t.LinkedEntityType = 'Submission' AND t.LinkedEntityId = sub.Id AND sub.IsDeleted = false
  LEFT JOIN Renewals ren   ON t.LinkedEntityType = 'Renewal'    AND t.LinkedEntityId = ren.Id AND ren.IsDeleted = false
WHERE t.AssignedTo = @currentUser
  AND t.DueDate < @today              -- strictly less than (due today is NOT overdue)
  AND t.Status != 'Done'
  AND t.IsDeleted = false
  AND (t.LinkedEntityId IS NULL       -- unlinked tasks are eligible
       OR b.Id IS NOT NULL OR a.Id IS NOT NULL OR sub.Id IS NOT NULL OR ren.Id IS NOT NULL)
                                      -- linked entity must exist and not be soft-deleted
ORDER BY t.DueDate ASC,               -- most overdue first
         t.Id ASC                     -- deterministic tie-break
LIMIT 3
```

**b. Stale submissions:**
```sql
SELECT s.Id, s.CurrentStatus,
       COALESCE(a.Name, b.LegalName) AS EntityName,
       EXTRACT(DAY FROM (@today - wt_latest.OccurredAt))::int AS DaysInCurrentStatus
FROM Submissions s
  LEFT JOIN Accounts a ON s.AccountId = a.Id
  LEFT JOIN Brokers b ON s.BrokerId = b.Id
  LEFT JOIN LATERAL (
    SELECT OccurredAt FROM WorkflowTransition wt
    WHERE wt.EntityId = s.Id AND wt.WorkflowType = 'Submission'
    ORDER BY wt.OccurredAt DESC LIMIT 1
  ) wt_latest ON true
WHERE s.CurrentStatus NOT IN ('Bound', 'Declined', 'Withdrawn')
  AND s.IsDeleted = false
  AND EXTRACT(DAY FROM (@today - wt_latest.OccurredAt)) >= @STALE_THRESHOLD_DAYS
  -- + ABAC scope filter
ORDER BY DaysInCurrentStatus DESC,    -- most stale first
         s.Id ASC                     -- deterministic tie-break
LIMIT 3
```

**c. Upcoming renewals:**
```sql
SELECT r.Id, r.CurrentStatus, r.PolicyExpirationDate,
       COALESCE(a.Name, b.LegalName) AS EntityName,
       (r.PolicyExpirationDate - @today) AS DaysUntilRenewal
FROM Renewals r
  LEFT JOIN Accounts a ON r.AccountId = a.Id
  LEFT JOIN Brokers b ON r.BrokerId = b.Id
WHERE r.PolicyExpirationDate >= @today       -- inclusive lower bound (today counts)
  AND r.PolicyExpirationDate <= @today + 14  -- inclusive upper bound (14 days out counts)
  AND r.CurrentStatus NOT IN ('Completed', 'Lost')
  AND r.IsDeleted = false
  -- + ABAC scope filter
ORDER BY r.PolicyExpirationDate ASC,         -- soonest first
         r.Id ASC                            -- deterministic tie-break
LIMIT 3
```

#### Step 2: Priority merge (pure function, unit-testable)

```
function mergeNudges(overdueTasks[], staleSubmissions[], upcomingRenewals[]) → NudgeCard[]:
    result = []

    // Priority 1: Overdue tasks (highest urgency)
    for task in overdueTasks:
        if result.length >= MAX_NUDGES: break
        result.append(NudgeCard {
            nudgeType:        "OverdueTask",
            title:            task.Title,
            description:      "{task.DaysOverdue} day(s) overdue",
            linkedEntityType: task.LinkedEntityType ?? "Task",
            linkedEntityId:   task.LinkedEntityId ?? task.Id,
            linkedEntityName: task.LinkedEntityName ?? task.Title,
            urgencyValue:     task.DaysOverdue,
            ctaLabel:         "Review Now"
        })

    // Priority 2: Stale submissions
    for sub in staleSubmissions:
        if result.length >= MAX_NUDGES: break
        result.append(NudgeCard {
            nudgeType:        "StaleSubmission",
            title:            sub.EntityName,
            description:      "Stuck in {sub.CurrentStatus} for {sub.DaysInCurrentStatus} days",
            linkedEntityType: "Submission",
            linkedEntityId:   sub.Id,
            linkedEntityName: sub.EntityName,
            urgencyValue:     sub.DaysInCurrentStatus,
            ctaLabel:         "Take Action"
        })

    // Priority 3: Upcoming renewals
    for ren in upcomingRenewals:
        if result.length >= MAX_NUDGES: break
        result.append(NudgeCard {
            nudgeType:        "UpcomingRenewal",
            title:            ren.EntityName,
            description:      "{ren.DaysUntilRenewal} day(s) until renewal",
            linkedEntityType: "Renewal",
            linkedEntityId:   ren.Id,
            linkedEntityName: ren.EntityName,
            urgencyValue:     ren.DaysUntilRenewal,
            ctaLabel:         "Start Outreach"
        })

    return result   // length 0..3
```

#### Boundary Conditions

| Condition | Behavior |
|-----------|----------|
| Task DueDate = today | **Not overdue.** Overdue requires `DueDate < today` (strictly past). |
| Task DueDate is NULL | **Not eligible.** Tasks without a due date cannot be overdue. |
| DaysInCurrentStatus = 5 | **Not stale.** Threshold is >= 6 (i.e. more than 5 full days). |
| No WorkflowTransition exists for submission | **DaysInCurrentStatus = NULL.** Excluded from stale nudges (NULL fails the >= 6 check). |
| PolicyExpirationDate = today | **Eligible.** Lower bound is inclusive. |
| PolicyExpirationDate = today + 14 | **Eligible.** Upper bound is inclusive. |
| PolicyExpirationDate = today + 15 | **Not eligible.** Outside the 14-day window. |
| Linked entity is soft-deleted | **Task excluded.** Overdue task nudge is skipped; the task still appears in the My Tasks widget (F0001-S0003) with "[Deleted]" label. |
| All 3 slots filled by overdue tasks | **Stale/renewal nudges suppressed.** Priority ordering is strict — lower priority types only fill remaining slots. |
| Two tasks have same DueDate | **Tie-break by Task.Id ascending.** Deterministic — same user always sees the same card. |
| Zero candidates across all types | **Return empty array.** Frontend hides the "Needs Your Attention" section entirely. |

### Nudge Response Shape

```json
{
  "nudges": [
    {
      "nudgeType": "OverdueTask",
      "title": "Follow up with Acme Insurance",
      "description": "3 days overdue",
      "linkedEntityType": "Broker",
      "linkedEntityId": "uuid-here",
      "linkedEntityName": "Acme Insurance",
      "urgencyValue": 3,
      "ctaLabel": "Review Now"
    }
  ]
}
```

### DaysInCurrentStatus Computation

`DaysInCurrentStatus` is computed at query time (not stored) as:

```sql
EXTRACT(DAY FROM (CURRENT_DATE - wt_latest.OccurredAt))::int
```

where `wt_latest` is the most recent WorkflowTransition for the entity, resolved via `LEFT JOIN LATERAL ... ORDER BY OccurredAt DESC LIMIT 1`. Uses the existing index on `WorkflowTransition(EntityId, OccurredAt DESC)`.

**Edge case:** If no WorkflowTransition exists for an entity (e.g., newly created submission with no transitions yet), `DaysInCurrentStatus` is NULL. NULL values are excluded from stale submission nudges (NULL fails the `>= 6` comparison). For pipeline mini-cards (F0001-S0002), NULL renders as "0" days in status.

---

## Consequences

### Positive

- **Minimal Entity:** Task table is lean enough for MVP dashboard but extensible for F0003 (Task Center)
- **No New Module:** Task lives in the existing TimelineAudit module (or a new lightweight TaskManagement module within the monolith)
- **Reusable:** Nudge endpoint can be extended with new nudge types (e.g., "pending approval") without changing the frontend
- **Testable:** Nudge priority logic is a pure function; unit-testable without database

### Negative

- **Polymorphic FK:** LinkedEntityType + LinkedEntityId is a loose coupling pattern — no DB-level referential integrity on linked entities. Mitigated by application-level validation and soft-delete awareness.
- **DaysInCurrentStatus is Computed:** Not stored, so requires a JOIN to WorkflowTransition on every nudge/pipeline query. Mitigated by the composite index and LIMIT 3 cap.

### Neutral

- **Task creation is manual for MVP:** Tasks are created by users or by workflow side-effects (e.g., "create follow-up task when submission enters WaitingOnBroker"). Automated task creation is a Phase 1 concern.

---

## Module Placement

The Task entity belongs to a new **TaskManagement** module within the modular monolith:

```
Nebula.Domain/
  TaskManagement/
    Task.cs                    # Domain entity
    TaskStatus.cs              # Value object/constants aligned to ReferenceTaskStatus seed values
    TaskPriority.cs            # Enum: Low, Normal, High, Urgent (CHECK constraint)

Nebula.Application/
  TaskManagement/
    CreateTaskCommand.cs
    UpdateTaskCommand.cs
    GetMyTasksQuery.cs
  Dashboard/
    GetNudgesQuery.cs          # Cross-module: reads Tasks, Submissions, Renewals
```

---

## Related ADRs

- ADR-002: Dashboard Data Aggregation Strategy (endpoint structure)
- ADR-004: Frontend Dashboard Widget Architecture (client-side dismiss handling)

---

**Last Updated:** 2026-02-22
