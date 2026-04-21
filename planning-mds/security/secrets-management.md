# Secrets Management

Status: Final
Last Updated: 2026-02-17
Owner: Security + DevOps

## Objective

Define how secrets are stored, accessed, rotated, and audited across environments.

## Secret Categories

| Category | Examples | Storage Target | Rotation Requirement |
|---|---|---|---|
| Application credentials | DB/API credentials | Managed secret store | Scheduled + on incident |
| Signing material | JWT/signing keys | Secret store or HSM | Scheduled with key versioning |
| External provider tokens | LLM/provider keys | Secret store | Scheduled + least privilege |
| CI/CD credentials | Pipeline tokens | CI secret store | Scheduled + scope-restricted |

## Baseline Controls

- Do not commit secrets to source control.
- Use environment-variable references to secret manager values.
- Separate secrets per environment (dev/staging/prod).
- Enforce least privilege for runtime service identities.

## Rotation And Revocation

- Rotation cadence: 90 days for application credentials, 180 days for signing keys.
- Emergency revocation playbook required for compromised credentials.
- Track key versions and rollout windows to avoid downtime.

## Local Development Rules

- Use placeholder values in .env.example, never real credentials.
- Prefer short-lived local credentials where available.
- Document onboarding path for secure local secret access.

## Incident Response Expectations

- Detect suspected secret exposure.
- Revoke and rotate affected secrets immediately.
- Record incident details and remediation actions.

## Sign-Off

Security Reviewer: Security Agent
DevOps Reviewer: DevOps Agent
Date: 2026-02-22
