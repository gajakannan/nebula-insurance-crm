# Security Review Report — F0027-coi-acord-and-outbound-document-generation run 2026-07-02-b9316621

## Scope

- Feature ID: F0027
- Run ID: 2026-07-02-b9316621
- Date: 2026-07-02
- Reviewer: Security Reviewer

## Reviewed Surfaces

Reviewed outbound document API endpoints, server-side authorization checks, generated-document storage path, template governance metadata checks, document panel issue controls, generated artifact provenance, runtime document metadata schema, dependency scanner output, and scan-tool availability.

## Threat Boundary

| Subject | Resource | Operation | Control |
|---------|----------|-----------|---------|
| Admin | outbound template metadata | manage | `outbound_template:manage` policy path |
| Service/distribution users | generated document | preview/issue/regenerate | `outbound_document:*` plus document/template gates |
| API caller | generated document binary | download | existing document download authorization |
| Renderer service | document storage | create available PDF | `IDocumentRepository.CreateGeneratedAvailableAsync` |

## Auth / Authz

F0027 uses `outbound_document:preview`, `outbound_document:issue`, and `outbound_document:regenerate`, plus generic document create and document template read gates. The policy rows are in `planning-mds/security/policies/policy.csv`, which is embedded into infrastructure through the existing project link.

## Validation

Request validation currently relies on service normalization and allow-list checks for parent type, classification, artifact family, template publication status, and generated metadata schema. Additional endpoint-level schema validation remains recommended.

## Audit / Logging

Issue/regenerate writes document sidecar event `generated_issued` and timeline events `OutboundDocumentIssued` / `OutboundDocumentRegenerated`. No secrets are logged by the new code.

## Secrets / Config

No new secrets or environment variables were introduced. Runtime config changes are non-secret taxonomy and metadata schema files.

## Scan Disposition

| Class | Ran | Result / Finding summary | Artifact or waiver reason |
|-------|-----|--------------------------|---------------------------|
| dependency | yes | PASS WITH WAIVER: frontend high/critical findings remediated; backend OpenAPI advisory explicitly suppressed because patched 3.x line breaks ASP.NET source generation | artifacts/security/dependency-scan-summary.md |
| secrets | no | Tool unavailable | artifacts/security/secrets-scan-summary.md (gitleaks not installed) |
| sast | no | Tool unavailable | artifacts/security/sast-scan-summary.md (semgrep not installed) |
| dast | no | Tool unavailable/runtime image unavailable | artifacts/security/dast-scan-summary.md (ZAP docker image unavailable) |

## OWASP Top 10 Coverage

| Category | Status | Notes |
|----------|--------|-------|
| A01 Broken Access Control | OK | Backend enforces outbound plus document/template gates. |
| A02 Cryptographic Failures | N/A | No cryptographic material introduced. |
| A03 Injection | OK | No DB query construction or shell execution in feature code. |
| A04 Insecure Design | OK | Document generation controls remain server-side and audited. |
| A05 Security Misconfiguration | Issue | DAST could not complete due ZAP image/tooling unavailability; waiver recorded. |
| A06 Vulnerable / Outdated Components | Issue | Frontend high/critical findings remediated; backend OpenAPI advisory accepted as explicit waiver. |
| A07 Identification & Authentication | OK | Existing auth middleware remains in force. |
| A08 Software & Data Integrity | Issue | Missing SAST and secrets scanner evidence. |
| A09 Security Logging & Monitoring | OK | Sidecar and timeline audit events exist. |
| A10 Server-Side Request Forgery | N/A | No outbound fetch introduced. |

## Findings

- [medium] Backend `Microsoft.OpenApi` advisory remains accepted under `NuGetAuditSuppress` because the patched 3.x line breaks the current ASP.NET OpenAPI source generator — owner: Backend Developer; follow-up: F0027-openapi-sourcegen-compatible-upgrade
- [medium] Secrets scan could not run because `gitleaks` is unavailable — owner: DevOps; follow-up: F0027-security-tooling
- [medium] SAST could not run because `semgrep` is unavailable — owner: DevOps; follow-up: F0027-security-tooling
- [medium] DAST could not complete because the ZAP Docker image is unavailable — owner: DevOps; follow-up: F0027-security-tooling

## Recommendation Disposition

Frontend dependency blockers were remediated. Backend OpenAPI advisory is explicitly waived in project metadata and must be revisited when an ASP.NET OpenAPI source-generator-compatible patched line is available. Scanner availability gaps remain accepted as G3 recommendations with complete waiver artifacts.

## Result

PASS WITH RECOMMENDATIONS
