# F0024 Phase B Architecture Plan

Plan run: `2026-07-03-f0024-phase-b`
Owner: Architect
Status: Drafted for G4/G5 approval

## Decision Summary

F0024 introduces `ServiceCase` as the durable CRM-side record for customer service requests and claim-support coordination. The service case owns internal servicing workflow, ownership, priority, due date, follow-up, and audit history. Claim information is stored only as reference context; Nebula does not adjudicate claims, calculate reserves, manage payments, or replace carrier claim systems in this feature.

Service cases can be created from account and policy context, linked to existing communication events, linked to follow-up tasks, and projected into `ActivityTimelineEvent` for account and policy 360 visibility. The append-only `ServiceCaseTransition` history is the source of servicing workflow evidence.

## Service Boundaries

- Service-case persistence and workflow live in the CRM API/application layer beside policy, communication, task, and timeline services.
- Policy services remain the owner of policy lifecycle and policy timeline reads. F0024 reads policy/account context but does not mutate policy state.
- Communication services remain the owner of `CommunicationEvent`; F0024 stores service-case links to existing communications and requires communication read access.
- Task services remain the owner of task persistence. F0024 follow-up creation calls existing task creation behavior and stores a service-case/task link.
- Timeline services read projected `ActivityTimelineEvent` rows and do not own service-case mutation behavior.
- Authorization combines `service_case:*` decisions with account/policy read scope, communication read scope when linking communications, and task create scope when creating follow-up tasks.

## Data Model Deltas

`ServiceCase`
- Source record for servicing requests and claim-support coordination.
- Required fields: `id`, `caseNumber`, `accountId`, `summary`, `type`, `status`, `priority`, `ownerUserId`, `createdByUserId`, `createdAt`.
- Optional fields: `policyId`, `description`, `dueDate`, `followUpSummary`, `resolvedAt`, `closedAt`, `resolutionSummary`, `updatedAt`, `rowVersion`.
- `caseNumber` is unique and human-readable. It is not reused after close or delete.

`ServiceCaseClaimReference`
- Optional 1:1 child record containing claim-reference context only.
- Fields: `serviceCaseId`, `carrierClaimNumber`, `dateOfLoss`, `claimantDisplayName`, `lossSummary`, `carrierContactReference`, `updatedByUserId`, `updatedAt`.
- Does not store reserve, payment, coverage determination, adjudication status, or carrier-system credentials.

`ServiceCaseCommunicationLink`
- Bridge between `ServiceCase` and `CommunicationEvent`.
- Required fields: `serviceCaseId`, `communicationEventId`, `linkType`, `createdByUserId`, `createdAt`.
- The linked communication remains owned by F0021.

`ServiceCaseTaskLink`
- Bridge between `ServiceCase` and `TaskItem`.
- Required fields: `serviceCaseId`, `taskId`, `relationship`, `createdByUserId`, `createdAt`.
- The linked task remains owned by F0004.

`ServiceCaseTransition`
- Append-only workflow evidence for status changes.
- Required fields: `serviceCaseId`, `fromStatus`, `toStatus`, `actorUserId`, `occurredAt`.
- Optional fields: `reasonCode`, `note`.

## Workflow Rules

Allowed service-case statuses:
- `Intake`
- `InProgress`
- `Waiting`
- `Resolved`
- `Closed`

Allowed transitions:
- `Intake -> InProgress`
- `Intake -> Waiting`
- `InProgress -> Waiting`
- `InProgress -> Resolved`
- `Waiting -> InProgress`
- `Waiting -> Resolved`
- `Resolved -> Closed`

Closed service cases are read-only in MVP except for audit reads. Reopen behavior is out of F0024 MVP scope and requires a later ADR or Phase B amendment.

Every create, update, claim-reference update, communication link, follow-up task creation, and status transition emits an `ActivityTimelineEvent` projection for the account and, when present, policy context.

## API And Schema Deltas

Canonical endpoint and schema deltas are captured in `api-schema-deltas.md` and the new `service-case*.schema.json` files. The OpenAPI file should be updated during the feature action from those approved deltas, matching the F0021 planning pattern.

## Authorization

Canonical authorization decisions are captured in `authorization-deltas.md`. F0024 adds `service_case:create/read/update/assign/transition/update_claim_reference/link_communication/create_follow_up` policy semantics and reuses linked account/policy read checks. BrokerUser and ExternalUser are denied for MVP service-case access.

## Frontend Surfaces

- Account 360 service-case list and create action.
- Policy 360 service-case list and create action.
- Service-case detail view with owner, priority, due date, status, claim-reference context, linked communications, linked tasks, and audit history.
- Workspace service-case list filtered by assignee, priority, due date, status, account, and policy.
- Follow-up task creation from a saved service case.

## Required Signoff Roles

- Architect: required.
- Quality Engineer: required for persistence, reload, workflow, timeline, and permission test coverage.
- Code Reviewer: required for service-case/source integration with policy, communication, task, and timeline contracts.
- Security Reviewer: required because claim-reference context can include sensitive claim and claimant information.
- DevOps: required because implementation adds new persisted entities, migrations, indexes, and seed/policy updates.
- AI Engineer: not required; F0024 has no AI scope.

## Feature Action Handoff

The separate feature action must begin with G0 and create `feature-assembly-plan.md`. This Phase B plan is not an implementation authorization by itself.
