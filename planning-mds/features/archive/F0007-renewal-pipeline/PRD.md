---
template: feature-prd
version: 2.0
applies_to: product-manager
---

# F0007: Renewal Pipeline

**Feature ID:** F0007
**Feature Name:** Renewal Pipeline
**Priority:** Critical
**Phase:** CRM Release MVP
**Status:** In Refinement

## Feature Statement

**As a** distribution user, underwriter, or distribution manager
**I want** a renewal pipeline with timing windows, outreach tracking, status management, and ownership handoff
**So that** expiring business is identified early, worked proactively, and retained more consistently

## Business Objective

- **Goal:** Make renewals visible and manageable before deadlines become urgent, with clear ownership at every stage.
- **Metric:** Renewals worked within 90/60/45-day windows, retention rate (Completed / total due), overdue renewal count trending toward zero.
- **Baseline:** Renewal follow-up is inconsistent, often too late, and lacks structured handoff between distribution and underwriting.
- **Target:** Every renewal is owned, tracked through a defined workflow, and advanced through Nebula with enough lead time to retain the account.

## Problem Statement

- **Current State:** Teams lack a dedicated renewal operating surface. Expiring policies are discovered ad hoc, outreach is untracked, and there is no structured handoff between distribution (who initiates outreach) and underwriting (who reviews and quotes).
- **Desired State:** Renewals are created from expiring policies, tracked through Identified → Outreach → InReview → Quoted → Completed/Lost, with configurable timing windows, ownership assignment, escalation flags, and full audit history.
- **Impact:** Better retention, less rush work, clearer accountability, and auditable renewal operations.

## Scope & Boundaries

**In Scope (MVP):**
- Renewal entity with lifecycle states: Identified, Outreach, InReview, Quoted, Completed, Lost
- Renewal list/pipeline view with due-window filtering (90/60/45 days, overdue)
- Renewal detail view with linked policy context, outreach history, and status
- Renewal status transitions with validation and gating rules
- Renewal ownership assignment and distribution-to-underwriting handoff
- Overdue renewal detection and escalation flag visibility
- Renewal creation from an expiring policy (boundary action consuming F0018 data)
- Append-only timeline and audit trail for all renewal activity
- Configurable renewal windows per Line of Business (LOB), following the WorkflowSlaThreshold pattern from ADR-009
- Dashboard integration: renewal-specific nudge card for overdue/approaching renewals

**Out of Scope (Future):**
- Carrier automation and automated quote generation
- Bulk renewal operations (mass reassign, mass advance)
- Automated renewal creation via Temporal scheduled workflows (MVP uses manual creation)
- External broker-facing renewal portal (deferred to F0029)
- Renewal-specific reporting and analytics (deferred to F0023)
- Queue-based routing of renewals (deferred to F0022)
- Renewal document attachment and management (deferred to F0020 integration)
- Predictive renewal risk scoring (future AI/Neuron capability)

## Scope Boundary Clarifications

| Capability | Owns | Delegates To | Boundary |
|------------|------|--------------|----------|
| Renewal entity, states, transitions | **F0007** | — | F0007 owns the Renewal aggregate and workflow |
| Policy data (expiration dates, terms, carrier) | — | **F0018** | F0007 reads policy data; F0018 is the authoritative source |
| Queue routing and automated assignment | — | **F0022** | F0007 supports manual assignment; F0022 adds rule-based routing later |
| Task creation linked to renewals | — | **F0003/F0004** | F0007 uses existing Task entity via LinkedEntityType=Renewal |
| Account and broker context | — | **F0016/F0002** | F0007 reads account and broker data; does not own those entities |
| Renewal timing/SLA thresholds | **F0007** | — | F0007 extends WorkflowSlaThreshold (ADR-009) with renewal-specific entries |
| Communication and outreach notes | — | **F0021** | MVP: outreach tracked via timeline events. F0021 adds structured communication later |

## Personas

### Distribution User (Primary)
- **Renewal job-to-be-done:** Identify upcoming renewals, initiate outreach to brokers/accounts, gather updated information, and advance renewals to underwriting review.
- **Key pain:** Discovers expiring policies too late, no structured view of what needs outreach, no way to prove outreach was done.
- **Success:** Can see all renewals in their book, filter by due window, initiate outreach, and hand off to underwriting with context.

### Underwriter
- **Renewal job-to-be-done:** Review renewals that have been through outreach, evaluate updated risk information, provide quotes, and advance to Completed or Lost.
- **Key pain:** Receives renewals without context on prior outreach, unclear what information has been gathered, no structured handoff.
- **Success:** Receives renewals in InReview with full outreach history, can advance through Quoted → Completed with audit trail.

### Distribution Manager
- **Renewal job-to-be-done:** Monitor team renewal pipeline health, identify overdue renewals, reassign ownership, and ensure nothing falls through the cracks.
- **Key pain:** No visibility into team-wide renewal status, cannot identify which renewals are stuck or unworked, no escalation mechanism.
- **Success:** Can see all team renewals, filter by overdue/approaching, reassign ownership, and track escalation resolution.

## Renewal Workflow

### States

| State | Description | Owner Role | Entry Condition |
|-------|-------------|------------|-----------------|
| **Identified** | Renewal created from expiring policy; not yet worked | Distribution User | Created from expiring policy |
| **Outreach** | Distribution has initiated broker/account contact | Distribution User | At least one outreach activity logged |
| **InReview** | Underwriting is reviewing the renewal | Underwriter | Handoff from distribution; renewal has outreach context |
| **Quoted** | Quote has been prepared and shared | Underwriter | Quote details attached or referenced |
| **Completed** | Renewal successfully bound; linked to new policy or submission | — (terminal) | Must link to a bound policy or renewal submission |
| **Lost** | Renewal not retained | — (terminal) | Must include a reason code |

### Allowed Transitions

```
Identified → Outreach
Outreach   → InReview
InReview   → Quoted
InReview   → Lost
Quoted     → Completed
Quoted     → Lost
```

### Transition Rules

- Invalid transition pairs return HTTP 409 (`code=invalid_transition`).
- `Identified → Outreach`: Actor must be Distribution User or Distribution Manager.
- `Outreach → InReview`: Actor must be Distribution User, Distribution Manager, or Underwriter. Represents handoff to underwriting.
- `InReview → Quoted`: Actor must be Underwriter.
- `InReview → Lost`: Actor must be Underwriter. Requires `reasonCode`.
- `Quoted → Completed`: Actor must be Underwriter. Requires `boundPolicyId` or `renewalSubmissionId`.
- `Quoted → Lost`: Actor must be Underwriter. Requires `reasonCode`.
- Every transition appends one WorkflowTransition record and one ActivityTimelineEvent record.

### Lost Reason Codes

Standard values (validated at API layer, not DB enum):
- `NonRenewal` — Carrier or insured chose not to renew
- `CompetitiveLoss` — Account moved to another broker/carrier
- `BusinessClosed` — Insured business no longer operating
- `CoverageNoLongerNeeded` — Insured no longer requires this coverage
- `PricingDeclined` — Quote terms unacceptable to insured
- `Other` — Free-text reason required in `reasonDetail`

## Renewal Timing Windows

Renewal windows are configurable per Line of Business using the `WorkflowSlaThreshold` pattern from ADR-009. Each LOB can define different target outreach dates relative to policy expiration.

| Window | Definition |
|--------|------------|
| **90-day** | Policy expires within 90 days; earliest standard outreach window |
| **60-day** | Policy expires within 60 days; outreach should be underway |
| **45-day** | Policy expires within 45 days; renewal should be advancing toward InReview |
| **Overdue** | Current date is past the LOB-specific target outreach date AND renewal is still in Identified state |

Default LOB thresholds (seed data, configurable per deployment):

| LOB | Outreach Target (days before expiry) | Warning (days before expiry) |
|-----|---------------------------------------|------------------------------|
| Property | 90 | 60 |
| GeneralLiability | 90 | 60 |
| WorkersCompensation | 120 | 90 |
| ProfessionalLiability | 90 | 60 |
| Cyber | 60 | 45 |
| Default (all others) | 90 | 60 |

**Overdue definition:** A renewal is overdue when `current_date > (policy_expiration_date - outreach_target_days)` AND the renewal has not transitioned beyond Identified.

## Acceptance Criteria Overview

- [ ] Users can view a filterable renewal pipeline list with due-window and overdue indicators
- [ ] Users can view renewal detail with linked policy, account, broker, and outreach history
- [ ] Renewals transition through Identified → Outreach → InReview → Quoted → Completed/Lost with validation
- [ ] Lost transitions require a reason code; Completed transitions require a bound policy or submission link
- [ ] Distribution users can assign/reassign renewal ownership; handoff to underwriting is explicit
- [ ] Overdue renewals are visually flagged and filterable across the pipeline
- [ ] Renewals are created from expiring policies with a 1:1 link
- [ ] All renewal transitions and activities are recorded in the append-only timeline
- [ ] Renewal timing windows are configurable per LOB via seed data

## UX / Screens

| Screen | Purpose | Key Actions |
|--------|---------|-------------|
| Renewal Pipeline List | Primary operating view for renewals | Filter by due window (90/60/45/overdue), status, owner, LOB, broker, account; sort by expiration date; paginate |
| Renewal Detail | Full context for a single renewal | View linked policy, account, broker; view outreach timeline; advance status; assign/reassign owner; log activity |
| Dashboard — Renewal Nudge Card | Surface overdue/approaching renewals on the dashboard | Shows count of overdue + approaching renewals; click navigates to filtered pipeline list |

**Key Workflows:**

1. **Renewal Outreach Flow** — Distribution user opens pipeline list → filters to 90-day window → selects a renewal in Identified → views detail with policy context → logs outreach activity → transitions to Outreach → continues working through the window.

2. **Renewal Handoff Flow** — Distribution user advances renewal to InReview → underwriter sees it in their filtered view → reviews outreach history and policy context → prepares quote → transitions to Quoted → advances to Completed (with bound policy link) or Lost (with reason code).

3. **Overdue Escalation Flow** — Distribution manager opens pipeline list → filters to Overdue → sees flagged renewals stuck in Identified → reassigns ownership or contacts the assigned user → monitors until resolved.

## Data Requirements

**Core Entity: Renewal**
- `Id` (uuid, PK): Unique renewal identifier
- `AccountId` (uuid, FK → Account): The insured account
- `BrokerId` (uuid, FK → Broker): The broker of record
- `PolicyId` (uuid, FK → Policy): The expiring policy (required, 1:1 link)
- `CurrentStatus` (string): Current workflow state
- `LineOfBusiness` (string, nullable): LOB classification (per ADR-009)
- `PolicyExpirationDate` (date): Expiration date of the linked policy
- `TargetOutreachDate` (date): Calculated from expiration date minus LOB outreach target days
- `AssignedToUserId` (uuid, FK → UserProfile): Current owner
- `LostReasonCode` (string, nullable): Required when status = Lost
- `LostReasonDetail` (string, nullable): Free-text detail when reason = Other
- `BoundPolicyId` (uuid, nullable, FK → Policy): Link to renewed/bound policy when Completed
- `RenewalSubmissionId` (uuid, nullable, FK → Submission): Link to renewal submission when Completed
- `CreatedAt`, `CreatedByUserId`, `UpdatedAt`, `UpdatedByUserId`, `IsDeleted`: Standard base entity fields

**Validation Rules:**
- `PolicyId` is required and must reference a valid, non-deleted policy
- A policy may have at most one active (non-deleted, non-terminal) renewal
- `AccountId` and `BrokerId` must match the linked policy's account and broker
- `LostReasonCode` is required when transitioning to Lost
- `BoundPolicyId` or `RenewalSubmissionId` is required when transitioning to Completed
- `LineOfBusiness` validated against known LOB values when provided

**Data Relationships:**
- Renewal → Policy: Required 1:1 (one active renewal per expiring policy)
- Renewal → Account: Required N:1 (via policy)
- Renewal → Broker: Required N:1 (via policy)
- Renewal → UserProfile (AssignedTo): Required N:1
- Renewal → Task: Optional 1:N (via LinkedEntityType=Renewal on Task)

## Role-Based Access

| Role | Access Level | Notes |
|------|-------------|-------|
| Distribution User | Create, Read (own + team), Update, Transition (Identified→Outreach, Outreach→InReview) | Can assign to self; cannot assign to others unless Distribution Manager |
| Distribution Manager | Create, Read (all), Update, Transition (Identified→Outreach, Outreach→InReview), Reassign | Full pipeline visibility; can reassign any renewal |
| Underwriter | Read (assigned + team), Transition (InReview→Quoted, InReview→Lost, Quoted→Completed, Quoted→Lost) | Owns underwriting stages; read access to outreach history |
| Relationship Manager | Read (own accounts/brokers) | Read-only visibility for account/broker context |
| Admin | Full CRUD + all transitions | Administrative override |

**Data Visibility:**
- InternalOnly: All renewal data is internal-only in MVP (no external broker visibility)
- ABAC enforcement: Account region, broker scope, and assigned user visibility per existing Casbin policy patterns

## Success Criteria

- Distribution users can identify upcoming renewals before deadlines become critical
- Renewal work is assigned, visible, and auditable through every stage
- Handoff from distribution to underwriting is explicit and traceable
- Overdue renewals are immediately identifiable by managers
- Every renewal outcome (Completed or Lost) has required context (bound policy link or reason code)
- Renewal pipeline supports consistent outreach and quote preparation

## Risks & Assumptions

- **Risk:** Renewal scope becomes impossible to separate from policy lifecycle. **Mitigation:** F0007 reads policy data from F0018 but does not own policy CRUD. Boundary is explicit in scope table above.
- **Risk:** Overdue detection requires reliable policy expiration dates. **Mitigation:** F0018 owns policy data quality; F0007 treats `PolicyExpirationDate` as authoritative.
- **Assumption:** Policy records and account context will be available from F0018 and F0016 before F0007 implementation begins. Per the release plan, F0018 is sequenced before F0007.
- **Assumption:** Manual renewal creation is sufficient for MVP. Automated creation from expiring policies (via Temporal) is Future scope.
- **Assumption:** LOB values on Renewal follow the same known set defined in ADR-009.

## Dependencies

- **F0018 Policy Lifecycle & Policy 360** — Provides policy entity with expiration dates, terms, and carrier data. F0007 reads policy data; does not own it.
- **F0016 Account 360 & Insured Management** — Provides account context displayed in renewal detail view.
- **F0002 Broker & MGA Relationship Management** — Provides broker context displayed in renewal detail view. Already complete.
- **F0003/F0004 Task Center** — Provides task creation linked to renewals via LinkedEntityType. Already complete.
- **F0022 Work Queues** (soft dependency, Future) — Will add rule-based queue routing for renewals. F0007 MVP uses manual assignment only.

## Architecture & Solution Design

### Solution Components

- Introduce a `Renewal` aggregate or renewal application layer that links expiring policies to renewal work ownership, due windows, outcomes, and related submissions.
- Add a renewal orchestration component for reminder scheduling and escalation timing instead of embedding date math in UI code or ad hoc cron jobs.
- Provide renewal worklist and pipeline projections optimized for due-date windows, ownership, and escalation visibility.
- Keep renewal submission creation as a boundary action so the renewal module can trigger downstream intake or quoting work without duplicating those modules.

### Data & Workflow Design

- Model renewal lifecycle states explicitly: `Identified`, `Outreach`, `InReview`, `Quoted`, `Completed`, `Lost`.
- Store policy expiration date, target outreach dates, renewal owner, and last-touch timestamps as first-class fields because they drive SLA and escalation behavior.
- Use Temporal for long-running reminder and escalation workflows, with workflow IDs stored for correlation to the renewal record.
- Record all renewal transitions and reminder actions in append-only audit history so the team can explain why a renewal was or was not advanced on time.

### API & Integration Design

- Expose renewal list/detail/transition endpoints plus filtered views by due window, status, owner, and overdue condition.
- Consume policy lifecycle data from F0018 as the authoritative source for effective dates, expiration dates, and policy relationships.
- Allow renewal workflows to emit tasks, notifications, or queue handoff signals while keeping external carrier automation out of scope for this release.
- Design the renewal workflow so scheduled reminders remain stable across restarts, retries, and deploys.

### Security & Operational Considerations

- Apply authorization based on account, broker, territory, and assigned team visibility, not only on the renewal owner field.
- Add observability for Temporal workflow execution, reminder delivery failures, overdue renewals, and escalation counts.
- Make reminder and escalation activities idempotent so retried workflows do not spam users or create duplicate tasks.
- Index renewal queries on expiration date, owner, and status because list performance is central to the operating model.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | Renewal aggregate, due-window worklists, and escalation handling | PRD only |
| Introduces: Cross-Cutting Component | Durable workflow orchestration for reminder scheduling and escalations | [ADR-010](../../architecture/decisions/ADR-010-temporal-durable-workflow-orchestration.md) (Proposed) |
| Introduces/Standardizes: Cross-Cutting Pattern | Renewal state machine with append-only transition and reminder audit history | [ADR-011](../../architecture/decisions/ADR-011-crm-workflow-state-machines-and-transition-history.md) (Proposed) |
| Extends: Configuration Entity | Renewal-specific WorkflowSlaThreshold entries for per-LOB timing windows | [ADR-009](../../architecture/decisions/ADR-009-lob-classification-and-sla-configuration.md) (Accepted) |

## Related User Stories

Stories are colocated in this feature folder as `F0007-S{NNNN}-{slug}.md`.

- [F0007-S0001](./F0007-S0001-renewal-pipeline-list-with-due-window-filtering.md) — Renewal pipeline list with due-window filtering
- [F0007-S0002](./F0007-S0002-renewal-detail-view-with-policy-context.md) — Renewal detail view with policy context and outreach history
- [F0007-S0003](./F0007-S0003-renewal-status-transitions.md) — Renewal status transitions
- [F0007-S0004](./F0007-S0004-renewal-ownership-assignment-and-handoff.md) — Renewal ownership assignment and handoff
- [F0007-S0005](./F0007-S0005-overdue-renewal-visibility-and-escalation-flags.md) — Overdue renewal visibility and escalation flags
- [F0007-S0006](./F0007-S0006-create-renewal-from-expiring-policy.md) — Create renewal from expiring policy
- [F0007-S0007](./F0007-S0007-renewal-activity-timeline-and-audit-trail.md) — Renewal activity timeline and audit trail

## Workflow State Note

This PRD defines the authoritative renewal workflow states as: Identified, Outreach, InReview, Quoted, Completed, Lost. These states replace the earlier placeholder lifecycle (Created, Early, OutreachStarted, InReview, Quoted, Bound, Lost, Lapsed) that existed in early baseline planning and runtime scaffolding.
