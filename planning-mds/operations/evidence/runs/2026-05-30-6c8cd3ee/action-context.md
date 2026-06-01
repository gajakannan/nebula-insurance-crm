# Action Context — F0036 Feature Review 2026-05-30-6c8cd3ee

## FR0 Feature Run And Diff Lock

- **Feature ID:** F0036
- **Mode:** closeout-audit
- **Product root:** `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm`
- **Feature path:** `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/features/archive/F0036-dynamic-product-attribute-form-engine`
- **Evidence root:** `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/operations/evidence/F0036-dynamic-product-attribute-form-engine`
- **Feature run ID:** `2026-05-28-077b7b30`
- **Feature run folder:** `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-05-28-077b7b30`
- **latest-run.json:** `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/operations/evidence/features/F0036-dynamic-product-attribute-form-engine/latest-run.json` resolves to `2026-05-28-077b7b30` with `status=approved`.
- **Requested DIFF_RANGE:** `base..head`
- **Diff resolution:** `base` is not a local git ref; changed-file set is locked from the feature evidence `scm.diff_artifact` instead: `planning-mds/operations/evidence/runs/2026-05-28-077b7b30/artifacts/diffs/changed-files.txt`.
- **Manifest SCM refs:** `main...HEAD` for feature branch `feat/F0036-dynamic-product-attribute-form-engine` (recorded by the feature run).
- **Current product git branch at review:** `feat/update-agent-stages`.
- **DevOps inclusion decision:** `RUN_DEVOPS=auto` resolves to `yes` because `runtime_bearing=true` and `deployability-check.md` is part of the reviewed evidence.
- **Review write scope:** this folder only: `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-05-30-6c8cd3ee`.

## Changed-File Set

```text
# git diff --name-only main...HEAD (F0036 run 2026-05-28-077b7b30) — implementation change set
experience/package.json
experience/pnpm-lock.yaml
experience/src/features/brokers/components/ContactFormModal.tsx
experience/src/features/brokers/components/EditBrokerModal.tsx
experience/src/features/brokers/tests/ContactFormModal.restore.test.tsx
experience/src/features/brokers/tests/ContactFormModal.test.tsx
experience/src/features/brokers/tests/EditBrokerModal.test.tsx
experience/src/features/forms/__tests__/dualBackend.test.tsx
experience/src/features/forms/__tests__/useControlledDirtyTracker.test.ts
experience/src/features/forms/index.ts
experience/src/features/forms/useControlledDirtyTracker.ts
experience/src/features/forms/useRegisteredForm.ts
experience/src/features/lob-attributes/components/DynamicAttributePanel.tsx
experience/src/features/lob-attributes/components/__tests__/DynamicAttributePanel.preservation.test.tsx
experience/src/features/lob-attributes/components/__tests__/DynamicAttributePanel.test.tsx
experience/src/features/lob-attributes/engine/FormPreservation.tsx
experience/src/features/lob-attributes/engine/SchemaDrivenForm.tsx
experience/src/features/lob-attributes/engine/__tests__/SchemaDrivenForm.test.tsx
experience/src/features/lob-attributes/engine/__tests__/deriveWidgets.test.ts
experience/src/features/lob-attributes/engine/__tests__/parity.test.ts
experience/src/features/lob-attributes/engine/__tests__/rhfDirtyAdapter.test.ts
experience/src/features/lob-attributes/engine/__tests__/usePinnedBundle.test.ts
experience/src/features/lob-attributes/engine/__tests__/widgetRegistry.test.ts
experience/src/features/lob-attributes/engine/__tests__/widgets.a11y.test.tsx
experience/src/features/lob-attributes/engine/__tests__/widgets.test.tsx
experience/src/features/lob-attributes/engine/ajvValidator.ts
experience/src/features/lob-attributes/engine/deriveWidgets.ts
experience/src/features/lob-attributes/engine/index.ts
experience/src/features/lob-attributes/engine/options.ts
experience/src/features/lob-attributes/engine/parity/cyber-examples.fixture.ts
experience/src/features/lob-attributes/engine/rhfDirtyAdapter.ts
experience/src/features/lob-attributes/engine/types.ts
experience/src/features/lob-attributes/engine/uiConditionalMap.ts
experience/src/features/lob-attributes/engine/usePinnedBundle.ts
experience/src/features/lob-attributes/engine/widgetRegistry.ts
experience/src/features/lob-attributes/engine/widgets/index.tsx
experience/src/features/lob-attributes/index.ts
experience/src/features/lob-attributes/lib/cyber.ts
experience/src/features/session-continuity/index.ts
experience/src/features/tasks/components/TaskCreateModal.tsx
experience/src/features/tasks/components/TaskDetailPanel.tsx
experience/src/features/tasks/components/__tests__/TaskCreateModal.test.tsx
experience/src/features/tasks/components/__tests__/TaskDetailPanel.test.tsx
experience/src/pages/AccountDetailPage.tsx
experience/src/pages/CreateAccountPage.tsx
experience/src/pages/CreateBrokerPage.tsx
experience/src/pages/CreatePolicyPage.tsx
experience/src/pages/CreateSubmissionPage.tsx
experience/src/pages/RenewalsPage.tsx
experience/src/pages/SubmissionDetailPage.tsx
experience/src/pages/__tests__/CreateAccountPage.test.tsx
experience/src/pages/__tests__/CreateBrokerPage.test.tsx
experience/src/pages/__tests__/CreatePolicyPage.test.tsx
experience/src/pages/__tests__/CreateSubmissionPage.test.tsx
experience/src/pages/__tests__/RenewalsPage.create.test.tsx
planning-mds/BLUEPRINT.md
planning-mds/architecture/decisions/ADR-021-form-engine-rhf-ajv-shadcn-registry.md
planning-mds/architecture/feature-assembly-plan.md
planning-mds/examples/personas/nebula-personas.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0001-engine-skeleton-and-dependencies.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0002-mvp-widget-vocabulary.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0003-schema-driven-rendering-ajv-parity.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0004-pin-during-edit.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0005-replace-cyber-panel-five-screen-regression.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0006-product-attribute-form-preservation.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0007-controlled-dirty-tracker-and-registration-helper.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0007-crud-rhf-migration-and-registration-helper.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0008-crud-form-preservation-restore.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/GETTING-STARTED.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/PRD.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/README.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/STATUS.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/acceptance-criteria-checklist.md
planning-mds/features/F0036-dynamic-product-attribute-form-engine/feature-assembly-plan.md
planning-mds/features/REGISTRY.md
planning-mds/features/ROADMAP.md
planning-mds/features/STORY-INDEX.md
planning-mds/knowledge-graph/canonical-nodes.yaml
planning-mds/knowledge-graph/coverage-report.yaml
planning-mds/knowledge-graph/feature-mappings.yaml
```

## Review Constraints

- Read feature evidence and source artifacts directly.
- Do not write into the feature evidence package.
- Do not edit implementation code, tests, feature docs, trackers, KG files, or closeout artifacts.
- Findings must cite concrete source/evidence paths.
