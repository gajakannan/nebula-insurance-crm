# Artifact Trace — F0027-coi-acord-and-outbound-document-generation plan run 2026-07-02-e8a31f35

## Artifacts Read

- `nandini-nebula-agents/agents/ROUTER.md`
- `nandini-nebula-agents/agents/AGENT-MAP.md`
- `nandini-nebula-agents/agents/AGENT-USE.md`
- `nandini-nebula-agents/agents/templates/prompts/evidence-contract/plan-operator-friendly.md`
- `nandini-nebula-agents/agents/actions/plan.md`
- `nandini-nebula-agents/agents/actions/feature.md`
- `nandini-nebula-agents/CONSUMER-CONTRACT.md`
- `nandini-nebula-agents/agents/product-manager/SKILL.md`
- `nandini-nebula-agents/agents/architect/SKILL.md`
- `planning-mds/features/REGISTRY.md`
- `planning-mds/features/ROADMAP.md`
- `planning-mds/BLUEPRINT.md`
- `planning-mds/architecture/SOLUTION-PATTERNS.md`
- `planning-mds/architecture/decisions/ADR-012-shared-document-storage-and-metadata-architecture.md`
- `planning-mds/architecture/decisions/ADR-025-submission-downstream-workflow-quote-approval-bind-and-archive.md`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/knowledge-graph/canonical-nodes.yaml`
- `planning-mds/knowledge-graph/solution-ontology.yaml`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/PRD.md`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/STATUS.md`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/README.md`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/GETTING-STARTED.md`

## Artifacts Created Or Updated

- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/PRD.md`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/ARCHITECTURE.md`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/README.md`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/STATUS.md`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/GETTING-STARTED.md`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/F0027-S0001-template-library-governance.md`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/F0027-S0002-preview-generated-document.md`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/F0027-S0003-issue-generated-artifact.md`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/F0027-S0004-regenerate-and-retrieve-artifacts.md`
- `planning-mds/features/F0027-coi-acord-and-outbound-document-generation/F0027-S0005-render-proposal-from-submission-packet.md`
- `planning-mds/features/STORY-INDEX.md`
- `planning-mds/features/ROADMAP.md`
- `planning-mds/BLUEPRINT.md`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- `planning-mds/schemas/README.md`
- `planning-mds/schemas/generated-document-request.schema.json`
- `planning-mds/schemas/generated-document-preview-response.schema.json`
- `planning-mds/schemas/generated-document-issue-response.schema.json`
- `planning-mds/knowledge-graph/canonical-nodes.yaml`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/knowledge-graph/coverage-report.yaml`
- `planning-mds/operations/evidence/runs/2026-07-02-e8a31f35/README.md`
- `planning-mds/operations/evidence/runs/2026-07-02-e8a31f35/action-context.md`
- `planning-mds/operations/evidence/runs/2026-07-02-e8a31f35/artifact-trace.md`
- `planning-mds/operations/evidence/runs/2026-07-02-e8a31f35/gate-decisions.md`
- `planning-mds/operations/evidence/runs/2026-07-02-e8a31f35/commands.log`
- `planning-mds/operations/evidence/runs/2026-07-02-e8a31f35/lifecycle-gates.log`

## Generated Evidence

- KG readiness repaired by regenerating `planning-mds/knowledge-graph/coverage-report.yaml`.
- G1 clarification recorded in `gate-decisions.md`.
- G2 tracker sync recorded as pass with recommendations in `gate-decisions.md`.
- G3 Phase A approval recorded in `gate-decisions.md`.
- G4 ontology sync recorded as pass in `gate-decisions.md`.
- Validation command outcomes recorded in `lifecycle-gates.log`.

## External Or Global Evidence References

- Full historical feature-evidence validation currently reports missing artifacts from older archived runs; scoped tracker validation passes for F0027 Phase A.

## Omissions And Waivers

- Feature evidence package intentionally omitted; it belongs to `agents/actions/feature.md` and implementation begins only after G5 Phase B approval.
- `feature-assembly-plan.md` intentionally omitted; it belongs to feature action G0, not plan action.

## Run Environment

- Absolute cwd: `/Users/wallstreet288/Nebula_pr/nandini-nebula-agents` — framework repo for harness-owned commands.
- Absolute cwd: `/Users/wallstreet288/Nebula_pr/nebula-insurance-crm` — resolved product root for product-owned KG and planning artifacts.
