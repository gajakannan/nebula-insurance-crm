# Artifact Trace - F0008 Broker Insights

## Artifacts Read

| Artifact | Purpose |
| --- | --- |
| `agents/actions/feature.md` | Feature action gate contract. |
| `agents/templates/prompts/evidence-contract/feature-operator-friendly.md` | Operator-friendly feature evidence contract. |
| `agents/architect/SKILL.md` | Architect role activation for G0. |
| `planning-mds/features/F0008-broker-insights/*` | Original feature PRD, stories, status, and getting-started context before PM archive correction. |
| `planning-mds/features/archive/F0008-broker-insights/*` | Archived feature PRD, stories, status, and getting-started context after PM closeout correction. |
| `planning-mds/architecture/decisions/ADR-031-broker-insights-read-models.md` | Governing architecture decision. |
| `planning-mds/api/nebula-api.yaml` | BrokerInsights OpenAPI contract. |
| `planning-mds/architecture/data-model.md` | BrokerInsightProjection data model. |
| `planning-mds/architecture/SOLUTION-PATTERNS.md` | Solution implementation patterns. |
| `engine/src/**`, `experience/src/**` | Existing implementation conventions inspected after KG hints. |

## Artifacts Created Or Updated

| Artifact | Status | Notes |
| --- | --- | --- |
| `evidence-manifest.json` | Created | Draft feature evidence manifest. |
| `README.md` | Created | Feature evidence index. |
| `action-context.md` | Created | Run identity and scope. |
| `artifact-trace.md` | Created | Initial artifact plan. |
| `gate-decisions.md` | Created | Gate table initialized. |
| `commands.log` | Created | Command log initialized with run setup commands. |
| `lifecycle-gates.log` | Created | Validator structure initialized. |
| `planning-mds/features/archive/F0008-broker-insights/feature-assembly-plan.md` | Created | G0 feature-local implementation execution plan; moved with feature archive. |
| `planning-mds/architecture/feature-assembly-plan.md` | Updated | Umbrella architecture plan references F0008 execution plan. |
| `planning-mds/features/archive/F0008-broker-insights/STATUS.md` | Updated | Required signoff roles initialized for feature action; closeout summary and archived state added. |
| `g0-assembly-plan-validation.md` | Created | G0 assembly validation report. |
| `g1-runtime-preflight.md` | Created | G1 runtime preflight report. |
| `engine/src/Nebula.Domain/Entities/BrokerInsightProjection.cs` | Created | Projection entity for broker insight read model. |
| `engine/src/Nebula.Application/DTOs/BrokerInsightDtos.cs` | Created | Query and response contracts for scorecards, trends, benchmarks, snapshots, and projection queries. |
| `engine/src/Nebula.Application/Services/BrokerInsightService.cs` | Created | Aggregation, trend, benchmark, snapshot, and visibility-aware service logic. |
| `engine/src/Nebula.Infrastructure/Repositories/BrokerInsightProjectionRepository.cs` | Created | EF query/upsert repository for projection rows. |
| `engine/src/Nebula.Api/Endpoints/BrokerInsightEndpoints.cs` | Created | Authorized broker insights API route group. |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260703185200_F0008BrokerInsights.cs` | Created | BrokerInsightProjections table migration. |
| `experience/src/features/broker-insights/*` | Created | Frontend hooks, types, workspace, cards, trend, benchmark, snapshot, and tests. |
| `experience/src/pages/BrokerInsightsPage.tsx` | Created | Protected broker insights page shell. |
| `experience/src/App.tsx` | Updated | Registered `/broker-insights` route. |
| `engine/tests/Nebula.Tests/Unit/BrokerInsights/BrokerInsightServiceTests.cs` | Created | Focused backend service tests. |
| `g2-self-review.md` | Created | G2 self-review report. |
| `test-plan.md` | Created | G2 QE test plan. |
| `test-execution-report.md` | Created | G2 QE execution report. |
| `coverage-report.md` | Created | G2 coverage summary. |
| `deployability-check.md` | Created | G2 DevOps deployability review. |
| `artifacts/security/secrets-scan.md` | Created | Targeted secrets scan evidence. |
| `artifacts/security/sast-scan.md` | Created | Targeted authorization/SAST evidence. |
| `code-review-report.md` | Created | G3 code review report. |
| `security-review-report.md` | Created | G3 security review report. |
| `planning-mds/features/archive/F0008-broker-insights/STATUS.md` | Updated | G5 story signoff provenance rows. |
| `signoff-ledger.md` | Created | G5 signoff ledger. |
| `feature-action-execution.md` | Created | G6 candidate execution summary. |
| `planning-mds/knowledge-graph/coverage-report.yaml` | Updated | Refreshed by KG validator repair command and post-archive coverage regeneration. |
| `kg-reconciliation.md` | Created | G7 KG reconciliation report. |
| `pm-closeout.md` | Created | G8 PM closeout report. |
| `planning-mds/operations/evidence/features/F0008-broker-insights/latest-run.json` | Created | Feature evidence pointer. |
| `evidence-manifest.json` | Updated | Approved state and closeout path recorded. |
| `planning-mds/features/archive/F0008-broker-insights/` | Moved | PM archive correction from active feature folder. |
| `planning-mds/features/REGISTRY.md` | Updated | F0008 moved from Planned to Archived Features. |
| `planning-mds/features/ROADMAP.md` | Updated | F0008 moved from Now to Completed. |
| `planning-mds/features/STORY-INDEX.md` | Regenerated | F0008 story links now point to archive path. |
| `planning-mds/BLUEPRINT.md` | Updated | F0008 feature/story status and links set to Done/Archived. |
| `planning-mds/knowledge-graph/feature-mappings.yaml` | Updated | F0008 feature/story paths set to archive path and status `archived-done`. |
| `planning-mds/knowledge-graph/canonical-nodes.yaml` | Updated | F0008 source docs set to archive path. |
| `planning-mds/knowledge-graph/code-index.yaml` | Updated | F0008 planning feature bindings set to archive path. |
| `artifacts/feature-evidence-validation.json` | Created | Final closeout validation JSON artifact after archive correction. |

## Generated Evidence

| Evidence | Status | Notes |
| --- | --- | --- |
| `g0-assembly-plan-validation.md` | Complete | G0 checklist pass before validator repair cycle. |
| `artifacts/diffs/changed-files.txt` | Complete | Initial changed-path artifact for G0. |
| `g1-runtime-preflight.md` | Complete | Docker Compose and API health evidence. |
| `g2-self-review.md` | Complete | Implementation self-review passed. |
| `test-execution-report.md` | Complete | Backend unit, frontend unit, frontend build, and backend solution build passed. |
| `coverage-report.md` | Complete | Focused coverage and gaps recorded. |
| `deployability-check.md` | Complete | Deployability passed with rebuild note. |
| `artifacts/security/secrets-scan.md` | Complete | No targeted secret matches. |
| `artifacts/security/sast-scan.md` | Complete | No blocking authorization findings for G2. |
| `code-review-report.md` | Complete | No blocking code review findings. |
| `security-review-report.md` | Complete | No blocking security findings. |
| `signoff-ledger.md` | Complete | Required story/role signoffs passed. |
| `feature-action-execution.md` | Complete | Candidate package prepared for G6 validation. |
| `kg-reconciliation.md` | Complete | F0008 KG bindings reconciled and validator passed. |
| `pm-closeout.md` | Complete | PM closeout approved and archive correction recorded. |
| `artifacts/feature-evidence-validation.json` | Complete | Final closeout validator JSON captured after archive correction. |

## External Or Global Evidence References

- Plan evidence: `planning-mds/operations/evidence/runs/2026-07-03-4b9ca863/`.
- No global frontend evidence lane is used as a substitute for feature evidence.

## Omissions And Waivers

- Security scans are omitted at G0 because `security_sensitive_scope` is reconciled at G2 after runtime changes and scan execution.
- At G2, dependency audit and authenticated DAST are waived in `security_scans` because external registry access and authenticated rebuilt-container runtime testing are not available in this sandbox turn.
- No AI role evidence is required because F0008 does not touch `neuron/`, LLM, MCP, prompts, or model behavior.

## Planned Runtime Artifacts

| Surface | Planned Artifacts |
| --- | --- |
| Backend | Domain projection entity, DTOs, service, repository/query access, API endpoints, tests |
| Frontend | Broker insights feature slice, route/page integration, query hooks, component tests |
| Quality | Feature test plan, test execution report, coverage report, E2E or scoped integration coverage |
| DevOps | Runtime preflight and deployability evidence |
| Security | Security scans and security review report |
| Architecture/KG | Feature assembly plan and as-built KG reconciliation |
| Product Closeout | PM closeout, manifest approval, feature pointer, tracker sync |

## Run Environment

- Top workspace `/Users/wallstreet48/nebula-feature-26` is not a Git repository; per-repo status commands are used.
- `nebula-insurance-crm` and `nebula-agents` are sibling repositories under the workspace root.
- `commands.log` records absolute `cwd` values because the harness run spans sibling repositories and the product-root echo requirement needs unambiguous command provenance.
- Sandbox-local Docker socket and localhost access are restricted; runtime preflight Docker/API health commands were rerun with approved escalation.
