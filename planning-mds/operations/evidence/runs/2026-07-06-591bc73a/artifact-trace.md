# Artifact Trace

## Plan Artifacts

| Artifact | Owner | Status | Notes |
|----------|-------|--------|-------|
| `planning-mds/features/F0032-admin-configuration-and-reference-data-console/PRD.md` | Product Manager | Phase A refined | Scope, personas, workflows, screens, 6-story release slicing |
| `planning-mds/features/F0032-admin-configuration-and-reference-data-console/README.md` | Product Manager | Phase A refined | Story index and planning notes |
| `planning-mds/features/F0032-admin-configuration-and-reference-data-console/STATUS.md` | Product Manager | Phase A refined | Story checklist and required signoff skeleton |
| `planning-mds/features/F0032-admin-configuration-and-reference-data-console/GETTING-STARTED.md` | Product Manager | Phase A refined | First-release domains and build-readiness notes |
| `planning-mds/features/F0032-admin-configuration-and-reference-data-console/F0032-S0001-admin-configuration-catalog.md` | Product Manager | Created | Admin catalog story |
| `planning-mds/features/F0032-admin-configuration-and-reference-data-console/F0032-S0002-draft-reference-and-sla-configuration.md` | Product Manager | Created | SLA/reference draft story |
| `planning-mds/features/F0032-admin-configuration-and-reference-data-console/F0032-S0003-govern-queue-routing-configuration.md` | Product Manager | Created | Queue/routing draft governance story |
| `planning-mds/features/F0032-admin-configuration-and-reference-data-console/F0032-S0004-validate-and-compare-configuration.md` | Product Manager | Created | Validation and compare story |
| `planning-mds/features/F0032-admin-configuration-and-reference-data-console/F0032-S0005-publish-and-rollback-configuration.md` | Product Manager | Created | Publish and rollback story |
| `planning-mds/features/F0032-admin-configuration-and-reference-data-console/F0032-S0006-audit-and-permission-safe-admin-configuration.md` | Product Manager | Created | Audit and permission-safe behavior story |
| `planning-mds/features/STORY-INDEX.md` | Product Manager | Regenerated | 201 story files indexed after F0032 story creation |
| `planning-mds/BLUEPRINT.md` | Product Manager | Updated | F0032 story snapshot and screen list |
| `planning-mds/knowledge-graph/feature-mappings.yaml` | Product Manager | Updated | Minimal feature/story mapping stubs |
| `planning-mds/knowledge-graph/coverage-report.yaml` | Product Manager | Regenerated | Fresh coverage after story mapping changes |
| `planning-mds/features/F0032-admin-configuration-and-reference-data-console/ARCHITECTURE.md` | Architect | Created | Service boundaries, ERD, API, authorization, workflow, NFRs |
| `planning-mds/architecture/decisions/ADR-032-admin-configuration-console-contract.md` | Architect | Created | F0032 governed configuration facade decision |
| `planning-mds/architecture/data-model.md` | Architect | Updated | F0032 data model section and ERD entities |
| `planning-mds/api/nebula-api.yaml` | Architect | Updated | AdminConfiguration endpoints and component schemas |
| `planning-mds/schemas/admin-configuration-domain.schema.json` | Architect | Created | Domain catalog schema |
| `planning-mds/schemas/admin-configuration-draft.schema.json` | Architect | Created | Draft lifecycle schema |
| `planning-mds/schemas/admin-configuration-validation-result.schema.json` | Architect | Created | Validation and compare schema |
| `planning-mds/schemas/admin-configuration-publish-request.schema.json` | Architect | Created | Publish/rollback request schema |
| `planning-mds/schemas/admin-configuration-audit-event.schema.json` | Architect | Created | Audit event schema |
| `planning-mds/security/authorization-matrix.md` | Architect | Updated | AdminConfiguration role/action matrix |
| `planning-mds/security/policies/policy.csv` | Architect | Updated | AdminConfiguration Casbin policy rows |
| `planning-mds/knowledge-graph/canonical-nodes.yaml` | Architect | Updated | F0032 canonical ADR/capability/entity/endpoint/policy/schema/role nodes |
| `planning-mds/knowledge-graph/code-index.yaml` | Architect | Updated | Retrieval binding for admin configuration governance |
| `scripts/kg/validate.py` | Architect | Updated | Drift validator recognizes F0032 roles |

## Dependency Evidence Audit

| Dependency | Current Evidence | Plan Note |
|------------|------------------|-----------|
| F0022 Work Queues, Assignment Rules & Coverage Management | `planning-mds/features/archive/F0022-work-queues-assignment-rules-and-coverage-management/PRD.md` | Confirms F0032 governs existing queue/routing contracts and does not replace F0022 execution |
| F0023 Global Search, Saved Views & Operational Reporting | `planning-mds/features/archive/F0023-global-search-saved-views-and-operational-reporting/PRD.md` | Confirms F0032 can govern defaults while F0023 owns search/report behavior |
| F0027 COI, ACORD & Outbound Document Generation | `planning-mds/features/archive/F0027-coi-acord-and-outbound-document-generation/PRD.md` | Confirms F0032 can govern template metadata while F0027 owns upload/generation/issue |
| F0034 Product Schema Registry and Dynamic LOB Attributes | `planning-mds/features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/PRD.md` | Confirms F0032 does not become a product schema authoring console |
| ADR-016 Published Operational Configuration Governance | `planning-mds/architecture/decisions/ADR-016-published-operational-configuration-governance.md` | Governs draft/validate/publish/rollback/audit semantics |
