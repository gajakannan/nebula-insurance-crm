# Authorization Matrix (Requirements)

Owner: Product Manager
Status: Final (MVP) + Phase 1 delta defined
Last Updated: 2026-06-06

Sources used: BLUEPRINT.md §1.2, §3.1, §3.2, §4.3, §4.4; F0001-S0001, F0001-S0002, F0001-S0003, F0001-S0004, F0001-S0005; F0002-S0001 through F0002-S0007; F0017-S0001 through F0017-S0005; ADR-026.
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

### 2.1b Carrier Market Relationship Management (F0028)

F0028 introduces CRM-side carrier and market relationship intelligence. It is InternalOnly and commercially sensitive; external broker/MGA users receive no access.

| Role | Resource | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|----------|--------|----------|------------------------------|----------------------|
| DistributionUser | carrier_market | read / search | **ALLOW** | Internal market directory rows scoped to authorized submissions, policies, and assigned opportunities. | F0028-S0001; F0028 PRD |
| DistributionUser | carrier_market | link_activity | **ALLOW** | May link carrier context only to submissions/policies the actor can read. No profile/contact/appetite/appointment mutation. | F0028-S0006 |
| DistributionUser | carrier_market | create / update / manage_contact / manage_appetite / manage_appointment | **DENY** | Relationship intelligence stewardship is manager/relationship-owned. | F0028-S0002 through S0005 |
| DistributionManager | carrier_market | read / search / create / update / manage_contact / manage_appetite / manage_appointment / link_activity | **ALLOW** | Region-scoped operational ownership; all mutations require `If-Match` when updating and emit timeline events. | F0028-S0001 through S0006 |
| Underwriter | carrier_market | read / search | **ALLOW** | Read-only market context for placement and submission review. | F0028-S0001, S0003, S0006 |
| Underwriter | carrier_market | link_activity | **ALLOW** | May link carrier context to submissions/policies visible to the underwriter; no relationship intelligence edits. | F0028-S0006 |
| Underwriter | carrier_market | create / update / manage_contact / manage_appetite / manage_appointment | **DENY** | Underwriter is read-only for relationship intelligence. | F0028 PRD |
| RelationshipManager | carrier_market | read / search / create / update / manage_contact / manage_appetite / manage_appointment / link_activity | **ALLOW** | Primary steward for carrier profile, contacts, appetite, appointments, and related activity links. | F0028-S0001 through S0006 |
| ProgramManager | carrier_market | read / search | **ALLOW** | Program-scoped market context only. No mutation. | F0028-S0001, S0005 |
| ProgramManager | carrier_market | create / update / manage_contact / manage_appetite / manage_appointment / link_activity | **DENY** | Program managers are read-only for F0028. | F0028 PRD |
| Admin | carrier_market | all | **ALLOW** | Full internal access; validation, concurrency, and audit rules still apply. | F0028-S0001 through S0006 |
| BrokerUser | carrier_market | all | **DENY** | F0028 is InternalOnly. | F0028 PRD non-goals |
| ExternalUser | carrier_market | all | **DENY** | No external carrier market visibility in MVP. | BLUEPRINT §3.1 non-goals |

**Constraints applying to all F0028 ALLOW decisions:**
- Every successful mutation emits an immutable `ActivityTimelineEvent` on `CarrierMarket`; rejected mutations emit no event.
- Update operations require `If-Match`; stale versions return `precondition_failed` (412).
- Duplicate carrier market codes return 409; semantic validation failures return 422.
- Activity links require read access to both the carrier market and the related submission or policy.
- Search rows, snippets, facets, and counts are filtered before response materialization.

---

### 2.1a Distribution Hierarchy, Producer Ownership, and Territory (F0017)

F0017 introduces structural distribution data and effective-dated ownership/territory records. Per ADR-026, hierarchy-aware read scoping is deferred to F0037; F0017 reads are authenticated internal-only, while mutations are limited to DistributionManager and Admin.

| Role | Resource | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|----------|--------|----------|------------------------------|----------------------|
| DistributionUser | distribution_node / producer_ownership / territory | read | **ALLOW** | Authenticated internal read; no hierarchy-aware subtree scoping in F0017. | F0017-S0002, S0003, S0004; ADR-026 §6 |
| DistributionUser | distribution_node:update / producer_ownership:assign / territory:create / territory:assign | mutate | **DENY** | Structural hierarchy, ownership, and territory mutations are manager-owned in MVP+. | F0017-S0001, S0003, S0004 |
| DistributionManager | distribution_node | read / update | **ALLOW** | May read full tree and set/clear parent on active nodes; cycle/self-parent/orphan rules still apply. | F0017-S0001, S0002 |
| DistributionManager | producer_ownership | read / assign | **ALLOW** | May assign/reassign producer ownership with effective dates; overlap/backdating rules still apply. | F0017-S0003 |
| DistributionManager | territory | read / create / assign | **ALLOW** | May create territories and assign members with effective dates; duplicate/overlap rules still apply. | F0017-S0004 |
| Underwriter | distribution_node / producer_ownership / territory | read | **ALLOW** | Authenticated internal read for operational context; no mutation. | F0017-S0002, S0003, S0004; ADR-026 §6 |
| Underwriter | distribution_node:update / producer_ownership:assign / territory:create / territory:assign | mutate | **DENY** | Read-only context. | F0017-S0001, S0003, S0004 |
| RelationshipManager | distribution_node / producer_ownership / territory | read | **ALLOW** | Authenticated internal read for broker/account relationship context; no mutation. | F0017-S0002, S0003, S0004 |
| RelationshipManager | distribution_node:update / producer_ownership:assign / territory:create / territory:assign | mutate | **DENY** | Relationship managers do not change distribution structure in this slice. | F0017-S0001, S0003, S0004 |
| ProgramManager | distribution_node / producer_ownership / territory | read | **ALLOW** | Authenticated internal read for program context; no mutation. | F0017-S0002, S0003, S0004 |
| ProgramManager | distribution_node:update / producer_ownership:assign / territory:create / territory:assign | mutate | **DENY** | Program managers are read-only for this structural model. | F0017-S0001, S0003, S0004 |
| Admin | distribution_node | read / update | **ALLOW** | Full internal read and mutation; validation and concurrency rules still apply. | F0017-S0001, S0002 |
| Admin | producer_ownership | read / assign | **ALLOW** | Full internal read and mutation; effective-date rules still apply. | F0017-S0003 |
| Admin | territory | read / create / assign | **ALLOW** | Full internal read and mutation; duplicate/overlap rules still apply. | F0017-S0004 |
| BrokerUser | distribution_node / producer_ownership / territory | all | **DENY** | F0017 data is InternalOnly; external portal and hierarchy-aware broker visibility are deferred. | F0017 PRD Out of Scope; ADR-026 §6 |
| ExternalUser | distribution_node / producer_ownership / territory | all | **DENY** | No external broker/MGA self-service portal in MVP. | BLUEPRINT §3.1 non-goals |

**Constraints applying to all F0017 ALLOW decisions:**
- Reads are authenticated internal-only and intentionally not hierarchy/territory scoped until F0037.
- `distribution_node:update` requires `If-Match`; stale versions return `precondition_failed` (412).
- `producer_ownership:assign` and `territory:assign` close prior effective-dated periods and open new periods transactionally; overlap conflicts return 409 and semantic date errors return 422.
- Every successful mutation emits an immutable ActivityTimelineEvent atomically with the mutation; rejected mutations emit no event.

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

### 2.6c Work Queues and Operational Routing (F0022)

Applies to queue administration, assignment rules, coverage windows, queue worklists, reassignment, rebalance, and routing-audit reads.

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | read | **ALLOW** | Queue worklists where user is a member or assignee; source-record ABAC still applies before returning item details. | F0022-S0005 |
| DistributionUser | manage | **DENY** | Cannot create queues, rules, memberships, or coverage windows. | F0022-S0001/S0002/S0004 |
| DistributionUser | assign | **DENY** | Manual reassignment and rebalance are manager/admin only. | F0022-S0006 |
| DistributionManager | read | **ALLOW** | Queues and queue items within managed region/territory/program scope; source-record ABAC still applies. | F0022-S0005/S0007 |
| DistributionManager | manage | **ALLOW** | Create/update queues, memberships, rules, and coverage windows within operational scope. | F0022-S0001/S0002/S0004 |
| DistributionManager | assign | **ALLOW** | Reassign/rebalance open queue items to active eligible users in scope; override reason required. | F0022-S0006 |
| ProgramManager | read | **ALLOW** | Queue items linked to programs/accounts the user manages. | F0022-S0005 |
| ProgramManager | manage | **DENY** | Program managers do not administer routing rules in MVP. | F0022-S0001/S0002 |
| ProgramManager | assign | **DENY** | Assignment control remains DistributionManager/Admin. | F0022-S0006 |
| Underwriter | read | **ALLOW** | Queue worklists where user is assignee/member and source-record ABAC permits access. | F0022-S0005 |
| Underwriter | manage | **DENY** | No queue/rule/coverage administration in MVP. | F0022-S0001/S0002/S0004 |
| Underwriter | assign | **DENY** | Cannot rebalance/reassign others' work. | F0022-S0006 |
| RelationshipManager | read | **ALLOW** | Queue items linked to managed broker/account relationships. | F0022-S0005 |
| RelationshipManager | manage | **DENY** | No queue/rule administration in MVP. | F0022-S0001/S0002 |
| RelationshipManager | assign | **DENY** | No queue reassignment authority in MVP. | F0022-S0006 |
| Admin | read | **ALLOW** | Unscoped internal read of queues, worklists, and routing audit. | F0022-S0005/S0007 |
| Admin | manage | **ALLOW** | Full queue/rule/membership/coverage administration. | F0022-S0001/S0002/S0004 |
| Admin | assign | **ALLOW** | Full reassignment/rebalance authority; override reason required. | F0022-S0006 |
| BrokerUser | all | **DENY** | Queue/routing data is InternalOnly. | F0022-S0007 |
| ExternalUser | all | **DENY** | Queue/routing data is InternalOnly. | F0022-S0007 |

**Constraints applying to all ALLOW decisions on Work Queues:**
- Queue item visibility is the intersection of queue authorization and source-record authorization. A user who can read a queue must still be authorized to read the linked Task, Submission, or Renewal before row details are returned.
- Queue/rule/coverage mutations require `If-Match` and return HTTP 412 `precondition_failed` on stale rowVersion values.
- Manual reassignment and rebalance require an override reason and emit `ActivityTimelineEvent` plus `RoutingDecisionLog`.
- Coverage activates only from explicit `CoverageWindow` rows; inactivity is never a coverage trigger.
- No-match routing places work in `Unassigned Operations Queue` with null assignee and a routing decision reason.

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

### 2.7a Communication Event — Capture / Read / Link / Correct / Redact / Follow-Up (F0021)

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | create / read / link / correct / create_follow_up | **ALLOW** | Must pass linked entity read access; task follow-up also requires `task:create`. | F0021-S0001 through S0005 |
| DistributionUser | redact | **DENY** | Redaction is Admin-only in MVP. | F0021-S0005; ADR-029 |
| DistributionManager | create / read / link / correct / create_follow_up | **ALLOW** | Region scope applies through linked broker/account/submission/policy/renewal/task records. | F0021-S0001 through S0005 |
| DistributionManager | redact | **DENY** | Redaction is Admin-only in MVP. | F0021-S0005; ADR-029 |
| Underwriter | create / read / link / correct / create_follow_up | **ALLOW** | Submission/policy access scope applies through linked records. | F0021-S0001 through S0005 |
| Underwriter | redact | **DENY** | Redaction is Admin-only in MVP. | F0021-S0005; ADR-029 |
| RelationshipManager | create / read / link / correct / create_follow_up | **ALLOW** | Broker/account relationship scope applies through linked records. | F0021-S0001 through S0005 |
| RelationshipManager | redact | **DENY** | Redaction is Admin-only in MVP. | F0021-S0005; ADR-029 |
| ProgramManager | create / read / link / correct / create_follow_up | **ALLOW** | Program scope applies through linked records. | F0021-S0001 through S0005 |
| ProgramManager | redact | **DENY** | Redaction is Admin-only in MVP. | F0021-S0005; ADR-029 |
| Admin | create / read / link / correct / redact / create_follow_up | **ALLOW** | Unscoped internal authority; redaction remains audit-preserving. | F0021-S0001 through S0005; ADR-029 |
| BrokerUser | all | **DENY** | External communication capture/history is out of MVP scope. | F0021 PRD Out of Scope |
| ExternalUser | all | **DENY** | No external self-service in MVP. | BLUEPRINT §3.1 non-goals |

**Constraints applying to all ALLOW decisions on Communication Event:**
- Communication source records are InternalOnly for MVP.
- Create/read/link/follow-up actions must also pass read access on the primary linked CRM record.
- Additional linked records must pass read access before persistence.
- Follow-up creation must pass existing task assignee validation and `task:create`.
- Corrections and redactions are append-only audit actions; no communication record is hard-deleted.
- Email-linked activity stores metadata/reference only and does not authorize outbound send, mailbox read, or connector ingestion.

---

### 2.7b Service Case — Create / Read / Update / Assign / Transition / Claim Reference / Links (F0024)

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | create / read / update / transition / update_claim_reference / link_communication / create_follow_up | **ALLOW** | Must pass account/policy read access; owner must be self or assigned queue in MVP. | F0024-S0001 through S0006 |
| DistributionUser | assign | **DENY** | Cross-user assignment is manager/admin scope in MVP. | F0024-S0003 |
| DistributionManager | create / read / update / assign / transition / update_claim_reference / link_communication / create_follow_up | **ALLOW** | Region scope applies through linked account/policy records. | F0024-S0001 through S0006 |
| Underwriter | create / read / update / transition / update_claim_reference / link_communication / create_follow_up | **ALLOW** | Policy/account access scope applies through linked records. | F0024-S0001 through S0006 |
| Underwriter | assign | **DENY** | Cross-user assignment is manager/admin scope in MVP. | F0024-S0003 |
| RelationshipManager | create / read / update / transition / update_claim_reference / link_communication / create_follow_up | **ALLOW** | Broker/account relationship scope applies through linked records. | F0024-S0001 through S0006 |
| RelationshipManager | assign | **DENY** | Cross-user assignment is manager/admin scope in MVP. | F0024-S0003 |
| ProgramManager | create / read / update / transition / update_claim_reference / link_communication / create_follow_up | **ALLOW** | Program scope applies through linked account/policy records. | F0024-S0001 through S0006 |
| ProgramManager | assign | **DENY** | Cross-user assignment is manager/admin scope in MVP. | F0024-S0003 |
| Admin | create / read / update / assign / transition / update_claim_reference / link_communication / create_follow_up | **ALLOW** | Unscoped internal authority; all actions remain audited. | F0024-S0001 through S0006; ADR-030 |
| BrokerUser | all | **DENY** | External service-case self-service is out of MVP scope. | F0024 PRD Out of Scope |
| ExternalUser | all | **DENY** | No external self-service in MVP. | BLUEPRINT §3.1 non-goals |

**Constraints applying to all ALLOW decisions on Service Case:**
- Service-case records are InternalOnly for MVP.
- Create/read/update/assign/transition/follow-up actions must also pass read access on the linked account and optional policy.
- If a policy is supplied, it must belong to the supplied account.
- Communication linking must also pass `communication_event:read` and linked communication record scope.
- Follow-up creation must pass existing task assignee validation and `task:create`.
- Claim-reference updates store reference context only and do not authorize adjudication, reserves, payments, carrier sync, or coverage decisions.
- Closed service cases are immutable except for audit reads.

---

### 2.8 Submission — Read / Create / Update / Transition / Assign / Approve / Archive

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
| Underwriter | approve | **ALLOW** | Single authorized approver: grant/decline the underwriting approval checkpoint on a Quoted submission. Applies to `POST /submissions/{submissionId}/approval`. | F0019-S0003; ADR-025 §3 |
| Admin | approve | **ALLOW** | Unscoped approval authority. Applies to `POST /submissions/{submissionId}/approval`. | F0019-S0003; ADR-025 §3 |
| Underwriter | archive | **ALLOW** | Archive/reactivate a terminal submission (Bound/Declined/Withdrawn). Explicit lifecycle action, not delete. Applies to `POST /submissions/{submissionId}/archive` and `/reactivate`. | F0019-S0006; ADR-025 §5 |
| Admin | archive | **ALLOW** | Unscoped archive/reactivate of terminal submissions. Applies to `POST /submissions/{submissionId}/archive`. | F0019-S0006; ADR-025 §5 |
| DistributionUser, DistributionManager, RelationshipManager, ProgramManager | approve, archive | **DENY** | Approval authority and archive are underwriting/admin-only in MVP (closed-by-default). | F0019-S0003/S0006; ADR-025 |
| ExternalUser | all | **DENY** | No external portal in MVP. | BLUEPRINT §3.1 non-goals |

**Constraints applying to all ALLOW decisions on Submission:**
- Invalid transition pairs return HTTP 409 with ProblemDetails code invalid_transition. (BLUEPRINT §4.3)
- Missing transition prerequisites return HTTP 409 with ProblemDetails code missing_transition_prerequisite. (BLUEPRINT §4.3)
- State-changing mutations (`PUT /submissions/{submissionId}`, `PUT /submissions/{submissionId}/assignment`, `POST /submissions/{submissionId}/transitions`) require `If-Match` and return HTTP 412 `precondition_failed` on stale rowVersion values. (API Guidelines + F0006 architecture)
- Every successful transition appends a WorkflowTransition and ActivityTimelineEvent record. (BLUEPRINT §4.3)
- Approval (`POST /submissions/{submissionId}/approval`) is gated by `submission:approve` (Underwriter/Admin), requires a Quoted submission with a ready packet, and a granted approval is a precondition for `Quoted -> BindRequested`. Approval decisions are append-only. (F0019-S0003; ADR-025 §3)
- Archive (`POST /submissions/{submissionId}/archive`) is gated by `submission:archive` (Underwriter/Admin), allowed only for terminal states (Bound/Declined/Withdrawn), audit-preserving, and reversible via `/reactivate`. There is no generic submission delete endpoint. (F0019-S0006; ADR-025 §5)
- F0019 introduces no rating/pricing/scoring; quote figures are recorded reference values, never computed by Nebula. (ADR-025 §6)

---

### 2.9 Renewal — Read / Create / Update / Transition / Assign (F0007, F0034)

Applies to the F0007 Renewal Pipeline endpoints: `GET /renewals`, `POST /renewals`, `GET /renewals/{renewalId}`, `PUT /renewals/{renewalId}/lob-attributes`, `POST /renewals/{renewalId}/transitions`, `PUT /renewals/{renewalId}/assignment`, `GET /renewals/{renewalId}/timeline`.

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| DistributionUser | read | **ALLOW** | Renewals assigned to the user only. Applies to list, detail, and timeline endpoints. | F0001-S0002; F0007-S0001/S0002/S0007; user requirement |
| DistributionUser | create | **ALLOW** | Distribution users may create a renewal from an expiring policy within their assigned scope. Applies to `POST /renewals`. | F0007-S0006 |
| DistributionUser | update | **ALLOW** | Assigned non-terminal renewals in Distribution-owned states may update dynamic LOB attributes. Applies to `PUT /renewals/{renewalId}/lob-attributes`. | F0034-S0005/S0007 |
| DistributionUser | transition | **ALLOW** | Only for assigned renewals and only for transitions in the Distribution-owned states (Identified ↔ Outreach ↔ InReview). | F0007-S0003; BLUEPRINT §4.3 |
| DistributionUser | assign | **DENY** | Self-assignment only — no cross-user assignment. Use create-time `assignedToUserId`. | F0007-S0004 |
| DistributionManager | read | **ALLOW** | All renewals within region. Applies to list, detail, and timeline endpoints. | F0001-S0002; F0007-S0001/S0002/S0007; user requirement |
| DistributionManager | create | **ALLOW** | Region-scoped; may seed renewals on behalf of distribution users. | F0007-S0006 |
| DistributionManager | update | **ALLOW** | Region-scoped non-terminal renewals in Distribution-owned states may update dynamic LOB attributes. | F0034-S0005/S0007 |
| DistributionManager | transition | **ALLOW** | Renewals within region; valid transitions only in Distribution-owned states. | F0007-S0003; BLUEPRINT §4.3 |
| DistributionManager | assign | **ALLOW** | Reassignment of renewal ownership within region. Applies to `PUT /renewals/{renewalId}/assignment`. Cannot assign terminal-state renewals. | F0007-S0004 |
| Underwriter | read | **ALLOW** | Renewals assigned to or accessible by the user (post-handoff). | F0007-S0002/S0007; BLUEPRINT §4.4 |
| Underwriter | create | **DENY** | Underwriters do not seed renewals; intake is distribution-owned. | F0007-S0006 |
| Underwriter | update | **ALLOW** | Assigned non-terminal renewals in Underwriter-owned states may update dynamic LOB attributes. | F0034-S0005/S0007 |
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
| Admin | update | **ALLOW** | Unscoped non-terminal renewal dynamic LOB attribute updates. | F0034-S0005/S0007 |
| Admin | transition | **ALLOW** | Unscoped; valid transitions only. | BLUEPRINT §4.4, §4.3 |
| Admin | assign | **ALLOW** | Unscoped; cannot assign terminal-state renewals. | F0007-S0004 |
| ExternalUser | all | **DENY** | No external portal in MVP. | BLUEPRINT §3.1 non-goals |

**Constraints applying to all ALLOW decisions on Renewal:**
- Role gating on transitions is enforced in the application layer via `WorkflowStateMachine.ValidateRenewalTransition` in addition to Casbin policy. Distribution roles cannot perform Underwriter-owned transitions and vice versa.
- One active (non-deleted, non-terminal) renewal per `PolicyId` is enforced via filtered unique index `IX_Renewals_PolicyId_Active`. Duplicate creation returns HTTP 409 with code `duplicate_renewal`.
- Lost transitions require `lostReasonCode`; Completed transitions require `boundPolicyId` (and may include `renewalSubmissionId`). Missing fields return HTTP 409 with code `missing_transition_prerequisite`. (BLUEPRINT §4.3)
- Invalid transition pairs return HTTP 409 with code `invalid_transition`. (BLUEPRINT §4.3)
- Assignment of a terminal-state renewal (`Completed`, `Lost`) returns HTTP 409 with code `assignment_not_allowed_in_terminal_state`.
- State-changing mutations (`PUT /renewals/{renewalId}/lob-attributes`, `POST /renewals/{renewalId}/transitions`, `PUT /renewals/{renewalId}/assignment`) require `If-Match` and return HTTP 412 `precondition_failed` on stale rowVersion values. (API Guidelines + F0007 architecture)
- Every successful transition appends a `WorkflowTransition` and `ActivityTimelineEvent` record. Assignment changes append an `ActivityTimelineEvent`. (BLUEPRINT §4.3)
- Per-LOB timing windows (`WorkflowSlaThreshold` keyed on `LineOfBusiness`) drive overdue/approaching computation; defaults are used when no LOB-specific row exists. (ADR-009, ADR-014)

---

### 2.9a Renewal — Draft Outreach (F0038, ADR-027 + ADR-028)

New dedicated, least-privilege action `renewal:draft_outreach`. Gates **both** the
persist-draft endpoint (`POST /renewals/{renewalId}/outreach-draft`, F0038-S0005)
and the mock-send endpoint (`POST /renewals/{renewalId}/outreach-mock-send`,
F0038-S0006). Distinct from `renewal:transition`. Called by the Neuron service as
the user (forwarded authentik token); the engine enforces this Casbin rule — no
authorization is re-implemented in Python.

| Role | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|--------|----------|------------------------------|----------------------|
| Underwriter | draft_outreach | **ALLOW** | Renewal owner. May draft (persist InternalOnly AI-generated `ActivityTimelineEvent` with provenance) and mock-send. Mock-send commits the real `Identified → Outreach` transition + a "sent (simulated)" event and **fakes SMTP** (no real email). | F0038-S0005, F0038-S0006; intake decisions B/C |
| DistributionUser | draft_outreach | **DENY** | No draft rights in v1. "Refer draft to Distribution for review" is a future feature. | F0038 intake decision C |
| DistributionManager | draft_outreach | **DENY** | No draft rights in v1. | F0038 intake decision C |
| RelationshipManager | draft_outreach | **DENY** | Read-only in MVP. | F0038 scope |
| ProgramManager | draft_outreach | **DENY** | Read-only in MVP. | F0038 scope |
| Admin | draft_outreach | **ALLOW** | Unscoped. | ADR-028 §3 |
| ExternalUser | draft_outreach | **DENY** | No external portal in MVP. | BLUEPRINT §3.1 non-goals |

**Constraints applying to `renewal:draft_outreach`:**
- **Outreach-commit exception (ADR-028 §3):** `WorkflowStateMachine.ValidateRenewalTransition`
  permits `Identified → Outreach` for an **Underwriter** **only** when performed
  through the outreach-mock-send path under `renewal:draft_outreach` authority. The
  general `renewal:transition` ownership split (Distribution owns
  `Identified ↔ Outreach ↔ InReview`) is **unchanged** for every other path. This
  is the deliberate F0038↔F0007 reconciliation: in commercial P&C the underwriter
  owns the renewal, so a dedicated single-purpose grant honors the locked intake
  without widening general transition rights.
- Draft content must not state/imply premium, quote figures, coverage terms, or
  any binding commitment (engine-validated; 422 on violation). Drafts are
  `InternalOnly` and labelled "AI-generated draft" until a human edits/approves.
- Drafting does **not** transition; the transition fires only on mock-send.
- Mock-send is atomic (transition + both events all-or-nothing) and requires
  `If-Match` (rowVersion); invalid state → 409 `invalid_transition`.
- Every draft and mock-send emits provenance (actor, model, prompt id/version,
  content hash). Under no circumstance is a real email dispatched in v1.

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

### 2.10b Global Search, Saved Views, and Operational Reports (F0023)

F0023 is internal-only. Search/report rows and counts are filtered at the query
layer after Casbin grants the high-level feature action. Saved views store
criteria only and never grant source-record access.

| Role | Resource | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|----------|--------|----------|------------------------------|----------------------|
| DistributionUser | global_search | read | **ALLOW** | Results/facets/counts scoped to source records the user may read. | F0023-S0001, S0002, S0007 |
| DistributionManager | global_search | read | **ALLOW** | Region/team scope filters apply at query layer. | F0023-S0001, S0002, S0007 |
| Underwriter | global_search | read | **ALLOW** | Underwriting source-record scope applies at query layer. | F0023-S0001, S0002, S0007 |
| RelationshipManager | global_search | read | **ALLOW** | Managed broker/account scope applies at query layer. | F0023-S0001, S0002, S0007 |
| ProgramManager | global_search | read | **ALLOW** | Program scope applies at query layer. | F0023-S0001, S0002, S0007 |
| Admin | global_search | read | **ALLOW** | Unscoped internal read subject to source-record availability. | F0023-S0001, S0002, S0007 |
| BrokerUser / ExternalUser | global_search | all | **DENY** | External global search is out of scope. | F0023-S0007 |
| DistributionUser | saved_view | read | **ALLOW** | Own personal views plus eligible team views only. | F0023-S0003, S0004 |
| DistributionUser | saved_view | manage/default | **ALLOW** | Own personal saved views only. Team mutation denied. | F0023-S0003 |
| DistributionManager | saved_view | read/manage/default | **ALLOW** | Own personal views plus team views/defaults for administered region/team scopes. | F0023-S0003, S0004 |
| Underwriter | saved_view | read | **ALLOW** | Own personal views plus eligible team views only. | F0023-S0003, S0004 |
| Underwriter | saved_view | manage/default | **ALLOW** | Own personal saved views only. Team mutation denied. | F0023-S0003 |
| RelationshipManager | saved_view | read | **ALLOW** | Own personal views plus eligible team views only. | F0023-S0003, S0004 |
| RelationshipManager | saved_view | manage/default | **ALLOW** | Own personal saved views only. Team mutation denied. | F0023-S0003 |
| ProgramManager | saved_view | read/manage/default | **ALLOW** | Own personal views plus team views/defaults for administered program/team scopes. | F0023-S0003, S0004 |
| Admin | saved_view | read/manage/default | **ALLOW** | Unscoped internal saved-view administration. | F0023-S0003, S0004 |
| BrokerUser / ExternalUser | saved_view | all | **DENY** | External saved views are out of scope. | F0023-S0007 |
| DistributionUser | operational_report | read | **ALLOW** | Report rows, counts, and drilldowns scoped to source records the user may read. | F0023-S0005, S0006, S0007 |
| DistributionManager | operational_report | read | **ALLOW** | Region/team report scope applies at query layer. | F0023-S0005, S0006, S0007 |
| Underwriter | operational_report | read | **ALLOW** | Underwriting source-record scope applies at query layer. | F0023-S0005, S0006, S0007 |
| RelationshipManager | operational_report | read | **ALLOW** | Managed broker/account scope applies at query layer. | F0023-S0005, S0006, S0007 |
| ProgramManager | operational_report | read | **ALLOW** | Program scope applies at query layer. | F0023-S0005, S0006, S0007 |
| Admin | operational_report | read | **ALLOW** | Unscoped internal report read subject to source-record availability. | F0023-S0005, S0006, S0007 |
| BrokerUser / ExternalUser | operational_report | all | **DENY** | External operational reporting is out of scope. | F0023-S0007 |

**Constraints applying to all F0023 ALLOW decisions:**
- Query-layer source-object filters are mandatory for rows, snippets, suggestions, facets, counts, report summaries, and drilldowns.
- Unauthorized matches are omitted and must not be exposed as hidden-record counts.
- Saved views store only criteria JSON; applying a view reruns authorization for the current user.
- Team saved views require `teamScopeType` and `teamScopeKey`; unauthorized scopes return `saved_view_scope_denied` without revealing hidden team metadata.
- Team view mutations are limited to DistributionManager, ProgramManager, and Admin for administered scopes.
- Saved-view create/update/delete/default mutations require immutable `SavedViewAuditEvent` evidence; updates/default/archive require `If-Match`.

---

### 2.10b.1 Commission, Producer Splits, and Revenue Tracking (F0025)

F0025 economic data is InternalOnly. All ALLOW decisions below must also pass
source-record read checks for linked policy, carrier/market, producer ownership,
territory, and broker/account context before rows, counts, totals, facets, or
drilldowns are returned.

| Role | Resource | Action(s) | Decision | Conditions | Stories |
|------|----------|-----------|----------|------------|---------|
| DistributionUser | commission | read / adjustment_request / rollup_read | **ALLOW** | Own/assigned source-record scope only; no schedule or split mutation. | F0025-S0001, S0005, S0006 |
| DistributionManager | commission | read / split_assign / calculate / adjustment_request / adjustment_approve / rollup_read | **ALLOW** | Region/team source-record scope; same-user request and approval is denied. | F0025-S0001 through S0006 |
| Underwriter | commission | read | **ALLOW** | Read-only expected commission context for visible policies. No rollup totals or economic mutation. | F0025-S0001, S0004 |
| RelationshipManager | commission | read / schedule_manage / calculate / rollup_read | **ALLOW** | Carrier/market and managed-relationship scope; no adjustment approval. | F0025-S0001, S0002, S0004, S0006 |
| ProgramManager | commission | read / rollup_read | **ALLOW** | Program-scoped source records only. No economic mutation. | F0025-S0001, S0006 |
| Admin | commission | all | **ALLOW** | Unscoped internal administration; validation, concurrency, and audit rules still apply. | F0025-S0001 through S0006 |
| BrokerUser / ExternalUser | commission | all | **DENY** | External compensation visibility is out of scope. | F0025 PRD non-goals |

**Constraints applying to all F0025 ALLOW decisions:**
- Schedule, split, calculation, adjustment, and projection refresh mutations emit immutable `ActivityTimelineEvent` rows.
- Schedule and split mutations require `If-Match` when updating existing rows and reject stale row versions with `precondition_failed`.
- Active producer split percentages must total exactly 100.0000 before an assignment can be activated.
- Adjustment approval requires a different approver from the requester in MVP.
- Rollup totals are computed after authorization filtering; hidden source rows are not surfaced as hidden counts.

---

### 2.10c Broker Insights (F0008)

F0008 is internal-only and read-only. Broker scorecards, trends, benchmarks,
snapshots, source links, peer sets, ranks, and counts are computed after
source-record authorization filtering. This feature does not introduce
hierarchy-aware read enforcement or distribution rollups; F0037 owns that scope.

| Role | Resource | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|----------|--------|----------|------------------------------|----------------------|
| DistributionUser | broker_insight | read | **ALLOW** | Assigned/opportunity source scope applies at query layer. | F0008-S0001, S0002, S0005 |
| DistributionManager | broker_insight | read | **ALLOW** | Region/team source scope applies at query layer. | F0008-S0001, S0003, S0005 |
| Underwriter | broker_insight | read | **ALLOW** | Underwriting source-record scope applies at query layer. | F0008-S0002, S0005 |
| RelationshipManager | broker_insight | read | **ALLOW** | Managed broker/account scope applies at query layer. | F0008-S0001, S0002, S0004, S0005 |
| ProgramManager | broker_insight | read | **ALLOW** | Program source scope applies at query layer. | F0008-S0003, S0005 |
| Admin | broker_insight | read | **ALLOW** | Unscoped internal read subject to source-record availability. | F0008-S0001 through S0005 |
| BrokerUser / ExternalUser | broker_insight | all | **DENY** | External broker insights are out of scope. | F0008-S0005 |

**Constraints applying to all F0008 ALLOW decisions:**
- Hidden source records must not influence counts, denominators, trend points, peer counts, medians, ranks, percentiles, snapshots, or source links.
- Unauthorized matches are indistinguishable from non-matches in user-facing result states.
- Benchmark rank and percentile require at least five visible peers; otherwise suppress them.
- F0008 surfaces are read-only. No timeline event is emitted by scorecard, trend, benchmark, or snapshot reads.

---

### 2.10d Hierarchy-Aware Access Scoping and Distribution Rollups (F0037)

F0037 resolves a current-user distribution scope from role, hierarchy, territory,
producer ownership, source-record visibility, and `asOf`. Search, saved-view,
broker-insight, operational-report, and direct-read paths continue to use their
existing resource gates, but must consume the F0037 scope before materializing
rows, counts, facets, suggestions, drilldowns, or aggregates. The new
`distribution_rollup:read` resource gates the leader/relationship rollup report.

| Role | Resource | Action | Decision | Business Scope / Constraints | Story / AC Reference |
|------|----------|--------|----------|------------------------------|----------------------|
| DistributionUser | global_search / operational_report / distribution_node / producer_ownership / territory | read | **ALLOW** | Existing read gates remain; F0037 resolver narrows rows to assigned source records, territories, producer ownership, and effective hierarchy scope. | F0037-S0001, S0002, S0003 |
| DistributionUser | distribution_rollup | read | **DENY** | Rollup workspace is leader/relationship scoped in MVP; DistributionUser still receives scoped search/report results. | F0037-S0004, S0005 |
| DistributionManager | distribution_rollup | read | **ALLOW** | Managed subtree, territory, producer, and source-record scope applies before grouping and totals. | F0037-S0001, S0004, S0005 |
| Underwriter | global_search / operational_report / distribution_node / producer_ownership / territory | read | **ALLOW** | Existing read gates remain; F0037 resolver narrows to underwriting source-record and assignment scope. | F0037-S0001, S0002, S0003 |
| Underwriter | distribution_rollup | read | **DENY** | Underwriters are not distribution rollup viewers in MVP. | F0037-S0004, S0005 |
| RelationshipManager | distribution_rollup | read | **ALLOW** | Assigned relationship, territory, producer, and source-record scope applies before grouping and totals. | F0037-S0001, S0004, S0005 |
| ProgramManager | distribution_rollup | read | **ALLOW** | Program/hierarchy/territory authority applies before grouping and totals. | F0037-S0001, S0004, S0005 |
| Admin | distribution_rollup | read | **ALLOW** | Full internal rollup scope, still subject to source-record existence and `asOf`. | F0037-S0004, S0005 |
| BrokerUser / ExternalUser | F0037 internal scope and rollup resources | all | **DENY** | F0037 internal hierarchy-aware search/report/rollup surfaces are not exposed externally. | F0037-S0001 through S0006 |

**Constraints applying to all F0037 ALLOW decisions:**
- Scope resolution fails closed; empty scope is not widened.
- Hidden records are omitted before rows, counts, facets, suggestions, drilldowns, broker-insight metrics, and distribution rollups are computed.
- Direct access to a hidden record returns no-leak not found behavior, preferably 404, unless authentication or broad resource-policy failure occurs first.
- `asOf` evaluates effective-dated hierarchy, producer ownership, and territory assignments consistently for one request.
- Saved views preserve criteria only; executing a saved view recomputes the current user's F0037 scope.
- `distribution_rollup:read` is read-only and emits no timeline event.
- Security review and rollup reconciliation evidence are mandatory before feature closeout.

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

All resources in this matrix are classified **InternalOnly** for MVP unless a later section explicitly layers a narrower public/document rule. No data is accessible to ExternalUser under any circumstances for Brokers, Contacts, Submissions, Renewals, Tasks, Dashboard, Timeline, Policies, Accounts, or Product Schema Registry.

Sources: BLUEPRINT §1.2 (external users are Future only), §3.1 non-goals ("No external broker/MGA self-service portal in MVP"), F0001-S0001 through F0001-S0005 Data Visibility sections, F0002-S0001 and F0002-S0002 Data Visibility sections.

Phase 1 exception (F0009): BrokerUser access can be enabled only for the explicitly broker-visible resources and actions listed in §2.10.

Phase 1 exception (F0020): Documents and document templates introduce a `public | confidential | restricted` classification model layered on parent ABAC. External roles (BrokerUser, MgaUser, ExternalUser) can only read/download `public` documents per the classification table; the parent-ABAC table in §4 is the necessary, not sufficient, condition.

---

## 4. Documents (F0020 — ADR-012 + ADR-019)

The effective access decision for any document operation is:

```
allow ⇔ parent_abac(user, parent, op)  ∧  classification_policy(role, classification, op)
```

Parent ABAC lives in `planning-mds/security/policies/policy.csv` §3 (Documents, DocumentTemplates). The classification policy lives in `<docroot>/configuration/casbin-document-roles.yaml` (a runtime configuration file, not source-of-truth code) and is validated by `planning-mds/schemas/document-classification-policy.schema.json`. Both gates must allow.

### 4.1 Document operations (parent ABAC)

| Role | read | create | replace | update_metadata | download | create:restricted | declassify |
|------|------|--------|---------|-----------------|----------|-------------------|------------|
| Admin | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Underwriter | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| DistributionUser | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| DistributionManager | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| RelationshipManager | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| ProgramManager | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Coordinator | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| BrokerUser | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |
| MgaUser | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |
| ExternalUser | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |

### 4.2 Document templates (parent ABAC)

| Role | read | create | replace | link |
|------|------|--------|---------|------|
| Admin | ✅ | ✅ | ✅ | ✅ |
| Underwriter | ✅ | ❌ | ❌ | ✅ |
| DistributionUser | ✅ | ✅ | ❌ | ✅ |
| DistributionManager | ✅ | ✅ | ✅ | ✅ |
| RelationshipManager | ✅ | ❌ | ❌ | ✅ |
| ProgramManager | ✅ | ❌ | ❌ | ✅ |
| Coordinator | ✅ | ✅ | ❌ | ✅ |
| BrokerUser | ✅ | ✅ | ❌ | ✅ |
| MgaUser | ✅ | ✅ | ❌ | ✅ |
| ExternalUser | ✅ | ❌ | ❌ | ✅ |

### 4.3 Classification policy (default MVP table)

The runtime YAML (`<docroot>/configuration/casbin-document-roles.yaml`) is closed-by-default. The MVP default table that ships in seed data:

| Role | public | confidential | restricted |
|------|--------|--------------|------------|
| Admin | all ops | all ops | all ops |
| Underwriter | all ops | all ops | read, download, create:restricted, declassify |
| DistributionUser, DistributionManager, RelationshipManager, ProgramManager, Coordinator | all ops except restricted | all ops except restricted | deny |
| BrokerUser, MgaUser | read, create, download | deny | deny |
| ExternalUser | read, download | deny | deny |

**Constraints applying to all ALLOW decisions on Document and DocumentTemplate:**

- The classification policy YAML is the authoritative source for the classification half of the gate; the table above is the MVP default that ships in seed data, not hardcoded in code.
- `restricted` creation requires both `document:create` and `document:create:restricted` — the latter is granted only to Admin and Underwriter in MVP.
- Declassifying a `restricted` document requires `document:declassify`; downgrades from `confidential` → `public` require `document:update_metadata` and a passing classification check on the new tier.
- Every document operation produces a sidecar JSON `events[]` row plus one `ActivityTimelineEvent` per SOLUTION-PATTERNS §2; failed authorisation never writes either.
- File ingest enforces extension allowlist (`pdf, png, docx, xlsx, csv`), 5 MB per-file cap, and 25-file / 50 MB batch cap before any byte leaves the request handler. Violations return ProblemDetails with structured `code` (`unsupported_type`, `file_too_large`, `batch_too_large`, `empty_file`, `invalid_filename`).
- Download responses are streamed (no full-buffer); resolution path is `documentId → sidecar JSON → version filename` with explicit confirmation that the resolved binary path lives inside the parent's directory before any read.

---

## 5. Product Schema Registry (F0034)

Product schema bundles are internal platform configuration. Runtime screens may read active bundles only after the same ABAC and tenant availability filters that govern the parent lifecycle record. Activation, deprecation, retirement, and rollback are steward/admin actions in MVP and must write activation audit rows.

---

## 6. Outbound Document Generation (F0027)

F0027 layers feature-specific actions over the F0020 document/template gates. This preserves the generic template-library behavior while enforcing the F0027 product decision that outbound COI, ACORD, and proposal templates are Admin-governed and operating users issue artifacts from published versions only.

Effective issue/regenerate access:

```text
allow ⇔ parent_abac(user, parent, read/create)
     ∧ document_template:read(template)
     ∧ outbound_document:{issue|regenerate}
     ∧ classification_policy(role, generatedClassification, create/download)
```

| Role | outbound_template:manage | outbound_document:preview | outbound_document:issue | outbound_document:regenerate |
|------|--------------------------|---------------------------|-------------------------|-------------------------------|
| Admin | ✅ | ✅ | ✅ | ✅ |
| Underwriter | ❌ | ✅ | ✅ | ✅ |
| DistributionUser | ❌ | ✅ | ✅ | ✅ |
| DistributionManager | ❌ | ✅ | ✅ | ✅ |
| Coordinator | ❌ | ✅ | ✅ | ✅ |
| RelationshipManager | ❌ | ❌ | ❌ | ❌ |
| ProgramManager | ❌ | ❌ | ❌ | ❌ |
| BrokerUser | ❌ | ❌ | ❌ | ❌ |
| MgaUser | ❌ | ❌ | ❌ | ❌ |
| ExternalUser | ❌ | ❌ | ❌ | ❌ |

Constraints:

- Preview never stores a document sidecar and never authorizes final issue by itself.
- Issue and regenerate reassemble source data server-side and must not trust rendered preview output from the client.
- Generated artifacts inherit ADR-012 classification policy, path safety, sidecar events, and timeline expectations.
- F0027 proposal rendering may read F0019 quote/proposal packet facts but must not approve packets, transition submissions, compute pricing, or alter bind readiness.

| Role | read active bundles | resolve direct bundle by id | activate / deprecate / retire |
|------|---------------------|-----------------------------|-------------------------------|
| Admin | ALLOW | ALLOW | ALLOW |
| Underwriter | ALLOW | ALLOW | DENY |
| DistributionUser | ALLOW | ALLOW | DENY |
| DistributionManager | ALLOW | ALLOW | DENY |
| RelationshipManager | ALLOW | ALLOW | DENY |
| ProgramManager | ALLOW | ALLOW | DENY |
| Coordinator | ALLOW | ALLOW | DENY |
| BrokerUser | DENY | DENY | DENY |
| MgaUser | DENY | DENY | DENY |
| ExternalUser | DENY | DENY | DENY |

**Constraints applying to all ALLOW decisions on Product Schema Registry:**

- Read responses filter to tenant-available active product versions. Direct bundle reads by id are allowed for authenticated internal users so historical rows pinned to Deprecated, Retired, or Internal bundles render correctly.
- Internal sentinel bundles (`_unspecified`, `_legacy_*`, `_bridge_*`) are never returned by active bootstrap listings.
- Activation requires bundle profile validation, OpenAPI projection compatibility, HMAC signature verification, deterministic UUID verification, and an append-only activation event.
- Activation failure must not partially update product-version status or served bundle content.
- Every product schema write operation is Admin-only in MVP; a future Product Steward role requires a separate authorization update.

---

## 7. Admin Configuration Console (F0032)

F0032 governs runtime business configuration through a central console. It does
not grant source-record access by itself; source modules still enforce their own
ABAC and redaction rules when validation or audit details reference underlying
queue, reporting, template, or workflow-SLA records.

| Role | read | draft | validate | publish | rollback | audit |
|------|------|-------|----------|---------|----------|-------|
| Admin | ALLOW | ALLOW | ALLOW | ALLOW | ALLOW | ALLOW |
| ConfigurationSteward | ALLOW | ALLOW | ALLOW | DENY | DENY | DENY |
| OperationsManager | ALLOW | DENY | ALLOW | DENY | DENY | DENY |
| ComplianceQualityLead | ALLOW | DENY | DENY | DENY | DENY | ALLOW |
| Underwriter | DENY | DENY | DENY | DENY | DENY | DENY |
| DistributionUser | DENY | DENY | DENY | DENY | DENY | DENY |
| DistributionManager | DENY | DENY | DENY | DENY | DENY | DENY |
| RelationshipManager | DENY | DENY | DENY | DENY | DENY | DENY |
| ProgramManager | DENY | DENY | DENY | DENY | DENY | DENY |
| BrokerUser | DENY | DENY | DENY | DENY | DENY | DENY |
| MgaUser | DENY | DENY | DENY | DENY | DENY | DENY |
| ExternalUser | DENY | DENY | DENY | DENY | DENY | DENY |

Constraints:

- Publish and rollback are Admin-only, even when a steward created or validated the draft.
- Validation details are redacted unless the caller also has read access to the source module and domain.
- Audit access never exposes hidden record counts, restricted payload fields, or source-record details that the caller cannot otherwise read.
- Every successful or failed mutation appends `ConfigurationAuditEvent`; successful mutations also append `ActivityTimelineEvent`.

## 8. Open Questions

None.
