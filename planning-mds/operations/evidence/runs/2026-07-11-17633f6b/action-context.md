# Action Context — F0037 standalone review run 2026-07-11-17633f6b

## Run Identity

- Action: `review` (`agents/actions/review.md`)
- MODE: `standalone`
- SCOPE: `path-set`
- FEATURE_ID (context): F0037 — Hierarchy-Aware Access Scoping & Distribution Rollups
- REVIEW_RUN_ID: `2026-07-11-17633f6b` (format `YYYY-MM-DD-[a-z0-9]{8}`; suffix from `secrets.token_hex(4)`; generated once at session start)
- REVIEW_RUN_FOLDER: `planning-mds/operations/evidence/runs/2026-07-11-17633f6b/` (base run path per §8; NOT under a feature evidence root)
- PRODUCT_ROOT (absolute): `/home/gajap/uSandbox/repos/nebula/nebula-insurance-crm`
- Run start (local): 2026-07-11T16:46:17-04:00

## Inputs

Operator-provided:

| Input | Value | Source |
|-------|-------|--------|
| `MODE` | `standalone` | operator prompt |
| `PR_URL` | `https://github.com/gajakannan/nebula-insurance-crm/pull/56` | operator prompt |
| `PRODUCT_ROOT` | `/home/gajap/uSandbox/repos/nebula/nebula-insurance-crm` | operator prompt |

Auto-resolved:

| Input | Value | Source |
|-------|-------|--------|
| `SCOPE` | `path-set` | derived from PR #56 changed files (per operator prompt note) |
| `PATHS` | PR #56 changed source paths (see below) | `gh pr view 56 --json files` |
| `DIFF_RANGE` | `e2f78be...27a5162` | `git merge-base origin/main pr-56` → PR head |
| `REVIEW_RUN_ID` | `2026-07-11-17633f6b` | generated at session start |
| `security_sensitive_scope` | `true` | auth/authorization/policy enforcement changed → Security review forced-required (§7) |

## PR Under Review

- Number / title: #56 — "Implement F0037 hierarchy-aware access scoping and distribution rollups"
- Head branch: `feature/F0037-distribution-rollups` (fetched locally as `pr-56`, `27a5162`)
- Base: `main`; merge-base `e2f78be`
- Totals: 179 files, +27,984 / −74,309 (the large deltas are regenerated KG artifacts — `symbol-index.yaml`, `coverage-report.yaml`).

## Scope In / Out

**In scope (path-set — reviewed source):**
- Backend enforcement & scope resolution — `engine/src/Nebula.Application/Services/` (`DistributionScopeService`, `OperationalReportService`, `SearchService`, `BrokerInsightService`, `ProjectionVisibilityResolver`), `Interfaces/IDistributionScopeService.cs`, `DTOs/`, `Validators/OperationalReportQueryValidator.cs`.
- Data access — `engine/src/Nebula.Infrastructure/Repositories/` (`DistributionScopeRepository`, `OperationalReportProjectionRepository`, `BrokerInsightProjectionRepository`, `SearchDocumentRepository`), `DependencyInjection.cs`.
- API surface — `engine/src/Nebula.Api/Endpoints/` (`OperationalReportEndpoints`, `TerritoryEndpoints`, `DistributionEndpoints`, `SearchEndpoints`).
- Authorization — `planning-mds/security/policies/policy.csv` (embedded into `Nebula.Infrastructure` at build), `CasbinAuthorizationServiceTests.cs`.
- Frontend — `experience/src/features/reports/*`, `experience/src/pages/OperationalReportsPage.tsx`, `SearchResultsPage.tsx`, `components/layout/Sidebar.tsx`, `features/search/components/SearchFilters.tsx`.
- Tests — unit (`SearchReporting/*`, `BrokerInsights/*`, `CasbinAuthorizationServiceTests`), integration (`TerritoryEndpointTests`), e2e (`experience/tests/e2e/f0037-distribution-rollups.spec.ts`).

**Out of scope:**
- Regenerated knowledge-graph artifacts (`planning-mds/knowledge-graph/*`) and evidence/run archive files — treated as generated output, not hand-authored logic. Tracker-governance coherence spot-checked only.
- The F0017 structural hierarchy/ownership/territory model and F0023 projection substrate (owned by prior features; consumed here).

## Context Loaded (per operator prompt order)

`agents/ROUTER.md` → `agents/agent-map.yaml` → `agents/docs/AGENT-USE.md` → `agents/actions/review.md` →
`agents/code-reviewer/SKILL.md` → `agents/security/SKILL.md`; plus PR context: `PRD.md`, `F0037-S0002`,
policy.csv, and the F0037 feature-run reports under run `2026-07-06-2e7e606d` (frontend-only follow-up).

## Standalone Disposition

This review does not contribute to F0037's feature evidence package and does not satisfy the per-feature
G3 review requirement. Findings are advisory to the PR author and the R2 approver.
