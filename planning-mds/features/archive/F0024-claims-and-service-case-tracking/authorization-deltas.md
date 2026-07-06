# F0024 Authorization Deltas

Plan run: `2026-07-03-f0024-phase-b`
Owner: Architect
Status: Drafted for G4/G5 approval

## Resources And Actions

Resource: `service_case`

- `create`: create CRM-side service cases from account or policy context.
- `read`: read service-case workspace, context lists, detail, and audit history.
- `update`: update mutable servicing fields.
- `assign`: change service-case owner.
- `transition`: move service case through approved servicing statuses.
- `update_claim_reference`: create or update claim-reference context.
- `link_communication`: link an existing communication event to the service case.
- `create_follow_up`: create a follow-up task from the service case.

## Role Decisions

| Role | create/read/update/transition/link_communication/create_follow_up | assign | update_claim_reference | Notes |
|------|-------------------------------------------------------------------|--------|------------------------|-------|
| DistributionUser | ALLOW | DENY | ALLOW | Must pass account/policy read scope; owner is self or assigned queue in MVP. |
| DistributionManager | ALLOW | ALLOW | ALLOW | Region scope applies through account/policy records. |
| Underwriter | ALLOW | DENY | ALLOW | Policy access scope applies through linked policy/account records. |
| RelationshipManager | ALLOW | DENY | ALLOW | Broker/account relationship scope applies through account/policy records. |
| ProgramManager | ALLOW | DENY | ALLOW | Program scope applies through linked policy/account records. |
| Admin | ALLOW | ALLOW | ALLOW | Unscoped internal authority; actions remain audited. |
| BrokerUser | DENY | DENY | DENY | External service-case self-service is out of MVP scope. |
| ExternalUser | DENY | DENY | DENY | No external self-service in MVP. |

## Constraints

- Every create/read/update/assign/transition/follow-up decision must also pass read access on the linked account and optional policy.
- If a policy is supplied, the policy must belong to the supplied account.
- Communication linking must also pass `communication_event:read` and read access to at least one linked communication entity.
- Follow-up task creation must pass existing `task:create` rules and assignee validation.
- Claim-reference updates store reference context only; they do not authorize coverage decisions, claim adjudication, reserves, payments, or carrier sync.
- Closed service cases are immutable except for audit reads.
- BrokerUser and ExternalUser receive no policy lines for service-case access in MVP.
