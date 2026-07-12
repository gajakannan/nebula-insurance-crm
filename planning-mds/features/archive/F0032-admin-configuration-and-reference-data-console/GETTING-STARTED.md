# F0032 — Admin Configuration & Reference Data Console — Getting Started

## Prerequisites

- [x] Read the current release framing in [ROADMAP.md](../ROADMAP.md)
- [x] Review which reference data and operational settings are already candidates for configuration
- [x] Refine this feature into stories before coding
- [x] Complete Phase B architecture, API/schema, authorization, and ontology binding work
- [ ] Produce `feature-assembly-plan.md` during the later feature action, not during this plan action

## How to Verify

1. Confirm the feature focuses on governed operational configuration, not unrestricted system administration.
2. Define which settings are worth exposing in the first admin console release.
3. Validate tracker sync after refinement.

## First-Release Domains

- F0022 queue/routing configuration governance.
- Workflow SLA thresholds for submission and renewal timing.
- F0023 saved-view/report default governance.
- F0027 template metadata governance.

## Build Readiness Notes

- Phase B artifacts are ready for approval review: [ARCHITECTURE.md](./ARCHITECTURE.md), [ADR-032](../../architecture/decisions/ADR-032-admin-configuration-console-contract.md), [data-model.md §13](../../architecture/data-model.md), [nebula-api.yaml](../../api/nebula-api.yaml), and `planning-mds/schemas/admin-configuration-*.schema.json`.
- F0032 is not build-ready until Phase B approval is recorded in the plan run.
- `feature-assembly-plan.md` is intentionally deferred to `agents/actions/feature.md`.
- Cross-instance cache invalidation remains a later DevOps/runtime decision if Nebula runs multiple API instances.
