# Code Quality Review Report

Scope: PR #56 — F0037 Hierarchy-Aware Access Scoping & Distribution Rollups (standalone, path-set)
Date: 2026-07-11
Diff range: `e2f78be...27a5162` (merge-base with `main` → PR head `pr-56`)

## Summary

- Assessment: **REQUEST CHANGES**
- Files reviewed: 32 hand-authored source/test files (backend enforcement + scope resolution, data
  access, API endpoints, authorization policy, frontend rollup surface, tests). ~179 files in the PR;
  the large remainder is regenerated KG/evidence output, spot-checked only.
- Total issues: 5 (0 critical / 2 high / 1 medium / 2 low)

The feature is well-structured and the security-critical intent — resolve a single distribution scope
before any rows/counts/facets/drilldowns/rollups materialize — is implemented consistently across the
search, operational-report, broker-insight, and rollup read paths. Endpoints are correctly protected,
deny-by-default holds, and the requested-scope path fails closed. The blocking concerns are (1) a
scope-composition **algebra** question that, as written, can hide records a user is entitled to see, and
(2) a **test-depth gap**: the actual database predicates that enforce no-leak are not exercised against a
real DB. Neither is a confidentiality leak.

## Findings by Severity

### Critical Issues (must fix before approval)

None.

### High Priority (should fix)

**CR-H1 — Scope dimensions are combined with AND (intersection), layered on the legacy owner/region
base filter; confirm this matches the PRD's union-of-scopes intent.**
- Location: `engine/src/Nebula.Infrastructure/Repositories/OperationalReportProjectionRepository.cs:31-45`;
  `SearchDocumentRepository.cs` `ApplyVisibility` (`:150-170`); `BrokerInsightProjectionRepository.cs:27-45`.
- What: For a non-`SeeAll` user the predicate is
  `(OwnerUserId == me OR Region ∈ regions)  AND  BrokerId ∈ brokerIds  AND  TerritoryId ∈ territoryIds  AND  OwnerUserId ∈ producerIds`.
  The broker/territory/producer authority sets are chained with `.Where` (AND) *on top of* the F0023
  owner/region base filter. Because they only ever AND, the managed-broker / territory authority can
  **only narrow** the owner/region result — it can never *grant* the hierarchy/territory visibility this
  feature exists to provide.
- Why it matters: The PRD ("users see only the channel data they are entitled to see") and F0037-S0002
  role-visibility ("DistributionManager — managed subtree **and** territory scoped records";
  "RelationshipManager — assigned broker, producer, **and** territory scoped records") describe a **union**
  of authorized scopes. As written the composition is an intersection, so entitled rows are dropped.
- Failure scenario: A `DistributionManager` who manages broker `B1` but whose token `regions` claim does
  **not** include `B1`'s region (or who has an empty `regions` claim) sees none of `B1`'s records:
  `(owner==me[false] OR region∈R[false]) AND broker∈{B1}[true]` → `false`. With an empty `regions` claim
  the manager sees only records they personally own, and the entire managed-broker/territory mechanism is
  inert. Separately, any managed-broker projection row with `Region == null` is always dropped for a
  non-owner manager. The e2e currently masks this because its `DistributionManager` token is granted all
  four regions (`['West','Central','East','South']`).
- Direction: this is **under-return** — safe for confidentiality (no leak), but it fails the S0002 happy-path
  AC. Confirm the intended algebra. If union is intended, the broker/territory/producer/owner/region
  clauses should be OR-combined (e.g., `seeAll || owner==me || region∈R || broker∈B || territory∈T ||
  producer∈P`) while keeping the requested-scope narrowing (which correctly uses intersection in
  `DistributionScopeService.IntersectOrFail`). If intersection-with-region is genuinely intended (managers
  are always granted all regions), document that invariant with a `// WHY:` and add a test that pins it.

**CR-H2 — The real row-level EF predicates that enforce no-leak are not exercised against a database;
unit tests re-implement the predicate in mocks and the e2e only proves the guaranteed-empty case.**
- Location: unit mocks `OperationalReportServiceTests.cs` (`RptRepo`/`InsightRepo`), `SearchServiceTests.cs`
  (`SScope`/`SrchRepo`), `BrokerInsightServiceTests.cs` (`BrokerInsightScope`); e2e
  `experience/tests/e2e/f0037-distribution-rollups.spec.ts`; untested: `DistributionScopeRepository.cs`
  (ancestry-path + as-of join logic), and the three projection repositories' `QueryAsync`/`ApplyVisibility`.
- What: The service-layer unit tests inject fake repositories that *duplicate* the visibility predicate in
  memory (LINQ-to-objects). They therefore validate the service, not the shipped EF predicate — a
  divergence like CR-H1 between the mock and the real `.Where` chain cannot be caught. The only test
  touching a real DB is the Playwright e2e, which asserts the scoped-away (`rootNodeId =
  00000000-0000-0000-0000-000000000000`) and external-user cases return empty — i.e. it proves the *deny*
  direction with a GUID that matches nothing, never positive/negative discrimination on a populated set.
- Why it matters: F0037-S0002 is **Critical** priority and its Non-Functional Expectations explicitly
  require "Tests must prove predicates run before pagination, counts, and related links" and
  "Sibling hierarchy branches are excluded from rows and counts." That proof currently exists only at the
  mock/service layer, not at the DB-predicate layer where the real enforcement lives. Per the code-reviewer
  severity framework, missing test evidence for changed security-sensitive behavior is at least High.
- Failure scenario: A regression that makes a projection repository OR the broker/territory filters (or
  drops the `!HasScope → Where(false)` guard) would pass every current unit test (mocks are unaffected) and
  pass the e2e (empty stays empty), yet leak sibling-branch rows in production.
- How to fix: Add a `CustomWebApplicationFactory` integration test that seeds a minimal hierarchy
  (root → child brokers `B1`/`B2` in territories `T1`/`T2`, a producer, and a few `OperationalReportProjection`
  / `SearchDocument` / `BrokerInsightProjection` rows) and asserts, against the real EF predicates, that a
  scoped manager sees `B1`'s rows and **not** sibling `B2`'s in rows, counts, and facets — plus an as-of
  boundary case for `DistributionScopeRepository` (expired vs. active assignment).

### Medium Priority (nice to have)

**CR-M1 — `ProjectionVisibilityResolver` is dead code after the migration, and its `SeeAll` role set
diverges from `DistributionScopeService`.**
- Location: `engine/src/Nebula.Application/Services/ProjectionVisibilityResolver.cs`.
- What: On the base commit this static resolver had callers in `SearchService`, `OperationalReportService`,
  and `BrokerInsightService`; this PR migrates all three to `IDistributionScopeService.ResolveAsync`,
  leaving the class with **zero callers**. It also treats `Admin`, `ProgramManager`, and
  `DistributionManager` as `SeeAll`, whereas `DistributionScopeService` treats only `Admin` as full-scope.
- Why it matters: Dead code with a *different, broader* authorization semantic is a trap — if anyone
  re-wires a read path to the old resolver, `ProgramManager`/`DistributionManager` silently regain see-all.
- How to fix: Delete the class (preferred), or, if kept for reference, add a `// WHY:` documenting that it
  is superseded by `DistributionScopeService` and must not be used for enforcement.

### Low Priority (optional improvements)

**CR-L1 — `CanReadBrokerAsync` passes a broker id into the `RootNodeId` slot; the shared-identity
invariant is implicit.**
- Location: `engine/src/Nebula.Application/Services/DistributionScopeService.cs:107-111`
  (`new DistributionScopeRequest(brokerId, null, null, asOf)`).
- What: This is correct given the model (a Broker is a `DistributionNode` with `NodeType == "Broker"` and a
  shared GUID, confirmed by `CanReadBrokerAsync_ReturnsFalseForHiddenSibling`), but a reader sees a broker
  id flowing into a parameter named `RootNodeId` and reasonably suspects a copy-paste bug.
- How to fix: Add a `// WHY:` noting that broker ids double as distribution-node ids, so hierarchy
  resolution over the broker node is the intended broker-scope check.

**CR-L2 — Rollup rows/totals emit `0` for cross-family metrics with `UnavailableReason = null` rather than
marking them unavailable.**
- Location: `engine/src/Nebula.Application/Services/OperationalReportService.cs` `MetricsFor(...)` and the
  `DistributionRollupRowDto(... UnavailableReason: null)` construction.
- What: A `Production`/`Activity` rollup routes to the broker-insight projection, where `WorkflowOpen`/
  `WorkflowOverdue` are hard-coded to `0`; conversely a `Workflow` rollup zeroes production/activity. The
  DTO carries an `UnavailableReason` field but it is always `null`, so the UI renders `0` for a metric that
  is actually not computed for that family.
- Why it matters: PRD Risks & Mitigations: "Missing metrics should be explicitly unavailable rather than
  fabricated." A displayed `0` reads as a real value.
- How to fix: Set `UnavailableReason` (e.g. `"not-applicable-for-metric-family"`) for the zeroed metrics
  and have the view show "—/unavailable" instead of `0`.

## Pattern Compliance

- [x] Clean architecture layers respected — scope resolution in Application, EF predicates in
      Infrastructure, endpoints thin; DTOs used at boundaries (no entities leak to controllers).
- [x] SOLID — `IDistributionScopeService` / `IDistributionScopeRepository` cleanly separate policy from
      data access; enforcement injected into the three read services.
- [x] SOLUTION-PATTERNS — visibility-predicate-first matches the F0023 projection-visibility pattern.
- [x] Frontend UX — empty / no-access / loading (`aria-busy`) states present; accessible table; the
      `Sidebar.isActive` query-string fix is correct.
- [x] Naming conventions consistent.
- [~] Error handling appropriate — repository/service paths are fine; see CR-L2 for the
      unavailable-vs-zero surfacing gap.

## Evidence Summary

- Runtime validation outputs reviewed: PR-checked-in test sources only (no container execution this pass —
  see `lifecycle-gates.log`).
- Coverage artifact path(s): none executed for this standalone pass; feature-run coverage lives under
  `runs/2026-07-06-2e7e606d/coverage-report.md` (out of scope here).
- Layer exceptions / skips: regenerated KG artifacts excluded from deep review (generated output).

## Test Quality

- Unit test coverage: good breadth at the **service** layer — external fail-closed, scope-echo,
  aggregation-over-filtered-rows, non-see-all region pass-through, sibling-denial at the scope-resolver
  layer (`DistributionScopeServiceTests`). Casbin allow/deny matrix for `distribution_rollup:read` is
  covered for 8 roles.
- Integration test coverage: `TerritoryEndpointTests` covers territory CRUD/authz (403 path) but not the
  scope/projection **predicates** against a DB — see CR-H2.
- E2E coverage: proves deny/empty and the tab/filter surface; does not prove positive/negative
  discrimination on populated data.
- Fast-layer proof for changed behavior: **partial** — the DB-predicate layer is unproven (CR-H2).

## Acceptance Criteria

- [x] Rollup contract (totals, grouping, asOf, generatedAt, scope echo, drilldown links) delivered.
- [x] External/Broker users denied; deny-by-default on `!HasScope`.
- [~] "Only records within the resolved hierarchy/territory/producer scope are returned" — at risk from
      CR-H1 (under-returns entitled rows under a union reading).
- [~] "Sibling branches excluded from rows and counts" — proven at the resolver/mock layer, not the DB
      predicate layer (CR-H2).

## Behavioral change (noted, not a finding)

Global search `SeeAll` is tightened from `{Admin, ProgramManager, DistributionManager}` (legacy resolver)
to `{Admin}` only (new service) — visible in the `SearchServiceTests` rename
`SearchAsync_BroadRole_GetsSeeAllVisibility → SearchAsync_Admin_GetsSeeAllVisibility`. This is intended per
F0037 (hierarchy scoping replaces broad see-all) and is the safe direction; flagging for release-note
awareness since it narrows what those two roles see in search.

## Recommendation

**REQUEST CHANGES** — resolve CR-H1 (confirm/fix the scope-composition algebra) and CR-H2 (add a DB-level
integration test proving sibling exclusion) before merge. CR-M1/CR-L1/CR-L2 are cleanups. Both High items
are under-return / test-depth concerns, **not** confidentiality leaks — so the R2 approver may legitimately
choose "approve with justification" if the team confirms managers are always granted all regions (making
CR-H1 a no-op in practice) and commits to the integration test as fast-follow.

## Action Items

1. [high] Confirm union-vs-intersection for the visibility predicate; fix the three repositories or
   document the all-regions invariant with a pinning test. — owner: backend; follow-up: F0037 fast-follow.
2. [high] Add a `CustomWebApplicationFactory` integration test seeding a small hierarchy and asserting
   sibling exclusion + as-of boundary against the real EF predicates. — owner: QE/backend; follow-up: F0037 fast-follow.
3. [medium] Remove or annotate the dead `ProjectionVisibilityResolver`. — owner: backend; follow-up: deferred-no-followup.
4. [low] Add `// WHY:` on the `CanReadBrokerAsync` broker-id-as-node-id call. — owner: backend; follow-up: deferred-no-followup.
5. [low] Populate `UnavailableReason` for cross-family zeroed rollup metrics. — owner: backend/frontend; follow-up: deferred-no-followup.

---

## Re-Review Addendum — Cycle 2 (post-fix)

Date: 2026-07-11 · Trigger: R2 user decision **"fix all high"** · Fix branch: `fix/F0037-scope-review` (from `pr-56`)

Per `agents/actions/review.md`, the fixes below were applied and the review re-run. The scope-composition
policy fork behind CR-H1 was decided by the user: **authority union** for the default view.

### Updated verdict: **APPROVED WITH RECOMMENDATIONS**
*(contingent on the CR-H2 integration test passing in CI — see CR-H2 outcome). New finding counts: 0 critical / 0 high / 1 medium→0 / 3 low.*

### Finding outcomes

- **CR-H1 — FIXED.** The visibility predicate now branches on a new `ProjectionVisibility.ExplicitScopeRequested`
  flag (`SearchDtos.cs`), set by `DistributionScopeService`. Default (unfiltered) view uses an **authority
  union** — `owner OR region OR authorized-broker OR authorized-producer` — in all three repositories
  (`OperationalReportProjectionRepository`, `SearchDocumentRepository.ApplyVisibility`,
  `BrokerInsightProjectionRepository`). Territory is deliberately excluded from the union (derived from
  broker authority → OR-ing it would leak sibling brokers sharing a territory); it only narrows explicit
  requests. Explicit-filter requests keep AND-narrowing, and admin narrowing via explicit filter is
  preserved. WHY comments added. Verified: 314/314 unit tests pass, including two new real-`DistributionScopeService`
  guards (`ResolveAsync_DefaultManagerView_UsesAuthorityUnionAndIsNotExplicitlyScoped`,
  `ResolveAsync_ExplicitTerritoryFilter_IsExplicitlyScoped`) and the mock repos updated to mirror the real predicate.

- **CR-H2 — ADDRESSED (CI-gated).** Added `engine/tests/Nebula.Tests/Integration/DistributionScopeReadScopingTests.cs`:
  a repository-level test that seeds a small hierarchy in a real PostgreSQL (Testcontainers) and asserts,
  against the **real EF predicates**, (1) a manager sees managed-broker rows across regions and **not** the
  sibling broker (union + no-leak), (2) an out-of-authority `rootNode` request fails closed to an empty
  scope, (3) `ListBrokerIdsForTerritoryAsync` excludes assignments not effective on `AsOf`. It **compiles**
  cleanly; it was **not executed locally** because Docker/Testcontainers is unavailable in this environment
  — it runs in CI. Final approval is contingent on this suite passing in CI.

- **CR-M1 — FIXED (bonus).** Dead `ProjectionVisibilityResolver` deleted (0 callers; also removed the
  divergent see-all role set). Verified no remaining references.

- **CR-L1 — FIXED (bonus).** Added a `// WHY:` on `CanReadBrokerAsync` explaining the broker-id-as-node-id invariant.

- **CR-L2 — OPEN (unchanged).** Rollup `UnavailableReason` still null for cross-family metrics (Low; out of "fix all high" scope).

- **CR-L3 — NEW (Low).** In an **explicitly-filtered** view (e.g. a manager passing `territoryId`), the
  requested path still ANDs the owner/region base with the requested filter, so a managed-broker row that is
  out-of-region and not owned is dropped even though the default view would show it. This is the same
  under-return as CR-H1 but only on explicitly-filtered views, and is under-return (safe for
  confidentiality). Fully fixing it requires separating authority sets from request sets in
  `ProjectionVisibility`; the user's decision scoped the union to the default view, so this is tracked as Low.
  — owner: backend; follow-up: deferred.

### Changeset (vs `pr-56`)

`SearchDtos.cs`, `DistributionScopeService.cs`, `OperationalReportProjectionRepository.cs`,
`SearchDocumentRepository.cs`, `BrokerInsightProjectionRepository.cs` (source); `ProjectionVisibilityResolver.cs`
(deleted); `DistributionScopeServiceTests.cs`, `OperationalReportServiceTests.cs`, `SearchServiceTests.cs`,
`BrokerInsightServiceTests.cs` (updated tests); `DistributionScopeReadScopingTests.cs` (new integration test).
Build: succeeded (0 errors). Unit tests: 314 passed / 0 failed.

---

## Re-Review Addendum — Cycle 3 (post-approval low cleanup)

Date: 2026-07-11 · Trigger: R2 decision **"approve + fix issues anyway (CR-L2 / CR-L3 / SEC-L1)"**

### Updated verdict: **APPROVED** (0 critical / 0 high / 0 medium / 0 low open) — contingent on CR-H2 CI run.

### Finding outcomes

- **CR-L3 — FIXED.** `ProjectionVisibility` now carries the **authority union** in `BrokerIds`/`TerritoryIds`/
  `ProducerUserIds` and the explicit **narrowing** request in new `RequestedBrokerIds`/`RequestedTerritoryIds`/
  `RequestedProducerUserIds`. All three repositories were unified to `authScope(union) AND Requested*(narrow)`,
  so even an explicitly-filtered view keeps managed-broker rows within the requested slice regardless of
  region/ownership. The fail-closed `IntersectOrFail` logic is unchanged (critical property preserved).
  New guard: `ResolveAsync_ExplicitTerritoryFilter_IsExplicitlyScoped` now asserts both the Requested set and
  the preserved authority union. 314/314 unit tests pass.

- **CR-L2 — FIXED.** `DistributionRollupReportView` renders `—` (not a fabricated `0`) for metric columns not
  applicable to the selected metric family (Production/Activity rollups don't compute workflow open/overdue or
  cross-family counts); `StatTile` accepts `null → —`. Two frontend tests added; `tsc` clean; 4/4 rollup tests pass.

- **SEC-L1 — FIXED.** `DistributionScopeService` now emits a structured `LogWarning` on out-of-authority scope
  denial (`requested_scope_outside_authority`) — a server-side detection signal for scope probing, recording
  user/roles/requested-dimensions. No existence is disclosed to the caller (still a no-leak empty scope). Logger
  is an optional ctor param (DI-injected in prod; `NullLogger` in tests), so no test churn.

- **CR-L1 (removed line 5 item)** and CR-M1 were already fixed in Cycle 2.

### Cycle-3 changeset (vs `pr-56`)

Adds to the Cycle-2 changeset: `SearchDtos.cs` (+3 Requested* fields), `DistributionScopeService.cs` (unified
resolve + logging), the three repositories (unified predicate), test mocks/stubs; frontend
`DistributionRollupReportView.tsx`, `ReportShared.tsx`, and the view test. Backend build succeeded; **314/314
unit tests pass**; frontend `tsc` clean; **4/4** rollup view tests pass. CR-H2 integration test still CI-gated.
