# Nebula CRM — Error Codes (MVP)

**Purpose:** Single source of truth for ProblemDetails `code` values used in MVP.
**Scope:** Global API ProblemDetails `code` values.

## Usage

- Returned in RFC 7807 ProblemDetails `code` field.
- Status codes are included here for clarity; the HTTP response is authoritative.
- Error responses use `application/problem+json`.

## Status Code Policy

- `400`: malformed request or baseline validation failure.
- `401`: missing/invalid/expired authentication.
- `403`: authenticated but forbidden (policy or scope failure).
- `404`: resource absent or intentionally hidden.
- `409`: business/workflow conflict.
- `412`: failed precondition (for example `If-Match` mismatch).
- `422`: semantic/domain validation failure.
- `429`: rate limit exceeded.
- `500`: unexpected server failure.
- `503`: dependency/service unavailable.

## Codes

| Code | HTTP Status | Description | Source |
|---|---|---|---|
| `invalid_transition` | 409 | Workflow transition pair is not allowed. | `planning-mds/BLUEPRINT.md` |
| `missing_transition_prerequisite` | 409 | Required checklist/data missing for a transition. | `planning-mds/BLUEPRINT.md` |
| `active_dependencies_exist` | 409 | Broker deactivation blocked because active submissions or renewals are linked to the broker. | F0002-S0005 |
| `already_active` | 409 | Broker reactivation rejected because the broker is already in Active status. | F0002-S0008 |
| `region_mismatch` | 400 | Account.Region not in BrokerRegion set on submission/renewal creation. | `planning-mds/BLUEPRINT.md` |
| `concurrency_conflict` | 409 | Resource conflict detected outside precondition-header semantics. Client should refresh and retry. | `planning-mds/architecture/SOLUTION-PATTERNS.md` |
| `precondition_failed` | 412 | `If-Match` precondition failed because the resource version changed. | `planning-mds/architecture/api-guidelines-profile.md` |
| `duplicate_license` | 409 | Broker with the given license number already exists. Record must not be created. | F0002-S0001 |
| `not_found` | 404 | Requested resource does not exist or the caller lacks visibility into it (e.g., non-Admin viewing a deactivated broker). | All entity endpoints |
| `validation_error` | 400 | Request payload failed schema validation. Response includes `errors` map. | `planning-mds/architecture/SOLUTION-PATTERNS.md` §3 |
| `policy_denied` | 403 | Authenticated caller lacks authorization for the resource/action. | Authorization matrix + policy.csv |
| `invalid_status_transition` | 409 | Task status transition is not allowed by the state machine (e.g. Open → Done). | F0003-S0002 |
| `broker_scope_unresolvable` | 403 | Broker scope could not be resolved from `broker_tenant_id` (missing/unknown/ambiguous). | F0009 contract |
| `duplicate_renewal` | 409 | An active (non-deleted, non-terminal) renewal already exists for this policy. | F0007-S0006 |
| `invalid_account` | 400 | Referenced account does not exist or is soft-deleted. | F0006-S0002 |
| `invalid_broker` | 400 | Referenced broker does not exist, is soft-deleted, or is inactive. | F0006-S0002 |
| `invalid_program` | 400 | Referenced program does not exist or is soft-deleted. | F0006-S0002 |
| `invalid_lob` | 400 | LineOfBusiness value is not in the known LOB set. | F0006-S0002 |
| `invalid_assignee` | 400 | Target user does not exist, is inactive, or lacks the required role for the current submission state. | F0006-S0006 |

## Notes

- Add new codes here when new stories introduce deterministic error cases.
- Keep codes stable once released to avoid breaking client-side error handling.
- For `403`, always return a subtype `code` (at minimum `policy_denied`).
