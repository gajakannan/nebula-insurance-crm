# Data Protection Strategy

Status: Final
Last Updated: 2026-02-17
Owner: Security + Architect

## Objective

Define baseline data protection requirements for the Nebula reference solution.

## Data Classification

| Data Class | Examples | Sensitivity | Required Controls |
|---|---|---|---|
| Public | Documentation metadata | Low | Integrity checks |
| Internal | Operational logs, non-sensitive configs | Medium | Access control + retention policy |
| Confidential | Customer/account/submission/renewal records | High | Encryption + strict authz + audit trail |
| Secret | API keys, signing keys, credentials | Critical | Secret manager + rotation + no plaintext storage |

## Protection Requirements

- Encryption in transit for all service-to-service and user-facing traffic.
- Encryption at rest for confidential/secret data stores.
- Field-level masking/redaction for logs and support outputs.
- Data minimization in prompts/workflows for neuron/ paths.

### PII Masking — Inactive Broker Rule

When `Broker.Status = Inactive`, the API **must** mask PII fields on all broker and contact responses:

| Endpoint | Masked Fields | Sentinel Value |
|----------|--------------|----------------|
| `GET /brokers` (list) | `email`, `phone` | `null` |
| `GET /brokers/{id}` (detail) | `email`, `phone` | `null` |
| `GET /contacts` (list) | `email`, `phone` (when parent broker is Inactive) | `null` |
| `GET /contacts/{id}` (detail) | `email`, `phone` (when parent broker is Inactive) | `null` |

- Masking is enforced **server-side** before serialization; the frontend never receives raw PII for inactive brokers.
- Frontend consumers must display `null` on an inactive broker/contact as "Masked", not as missing data.
- See `nebula-api.yaml` Broker and Contact schema descriptions for the API contract.

## Retention And Deletion

| Data Type | Retention | Rationale |
|---|---|---|
| Audit timeline | 7 years | Compliance and dispute resolution |
| Workflow transitions | 7 years | Regulatory evidence |
| Tasks and reminders | 24 months | Operational history |
| Auth logs | 12 months | Security investigations |
| AI workflow prompts/outputs | 30 days | Minimize exposure |

## Data Access And Auditability

- Access to confidential data must be scoped by ABAC policies.
- All data mutations must generate audit/timeline events.
- Access to secret data must be tightly restricted and monitored.

## Open Items For Implementation Phase

- Confirm legal/compliance retention requirements by region.
- Define data export and subject access request workflows.

## Sign-Off

Security Reviewer: Security Agent
Architect: Architect Agent
Date: 2026-02-22
