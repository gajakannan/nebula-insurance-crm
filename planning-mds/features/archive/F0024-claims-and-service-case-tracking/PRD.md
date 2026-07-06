---
template: feature-prd
version: 2.0
applies_to: product-manager
---

# F0024: Claims & Service Case Tracking

**Feature ID:** F0024
**Feature Name:** Claims & Service Case Tracking
**Priority:** Medium
**Phase:** CRM Release MVP+
**Status:** In Progress — Drift Reconciliation

## Feature Statement

**As a** service, relationship, or underwriting user
**I want** lightweight service cases and claim-reference context linked to accounts and policies
**So that** Nebula supports post-bind customer service without becoming a full claims-management platform

## Business Objective

- **Goal:** Extend Nebula into post-bind servicing context for policies and accounts.
- **Metric:** Service cases created, overdue follow-ups visible, and account/policy records with service context available from 360 views.
- **Baseline:** Claims and service issues are outside Nebula or hidden in unstructured communication history.
- **Target:** Users can create, view, own, transition, and audit service cases from account and policy context while reusing completed Policy 360 and Communication Hub foundations.

## Problem Statement

- **Current State:** Nebula has first-class accounts, policies, timelines, and communication capture, but no structured way to track post-bind service issues or claim-reference context.
- **Desired State:** Users can track service cases beside account and policy context, preserve claim-reference details, link communications/follow-ups, and audit status changes.
- **Impact:** Better customer continuity after binding, fewer missed service follow-ups, and stronger account/policy context for servicing and relationship users.

## Scope & Boundaries

**In Scope (MVP+):**
- Service case intake from account and policy 360 surfaces.
- Service case list/detail view with account, policy, owner, priority, due date, status, and optional claim-reference fields.
- Case ownership, priority, due-date, and follow-up management.
- Case status workflow: Intake, In Progress, Waiting, Resolved, Closed.
- Communication Hub linkage for notes, related communication events, and follow-up tasks.
- Claim-reference context such as carrier claim number, date of loss, claimant/contact reference, loss summary, and carrier contact reference.
- Timeline/audit entries for case creation, ownership changes, priority/due-date changes, claim-reference changes, and status transitions.

**Out of Scope:**
- Full claims adjudication or coverage determination.
- Payments, reserves, recoveries, or financial accounting.
- Carrier claims platform replacement.
- External carrier claim synchronization or claim feed ingestion.
- Legal diary management, litigation management, or adjuster assignment marketplace.

## Acceptance Criteria Overview

- [ ] Authorized internal users can create a service case from account or policy context and see it after reload.
- [ ] Users can view service cases from account 360, policy 360, and a service-case workspace.
- [ ] Authorized users can update owner, priority, due date, and follow-up details with audit evidence.
- [ ] Authorized users can transition cases through the approved status workflow and cannot use invalid transitions.
- [ ] Users can attach claim-reference context without treating Nebula as the carrier claim system of record.
- [ ] Communication events and follow-up tasks can be linked to a service case using F0021/F0004 foundations.
- [ ] Sensitive service and claim-adjacent content follows account/policy authorization boundaries.

## UX / Screens

| Screen | Purpose | Key Actions |
|--------|---------|-------------|
| Service Cases Workspace | Operational list of open, overdue, waiting, and recently closed cases | Search, filter, open case, create case |
| Add Service Case Drawer | Capture the first structured case from account or policy context | Enter summary, type, priority, owner, due date, claim reference, save |
| Service Case Detail | Work a case through ownership, follow-up, status, and communication context | Update fields, transition status, link communication, create follow-up |
| Account 360 Service Cases Rail | Show account-level service context | View open cases, create case, open detail |
| Policy 360 Service Cases Rail | Show policy-specific servicing and claim-reference context | View linked cases, create case, open detail |

**Key Workflows:**
1. **Case intake from context** — Account/policy detail to Add Service Case drawer to saved case visible in context.
2. **Daily service triage** — Service workspace filter to overdue/open cases, open detail, update owner/due date/follow-up.
3. **Case status progression** — Intake to In Progress or Waiting, then Resolved and Closed with audit evidence.
4. **Claim-reference enrichment** — Add carrier claim number/date-of-loss/reference details to a case while preserving carrier-system boundary.
5. **Communication follow-up** — Link an existing communication event or create a follow-up task from the case detail.

## Screen Layouts (ASCII)

### Service Cases Workspace — Desktop

```text
+----------------------------------------------------------------------------+
| Service Cases                                  [Create Service Case]         |
+----------------------------------------------------------------------------+
| Search [case, account, policy, claim ref]  Status [Open v] Priority [All v] |
| Owner [Mine v]  Due [Overdue + Next 7 days v]                               |
+----------------------------------------------------------------------------+
| Case # | Summary                  | Account       | Policy | Status | Due    |
| SC-104 | Water damage follow-up   | Acme Foods    | P-1007 | Waiting| Jul 08 |
| SC-105 | COI request after bind   | Metro Builders| P-1021 | Intake | Jul 05 |
| SC-106 | Claim ref update needed  | Northwind     | P-0882 | Active | Jul 10 |
+----------------------------------------------------------------------------+
```

### Service Case Detail — Desktop

```text
+----------------------------------------------------------------------------+
| SC-104 Water damage follow-up                         Status [Waiting v]    |
| Account: Acme Foods      Policy: P-1007      Owner [J. Lee v] Due [Jul 08]  |
+----------------------------------------------------------------------------+
| Summary / Claim Reference                                                  |
| Type [Claim support v] Priority [High v] Carrier Claim # [CLM-77812]       |
| Date of loss [2026-06-28] Carrier contact [Adjuster name / reference]      |
+----------------------------------------------------------------------------+
| Communications & Follow-up                         [Link Communication]     |
| Jul 02 Call with broker · Follow-up task open                              |
| Jun 30 Email ref · Carrier acknowledged claim number                       |
+----------------------------------------------------------------------------+
| Timeline / Audit                                                           |
| Created · Owner changed · Claim reference updated · Waiting for carrier    |
+----------------------------------------------------------------------------+
```

### Account / Policy Service Rail — Narrow

```text
+------------------------------+
| Service Cases                |
| [Add] [View all]             |
+------------------------------+
| High · Waiting               |
| Water damage follow-up       |
| Due Jul 08 · Owner J. Lee    |
| [Open]                       |
+------------------------------+
| Intake                       |
| COI request after bind       |
| Due Jul 05                   |
| [Open]                       |
+------------------------------+
```

## Data Requirements

**Core Entities:**
- Service Case: service issue or claim-adjacent work item linked to account and optionally policy.
- Claim Reference: optional carrier claim context stored as reference data, not adjudication truth.
- Case Timeline Event: audit/progress event for creation, updates, and transitions.

**Validation Rules:**
- Account link is required.
- Policy link is optional, but when present must belong to the selected account.
- Summary, type, priority, owner, status, and due date are required for active cases.
- Carrier claim number is optional and reference-only.
- Closed cases are read-only in MVP; reopen behavior is out of scope until a later ADR or Phase B amendment approves it.

**Data Relationships:**
- Account -> Service Case: required ownership/context relationship.
- Policy -> Service Case: optional policy-specific context relationship.
- Service Case -> Communication Event: optional related communication history.
- Service Case -> Task: optional follow-up task linkage.

## Role-Based Access

| Role | Access Level | Notes |
|------|--------------|-------|
| Underwriter | Create / Read / Update / Transition | Subject to account/policy visibility |
| Distribution User | Create / Read / Update own or assigned cases | Subject to account/policy visibility |
| Distribution Manager | Create / Read / Update / Reassign / Transition | Within managed scope |
| Relationship Manager | Create / read / update / transition where account scope allows | Cross-user assignment denied in MVP |
| Admin | Administrative read/update for correction and support | Must preserve audit trail |
| BrokerUser / ExternalUser | No access in MVP+ | External servicing portal remains out of scope |

## Success Criteria

- A service user can create a case from an account or policy and confirm persistence after reload.
- A manager can find overdue/open cases by owner, status, priority, and due date.
- A user can add claim-reference context without creating financial/reserve/adjudication data.
- A user can link communications and follow-up tasks from the case detail.
- Every mutation produces audit/timeline evidence.

## Risks & Assumptions

- **Risk:** The feature expands into a claims platform. **Mitigation:** Keep all claim fields reference-only and exclude reserves, payments, coverage determination, and carrier sync.
- **Risk:** Sensitive claim-adjacent content leaks through cross-links. **Mitigation:** Anchor access to account/policy authorization and require security review in build.
- **Risk:** Service workflow duplicates F0022 queue/routing scope. **Mitigation:** Provide owner/due/status basics only; advanced queue rules remain with F0022.
- **Assumption:** Internal users need lightweight CRM servicing context before external collaboration or carrier integration.

## Dependencies

- F0016 Account 360 & Insured Management — completed account context.
- F0018 Policy Lifecycle & Policy 360 — completed policy anchor.
- F0021 Communication Hub & Activity Capture — completed communication and follow-up foundation.
- F0004 Task Center UI + Manager Assignment — completed task/follow-up foundation.

## Related Stories

| Story | Title | Cluster |
|-------|-------|---------|
| [F0024-S0001](./F0024-S0001-create-service-case-from-context.md) | Create a service case from account or policy context | Intake |
| [F0024-S0002](./F0024-S0002-view-service-cases-in-context.md) | View service cases in workspace and 360 context | Visibility |
| [F0024-S0003](./F0024-S0003-manage-service-case-ownership-and-follow-up.md) | Manage service case ownership, priority, and follow-up | Work management |
| [F0024-S0004](./F0024-S0004-transition-service-case-status.md) | Transition a service case through servicing statuses | Workflow |
| [F0024-S0005](./F0024-S0005-capture-claim-reference-context.md) | Capture claim-reference context on a service case | Claim context |
| [F0024-S0006](./F0024-S0006-audit-and-permission-safe-service-case-history.md) | Audit and permission-safe service case history | Governance |

## Rollout & Enablement

- Start with internal users only.
- Treat all external/broker-facing servicing visibility as future scope.
- Phase B drafted exact API, schema, authorization, and data-model contracts on 2026-07-03; G4/G5 approval is required before build.
