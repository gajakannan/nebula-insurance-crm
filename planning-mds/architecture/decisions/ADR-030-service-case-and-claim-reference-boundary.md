# ADR-030: Service Case And Claim Reference Boundary

Status: Proposed
Date: 2026-07-03
Owner: Architect

## Context

F0024 needs a CRM-side way to track customer service requests and claim-support coordination from account and policy context. Existing task, communication, policy, and timeline records are necessary related records, but none of them should become the durable source of servicing workflow state.

The feature also needs limited claim-reference context so teams can coordinate with carriers and insureds. That context must not turn Nebula into a claim adjudication, reserve, payment, or carrier claim-management system.

## Decision

Introduce `ServiceCase` as the source business record for CRM servicing and claim-support coordination. Service cases own internal status, ownership, priority, due date, follow-up summary, claim-reference child data, communication links, task links, and append-only transition history.

Claim data is limited to reference context: carrier claim number, date of loss, claimant display name, loss summary, and carrier contact reference. F0024 explicitly excludes reserves, payments, coverage determinations, claim adjudication state, carrier claim-system synchronization, and external self-service.

Every service-case mutation emits an `ActivityTimelineEvent` projection for account and optional policy 360 visibility. Status changes are recorded in append-only `ServiceCaseTransition` rows.

## Consequences

- Account and policy 360 views can show servicing context without mutating policy lifecycle state.
- Communication and task integrations remain references to their owning services, avoiding duplicate source records.
- Security Reviewer signoff is mandatory because claim-reference context can include sensitive claimant and loss information.
- BrokerUser and ExternalUser remain denied for MVP service-case access.
- Reopen behavior for closed service cases is out of MVP scope and requires a future ADR or Phase B amendment.
