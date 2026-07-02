# F0021 Authorization Deltas

Plan run: `2026-07-01-c1726908`
Owner: Architect
Status: Drafted for G4/G5 approval

## Resources And Actions

Resource: `communication_event`

- `create`: create structured notes, calls, meetings, or email references.
- `read`: read communication history and detail.
- `link`: associate communication with approved CRM records.
- `correct`: append correction metadata without deleting prior evidence.
- `redact`: mask sensitive communication content while preserving audit metadata.
- `create_follow_up`: create a follow-up task from a communication.

## Role Decisions

| Role | create/read/link/create_follow_up | correct | redact | Notes |
|------|-----------------------------------|---------|--------|-------|
| DistributionUser | ALLOW | ALLOW | DENY | Must also pass linked entity read scope. |
| DistributionManager | ALLOW | ALLOW | DENY | Region scope applies through linked entities. |
| Underwriter | ALLOW | ALLOW | DENY | Submission/policy scope applies through linked entities. |
| RelationshipManager | ALLOW | ALLOW | DENY | Broker/account relationship scope applies through linked entities. |
| ProgramManager | ALLOW | ALLOW | DENY | Program scope applies through linked entities. |
| Admin | ALLOW | ALLOW | ALLOW | Unscoped admin authority; redaction remains audited. |
| BrokerUser | DENY | DENY | DENY | External communication capture/history is out of MVP scope. |
| ExternalUser | DENY | DENY | DENY | No external self-service in MVP. |

## Constraints

- Every create/read/link/follow-up decision must also pass read access on the primary linked entity.
- Every additional linked entity must pass read access before persistence.
- Follow-up task creation must pass existing `task:create` rules and assignee validation.
- Redaction does not delete records; it masks user-facing content and emits audit/timeline evidence.
- Email-linked activity stores metadata/reference only; it does not authorize outbound send, mailbox read, or connector ingestion.
