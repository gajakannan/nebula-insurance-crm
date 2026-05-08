# F0034 - Product Schema Registry and Dynamic LOB Attributes - Getting Started

## Planning Context

- Product root: `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm`
- Feature path: `planning-mds/features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/`
- Current mode: greenfield plan over an existing draft folder
- Run id: `e726ab45-2563-442f-a360-c3173e66621b`

## Read First

1. [PRD.md](./PRD.md)
2. `planning-mds/architecture/lob-extensible-attribute-plan.md`
3. F0019 PRD, specifically the quote/proposal product-attribute guardrail
4. F0020 README and closeout notes for the document metadata schema renderer precedent
5. ADR-001 and ADR-018 before Phase B architecture work

## Resolved Phase A Decisions

- Cyber is the first product pilot.
- Submission, PolicyVersion, PolicyEndorsement, and Renewal carry product attributes.
- Policy reads current product attributes through PolicyVersion and does not own an independent attribute payload in the first slice.
- Existing null-LOB Submission/Renewal rows use `_unspecified/0.0.0`; existing non-null LOB rows use per-LOB legacy sentinels.
- F0019 quote/proposal work depends on this feature for product-specific attributes.
- A no-code product administration console and full LOB rollout remain out of scope.

## Phase B Handoff Topics

- ADR numbers are assigned: ADR-020 through ADR-023.
- OpenAPI and static schema files are updated for the LOB attribute envelope and schema-bundle APIs.
- KG canonical nodes and F0034 story mappings are bound to the Phase B architecture artifacts.
- Authorization actions are defined: `lob_schema:read`, `lob_schema:resolve`, `lob_schema:activate`, `lob_schema:deprecate`, `lob_schema:retire`.
- Runtime implementation still must fill numeric benchmark results in `planning-mds/architecture/validation-perf-baseline.md` before F0034 exits Phase C.

## Architect Artifacts

1. `planning-mds/architecture/decisions/ADR-020-lob-extensible-attribute-architecture.md`
2. `planning-mds/architecture/decisions/ADR-021-form-engine-rhf-ajv-shadcn-registry.md`
3. `planning-mds/architecture/decisions/ADR-022-validator-equivalence-restricted-profile.md`
4. `planning-mds/architecture/decisions/ADR-023-rules-governance-jsonlogic.md`
5. `planning-mds/architecture/openapi-30-projection-matrix.md`
6. `planning-mds/architecture/validation-perf-baseline.md`

## Validation Commands

Run these from `nebula-agents`:

```bash
python3 agents/product-manager/scripts/validate-stories.py /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes
python3 agents/product-manager/scripts/generate-story-index.py /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/features/
python3 agents/product-manager/scripts/validate-trackers.py
python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/validate.py
python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/validate.py --check-drift
python3 agents/scripts/validate_templates.py
```
