# F0024 API And Schema Deltas

Plan run: `2026-07-03-f0024-phase-b`
Owner: Architect
Status: Drafted for G4/G5 approval

## Endpoints

### `POST /service-cases`

Creates a service case from account or policy context.

- Request schema: `ServiceCaseCreateRequest`
- Response schema: `ServiceCase`
- Authorization: `service_case:create` and read access to account and optional policy.
- Validation failures: missing account, inaccessible account/policy, policy not attached to account, invalid owner, invalid priority, invalid claim-reference fields.
- Persistence evidence: service case, optional claim reference, transition from null to `Intake`, and account/policy timeline projections are committed atomically.

### `GET /service-cases`

Lists service cases for workspace and contextual filters.

- Query: `accountId`, `policyId`, `ownerUserId`, `status`, `priority`, `dueBefore`, `page`, `pageSize`
- Response schema: `PaginatedServiceCaseList`
- Authorization: `service_case:read` plus linked account/policy read scope for every returned row.
- Sort: due date ascending with nulls last, then priority, then createdAt descending.

### `GET /service-cases/{serviceCaseId}`

Returns service-case detail.

- Response schema: `ServiceCase`
- Authorization: `service_case:read` plus linked account/policy read scope.
- Audit history includes service-case transitions and timeline references visible to the caller.

### `PATCH /service-cases/{serviceCaseId}`

Updates mutable servicing fields.

- Request schema: `ServiceCaseUpdateRequest`
- Response schema: `ServiceCase`
- Authorization: `service_case:update`; owner changes additionally require `service_case:assign`.
- Mutable fields: `summary`, `description`, `priority`, `ownerUserId`, `dueDate`, `followUpSummary`, `resolutionSummary`.
- Closed records reject mutation with RFC 7807 `closed_service_case`.

### `POST /service-cases/{serviceCaseId}/transition`

Transitions a service case through the servicing workflow.

- Request schema: `ServiceCaseTransitionRequest`
- Response schema: `ServiceCase`
- Authorization: `service_case:transition`.
- Validation failures: invalid transition, closed service case, missing resolution summary when moving to `Resolved`, stale row version.
- Persistence evidence: append-only `ServiceCaseTransition`, updated service-case status, and timeline projection.

### `PATCH /service-cases/{serviceCaseId}/claim-reference`

Creates or updates claim-reference context.

- Request schema: `ServiceCaseClaimReferenceUpdateRequest`
- Response schema: `ServiceCase`
- Authorization: `service_case:update_claim_reference`.
- Persistence evidence: claim-reference version update and timeline/audit projection.
- Exclusions: no reserves, payments, claim adjudication, carrier sync, or coverage decisions.

### `POST /service-cases/{serviceCaseId}/communication-links`

Links an existing communication event to the service case.

- Request schema: `ServiceCaseCommunicationLinkRequest`
- Response schema: `ServiceCase`
- Authorization: `service_case:link_communication`, `communication_event:read`, and linked entity read access on the communication.
- Persistence evidence: service-case communication link and timeline projection.

### `POST /service-cases/{serviceCaseId}/follow-up-task`

Creates a task linked to the service case.

- Request schema: `ServiceCaseFollowUpTaskRequest`
- Response schema: `Task`
- Authorization: `service_case:create_follow_up`, `task:create`, and linked account/policy read scope.
- Persistence evidence: task, service-case task link, task timeline event, and service-case timeline projection.

## Timeline Projection

F0024 extends timeline payload definitions with:

- `ServiceCaseCreated`
- `ServiceCaseUpdated`
- `ServiceCaseTransitioned`
- `ServiceCaseClaimReferenceUpdated`
- `ServiceCaseCommunicationLinked`
- `ServiceCaseFollowUpTaskCreated`

Timeline payloads reference the `serviceCaseId`, `caseNumber`, status, priority, owner, account id, optional policy id, and related communication/task ids when relevant. Claim-reference timeline payloads must avoid sensitive loss narrative bodies and should use summary-safe display text.

## Schema Files

- `planning-mds/schemas/service-case.schema.json`
- `planning-mds/schemas/service-case-create-request.schema.json`
- `planning-mds/schemas/service-case-update-request.schema.json`
- `planning-mds/schemas/service-case-transition-request.schema.json`
- `planning-mds/schemas/service-case-claim-reference-update-request.schema.json`
- `planning-mds/schemas/service-case-communication-link-request.schema.json`
- `planning-mds/schemas/service-case-follow-up-task-request.schema.json`
- `planning-mds/schemas/paginated-service-case-list.schema.json`
