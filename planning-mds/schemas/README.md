# Shared JSON Schemas

This directory stores canonical JSON Schemas shared across frontend and backend.

Current baseline:
- `problem-details.schema.json` - RFC 7807 error contract with Nebula extension fields
- `broker.schema.json` - broker response contract
- `broker-create-request.schema.json` - broker creation payload
- `broker-update-request.schema.json` - broker update payload
- `broker-search-query.schema.json` - broker search query contract
- `paginated-broker-list.schema.json` - broker list response
- `contact.schema.json` - contact response contract
- `contact-create-request.schema.json` - contact creation payload
- `contact-update-request.schema.json` - contact update payload
- `dashboard-kpis.schema.json` - KPI widget response
- `dashboard-pipeline.schema.json` - pipeline summary response
- `line-of-business.schema.json` - canonical LOB enum shared by submission/renewal contracts
- `submission.schema.json` - submission response contract
- `submission-create-request.schema.json` - submission creation payload
- `submission-update-request.schema.json` - submission update payload
- `renewal.schema.json` - renewal response contract
- `renewal-create-request.schema.json` - renewal creation payload
- `renewal-update-request.schema.json` - renewal update payload
- `pipeline-status-count.schema.json` - pipeline status count item
- `pipeline-mini-card.schema.json` - pipeline mini-card item
- `opportunity-breakdown.schema.json` - grouped stage breakdown response
- `opportunity-breakdown-group.schema.json` - grouped stage breakdown item
- `nudge-card.schema.json` - nudge card response item
- `opportunity-aging.schema.json` - aging matrix response
- `opportunity-aging-status.schema.json` - aging status entry
- `opportunity-aging-sla.schema.json` - aging SLA object
- `opportunity-aging-bucket.schema.json` - aging bucket entry
- `opportunity-hierarchy.schema.json` - hierarchy response
- `opportunity-hierarchy-node.schema.json` - hierarchy node entry
- `task.schema.json` - task response contract
- `task-summary.schema.json` - dashboard task summary item
- `task-create-request.schema.json` - task creation payload
- `task-update-request.schema.json` - task update payload
- `timeline-event.schema.json` - activity timeline event response
- `distribution-node.schema.json` - F0017 distribution hierarchy node response
- `distribution-node-parent-request.schema.json` - F0017 set/clear parent payload
- `distribution-node-ancestors-response.schema.json` - F0017 ancestor breadcrumb response
- `paginated-distribution-node-list.schema.json` - F0017 descendant list response
- `producer-ownership.schema.json` - F0017 effective-dated producer ownership period
- `producer-ownership-assignment-request.schema.json` - F0017 producer ownership assignment payload
- `producer-ownership-lookup-response.schema.json` - F0017 point-in-time ownership lookup response
- `territory.schema.json` - F0017 territory response
- `territory-create-request.schema.json` - F0017 territory creation payload
- `territory-assignment.schema.json` - F0017 effective-dated territory assignment period
- `territory-member-assignment-request.schema.json` - F0017 territory member assignment payload
- `paginated-territory-assignment-list.schema.json` - F0017 territory member list response
- `territory-assignment-lookup-response.schema.json` - F0017 point-in-time territory assignment lookup response
- `generated-document-request.schema.json` - F0027 outbound generation source/template selector
- `generated-document-preview-response.schema.json` - F0027 ephemeral preview diagnostics and preview URL response
- `generated-document-issue-response.schema.json` - F0027 issued generated artifact response

Use these files as the source of truth for validation, OpenAPI alignment, and generated types.

## API schemas without JSON Schema files (intentional)

The following `components/schemas` entries in `nebula-api.yaml` do **not** have standalone JSON Schema
files because they are outside F0001/F0002 MVP implementation scope. They are read-only reference entities
or workflow entities that will gain schemas when their future features (F0005–F0008) are specified:

- `Account`, `MGA`, `Program` — reference lookups only; no create/update in MVP
- `WorkflowTransitionRequest`, `WorkflowTransitionRecord` — workflow payloads; schemas needed with F0006/F0007
- `PaginatedContactList` — follows the same structure as `PaginatedBrokerList`; add schema file when contact pagination testing begins
