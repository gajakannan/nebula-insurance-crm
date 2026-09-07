# One-time repository validation — 2026-09-07

**Verdict: the repository does not currently support a clean process-validation or production-readiness signoff.** The application builds, the principal trackers agree, and substantial implementation exists. However, this review reproduced a critical conversation-access vulnerability, found broken frontend integration routes and unsafe deployment behavior, and confirmed gaps in completion evidence and quality enforcement.

This is a one-off review, not a new lifecycle, scheduled audit, or feature implementation. The original findings below are preserved as the review baseline; remediation changes and post-fix validation are recorded in [REMEDIATION.md](REMEDIATION.md) and the separate base action run. This folder contains the report and captured evidence.

The follow-up resolved R01–R04, R06, R08–R11, and the repository-side portion of
R07/R10/R12. The current frontend, KG, policy, API, and Neuron checks are
recorded in `REMEDIATION.md` and the revision-bound frontend manifest. R05 still
requires historical evidence recovery or re-entry. The one-time disposition is
recorded in [HISTORICAL-EVIDENCE-DISPOSITION.md](HISTORICAL-EVIDENCE-DISPOSITION.md);
the contract-evolution concession is recorded in
[HISTORICAL-CONTRACT-CONCESSION.md](HISTORICAL-CONTRACT-CONCESSION.md). The
strict current-validator result remains visible and is not reinterpreted as a
current evidence pass.
the original validation table below remains unchanged so the baseline is
auditable.

Reviewed product commit: `47375571b19e3f917c1d22cbc076df41320100ff`. Five existing local modifications to the F0040 plan evidence were preserved. File hashes confirmed that no pre-existing tracked file changed during validation. Exact environment, framework revision, and artifact hashes are in [review-metadata.json](review-metadata.json).

## Scope and confidence

The review covered repository setup, the lifecycle declaration and CI, feature inventory and stories, canonical completion evidence, KG integrity and its tests, API and authorization contracts, frontend build/test/coverage, backend compilation, Neuron tests, deployment scripts, and representative implementation paths across the three application layers. The inventory contains 7,373 tracked files, 231 strict story files, and 41 feature IDs: 35 archived, four planned, two retired, and none active.

This is repo-wide process validation with targeted source inspection and executable reproductions. It is not a line-by-line certification of every source file or a successful full-stack acceptance run. Docker is unavailable, backend test communication is blocked by sandbox socket restrictions, and live Postgres, identity, browser E2E, and Phi validation were not available. No production environment was contacted or deployed. Dependency vulnerability databases were not refreshed; historical advisory claims are not asserted as current findings here.

## Prior findings recovered

The earlier review records are still present:

- [May 26 F0036 plan review](../runs/2026-05-26-aaa8bd7c/plan-review-report.md): the original form-engine plan assumed a schema/rules platform that had not been implemented.
- [May 26 second plan review](../runs/2026-05-26-378ac7da/plan-review-report.md): subsequent scope and implementation-readiness findings.
- [May 30 F0036 completion review](../runs/2026-05-30-6c8cd3ee/feature-review-report.md): verdict **NOT DONE**, with two critical, two high, one medium, and two low findings under that review's severity model.
- [June 30 F0036 remediation run](../runs/2026-06-30-6974ec2c/README.md): later evidence remediation, approved July 1, is the current canonical run selected by `latest-run.json`.

The old findings must not all be carried forward as unresolved code defects:

| Earlier finding | Current disposition |
|---|---|
| RHF/AJV/widget engine missing; preservation wired to no forms | Implementation now exists under `experience/src/features/lob-attributes/engine/` and `experience/src/features/forms/`. The original blanket claim is obsolete. |
| Account-contact edit identity lost on re-auth return | Source now discovers stored form keys, resolves the contact ID, and opens the matching edit form: [AccountDetailPage](../../../../experience/src/pages/AccountDetailPage.tsx#L215). The original omission has been repaired in code; this review did not perform a live forced-login E2E run. |
| Account snapshots retain `taxId` | Both [CreateAccountPage](../../../../experience/src/pages/CreateAccountPage.tsx#L48) and [AccountDetailPage](../../../../experience/src/pages/AccountDetailPage.tsx#L129) now pass `sensitiveFieldPaths: ['taxId']`. |
| Completion documents disagree | Still present. F0036's current feature documents retain planning/draft text and old run references; see R11. |
| Required completion evidence fails | Still present, but the current failing package is the June 30 remediation run, not simply the original May 28 package. Its missing tracker-sync log reference is recorded in R05. |
| Pre-existing integration failures left outside feature scope | The CreateSubmission integration test still fails today; see R09. |

## Findings and missing controls

### R01 — Critical: Neuron conversation ownership trusts an unauthenticated identity

[require_bearer](../../../../neuron/app/main.py#L50) checks only the header prefix. [subject_from_token](../../../../neuron/app/auth.py#L36) decodes the JWT payload without validating its signature, issuer, audience, or expiry. The thread list/detail/history/create/rename/delete handlers use that claimed subject directly against storage. These handlers do not call the engine, so the stated design assumption that the engine authenticates every read/write does not protect them.

**Reproduced:** with the real FastAPI routes and an isolated in-memory repository containing only synthetic data, a forged token with an invalid signature, expired `exp`, and untrusted issuer listed the synthetic victim's conversations (`200`), read their messages (`200`), and deleted the thread (`204`). An empty `Bearer ` header created a conversation (`201`). No real accounts or data were accessed. [Raw result](artifacts/neuron-auth-repro.log), [reproduction script](artifacts/reproduce-neuron-auth.py).

**Missing:** verified identity before any Neuron-owned persistence access, issuer-qualified ownership where applicable, and negative HTTP tests for forged, expired, wrong-issuer/audience, empty, and unauthorized-role tokens. The existing cross-owner tests change `sub`; they do not prove that a caller cannot forge it. F0039's [security review](../runs/2026-07-25-273d5672/security-review-report.md) says every read/write is engine-authorized, which conflicts with this implementation.

**Owner:** AI/backend and Security. Resolve before exposing Neuron to untrusted callers or treating its conversation isolation as validated.

### R02 — High: the production deploy command can select the development stack

[deploy.sh](../../../../scripts/deploy.sh#L183) accepts the base Compose file when no environment-specific file exists. There is no tracked production/staging Compose overlay or Kubernetes deployment in this repository. After the production confirmation flag, the Compose path can therefore use [docker-compose.yml](../../../../docker-compose.yml), whose API runs with `ASPNETCORE_ENVIRONMENT: Development`. In that environment, [Program.cs](../../../../engine/src/Nebula.Api/Program.cs#L51) disables token signature, audience, issuer, and lifetime validation.

**Missing:** a deployment path that refuses development configuration for staging/production, plus concrete production environment configuration. The provided production [nginx.conf](../../../../experience/nginx.conf) serves the SPA but contains no engine/Neuron reverse proxy, while the frontend calls same-origin API paths. A deployer could supply an external gateway, but no complete topology is defined here.

This is a source-confirmed deployment-path defect, not evidence that an existing production deployment uses the configuration. No production command was executed.

**Owner:** DevOps and Security. Require the actual non-development environment configuration and validate its identity and routing behavior before release.

### R03 — High: custom deployment and rollback ignore `--dry-run`

[deploy.sh `run_custom`](../../../../scripts/deploy.sh#L169) and [rollback.sh `run_custom_rollback`](../../../../scripts/rollback.sh#L98) invoke the configured shell command without checking `DRY_RUN`. Both subsequently print that a dry run completed.

**Reproduced safely:** custom development commands that only wrote marker files under `/tmp` were executed with `--dry-run`; both markers were created. [Exact commands and marker results](artifacts/dry-run-reproduction.json), [deployment output](artifacts/deploy-dry-run.log), [rollback output](artifacts/rollback-dry-run.log). No deployment or rollback occurred.

**Missing:** enforce dry-run semantics before custom command execution, with a regression check demonstrating that an arbitrary custom command is not invoked.

**Owner:** DevOps.

### R04 — High: shipped billing and outbound-document UI calls lack dev proxy routes

The API client uses an empty [API_BASE](../../../../experience/src/services/api.ts#L24), so calls go to the frontend origin. [Vite's proxy list](../../../../experience/vite.config.ts#L38) omits the prefixes used by billing and outbound-document hooks:

`/billing-invoices`, `/payment-receipts`, `/payment-receipt-imports`, `/payment-applications`, `/reconciliation-exceptions`, `/reconciliation-backlog`, `/billing-corrections`, and `/outbound-documents`.

The backend endpoints exist. The frontend's standard development setup does not forward these requests to them. A static comparison identified 15 affected call sites across [billing hooks](../../../../experience/src/features/billing/hooks.ts) and [document hooks](../../../../experience/src/features/documents/hooks.ts). [Exact paths and lines](artifacts/proxy-gaps.json).

**Missing:** routing for all shipped API prefixes and a real frontend-to-backend smoke check for F0026/F0027. Mocked hook/component tests cannot detect missing proxy configuration. Live browser reproduction was unavailable in this environment.

**Owner:** Frontend and QE.

### R05 — High: completion evidence is not consistently durable or closeout-valid

The full feature-evidence validator returned **294 error records across ten archived features**, plus three warnings. These are not 294 distinct product bugs: a single missing artifact can trigger several validation rules. Eighteen archived features fall under the validator's effective-date boundary; eight pass, ten fail. Seventeen earlier archives and two retired features are explicitly exempt from this validator, rather than being counted as passes.

| Feature | Selected canonical run | Error records | Main issue |
|---|---|---:|---|
| F0017 | `2026-06-07-771a5ef6` | 185 | Missing test/coverage artifacts, manifest file references, and scratch paths |
| F0008 | `2026-07-03-fd732693` | 56 | Missing artifacts, temporary paths, missing TRX |
| F0024 | `2026-07-03-ba011af8` | 19 | Missing artifacts, temporary paths, coverage claims without retained proof |
| F0022 | `2026-07-03-b9f40b31` | 15 | Missing referenced artifacts |
| F0028 | `2026-07-02-736e7854` | 9 | Missing coverage references; an `.env` reference is unsuitable as durable proof |
| F0027 | `2026-07-02-b9316621` | 6 | Missing referenced coverage artifacts |
| F0019 | `2026-06-30-6187bd30` | 1 | Canonical lifecycle log lacks tracker-sync invocation/reference |
| F0023 | `2026-06-30-691ec6b8` | 1 | Same closeout-log omission |
| F0035 | `2026-06-30-bac66bac` | 1 | Same closeout-log omission |
| F0036 | `2026-06-30-6974ec2c` | 1 | Same closeout-log omission |

Passing governed packages: **F0021, F0025, F0026, F0032, F0037, F0038, F0039, F0040**. In particular, F0040's current completion package passes; its superseded plan run is not the completion evidence.

See [machine-readable errors](artifacts/feature-evidence.json) and [validator output](artifacts/feature-evidence.log). The external framework was clean at commit `9a68f7a9c6ba9a0b13d2b5814e06b7fed055e836`, using the default effective date `2026-05-19`. These results establish present reproducibility gaps; they do not by themselves establish that an older run violated the exact validator version used at the time.

**Missing:** retained raw artifacts in durable locations, accurate latest-run pointers and logs, and explicit treatment of validator-version changes. The repository-scoped disposition assigns recovery or re-entry, while the one-off contract concession preserves the original approved decisions as historical assertions when recovery is impossible. Restore original evidence where available; otherwise record a new, correctly identified revalidation for any reopened feature. Do not fabricate old outputs, copy secrets into evidence, rewrite historical verdicts, or weaken the shared framework contract.

**Owner:** PM/evidence owner and QE, with framework compatibility review where needed.

### R06 — High: the frontend quality gate neither proves freshness nor enforces coverage

The [latest frontend manifest](../frontend-quality/latest-run.json) still identifies **F0015, March 21**. Its required visual report is absent, so the declared lifecycle gate currently fails. [Failure output](artifacts/frontend-evidence.log).

Separately, [the validator](../../../../planning-mds/testing/validate-frontend-quality-gate.py) accepts any nonempty date string, does not bind the run to the current code revision, does not evaluate `coverage_target`, and skips existence checks for generated coverage artifacts. An in-memory probe using an existing old log, `recorded_on: not-a-date`, and explicitly failed zero-percent coverage returned no validation errors after the missing visual path was replaced with an existing log. [Probe result](artifacts/frontend-gate-negative-probe.json).

Fresh coverage under the current Vite configuration was **73.51% lines/statements, 60.78% functions, 76.40% branches**. That configuration includes 78 test files in coverage. Removing only `.test.ts/.test.tsx` entries from the same report yields **67.48% lines/statements, 58.37% functions, 72.31% branches**; this is still not a separately agreed business-logic-only denominator. [Original coverage summary](artifacts/frontend-coverage-summary.json), [recalculated summary](artifacts/frontend-coverage-without-test-files.json).

**Missing:** a fresh, revision-bound manifest; retained artifacts; a defined coverage scope excluding tests; and actual threshold/exception enforcement. The documented 80% target should not be inferred from a stale `met: true` field. Fixing the missing HTML alone would leave the validation gap intact.

**Owner:** QE and Frontend.

### R07 — High: repository CI does not enforce application or lifecycle readiness

The only tracked workflow, [kg-reproducibility.yml](../../../../.github/workflows/kg-reproducibility.yml), checks compiled KG projections and tracker regions. It runs neither application builds/tests nor the other declared lifecycle gates, policy parity, feature evidence, or security scans. Its push trigger excludes `main`; remote branch-protection settings were not inspected.

[lifecycle-stage.yaml](../../../../lifecycle-stage.yaml) still leaves API structural, infrastructure, and strict security gates in the post-split Phase B backlog. `release-readiness` has the same three required gates as implementation, even though its description says additional gates are needed before use. F0040's [security report](../runs/2026-09-01-fd408477/security-review-report.md) still records scanner waivers and CI hardening as a follow-up.

**Missing:** an enforced path from code change to current build/test/security/evidence results, and a release stage that requires the release controls it describes. A green KG workflow cannot be interpreted as a green product. No pipeline was added by this one-off review.

**Owner:** DevOps, QE, and Security.

### R08 — Medium: authorization intent and policy no longer agree

[check-policy-parity.py](../../../../scripts/check-policy-parity.py) fails with seven additional BrokerUser grants. Six concern document/template actions now described in separate authorization sections, so they require reconciling the checker's narrow F0009 scope with the later F0020 rules rather than automatically removing valid grants.

The account permission is a direct contradiction: [authorization matrix §2.11](../../../../planning-mds/security/authorization-matrix.md#L780) says BrokerUser has no account access, while [policy.csv](../../../../planning-mds/security/policies/policy.csv#L375) grants `account, read`. Account handlers consult that policy and the service implements broker scoping. This review did not run live database-backed authorization tests or establish which intent is correct. [Parity output](artifacts/policy-parity.log).

**Missing:** one current, explicit BrokerUser permission contract and parity tests covering later feature additions. Resolve the account decision with the owner; do not silently infer that the broadening was approved.

**Owner:** Architect, Security, and backend.

### R09 — Medium: the current automated test/lint baseline is red

- **Frontend:** 333 of 334 tests pass. `CreateSubmissionPage.integration.test.tsx` fails both in the complete run and alone. It selects Cyber but omits attributes now required by the create form, so navigation to the expected detail heading never occurs. This appears to be stale acceptance-test setup, not proof that valid Cyber submissions cannot be created. [Isolated result](artifacts/submission-regression.json), [test source](../../../../experience/src/pages/tests/CreateSubmissionPage.integration.test.tsx#L79).
- **Lint:** one blocking unused `Page` import in [f0037-distribution-rollups.spec.ts](../../../../experience/tests/e2e/f0037-distribution-rollups.spec.ts#L1), plus 15 warnings. [Output](artifacts/frontend-lint.log).
- **KG tools:** 182 tests pass, five fail. The failures include hardcoded old feature counts, next ID `F0041` instead of `F0042`, old roadmap ordering, and old source-document expectations. These are test-maintenance gaps; KG reproducibility itself passes. [Output](artifacts/kg-tests.log).

**Missing:** a green maintained baseline and ownership of inherited failing tests. Scope-limited feature success has allowed unrelated reds to persist. Fix test fixtures to express current contracts and stable invariants, without weakening required behavior just to obtain a pass.

**Owner:** QE, Frontend, and KG tooling owner.

### R10 — Medium: API validation covers too little, and Neuron's error contract drifts

The product's [solution-contract validator](../../../../planning-mds/testing/validate-nebula-api-contract.py) checks the reusable error shape plus two broker stories. The engine spec now has **194 operations across 154 paths**; Neuron has a separate spec with **10 operations across seven paths**, which is not included in that gate. There is no broad implementation/spec equivalence check in the tracked CI.

The framework validator passes the engine spec with 80 advisory warnings and fails the Neuron spec with six errors. Neuron's `ProblemDetails` omits `code` and `traceId` and a required-field list; the runtime [_problem](../../../../neuron/app/main.py#L30) omits these fields too, contrary to the product's [error profile](../../../../planning-mds/architecture/api-guidelines-profile.md#L28). Runtime `/ready` is absent from the Neuron OpenAPI document.

Not every generic validator diagnostic is a product defect: its `/health` media-type complaint and 403 warnings require interpreting the specific profile, including intentional 404 non-disclosure. Similarly, the generic strict infrastructure check complains about a root Dockerfile although this monorepo has layer Dockerfiles. Those validators need product-specific adaptation, not blind compliance. [Neuron contract output](artifacts/openapi-neuron.log), [engine output](artifacts/openapi-engine.log), [infrastructure output](artifacts/infra-strict.log).

**Missing:** declared Neuron profile exceptions or contract alignment, documented readiness endpoints, and implementation-backed contract checks for the actual application surface.

**Owner:** Architect, backend, AI, and QE.

### R11 — Medium: terminal feature summaries retain superseded planning/run state

F0036 [STATUS.md](../../../../planning-mds/features/archive/F0036-dynamic-product-attribute-form-engine/STATUS.md) says Done and still names the May 28 run. Its README/PRD still say plan complete/pending readiness confirmation, and its assembly plan says Draft. Its canonical [latest-run pointer](../features/F0036-dynamic-product-attribute-form-engine/latest-run.json) selects the June 30 remediation instead. The historical May 28 evidence README also says draft; that historical artifact should be interpreted through supersession, not silently rewritten.

The latest F0040 [STATUS.md](../../../../planning-mds/features/archive/F0040-neuron-second-specialist-head/STATUS.md) says implementation and G0–G8 are complete but retains plan-time prose about the future feature action, tracker validation with evidence skipped, and dependency evidence being audit-pending. These statements need an explicit historical/current boundary. They do not negate its passing canonical completion package.

The structural tracker validator reports zero errors despite these contradictions. Its PASS is narrower than the governance claim that all current planning and execution summaries are trustworthy.

**Missing:** current summaries reconciled to canonical runs, explicit dispositions of earlier findings, and historical labels for retained planning text. Preserve append-only history; update current interpretation and links.

**Owner:** PM and Architect.

### R12 — Medium: a fresh checkout lacks complete run and release-operating instructions

[README.md](../../../../README.md#L30) asks for .NET 8 although the projects and Dockerfile target .NET 10; `docker compose up -d db authentik` names a nonexistent service; and `dotnet run --project engine/<api-project>` remains a placeholder. The migration-provenance link points to a missing document. The architecture assembly plan also has a stale pre-archive F0040 link. A scan of 78 current docs found those two broken Markdown file links; it did not validate every historic log reference. [Link results](artifacts/current-doc-links.json).

Beyond setup, no product backup/restore drill, recovery objectives, or deploy/rollback runbook for the provided Compose topology was found in the operational material. The rollback runner supports custom/Kubernetes paths, whereas the checked-in runnable stack is Compose. PostgreSQL, document storage, Neuron-owned migrations, identity, and application deployment need a coherent recovery/cutover procedure before real customer onboarding.

**Missing:** executable setup instructions and an explicit release operations package covering environment configuration, identity/TLS/routing, migrations, database/document backup and restore, rollback, health/alerts, and ownership. These are release gaps; the repository correctly describes its current posture as public preview.

**Owner:** DevOps, backend, and documentation owner.

## Product scope still missing by design

These are roadmap/acceptance boundaries, not newly discovered failures of archived MVPs:

| Capability | Current status and implication |
|---|---|
| Customer data import, deduplication, and go-live migration | **F0031 is Next, still planned.** Its PRD explicitly identifies this as necessary for customer adoption. Existing narrow policy/document/mock receipt imports do not constitute the planned cross-entity migration pipeline. |
| Production integrations and data exchange | **F0030 is Later.** Real bank/vendor transport and broader external synchronization remain outside delivered scope. |
| External broker collaboration portal | **F0029 is Later.** Do not infer a delivered portal from the presence of a BrokerUser role. |
| Contextual AI adjudication | **F0041 is gated and planned.** F0039 ships in shadow mode because its recorded routing-accuracy gates are red. A reviewed green evaluation and stable direct routing are prerequisites. |
| Further live AI heads and cross-zone reasoning | Renewals and broker activity are live; Tasks/Pipeline, cross-zone composition, real outbound send, and external hosts remain deferred. |
| Complete billing/accounting operation | F0026 is agency-bill MVP with manual/mock receipts and exact reconciliation. Direct bill, real vendor transport, partial/overpayment handling, write-offs, refunds, settlement, ledger, tax, and statements remain deferred. |
| Durable Temporal business workflows | Infrastructure is provisioned, but [ADR-010](../../../../planning-mds/architecture/decisions/ADR-010-temporal-durable-workflow-orchestration.md#L48) explicitly defers renewal workflow adoption. No application worker should be assumed to exist. |

The unfilled release-operations work in R02/R07/R12 should be assigned explicitly alongside F0031. It does not disappear because the functional feature registry has many archived rows.

## Validation results

| Check | Current result | Evidence / limitation |
|---|---|---|
| KG integrity | PASS with warnings | [kg.log](artifacts/kg.log): five stale symbol references plus one low-confidence edge |
| KG drift/symbol/decision/orphan/coverage checks | PASS with warnings | [kg-extra.log](artifacts/kg-extra.log): also orphan nodes and seven binding gaps; advisory, not clean traceability |
| KG reproducibility | PASS | [reproducibility.log](artifacts/reproducibility.log) |
| Structural trackers, evidence skipped | PASS, zero errors/warnings | [trackers.log](artifacts/trackers.log); does not imply completion evidence passes |
| Complete feature-evidence audit | FAIL | Eight governed packages pass; ten fail; 294 error records |
| Story validation | FAIL on 15 of 231 files | [stories.log](artifacts/stories.log): F0004's six, F0005's four, F0009's five fail current template checks. These are historical format gaps, not proof that their code is unfinished. |
| Product API contract gate | PASS | [api-contract.log](artifacts/api-contract.log); limited scope described in R10 |
| Frontend evidence gate | FAIL | Missing visual artifact; stale manifest |
| BrokerUser policy parity | FAIL | Seven differences; see R08 for interpretation |
| Backend solution build | PASS | [backend-build.log](artifacts/backend-build.log), cached restore, single-process build; compiler warnings remain |
| Backend unit tests | BLOCKED | [backend-unit-retry.log](artifacts/backend-unit-retry.log): test host socket denied. No passing backend test count is claimed. |
| Backend integration / full runtime | NOT RUN | Docker unavailable |
| Frontend production build | PASS | [frontend-build.log](artifacts/frontend-build.log); approximately 1.145 MB minified JS chunk, 296 KB gzip, size warning |
| Frontend lint | FAIL | One error, 15 warnings |
| CSS/theme/effects checks | PASS | Corresponding logs in artifacts |
| Frontend tests | FAIL | 333 passed, one failed; failed test also reproduced alone |
| Frontend coverage | GENERATED, suite still fails | Coverage collected with `reportOnFailure`; percentages and scope in R06 |
| KG tooling tests | FAIL | 182 passed, five failed |
| Neuron available suite | PARTIAL PASS | [neuron-final-tests.log](artifacts/neuron-final-tests.log): 402 passed, 40 skipped, seven deselected, 12 subtests passed |
| Neuron integration exclusions | UNVERIFIED | 27 Postgres tests and 13 live Phi tests skipped; seven `ThreadHttpSurfaceTest` tests excluded after TestClient timeout. Initial external telemetry calls also stalled; final test environment uses a bounded unavailable local engine URL. |
| Synthetic Neuron auth reproduction | VULNERABILITY CONFIRMED | Real ASGI routes, in-memory data; independent of the timed-out TestClient class |
| Browser E2E/visual/performance | NOT RUN | No live stack/browser acceptance environment |
| Fresh dependency/secrets/SAST/DAST scans | NOT RUN | No current security-scan pass is claimed |

## Suggested repair order

1. **Close the trust and execution defects:** R01 conversation authentication, R02 production/development separation, and R03 dry-run behavior.
2. **Restore working user journeys and a meaningful baseline:** R04 proxy routes, R08 permission intent, R09 failing checks, and R06 current quality evidence/coverage enforcement.
3. **Reconcile proof of completion:** restore/revalidate R05 artifacts, update R11 current summaries, and retain explicit dispositions for the recovered review findings.
4. **Complete release validation and operation:** R07/R10 enforcement and contracts, R12 operating procedures, and F0031 migration readiness. Run real identity/Postgres/browser/restore scenarios in the intended deployment environment before release signoff.

These are recommendations from this one-time validation. The follow-up fixed
repository defects and refreshed current evidence without rewriting prior
historical runs, changing the release stage, activating a feature, or creating a
recurring process.
