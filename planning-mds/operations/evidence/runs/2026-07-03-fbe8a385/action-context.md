# Action Context - F0008 Standalone Test Run

## Run Identity

- Action: test
- Mode: standalone
- Test scope: all
- Feature under test: F0008 Broker Insights
- Test run ID: 2026-07-03-fbe8a385
- Parent feature run ID: 2026-07-03-fd732693
- Product root: `/Users/wallstreet48/nebula-feature-26/nebula-insurance-crm`
- Agents root: `/Users/wallstreet48/nebula-feature-26/nebula-agents`

## Inputs

- Operator requested implementation of the post-closeout testing plan.
- Operator required strict use of the nebula-agents harness.
- Approved feature evidence is `planning-mds/operations/evidence/runs/2026-07-03-fd732693`.

## Assumptions

- F0008 implementation is already approved; this test run does not reopen feature closeout.
- Runtime testing may require approved Docker and localhost escalation.
- Frontend tests run on host Node because no frontend Dockerfile or Compose service exists.
- Authenticated API smoke uses the documented development JWT behavior.

## Scope Boundaries

- In scope: runtime rebuild/restart, API health, F0008 backend tests, frontend component/build tests, API smoke, and coverage evidence.
- Out of scope: production code changes unless a blocking defect is found and explicitly repaired under a new scoped loop.

## Lifecycle Stage

- Current gate: T0 Test Plan.
- Manifest status: in-progress.
