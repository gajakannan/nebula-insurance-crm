# Code Review Report — F0021-communication-hub-and-activity-capture run 2026-07-01-9cee64f0

**Result:** APPROVED WITH RECOMMENDATIONS

## Reviewed Files

Reviewed the F0021 source, tests, and feature evidence paths listed in `evidence-manifest.json`, with emphasis on:

- `engine/src/Nebula.Api/Endpoints/CommunicationEndpoints.cs`
- `engine/src/Nebula.Application/Services/CommunicationService.cs`
- `engine/src/Nebula.Application/Validators/CommunicationValidators.cs`
- `engine/src/Nebula.Infrastructure/Repositories/CommunicationRepository.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260701140200_F0021CommunicationActivityCapture.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260603220000_F0019_SubmissionQuotingApproval.cs`
- `engine/tests/Nebula.Tests/Unit/CommunicationValidatorsTests.cs`
- `experience/src/features/communications/components/CommunicationPanel.tsx`
- `experience/src/features/communications/components/__tests__/CommunicationPanel.test.tsx`
- `experience/src/pages/AccountDetailPage.tsx`
- `experience/src/pages/BrokerDetailPage.tsx`
- `experience/src/pages/SubmissionDetailPage.tsx`
- `experience/src/pages/PolicyDetailPage.tsx`
- `experience/src/pages/RenewalDetailPage.tsx`

The canonical changed-file set remains the run manifest plus the working-tree diff artifacts under `artifacts/code-review/`.

## Validation Artifacts

- `artifacts/code-review/code-quality.txt` — repo-wide quality scan; failed on pre-existing TODO/line-length/large-file noise.
- `artifacts/code-review/lint.txt` — ESLint passed with warnings; wrapper exited 2 because the project has no `format` script.
- `artifacts/code-review/pr-size.txt` — commit-range size wrapper is not meaningful for this uncommitted working tree.
- `artifacts/code-review/working-tree-numstat.txt` and `artifacts/code-review/working-tree-stat.txt` — working-tree diff size evidence.
- `test-execution-report.md` — focused backend/frontend tests and build evidence.
- `coverage-report.md` — focused F0021 coverage evidence.
- `deployability-check.md` — migration/runtime deployability evidence.

## Severity-Ranked Findings

No critical or high code-quality findings were found in the F0021 implementation.

Medium:

- [medium] `CommunicationPanel.tsx` has a React hook dependency warning because `communications` is initialized with a new fallback array each render; this does not break behavior, but it should be stabilized to keep lint clean - owner: Frontend Developer; follow-up: post-G3 cleanup before final closeout.
- [medium] The legacy F0019 migration drift repair changes an older migration to tolerate pre-existing archive columns/index; this was necessary for the local runtime but should receive focused reviewer attention before signoff - owner: Code Reviewer; follow-up: carry into G5 signoff review.

Low:

- [low] The lint wrapper expects a `format` script that the frontend package does not provide, so the wrapper exits 2 after ESLint succeeds; align harness lint expectations with the current package scripts - owner: DevOps; follow-up: deferred-no-followup.

## Non-Blocking Recommendations With Owner/Follow-up

- [medium] Add API integration tests for create/list/correct/redact/follow-up behavior to complement validator-unit coverage - owner: Quality Engineer; follow-up: post-G3 hardening.
- [medium] Add at least one browser-level create-flow test from a contextual record page before final closeout - owner: Quality Engineer; follow-up: post-G3 hardening.
- [medium] Stabilize the `communications` fallback array in `CommunicationPanel.tsx` to remove the F0021 ESLint warning - owner: Frontend Developer; follow-up: post-G3 cleanup before final closeout.

## Vertical-Slice Completeness

- Backend: complete for the approved MVP slice. Endpoints, service logic, persistence, migration, authorization policy calls, redaction/correction, and timeline emission are present.
- Frontend: complete for contextual panels on Account, Broker, Submission, Policy, and Renewal detail pages.
- AI layer: not in scope.
- Tests: focused unit/component coverage is present. Broader API integration and browser flow coverage remain recommended.
- Deployability: local Docker API build, migration, table checks, and health checks passed after the documented F0019 drift recovery.

## AC / Test Adequacy

The implementation maps to the planned stories:

- Capture communication event: implemented through `/communications` create path, DTO validation, EF persistence, and frontend modal.
- View contextual communication history: implemented through list endpoint and contextual panels.
- Link communications to related records/participants: implemented through `CommunicationLinks`, participants, and contextual entity links.
- Create follow-up task: implemented through create payload and `/follow-up-task` mutation.
- Correct/redact with audit: implemented through correction/redaction mutation, append-only corrections, redacted response mapping, and timeline events.

Focused tests cover validators and frontend create payload behavior. Missing API integration/browser tests are non-blocking recommendations for this G3 review.

## Architecture Compliance

Clean Architecture boundaries are respected:

- API endpoints delegate validation and behavior to Application services.
- Application service depends on abstractions (`ICommunicationRepository`, `ITaskRepository`, `ITimelineRepository`, `IAuthorizationService`, `IUnitOfWork`).
- Infrastructure owns EF queries and entity existence checks.
- Frontend communication hooks isolate API calls from page components.

No Domain dependency leaks or API-to-Infrastructure direct dependencies were found in the F0021 code.

## Coverage Verification

`coverage-report.md` accurately reflects focused evidence:

- Backend targeted tests: 5 passed.
- Frontend targeted tests: 2 passed.
- No aggregate 80% feature coverage number was claimed, so there is no numeric coverage drift to flag.

## Result

APPROVED WITH RECOMMENDATIONS
