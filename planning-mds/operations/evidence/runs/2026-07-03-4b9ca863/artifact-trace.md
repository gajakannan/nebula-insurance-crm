# Artifact Trace

## Planned Artifacts

| Artifact | Owner Phase | Status | Notes |
| --- | --- | --- | --- |
| F0008 PRD refinement | Phase A | Pending | Product-manager planning updates only. |
| F0008 story set | Phase A | Pending | Broker Insights MVP stories and acceptance criteria. |
| F0008 feature README/STATUS/GETTING-STARTED sync | Phase A | Pending | Tracker-aligned feature metadata. |
| Story index regeneration | Phase A close | Pending | Generated from feature story files. |
| KG mapping alignment | Phase B | Pending | Architect-owned ontology and mapping sync only after Phase A approval. |
| Architecture plan notes | Phase B | Pending | Architect planning output only after G3 approval. |

## Non-Artifacts

- No implementation code is created by this plan action.
- No feature evidence package is created by this plan action.

## Produced Phase A Artifacts

| Artifact | Status | Notes |
| --- | --- | --- |
| `planning-mds/features/F0008-broker-insights/PRD.md` | Complete | Added Phase A refinement, personas, MVP scope, business rules, screen responsibilities, and ASCII layouts. |
| `planning-mds/features/F0008-broker-insights/F0008-S0001-broker-scorecard-overview.md` | Complete | New story. |
| `planning-mds/features/F0008-broker-insights/F0008-S0002-trend-drilldown-source-records.md` | Complete | New story. |
| `planning-mds/features/F0008-broker-insights/F0008-S0003-authorized-benchmark-comparison.md` | Complete | New story. |
| `planning-mds/features/F0008-broker-insights/F0008-S0004-review-snapshot.md` | Complete | New story. |
| `planning-mds/features/F0008-broker-insights/F0008-S0005-permission-safe-insights.md` | Complete | New story. |
| `planning-mds/features/F0008-broker-insights/README.md` | Complete | Story table synced. |
| `planning-mds/features/F0008-broker-insights/STATUS.md` | Complete | Story checklist and planning notes synced. |
| `planning-mds/features/F0008-broker-insights/GETTING-STARTED.md` | Complete | Prerequisites and verification notes synced. |
| `planning-mds/BLUEPRINT.md` | Complete | Section 3 feature/story/screen snapshot synced. |
| `planning-mds/features/STORY-INDEX.md` | Complete | Regenerated. |
| `planning-mds/knowledge-graph/feature-mappings.yaml` | Complete | PM-owned minimal F0008 feature/story stub seeded. |
| `planning-mds/knowledge-graph/coverage-report.yaml` | Complete | Regenerated after KG mapping change. |

## Produced Phase B Artifacts

| Artifact | Status | Notes |
| --- | --- | --- |
| `planning-mds/architecture/decisions/ADR-031-broker-insights-read-models.md` | Complete | Accepted after operator G5 approval. |
| `planning-mds/api/nebula-api.yaml` | Complete | Added `BrokerInsights` read-only endpoints and OpenAPI component schemas. |
| `planning-mds/schemas/broker-insight-scorecard.schema.json` | Complete | New Draft-07 schema. |
| `planning-mds/schemas/broker-insight-trend.schema.json` | Complete | New Draft-07 schema. |
| `planning-mds/schemas/broker-insight-benchmark.schema.json` | Complete | New Draft-07 schema. |
| `planning-mds/schemas/broker-insight-snapshot.schema.json` | Complete | New Draft-07 schema. |
| `planning-mds/architecture/data-model.md` | Complete | Added §12 `BrokerInsightProjection`. |
| `planning-mds/security/authorization-matrix.md` | Complete | Added §2.10c F0008 Broker Insights authorization. |
| `planning-mds/security/policies/policy.csv` | Complete | Added `broker_insight:read` internal-only policy lines. |
| `planning-mds/knowledge-graph/canonical-nodes.yaml` | Complete | Added Architect-owned canonical nodes for F0008 contracts. |
| `planning-mds/knowledge-graph/code-index.yaml` | Complete | Added planning-only routing bindings. |
| `planning-mds/knowledge-graph/feature-mappings.yaml` | Complete | Upgraded F0008 to architecture-complete and bound ADR/API/schema/policy nodes. |
| `planning-mds/features/F0008-broker-insights/PRD.md` | Complete | Added Phase B architecture contract and ADR traceability. |
| `planning-mds/features/F0008-broker-insights/README.md` | Complete | Added ERD and C4 component view. |
| `planning-mds/features/F0008-broker-insights/STATUS.md` | Complete | Set signoff roles and Phase B status. |
| `planning-mds/features/F0008-broker-insights/GETTING-STARTED.md` | Complete | Added implementation prerequisites/verification notes. |

## Phase A Validation

| Command | Result |
| --- | --- |
| `python3 agents/product-manager/scripts/validate-stories.py ../nebula-insurance-crm/planning-mds/features/F0008-broker-insights` | PASS, no warnings after repair |
| `python3 agents/product-manager/scripts/generate-story-index.py ../nebula-insurance-crm/planning-mds/features/` | PASS, 166 stories indexed |
| `python3 agents/product-manager/scripts/validate-trackers.py --product-root ../nebula-insurance-crm --skip-feature-evidence` | PASS, 0 errors, 0 warnings |
| `python3 scripts/kg/validate.py --write-coverage-report` | PASS, F0008 mapped |
| `python3 scripts/kg/validate.py` | PASS |
| `python3 scripts/kg/validate.py --check-drift` | PASS |
| `python3 agents/scripts/validate_templates.py` | PASS |

## Phase B Validation

| Command | Result |
| --- | --- |
| `python3 scripts/kg/validate.py --write-coverage-report` | PASS, 197 code bindings |
| `python3 scripts/kg/validate.py` | PASS |
| `python3 scripts/kg/validate.py --check-drift` | PASS |
| `python3 agents/product-manager/scripts/validate-stories.py ../nebula-insurance-crm/planning-mds/features/F0008-broker-insights` | PASS, no warnings |
| `python3 agents/product-manager/scripts/validate-trackers.py --product-root ../nebula-insurance-crm --skip-feature-evidence` | PASS, 0 errors, 0 warnings |
| `python3 agents/scripts/validate_templates.py` | PASS |

## Post-Approval Closeout Validation

| Command | Result |
| --- | --- |
| `python3 agents/product-manager/scripts/validate-stories.py ../nebula-insurance-crm/planning-mds/features/F0008-broker-insights` | PASS, no issues |
| `python3 agents/product-manager/scripts/generate-story-index.py ../nebula-insurance-crm/planning-mds/features/` | PASS, 166 stories indexed |
| `python3 agents/product-manager/scripts/validate-trackers.py --product-root ../nebula-insurance-crm --skip-feature-evidence` | PASS, 0 errors, 0 warnings |
| `python3 scripts/kg/validate.py --write-coverage-report` | PASS |
| `python3 scripts/kg/validate.py` | PASS |
| `python3 scripts/kg/validate.py --check-drift` | PASS |
| `python3 agents/scripts/validate_templates.py` | PASS |

## Final Working Tree Snapshot

- `nebula-insurance-crm`: F0008 planning artifacts, architecture contracts, KG mappings, security matrix/policy, roadmap/story index/blueprint updates are modified or untracked as expected.
- `nebula-insurance-crm`: unrelated untracked `scripts/kg/ts-symbols/pnpm-lock.yaml` remains untouched.
- `nebula-agents`: `agents/templates/prompts/evidence-contract/plan-operator-friendly.md` remains modified from the earlier prompt-injection step; no further agent harness source edits were made during closeout.
