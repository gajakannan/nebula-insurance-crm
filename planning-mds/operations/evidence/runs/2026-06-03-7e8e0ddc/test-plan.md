---
template: test-plan
version: 2.0
applies_to: quality-engineer
---

# Test Plan - F0019-submission-quoting-proposal-and-approval run 2026-06-03-7e8e0ddc

## Story-to-AC Mapping

| Story | AC | Lane | Test ID | Owner |
|-------|----|------|---------|-------|
| F0019-S0001 | Activate ReadyForUWReview -> InReview and downstream workflow states | Unit | WorkflowStateMachineTests | Developer |
| F0019-S0001 | Reject unauthorized or invalid downstream jumps | Unit | WorkflowServiceTests | Developer |
| F0019-S0002 | Upsert quote/proposal packet and mark ready | Unit | SubmissionService_UpdateQuotePacketAsync_MarkReady_TransitionsToQuotedAndAuditsPacket | Developer |
| F0019-S0002 | Render packet editing in submission detail | Integration | SubmissionDetailPage.integration.test.tsx | QE |
| F0019-S0003 | Record granted/declined approval with reason and blockers | Unit/Build | WorkflowServiceTests plus TypeScript build | Developer |
| F0019-S0004 | Request and confirm bind using idempotency key | Unit/Build | WorkflowServiceTests plus TypeScript build | Developer |
| F0019-S0005 | Require reasonCode for Declined/Withdrawn and reasonDetail for Other | Unit | WorkflowStateMachineTests and WorkflowServiceTests | Developer |
| F0019-S0006 | Archive terminal submissions and reactivate archived submissions | Build/Integration | Backend build plus SubmissionDetailPage integration shell | Developer/QE |
| F0019-S0007 | Show approval-pending, stuck, archived, and downstream statuses in list | Integration | SubmissionsPage.integration.test.tsx | QE |
| F0019-S0008 | Emit timeline/audit payloads for downstream events | Contract/Build | activity-event-payloads.schema.json validation plus backend build | Developer |

## Test Strategy

- Unit tests: backend workflow state machine and submission service guard/idempotency tests.
- Component tests: frontend submission pages exercised through existing React integration harness.
- Integration tests: submissions list/detail integration tests against shared MSW runtime state.
- E2E tests: not added for this feature run; existing UI integration lane covers changed submission surfaces.
- API tests: endpoint contracts covered by build-time endpoint compilation and OpenAPI sync; no live HTTP endpoint suite added in G2.
- Accessibility tests: no new a11y-specific lane was added; changed controls use existing form/button primitives.

## Developer-vs-QE Test Ownership

- Developer-owned: backend unit tests, service guard tests, TypeScript build, frontend production build.
- QE-owned: test plan, submissions integration evidence review, test execution report, coverage report.
- DevOps-owned: migration SQL generation and deployability check because deployment_config_changed is true.

## Test Data / Fixtures

- Backend tests use in-memory service stubs for submission repository, quote packet repository, approval decision repository, bind handoff repository, workflow transition repository, timeline repository, and unit of work.
- Frontend tests use the existing MSW-backed submissions runtime fixture in experience/src/mocks/submissions.ts.
- Personas covered by implementation and test data: Underwriter, Admin, Distribution user.

## Happy / Edge / Error / Auth / Accessibility / Regression Cases

- Happy path: InReview quote packet is marked ready, submission becomes Quoted, approval can be granted, bind can be requested and confirmed.
- Edge path: archived submissions are read-only until reactivated; includeArchived list filter restores visibility.
- Error path: missing packet readiness items block Quoted; duplicate granted approval blocks repeated approval.
- Auth path: approval requires submission:approve; archive/reactivate requires submission:archive; downstream mutations respect user role and assignment scope.
- Regression path: ReadyForUWReview remains guarded and direct ReadyForUWReview -> Bound is rejected.
- Accessibility path: no custom keyboard model added; controls use existing input, textarea, select, and button components.

## Risks And Mitigations

- Broad frontend unit command failed outside F0019 due fixed-date session telemetry fixture expiration; F0019 uses passing submissions integration evidence and production build.
- Frontend coverage percentage was not generated for the scoped integration lane; behavior coverage is documented in coverage-report.md and raw backend Cobertura is retained.
- Migration was not applied to a live local database in G2; scoped SQL generation validates migration rendering and rollback is supplied by Down().

## Recommendations

- None.

## Result

Result: PASS

