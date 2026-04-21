# Authorization Matrix (Requirements)

Owner: Product Manager
Status: Final (MVP) + Phase 1 delta defined
Last Updated: 2026-03-04

Sources used: BLUEPRINT.md §1.2, §3.1, §3.2, §4.3, §4.4; F0001-S0001, F0001-S0002, F0001-S0003, F0001-S0004, F0001-S0005; F0002-S0001 through F0002-S0007.
No requirements invented. Gaps are marked "Not yet specified" with a reference to the blocking story.

---

## 1. Roles

| Role | Description | Source |
|------|-------------|--------|
| DistributionUser | Internal Distribution & Marketing user. Works assigned opportunities only. | BLUEPRINT §1.2, §3.2; user requirement for assigned opportunities |
| DistributionManager | Internal Distribution manager. Can see and act on all opportunities within their region. | User requirement for manager access |
| Underwriter | Internal underwriter. Reviews triaged submissions and provides quote/bind decisions. Read-only access to broker and account context. | BLUEPRINT §1.2, §3.2, §4.4; F0001-S0002, F0001-S0003 |
| RelationshipManager | Internal broker relationship manager. Maintains broker/account relationships and timeline context. | BLUEPRINT §1.2, §3.2; F0002-S0002, F0001-S0004 |
| ProgramManager | Internal MGA/program manager. Oversees MGA program-level relationships. | BLUEPRINT §1.2, §3.2; F0001-S0001, F0001-S0004 |
| Admin | Internal administrator. Broad management access including policy administration. | BLUEPRINT §4.4 |
| BrokerUser | External broker user for scoped Phase 1 login. Access is constrained to broker-visible resources only. | F0009 PRD; F0009-S0004 |
| ExternalUser | External broker/MGA user. No access to any MVP resource. Self-service portal deferred to future. | BLUEPRINT §3.1 non-goals |

---

## 2. Authorization Matrix

### 2.1 Broker

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | create | **ALLOW** | Must hold broker:create permission. License number must be globally unique. | F0002-S0001 AC1, AC3; BLUEPRINT §4.4 |
| DistributionUser | read / search | **ALLOW** | Full name search; license search is exact match only. InternalOnly metadata (inactive flags) visible. Results scoped to authorized entities. | F0002-S0002 AC1–AC4, Role Visibility; BLUEPRINT §4.4 |
| DistributionUser | update | **ALLOW** | Internal distribution role may update broker profile. | BLUEPRINT §4.4 |
| DistributionUser | delete | **ALLOW** | Scoped to authorized entities. Delete blocked if active submissions or renewals exist. | F0002-S0005 ACs |
| DistributionManager | create | **ALLOW** | Same as DistributionUser; manager can act across all opportunities. | F0002-S0001; user requirement |
| DistributionManager | read / search | **ALLOW** | Scoped to region; no team/user restrictions within region. License search is exact match only. | F0002-S0002 Role Visibility; user requirement |
| DistributionManager | update | **ALLOW** | Scoped to region; no team/user restrictions within region. | BLUEPRINT §4.4; user requirement |
| DistributionManager | delete | **ALLOW** | Scoped to region; no team/user restrictions within region. Delete blocked if active submissions or renewals exist. | F0002-S0005 ACs; user requirement |
| Underwriter | create | **DENY** | Read-only access to broker context. | BLUEPRINT §4.4 |
| Underwriter | read | **ALLOW** | Read access to broker context for submission review. No write access. | BLUEPRINT §4.4 |
| Underwriter | update | **DENY** | Read-only access to broker context. | BLUEPRINT §4.4 |
| Underwriter | delete | **DENY** | Read-only access. | BLUEPRINT §4.4 |
| RelationshipManager | create | **ALLOW** | Internal relationship role may create brokers. | BLUEPRINT §4.4 |
| RelationshipManager | read / search | **ALLOW** | Full broker search. License search is exact match only. Results scoped to authorized entities. | F0002-S0002 Role Visibility; BLUEPRINT §4.4 |
| RelationshipManager | update | **ALLOW** | Internal relationship role may update broker profile. | BLUEPRINT §4.4 |
| RelationshipManager | delete | **DENY** | Delete reserved for Distribution roles and Admin in MVP. | F0002-S0005 ACs |
| ProgramManager | create | **DENY** | Program managers are read-only for broker records in MVP. | BLUEPRINT §4.4 |
| ProgramManager | read | **ALLOW** | Implied by broker activity feed scoped to their programs. License search is exact match only. | F0001-S0004 Role Visibility |
| ProgramManager | update | **DENY** | Program managers are read-only for broker records in MVP. | BLUEPRINT §4.4 |
| ProgramManager | delete | **DENY** | Program managers are read-only for broker records in MVP. | BLUEPRINT §4.4 |
| Admin | create | **ALLOW** | Full unscoped access. | F0002-S0001 Role Visibility; BLUEPRINT §4.4 |
| Admin | read / search | **ALLOW** | Full unscoped access. | F0002-S0002 Role Visibility; BLUEPRINT §4.4 |
| Admin | update | **ALLOW** | Full unscoped access. | BLUEPRINT §4.4 |
| Admin | delete | **ALLOW** | Full unscoped access. | BLUEPRINT §4.4 |
| ExternalUser | all | **DENY** | No external broker portal in MVP. | BLUEPRINT §3.1 non-goals |

**Constraints applying to all ALLOW decisions on Broker:**
- Duplicate license number on create must return a deterministic conflict error; the record must not be created. (F0002-S0001 edge case)
- All read results must be limited to entities the user is authorized to access; no cross-scope reads. (F0002-S0002 AC4)
- Broker records are InternalOnly in MVP; no content is visible to ExternalUser. (F0002-S0001, F0002-S0002 Data Visibility)
- Broker delete is blocked if active submissions or renewals exist. (F0002-S0005 ACs)

---

### 2.2 Contact

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | create | **ALLOW** | Internal distribution role may create contacts. | BLUEPRINT §4.4 |
| DistributionUser | read | **ALLOW** | Full contact read scoped to authorized entities. | BLUEPRINT §4.4 |
| DistributionUser | update | **ALLOW** | Internal distribution role may update contacts. | BLUEPRINT §4.4 |
| DistributionUser | delete | **DENY** | Delete reserved for DistributionManager and Admin in MVP. | F0002-S0006 ACs |
| DistributionManager | create | **ALLOW** | Same as DistributionUser; manager can act across all opportunities. | BLUEPRINT §4.4; user requirement |
| DistributionManager | read | **ALLOW** | Scoped to region; no team/user restrictions within region. | BLUEPRINT §4.4; user requirement |
| DistributionManager | update | **ALLOW** | Scoped to region; no team/user restrictions within region. | BLUEPRINT §4.4; user requirement |
| DistributionManager | delete | **ALLOW** | Scoped to region; no team/user restrictions within region. | F0002-S0006 ACs; user requirement |
| Underwriter | create | **DENY** | Read-only access to contact context. | BLUEPRINT §4.4 |
| Underwriter | read | **ALLOW** | Read access to contact context. No write. | BLUEPRINT §4.4 |
| Underwriter | update | **DENY** | Read-only access. | BLUEPRINT §4.4 |
| Underwriter | delete | **DENY** | Read-only access. | BLUEPRINT §4.4 |
| RelationshipManager | create | **ALLOW** | Internal relationship role may create contacts. | BLUEPRINT §4.4 |
| RelationshipManager | read | **ALLOW** | Full contact read scoped to authorized entities. | BLUEPRINT §4.4 |
| RelationshipManager | update | **ALLOW** | Internal relationship role may update contacts. | BLUEPRINT §4.4 |
| RelationshipManager | delete | **DENY** | Delete reserved for DistributionManager and Admin in MVP. | F0002-S0006 ACs |
| ProgramManager | create | **DENY** | Contact management is not within ProgramManager scope in MVP. | BLUEPRINT §4.4 |
| ProgramManager | read | **ALLOW** | Read-only for program context; no mutations. | BLUEPRINT §4.4 |
| ProgramManager | update | **DENY** | Contact management is not within ProgramManager scope in MVP. | BLUEPRINT §4.4 |
| ProgramManager | delete | **DENY** | Contact management is not within ProgramManager scope in MVP. | BLUEPRINT §4.4 |
| Admin | create | **ALLOW** | Full unscoped access. | BLUEPRINT §4.4 |
| Admin | read | **ALLOW** | Full unscoped access. | BLUEPRINT §4.4 |
| Admin | update | **ALLOW** | Full unscoped access. | BLUEPRINT §4.4 |
| Admin | delete | **ALLOW** | Full unscoped access. | BLUEPRINT §4.4 |
| ExternalUser | all | **DENY** | No external contact access in MVP. | BLUEPRINT §3.1 non-goals |

**Constraints applying to all ALLOW decisions on Contact:**
- Contact data is InternalOnly in MVP; no content visible to ExternalUser. (F0002-S0001, F0002-S0002 Data Visibility)

---

### 2.3 Dashboard — KPI Cards

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | read | **ALLOW** | Counts scoped to the user's assigned opportunities only. | F0001-S0001 Role Visibility; user requirement |
| DistributionManager | read | **ALLOW** | Scoped to region; no team/user restrictions within region. | F0001-S0001 Role Visibility; user requirement |
| Underwriter | read | **ALLOW** | Counts scoped to submissions assigned to or accessible by the user. | F0001-S0001 Role Visibility |
| RelationshipManager | read | **ALLOW** | Counts scoped to the user's managed broker relationships. | F0001-S0001 Role Visibility |
| ProgramManager | read | **ALLOW** | Counts scoped to the user's programs. | F0001-S0001 Role Visibility |
| Admin | read | **ALLOW** | Unscoped; sees all counts across all entities. | F0001-S0001 Role Visibility |
| ExternalUser | read | **DENY** | KPI data is InternalOnly. | F0001-S0001 Data Visibility |

**Constraints applying to all ALLOW decisions on KPI Cards:**
- Active Brokers count: includes only brokers within the user's authorized scope.
- Open Submissions and Renewal Rate: computed only from entities the user is authorized to access.
- Each card must show "—" (not an error) if underlying data is missing or the query fails; the failure must not block other widgets. (F0001-S0001 AC: edge cases, reliability)
- Read-only. No mutations are permitted from this view. (F0001-S0001 AC Checklist)

---

### 2.4 Dashboard — Pipeline Summary (Status Counts and Mini-Cards)

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | read | **ALLOW** | Submissions and renewals scoped to user's assigned opportunities only. | F0001-S0002 Role Visibility; user requirement |
| DistributionManager | read | **ALLOW** | Scoped to region; no team/user restrictions within region. | F0001-S0002 Role Visibility; user requirement |
| Underwriter | read | **ALLOW** | Submissions assigned to or accessible by the user. | F0001-S0002 Role Visibility |
| RelationshipManager | read | **ALLOW** | Submissions and renewals linked to managed broker relationships. | F0001-S0002 Role Visibility |
| ProgramManager | read | **ALLOW** | Submissions and renewals within the user's programs. | F0001-S0002 Role Visibility |
| Admin | read | **ALLOW** | Unscoped; sees all statuses and mini-cards. | F0001-S0002 Role Visibility |
| ExternalUser | read | **DENY** | Pipeline data is InternalOnly. | F0001-S0002 Data Visibility |

**Constraints applying to all ALLOW decisions on Pipeline Summary:**
- Only non-terminal statuses are shown. Terminal statuses (Bound, Declined, Withdrawn, Completed, Lost) must be excluded. (F0001-S0002 Validation Rules)
- Zero-count status pills must remain visible; they may not be hidden. (F0001-S0002 edge cases)
- Mini-card expansion: up to 5 items per status; sorted by days-in-status descending (longest-stuck first). Same scope as counts. (F0001-S0002 edge cases)
- "View all" navigation must carry the same authorization scope to the destination list screen. (F0001-S0002 AC)
- Read-only. No mutations permitted from this view. (F0001-S0002 AC Checklist)

---

### 2.5 Dashboard — Nudge Cards

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | read | **ALLOW** | Own overdue tasks + submissions and renewals within assigned opportunities only. | F0001-S0005 Role Visibility; user requirement |
| DistributionManager | read | **ALLOW** | Own overdue tasks + submissions and renewals within region. | F0001-S0005 Role Visibility; user requirement |
| Underwriter | read | **ALLOW** | Own overdue tasks + submissions and renewals within ABAC scope. | F0001-S0005 Role Visibility |
| RelationshipManager | read | **ALLOW** | Own overdue tasks + submissions and renewals within ABAC scope. | F0001-S0005 Role Visibility |
| ProgramManager | read | **ALLOW** | Own overdue tasks + submissions and renewals within ABAC scope. | F0001-S0005 Role Visibility |
| Admin | read | **ALLOW** | Own overdue tasks + submissions and renewals within ABAC scope. | F0001-S0005 Role Visibility |
| ExternalUser | read | **DENY** | Nudge data is InternalOnly. | F0001-S0005 Data Visibility |

**Constraints applying to all ALLOW decisions on Nudge Cards:**
- Overdue task nudges: only tasks assigned to the authenticated user. Linked entity must not be soft-deleted. (F0001-S0005 Nudge Selection Rules, edge cases)
- Stale submission nudges: only submissions the user is authorized to access. Submission must not be soft-deleted. (F0001-S0005 Nudge Selection Rules)
- Upcoming renewal nudges: only renewals the user is authorized to access. (F0001-S0005 Nudge Selection Rules)
- Priority order is fixed: overdue tasks > stale submissions > upcoming renewals. Maximum 3 cards shown. (F0001-S0005 AC Checklist)
- Dismiss is session-scoped only (no persisted state in MVP). Dismiss does not constitute a mutation requiring audit. (F0001-S0005 AC Checklist, out of scope)
- If the nudge query fails, the "Needs Your Attention" section must be omitted entirely; the failure must not block other widgets. (F0001-S0005 Non-Functional)
- Read-only. No persisted mutations permitted from this view in MVP. (F0001-S0005 AC Checklist)

---

### 2.6 Task — Manage Own Tasks

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | create | **ALLOW** | Self-assigned tasks only. `AssignedToUserId` must match authenticated user's UserId. | F0003-S0001 ACs |
| DistributionUser | read | **ALLOW** | Own tasks only (task assigned to the authenticated user). Dashboard list excludes Done. | F0001-S0003 AC Checklist, Role Visibility |
| DistributionUser | update | **ALLOW** | Own tasks only. `AssignedToUserId` must match authenticated user's UserId. | F0003-S0002 ACs |
| DistributionUser | delete | **ALLOW** | Own tasks only. Soft delete only. | F0003-S0003 ACs |
| DistributionManager | create | **ALLOW** | Self-assigned tasks only. `AssignedToUserId` must match authenticated user's UserId. | F0003-S0001 ACs |
| DistributionManager | read | **ALLOW** | Own tasks only for the dashboard widget. Viewing other users' tasks is Future (not MVP). | F0001-S0003 Role Visibility |
| DistributionManager | update | **ALLOW** | Own tasks only. `AssignedToUserId` must match authenticated user's UserId. | F0003-S0002 ACs |
| DistributionManager | delete | **ALLOW** | Own tasks only. Soft delete only. | F0003-S0003 ACs |
| Underwriter | create | **ALLOW** | Self-assigned tasks only. `AssignedToUserId` must match authenticated user's UserId. | F0003-S0001 ACs |
| Underwriter | read | **ALLOW** | Own tasks only. Dashboard list excludes Done. | F0001-S0003 Role Visibility |
| Underwriter | update | **ALLOW** | Own tasks only. `AssignedToUserId` must match authenticated user's UserId. | F0003-S0002 ACs |
| Underwriter | delete | **ALLOW** | Own tasks only. Soft delete only. | F0003-S0003 ACs |
| RelationshipManager | create | **ALLOW** | Self-assigned tasks only. `AssignedToUserId` must match authenticated user's UserId. | F0003-S0001 ACs |
| RelationshipManager | read | **ALLOW** | Own tasks only. Dashboard list excludes Done. | F0001-S0003 Role Visibility |
| RelationshipManager | update | **ALLOW** | Own tasks only. `AssignedToUserId` must match authenticated user's UserId. | F0003-S0002 ACs |
| RelationshipManager | delete | **ALLOW** | Own tasks only. Soft delete only. | F0003-S0003 ACs |
| ProgramManager | create | **ALLOW** | Self-assigned tasks only. `AssignedToUserId` must match authenticated user's UserId. | F0003-S0001 ACs |
| ProgramManager | read | **ALLOW** | Own tasks only. Dashboard list excludes Done. | F0001-S0003 Role Visibility |
| ProgramManager | update | **ALLOW** | Own tasks only. `AssignedToUserId` must match authenticated user's UserId. | F0003-S0002 ACs |
| ProgramManager | delete | **ALLOW** | Own tasks only. Soft delete only. | F0003-S0003 ACs |
| Admin | create | **ALLOW** | Self-assigned tasks only in MVP. `AssignedToUserId` must match authenticated user's UserId. | F0003-S0001 ACs |
| Admin | read | **ALLOW** | Own tasks only for the dashboard widget. Viewing other users' tasks is explicitly Future (not MVP). | F0001-S0003 Role Visibility |
| Admin | update | **ALLOW** | Own tasks only. `AssignedToUserId` must match authenticated user's UserId. | F0003-S0002 ACs |
| Admin | delete | **ALLOW** | Own tasks only. Soft delete only. | F0003-S0003 ACs |
| ExternalUser | all | **DENY** | Task data is InternalOnly. | F0001-S0003 Data Visibility |

**Constraints applying to all ALLOW decisions on Task (F0003 — self-assigned):**
- A user may only create/update/delete tasks where `AssignedToUserId` equals their authenticated user's UserId. No cross-user assignment in MVP. (F0003-S0001, F0003-S0002, F0003-S0003)
- A user may only read tasks where they are the assigned user. No cross-user task visibility in MVP. (F0001-S0003 AC Checklist, Non-Functional)
- Dashboard list excludes Done tasks; `GET /tasks/{taskId}` may return any status for own tasks. (F0001-S0003 Validation Rules)
- If a linked entity on a task has been soft-deleted, the task is still displayed but the entity name must show as "[Deleted]". (F0001-S0003 edge cases)
- Read-only in dashboard context. No create, update, or delete from the dashboard widget in MVP. (F0001-S0003 out of scope)

---

### 2.6a Task — Manager Assignment (F0004 Delta)

F0004 extends the self-assigned-only task model with creator-based access for DistributionManager and Admin.

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionManager | create (assign to other) | **ALLOW** | Can assign tasks to any active internal user. Target user must exist and be active. | F0004-S0003 |
| DistributionManager | read (created tasks) | **ALLOW** | Tasks where `CreatedByUserId` = authenticated user. | F0004-S0003 |
| DistributionManager | update (created tasks) | **ALLOW** | Tasks where `CreatedByUserId` = authenticated user. Can edit fields and reassign. Cannot change status (assignee-only). | F0004-S0003 |
| DistributionManager | delete (created tasks) | **ALLOW** | Tasks where `CreatedByUserId` = authenticated user. Soft delete only. | F0004-S0003 |
| Admin | create (assign to other) | **ALLOW** | Same as DistributionManager. | F0004-S0003 |
| Admin | read (created tasks) | **ALLOW** | Tasks where `CreatedByUserId` = authenticated user. | F0004-S0003 |
| Admin | update (created tasks) | **ALLOW** | Same as DistributionManager. | F0004-S0003 |
| Admin | delete (created tasks) | **ALLOW** | Same as DistributionManager. | F0004-S0003 |
| DistributionUser | create (assign to other) | **DENY** | Self-assigned only. | F0004-S0003 |
| Underwriter | create (assign to other) | **DENY** | Self-assigned only. | F0004-S0003 |
| RelationshipManager | create (assign to other) | **DENY** | Self-assigned only. | F0004-S0003 |
| ProgramManager | create (assign to other) | **DENY** | Self-assigned only. | F0004-S0003 |

**Constraints applying to F0004 ALLOW decisions:**
- Self-assigned task rules (§2.6) remain active. A request is allowed if it satisfies EITHER self-assigned rules OR creator-based rules (OR semantics in Casbin).
- Creator-based access applies only when `CreatedByUserId = authenticated user`. Managers cannot access tasks created by other managers.
- Reassignment: only the creator (DistributionManager/Admin) can change `AssignedToUserId`. Assignees cannot reassign.
- Status change: only the current assignee (`AssignedToUserId = authenticated user`) can change status. Creator attempting status change on a task assigned to someone else returns 403 (`status_change_restricted`).
- Assignee validation: target user must exist in UserProfile and have `IsActive = true`. Inactive assignee returns 422 (`inactive_assignee`). Non-existent user returns 422 (`invalid_assignee`).
- Reassignment emits `TaskReassigned` timeline event with previous/new assignee details.
- `GET /my/tasks` (dashboard widget) is unchanged and returns only self-assigned tasks (assignee-based).
- `GET /tasks?view=assignedByMe` returns only creator-based tasks where assignee ≠ creator (requires DistributionManager/Admin role).

---

### 2.6b User — Search (F0004)

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | search | **ALLOW** | Search UserProfile by DisplayName/Email. | F0004-S0002 |
| DistributionManager | search | **ALLOW** | Same. | F0004-S0002 |
| Underwriter | search | **ALLOW** | Same. | F0004-S0002 |
| RelationshipManager | search | **ALLOW** | Same. | F0004-S0002 |
| ProgramManager | search | **ALLOW** | Same. | F0004-S0002 |
| Admin | search | **ALLOW** | Same. | F0004-S0002 |
| ExternalUser | search | **DENY** | No external access. | F0004-S0002 |
| BrokerUser | search | **DENY** | No external access. | F0004-S0002 |

**Constraints:**
- Does not expose IdpIssuer, IdpSubject, or other sensitive UserProfile fields.
- Default returns only active users (`IsActive = true`). `activeOnly=false` includes inactive for display purposes.
- Minimum 2-character query required.

---

### 2.7 Activity Timeline Event — Broker Events

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | read | **ALLOW** | Events for brokers within the user's authorized scope (assigned opportunities only). | F0001-S0004 Role Visibility; user requirement |
| DistributionManager | read | **ALLOW** | Broker events within region; no team/user restrictions within region. | F0001-S0004 Role Visibility; user requirement |
| Underwriter | read | **ALLOW** | Events for brokers linked to submissions accessible by the user. | F0001-S0004 Role Visibility |
| RelationshipManager | read | **ALLOW** | Events for brokers the user manages. | F0001-S0004 Role Visibility |
| ProgramManager | read | **ALLOW** | Events for brokers within the user's programs. | F0001-S0004 Role Visibility |
| Admin | read | **ALLOW** | Unscoped; sees all broker timeline events. | F0001-S0004 Role Visibility |
| ExternalUser | read | **DENY** | Timeline events are InternalOnly. | F0001-S0004 Data Visibility |

**Constraints applying to all ALLOW decisions on Activity Timeline Event:**
- Only events where EntityType = "Broker" are included in the dashboard feed view. (F0001-S0004 Validation Rules)
- Maximum 20 most recent events per load; sorted by occurrence time descending. (F0001-S0004 Validation Rules)
- If the actor account has been deactivated, the actor display name must show as "Unknown User" (not an error). (F0001-S0004 edge cases)
- Timeline event records are append-only and must never be modified or deleted by any role. (BLUEPRINT §1.4 non-negotiables)
- Read-only view. No mutations permitted from the dashboard feed. (F0001-S0004 AC Checklist)

---

### 2.8 Submission — Read / Create / Update / Transition / Assign

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | read | **ALLOW** | Submissions assigned to the user only. Applies to `GET /submissions/{submissionId}`. | F0001-S0002; user requirement |
| DistributionUser | create | **ALLOW** | Can create new submissions. Initial owner is the authenticated user. Applies to `POST /submissions`. | F0006-S0002 |
| DistributionUser | update | **ALLOW** | Only for submissions currently assigned to the user. Applies to `PUT /submissions/{submissionId}`. | F0006-S0003 |
| DistributionUser | transition | **ALLOW** | Only for assigned submissions and only for valid transitions. Applies to `POST /submissions/{submissionId}/transitions`. | BLUEPRINT §4.3; user requirement |
| DistributionUser | assign | **ALLOW** | Only for submissions currently assigned to the user. Applies to `PUT /submissions/{submissionId}/assignment`. | F0006-S0006 |
| DistributionManager | read | **ALLOW** | All submissions within region. Applies to `GET /submissions/{submissionId}`. | F0001-S0002; user requirement |
| DistributionManager | create | **ALLOW** | Can create submissions within manager scope. Applies to `POST /submissions`. | F0006-S0002 |
| DistributionManager | update | **ALLOW** | Region-scoped mutable edit access. Applies to `PUT /submissions/{submissionId}`. | F0006-S0003 |
| DistributionManager | transition | **ALLOW** | Submissions within region; valid transitions only. Applies to `POST /submissions/{submissionId}/transitions`. | BLUEPRINT §4.3; user requirement |
| DistributionManager | assign | **ALLOW** | Can assign or reassign any submission in region scope. Applies to `PUT /submissions/{submissionId}/assignment`. | F0006-S0006 |
| Underwriter | read | **ALLOW** | Submissions assigned to the underwriter in F0006 intake scope. Applies to `GET /submissions/{submissionId}`. | F0006 PRD Role-Based Access |
| Underwriter | create | **DENY** | Intake creation stays with distribution/admin roles. Applies to `POST /submissions`. | F0006-S0002 Role-Based Access |
| Underwriter | update | **DENY** | Underwriter is read-only in F0006 intake scope. Applies to `PUT /submissions/{submissionId}`. | F0006-S0003 Role-Based Visibility |
| Underwriter | transition | **ALLOW** | Underwriters can transition within underwriting stages. Applies to `POST /submissions/{submissionId}/transitions`. | BLUEPRINT §4.4; §4.3 |
| Underwriter | assign | **DENY** | Ownership reassignment remains with distribution manager/admin in MVP. Applies to `PUT /submissions/{submissionId}/assignment`. | F0006-S0006 |
| RelationshipManager | read | **ALLOW** | Submissions linked to managed broker relationships. Applies to `GET /submissions/{submissionId}`. | F0001-S0002 Role Visibility |
| RelationshipManager | create | **DENY** | RelationshipManager is read-only for submission intake. Applies to `POST /submissions`. | F0006 PRD Role-Based Access |
| RelationshipManager | update | **DENY** | Read-only access; no intake edits in MVP. Applies to `PUT /submissions/{submissionId}`. | F0006-S0003 |
| RelationshipManager | transition | **DENY** | Read-only access; no submission transitions in MVP. Applies to `POST /submissions/{submissionId}/transitions`. | BLUEPRINT §4.4 |
| RelationshipManager | assign | **DENY** | Read-only access; no ownership changes in MVP. Applies to `PUT /submissions/{submissionId}/assignment`. | F0006-S0006 |
| ProgramManager | read | **ALLOW** | Submissions within the user's programs. Applies to `GET /submissions/{submissionId}`. | F0001-S0002 Role Visibility |
| ProgramManager | create | **DENY** | ProgramManager is read-only for submission intake. Applies to `POST /submissions`. | F0006 PRD Role-Based Access |
| ProgramManager | update | **DENY** | Read-only access; no intake edits in MVP. Applies to `PUT /submissions/{submissionId}`. | F0006-S0003 |
| ProgramManager | transition | **DENY** | Read-only access; no submission transitions in MVP. Applies to `POST /submissions/{submissionId}/transitions`. | BLUEPRINT §4.4 |
| ProgramManager | assign | **DENY** | Read-only access; no ownership changes in MVP. Applies to `PUT /submissions/{submissionId}/assignment`. | F0006-S0006 |
| Admin | read | **ALLOW** | Unscoped access. Applies to `GET /submissions/{submissionId}`. | BLUEPRINT §4.4 |
| Admin | create | **ALLOW** | Unscoped create access. Applies to `POST /submissions`. | F0006-S0002 |
| Admin | update | **ALLOW** | Unscoped mutable edit access. Applies to `PUT /submissions/{submissionId}`. | F0006-S0003 |
| Admin | transition | **ALLOW** | Unscoped; valid transitions only. Applies to `POST /submissions/{submissionId}/transitions`. | BLUEPRINT §4.4; §4.3 |
| Admin | assign | **ALLOW** | Unscoped assignment and reassignment access. Applies to `PUT /submissions/{submissionId}/assignment`. | F0006-S0006 |
| ExternalUser | all | **DENY** | No external portal in MVP. | BLUEPRINT §3.1 non-goals |

**Constraints applying to all ALLOW decisions on Submission:**
- Invalid transition pairs return HTTP 409 with ProblemDetails code invalid_transition. (BLUEPRINT §4.3)
- Missing transition prerequisites return HTTP 409 with ProblemDetails code missing_transition_prerequisite. (BLUEPRINT §4.3)
- State-changing mutations (`PUT /submissions/{submissionId}`, `PUT /submissions/{submissionId}/assignment`, `POST /submissions/{submissionId}/transitions`) require `If-Match` and return HTTP 412 `precondition_failed` on stale rowVersion values. (API Guidelines + F0006 architecture)
- Every successful transition appends a WorkflowTransition and ActivityTimelineEvent record. (BLUEPRINT §4.3)

---

### 2.9 Renewal — Read / Create / Transition / Assign (F0007)

Applies to the F0007 Renewal Pipeline endpoints: `GET /renewals`, `POST /renewals`, `GET /renewals/{renewalId}`, `POST /renewals/{renewalId}/transitions`, `PUT /renewals/{renewalId}/assignment`, `GET /renewals/{renewalId}/timeline`.

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | read | **ALLOW** | Renewals assigned to the user only. Applies to list, detail, and timeline endpoints. | F0001-S0002; F0007-S0001/S0002/S0007; user requirement |
| DistributionUser | create | **ALLOW** | Distribution users may create a renewal from an expiring policy within their assigned scope. Applies to `POST /renewals`. | F0007-S0006 |
| DistributionUser | transition | **ALLOW** | Only for assigned renewals and only for transitions in the Distribution-owned states (Identified ↔ Outreach ↔ InReview). | F0007-S0003; BLUEPRINT §4.3 |
| DistributionUser | assign | **DENY** | Self-assignment only — no cross-user assignment. Use create-time `assignedToUserId`. | F0007-S0004 |
| DistributionManager | read | **ALLOW** | All renewals within region. Applies to list, detail, and timeline endpoints. | F0001-S0002; F0007-S0001/S0002/S0007; user requirement |
| DistributionManager | create | **ALLOW** | Region-scoped; may seed renewals on behalf of distribution users. | F0007-S0006 |
| DistributionManager | transition | **ALLOW** | Renewals within region; valid transitions only in Distribution-owned states. | F0007-S0003; BLUEPRINT §4.3 |
| DistributionManager | assign | **ALLOW** | Reassignment of renewal ownership within region. Applies to `PUT /renewals/{renewalId}/assignment`. Cannot assign terminal-state renewals. | F0007-S0004 |
| Underwriter | read | **ALLOW** | Renewals assigned to or accessible by the user (post-handoff). | F0007-S0002/S0007; BLUEPRINT §4.4 |
| Underwriter | create | **DENY** | Underwriters do not seed renewals; intake is distribution-owned. | F0007-S0006 |
| Underwriter | transition | **ALLOW** | Underwriter-owned states (InReview → Quoted → Completed/Lost). Cannot perform Distribution-owned transitions. | F0007-S0003; BLUEPRINT §4.4, §4.3 |
| Underwriter | assign | **DENY** | Underwriters do not reassign ownership in MVP. | F0007-S0004 |
| RelationshipManager | read | **ALLOW** | Renewals linked to managed broker relationships. List, detail, and timeline. | F0001-S0002 Role Visibility; F0007-S0007 |
| RelationshipManager | create | **DENY** | Out of scope — relationship managers do not seed pipeline records in MVP. | F0007 PRD scope |
| RelationshipManager | transition | **DENY** | Read-only in MVP. | BLUEPRINT §4.4 |
| RelationshipManager | assign | **DENY** | Read-only in MVP. | BLUEPRINT §4.4 |
| ProgramManager | read | **ALLOW** | Renewals within the user's programs. List, detail, and timeline. | F0001-S0002 Role Visibility; F0007-S0007 |
| ProgramManager | create | **DENY** | Out of scope in MVP. | F0007 PRD scope |
| ProgramManager | transition | **DENY** | Read-only in MVP. | BLUEPRINT §4.4 |
| ProgramManager | assign | **DENY** | Read-only in MVP. | BLUEPRINT §4.4 |
| Admin | read | **ALLOW** | Unscoped access to all renewal endpoints. | BLUEPRINT §4.4 |
| Admin | create | **ALLOW** | Unscoped. | F0007-S0006 |
| Admin | transition | **ALLOW** | Unscoped; valid transitions only. | BLUEPRINT §4.4, §4.3 |
| Admin | assign | **ALLOW** | Unscoped; cannot assign terminal-state renewals. | F0007-S0004 |
| ExternalUser | all | **DENY** | No external portal in MVP. | BLUEPRINT §3.1 non-goals |

**Constraints applying to all ALLOW decisions on Renewal:**
- Role gating on transitions is enforced in the application layer via `WorkflowStateMachine.ValidateRenewalTransition` in addition to Casbin policy. Distribution roles cannot perform Underwriter-owned transitions and vice versa.
- One active (non-deleted, non-terminal) renewal per `PolicyId` is enforced via filtered unique index `IX_Renewals_PolicyId_Active`. Duplicate creation returns HTTP 409 with code `duplicate_renewal`.
- Lost transitions require `lostReasonCode`; Completed transitions require `boundPolicyId` (and may include `renewalSubmissionId`). Missing fields return HTTP 409 with code `missing_transition_prerequisite`. (BLUEPRINT §4.3)
- Invalid transition pairs return HTTP 409 with code `invalid_transition`. (BLUEPRINT §4.3)
- Assignment of a terminal-state renewal (`Completed`, `Lost`) returns HTTP 409 with code `assignment_not_allowed_in_terminal_state`.
- State-changing mutations (`POST /renewals/{renewalId}/transitions`, `PUT /renewals/{renewalId}/assignment`) require `If-Match` and return HTTP 412 `precondition_failed` on stale rowVersion values. (API Guidelines + F0007 architecture)
- Every successful transition appends a `WorkflowTransition` and `ActivityTimelineEvent` record. Assignment changes append an `ActivityTimelineEvent`. (BLUEPRINT §4.3)
- Per-LOB timing windows (`WorkflowSlaThreshold` keyed on `LineOfBusiness`) drive overdue/approaching computation; defaults are used when no LOB-specific row exists. (ADR-009, ADR-014)

---

### 2.10 BrokerUser (Phase 1 Delta — F0009)

This section applies only when F0009 is enabled. It does not alter MVP InternalOnly rules for existing external users by default.

| Role | Resource | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|----------|--------|----------|------------------------------|----------------------|
| BrokerUser | broker | read / search | **ALLOW** | Only broker records mapped to authenticated broker identity; no cross-broker visibility. | F0009-S0004 |
| BrokerUser | broker | create / update / delete / reactivate | **DENY** | BrokerUser is read-first in Phase 1. | F0009 PRD Out of Scope; F0009-S0004 |
| BrokerUser | contact | read | **ALLOW** | Only contacts for broker-visible broker records. Internal-only fields masked/omitted. | F0009-S0004 |
| BrokerUser | contact | create / update / delete | **DENY** | No broker-side contact mutations in this phase. | F0009 PRD Out of Scope |
| BrokerUser | dashboard_kpi | read | **DENY** | KPI response aggregates submission and renewal data (both DENY resources). Endpoint shape cannot be safely filtered in Phase 1 — returning only `activeBrokers` would require a new endpoint contract. | F0009-S0003, F0009-S0004; F-006 Resolution |
| BrokerUser | dashboard_pipeline | read | **DENY** | Pipeline response is entirely submission/renewal status counts. No BrokerVisible field exists in this response shape. | F0009-S0003, F0009-S0004; F-006 Resolution |
| BrokerUser | dashboard_nudge | read | **ALLOW** | Mandatory server-side scope filter: return only `OverdueTask` nudges where `linkedEntityType = 'Broker'` AND `linkedEntityId` is within the authenticated BrokerUser's resolved broker scope. `StaleSubmission` and `UpcomingRenewal` nudge types must be excluded. If broker scope is empty, return empty array — not 403. All NudgeCard fields are BrokerVisible; InternalOnly protection is at nudge type filter level. | F0009-S0004; F-006 Resolution |
| BrokerUser | timeline_event | read | **ALLOW** | Only events explicitly classified BrokerVisible. | F0009-S0004 |
| BrokerUser | task | read | **ALLOW** | Broker-visible tasks assigned to or linked to authenticated broker identity only. | F0009-S0004 |
| BrokerUser | task | create / update / delete | **DENY** | No broker task mutation in Phase 1. | F0009 PRD Out of Scope |
| BrokerUser | submission | read | **DENY** | Submission self-service is out of scope for this feature phase. | F0009 PRD Out of Scope |
| BrokerUser | submission | transition | **DENY** | No workflow transitions by BrokerUser in Phase 1. | F0009 PRD Out of Scope |
| BrokerUser | renewal | read | **DENY** | Renewal self-service is out of scope for this feature phase. | F0009 PRD Out of Scope |
| BrokerUser | renewal | transition | **DENY** | No workflow transitions by BrokerUser in Phase 1. | F0009 PRD Out of Scope |

**Constraints applying to all BrokerUser ALLOW decisions:**
- Default deny applies for any resource/action not explicitly listed as ALLOW above.
- Server-side ABAC enforcement is authoritative; frontend hiding is defense-in-depth only.
- BrokerUser tenant scope must resolve from authenticated `broker_tenant_id` claim.
- If `broker_tenant_id` is missing, unknown, or maps ambiguously, access is denied.
- Enforcement order is fixed: tenant query isolation -> ABAC decision -> DTO field filtering.
- InternalOnly fields must be masked or omitted from all BrokerUser responses.
- All BrokerUser reads must be broker-tenant scoped and auditable.

---

### 2.11 Account (F0016)

Resource: `account`. Actions: `read`, `create`, `update`, `deactivate`, `reactivate`, `delete`, `merge`, `contact:manage`, `relationship:change`. See [ADR-017](../architecture/decisions/ADR-017-account-merge-tombstone-and-fallback-contract.md) for merge and tombstone contract.

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | read | **ALLOW** | Own region + assigned broker scope. Deleted accounts excluded unless admin. Merged surface as tombstone with survivor pointer. | F0016-S0001, S0003, S0004, S0009 |
| DistributionUser | create | **ALLOW** | Own region; may create from-submission / from-policy. Duplicate hint advisory only. | F0016-S0002 |
| DistributionUser | update | **ALLOW** | Own region + assigned broker scope. If-Match required. Disallowed on Merged/Deleted (409). | F0016-S0003 |
| DistributionUser | deactivate / reactivate | **DENY** | Reserved for Manager/Admin. | F0016-S0007 |
| DistributionUser | delete | **DENY** | Reserved for Manager/Admin. | F0016-S0007 |
| DistributionUser | merge | **DENY** | Reserved for Manager/Admin. | F0016-S0008 |
| DistributionUser | contact:manage | **ALLOW** | Scoped as read. No primary-contact reassignment across accounts. | F0016-S0005 |
| DistributionUser | relationship:change | **DENY** | Reserved for Manager/Admin. | F0016-S0006 |
| DistributionManager | read | **ALLOW** | Own territory. | F0016-S0001, S0004 |
| DistributionManager | create | **ALLOW** | Own territory. | F0016-S0002 |
| DistributionManager | update | **ALLOW** | Own territory. If-Match required. | F0016-S0003 |
| DistributionManager | deactivate / reactivate | **ALLOW** | Own territory. State must match from-state. | F0016-S0007 |
| DistributionManager | delete | **ALLOW** | Own territory. `reasonCode` required; `reasonDetail` required when reason=Other. | F0016-S0007 |
| DistributionManager | merge | **ALLOW** | Own territory. Survivor must be Active; self-merge rejected. Synchronous commit; idempotent on retry. | F0016-S0008 |
| DistributionManager | contact:manage | **ALLOW** | Own territory. | F0016-S0005 |
| DistributionManager | relationship:change | **ALLOW** | Own territory. Appends AccountRelationshipHistory + ActivityTimelineEvent. | F0016-S0006 |
| Underwriter | read | **ALLOW** | Read-only on accounts in assigned underwriting book. Tombstones render with stable display name. | F0016-S0001, S0004, S0009 |
| Underwriter | create / update / deactivate / reactivate / delete / merge / contact:manage / relationship:change | **DENY** | Read-only role. | F0016 PRD §Role-Based Access |
| RelationshipManager | read | **ALLOW** | Accounts linked to managed brokers. | F0016-S0001, S0004 |
| RelationshipManager | create | **DENY** | Read-first role for accounts. | F0016 PRD §Role-Based Access |
| RelationshipManager | update | **DENY** | Profile updates reserved for Distribution roles + Admin. | F0016 PRD §Role-Based Access |
| RelationshipManager | contact:manage | **ALLOW** | Contacts on managed-broker accounts only. | F0016-S0005 |
| RelationshipManager | deactivate / reactivate / delete / merge / relationship:change | **DENY** | Not in this role in MVP. | F0016 PRD §Role-Based Access |
| ProgramManager | all | **DENY** | Accounts are not in ProgramManager scope in MVP. | F0016 PRD §Scope |
| Admin | all | **ALLOW** | Full unscoped. Only role that may use `includeRemoved=true` on list. | F0016 PRD §Role-Based Access |
| BrokerUser | all | **DENY** | No external account access in MVP. | F0016 PRD §Out of Scope |
| ExternalUser | all | **DENY** | No external account access in MVP. | F0016 PRD §Out of Scope |

**Constraints applying to all ALLOW decisions on Account:**

- Account reads and writes enforce ABAC via Casbin on resource `account`; scope predicates reuse region / territory / broker patterns from F0002 and F0007.
- Write endpoints require `If-Match` with `rowVersion`. Stale → 412.
- Lifecycle transitions must follow the state machine in F0016 PRD §Account Lifecycle Workflow; invalid transitions return 409 `code=invalid_transition`.
- Every mutation appends one `WorkflowTransition` (when lifecycle change) and one `ActivityTimelineEvent` on the account; merges append an additional mirror timeline event on the survivor.
- Dependent list/detail endpoints (submissions, renewals, policies) MUST surface `accountDisplayName`, `accountStatus`, `accountSurvivorId` denormalized per ADR-017; failure to do so is a regression.
- `includeRemoved=true` on `GET /accounts` is Admin-only; all other roles receive Active + Inactive (plus explicitly-requested Merged via filter).

---

## 3. InternalOnly Content Rule

All resources in this matrix are classified **InternalOnly** for MVP. No data is accessible to ExternalUser under any circumstances. This rule applies universally to all resources above.

Sources: BLUEPRINT §1.2 (external users are Future only), §3.1 non-goals ("No external broker/MGA self-service portal in MVP"), F0001-S0001 through F0001-S0005 Data Visibility sections, F0002-S0001 and F0002-S0002 Data Visibility sections.

Phase 1 exception (F0009): BrokerUser access can be enabled only for the explicitly broker-visible resources and actions listed in §2.10.

---

## 4. Open Questions

None.
