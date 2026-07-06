# F0021 End-to-End Reference

## Purpose

This document summarizes the work performed from initial local setup through F0021 testing, with emphasis on the Nebula agent harness flow, participating roles, actions executed, artifacts generated, validations run, knowledge-graph updates, and final test status.

Feature: F0021 — Communication Hub & Activity Capture  
Product root: `/Users/wallstreet62/Desktop/nebula3/nebula-insurance-crm`  
Agents root: `/Users/wallstreet62/Desktop/nebula3/nebula-agents`  
Final feature run: `2026-07-01-9cee64f0`  
Final standalone E2E test run: `2026-07-02-ddeb8492`

## Executive Summary

F0021 was moved forward, planned, implemented, archived, defect-tested, and E2E-tested through the Nebula harness.

Delivered F0021 capability:

- Structured communication capture for notes, calls, meetings, and email references.
- Contextual communication panels on Account, Broker, Submission, Policy, and Renewal detail pages.
- Related record links and participant capture.
- Follow-up task linkage from communication capture.
- Correction and Admin-only redaction with audit/timeline behavior.
- Communication event API, persistence model, frontend panel, authorization policy rows, schemas, ADR, and KG bindings.

Final validation result: PASS. F0021 is ready for user testing on the local stack.

## High-Level Timeline

| Step | Harness Action / Work | Run ID / Evidence | Outcome |
| --- | --- | --- | --- |
| 1 | Installed dependencies for `nebula-agents` and `nebula-insurance-crm`; started local harness/app stack | Local setup, no feature evidence package | Dependencies and local runtime became available. |
| 2 | Ran `nebula-agents`; switched operating posture to Product Manager role | Session context | Planning/review work used product-manager ownership. |
| 3 | Reviewed F0021 readiness and moved F0021 from `Next` to `Now` | `planning-mds/features/ROADMAP.md` | F0021 promoted for immediate planning/build. |
| 4 | Injected evidence-contract prompt for strict Nebula process | `nebula-agents/agents/templates/prompts/evidence-contract/plan-operator-friendly.md` | Process anchored to harness gates and evidence rules. |
| 5 | Ran F0021 plan action, Phase A+B | `planning-mds/operations/evidence/runs/2026-07-01-c1726908/` | Requirements and architecture approved. |
| 6 | Ran F0021 feature action | `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/` | Feature implemented, reviewed, signed off, KG-reconciled, archived. |
| 7 | Fixed local runtime/auth and communication-load issues using defect runs | `2026-07-02-f334b465`, `2026-07-02-965c66a1` | App loaded and communications panel reached API. |
| 8 | Ran standalone F0021 E2E test action | `planning-mds/operations/evidence/runs/2026-07-02-ddeb8492/` | 5/5 focused E2E scenarios passed. |
| 9 | Fixed two E2E-discovered defects through defect runs | `2026-07-02-09589802`, `2026-07-02-314d22e1` | `/users` proxy and correction persistence fixed. |

## Agent / Harness Flow

The process followed separate harness actions instead of shortcutting:

```text
Setup
  -> Product Manager review / roadmap sequencing
  -> plan action: F0021 Phase A+B
      Product Manager: requirements, stories, tracker sync
      Architect: architecture, ontology, schemas, ADR, policy mapping
      User: explicit approval gates
  -> feature action: F0021 implementation
      Architect: G0 assembly plan and KG reconciliation
      DevOps: runtime preflight and deployability
      Backend Developer: API, entities, services, persistence, migrations
      Frontend Developer: communication panel and contextual page wiring
      Quality Engineer: tests and coverage evidence
      Code Reviewer: implementation review
      Security Reviewer: auth/redaction/security review
      Product Manager: signoff, closeout, archive
  -> defect-bugfix actions
      Architect / Frontend / Backend / QE: scoped fixes and validation
  -> standalone test action
      Quality Engineer: E2E plan, execution, coverage, quality gate
```

## Plan Action: F0021 Phase A+B

Run folder: `planning-mds/operations/evidence/runs/2026-07-01-c1726908/`

Key gates:

| Gate | Owner | Result | What Happened |
| --- | --- | --- | --- |
| G1 Clarification | Product Manager | PASS WITH RECOMMENDATIONS | MVP scope clarified: structured capture/visibility, not full email client or outbound messaging. |
| G2 Tracker Sync | Product Manager | PASS | Tracker sync blockers were fixed, story index and validation commands passed. |
| G3 Phase A Approval | User | PASS | User approved requirements. |
| G4 Ontology Sync | Architect | PASS | F0021 mappings aligned with communication/timeline/task/account/broker/policy/submission semantics. |
| G5 Phase B Approval | User | PASS | User approved architecture. |

Major Phase A/B artifacts:

- `PRD.md`
- `STATUS.md`
- `GETTING-STARTED.md`
- `F0021-S0001-capture-communication-event.md`
- `F0021-S0002-view-contextual-communication-history.md`
- `F0021-S0003-link-communications-to-related-records-and-participants.md`
- `F0021-S0004-create-follow-up-task-from-communication.md`
- `F0021-S0005-correct-or-redact-communication-with-audit.md`
- `architecture-plan.md`
- `api-schema-deltas.md`
- `authorization-deltas.md`
- `feature-assembly-plan.md`

## Feature Action: F0021 Build

Run folder: `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/`

Key gates:

| Gate | Owner | Result | What Happened |
| --- | --- | --- | --- |
| G0 Assembly Plan | Architect | PASS | Feature-local implementation plan validated. |
| G1 Runtime Preflight | DevOps / Orchestrator | PASS WITH RECOMMENDATIONS | Docker, API health, and frontend app shell verified after local recovery. |
| G2 Self-Review | Implementation roles / QE | PASS WITH RECOMMENDATIONS | Backend/frontend build and focused tests completed. |
| G3 Code + Security Review | Code Reviewer / Security Reviewer | PASS WITH RECOMMENDATIONS | No blocking findings; recommendations carried forward. |
| G4 Approval | Product Manager / User | PASS | User approved after G3 review summary. |
| G5 Signoff | Required roles | PASS WITH RECOMMENDATIONS | QE, Code Reviewer, Security Reviewer, DevOps, and Architect signoffs recorded. |
| G6 Candidate Validation | Orchestrator | PASS | Candidate evidence package validated before closeout/archive. |
| G7 KG Reconciliation | Architect | PASS | As-built communication bindings added/confirmed. |
| G8 PM Closeout | Product Manager | PASS | Feature archived and trackers/KG/story index synchronized. |

Important feature evidence:

- `g0-assembly-plan-validation.md`
- `g1-runtime-preflight.md`
- `g2-self-review.md`
- `test-plan.md`
- `test-execution-report.md`
- `coverage-report.md`
- `code-review-report.md`
- `security-review-report.md`
- `deployability-check.md`
- `kg-reconciliation.md`
- `signoff-ledger.md`
- `pm-closeout.md`
- `evidence-manifest.json`

## Implemented Product Surface

Backend/API:

- `engine/src/Nebula.Api/Endpoints/CommunicationEndpoints.cs`
- `engine/src/Nebula.Application/DTOs/CommunicationDtos.cs`
- `engine/src/Nebula.Application/Services/CommunicationService.cs`
- `engine/src/Nebula.Application/Validators/CommunicationValidators.cs`
- `engine/src/Nebula.Application/Interfaces/ICommunicationRepository.cs`
- `engine/src/Nebula.Infrastructure/Repositories/CommunicationRepository.cs`
- `engine/src/Nebula.Domain/Entities/CommunicationEvent.cs`
- `engine/src/Nebula.Domain/Entities/CommunicationLink.cs`
- `engine/src/Nebula.Domain/Entities/CommunicationParticipant.cs`
- `engine/src/Nebula.Domain/Entities/CommunicationCorrection.cs`
- `engine/src/Nebula.Domain/Entities/CommunicationFollowUpTaskLink.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Configurations/Communication*.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260701140200_F0021CommunicationActivityCapture.cs`

Frontend:

- `experience/src/features/communications/`
- `experience/src/pages/AccountDetailPage.tsx`
- `experience/src/pages/BrokerDetailPage.tsx`
- `experience/src/pages/SubmissionDetailPage.tsx`
- `experience/src/pages/PolicyDetailPage.tsx`
- `experience/src/pages/RenewalDetailPage.tsx`

Security / policy / schemas:

- `planning-mds/security/policies/policy.csv`
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/schemas/communication-event.schema.json`
- `planning-mds/schemas/communication-event-create-request.schema.json`
- `planning-mds/schemas/communication-event-correction-request.schema.json`
- `planning-mds/schemas/communication-event-follow-up-request.schema.json`
- `planning-mds/architecture/decisions/ADR-029-communication-activity-capture-and-redaction.md`

Testing:

- `engine/tests/Nebula.Tests/Unit/CommunicationValidatorsTests.cs`
- `experience/src/features/communications/components/__tests__/CommunicationPanel.test.tsx`
- `experience/playwright.f0021.config.ts`
- `experience/tests/e2e/f0021-communications.spec.ts`

## Knowledge Graph Work

F0021 KG work happened in both plan and feature phases.

Plan-phase KG / ontology:

- Seeded F0021 mappings in `planning-mds/knowledge-graph/feature-mappings.yaml`.
- Added/confirmed communication source semantics, endpoint nodes, policy-rule nodes, schemas, and ADR binding.
- Ran KG validation, coverage write, and drift checks during G4.

Feature-phase KG reconciliation:

- Run: `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/kg-reconciliation.md`
- Architect confirmed as-built bindings for:
  - `entity:communication-event`
  - `entity:communication-link`
  - `entity:communication-participant`
  - communication endpoints
  - communication schemas
  - `policy_rule:communication-event-*`
  - `adr:029`
- Updated/confirmed source bindings in:
  - `planning-mds/knowledge-graph/code-index.yaml`
  - `planning-mds/knowledge-graph/symbol-index.yaml`
  - `planning-mds/knowledge-graph/coverage-report.yaml`
  - `planning-mds/knowledge-graph/feature-mappings.yaml`

KG validation commands recorded in feature evidence:

- `python3 scripts/kg/validate.py --regenerate-symbols --check-symbols`
- `python3 scripts/kg/validate.py --check-drift`
- `python3 scripts/kg/validate.py --write-coverage-report`
- `python3 scripts/kg/validate.py`

## Tracker / Archive Results

F0021 was archived after G8 closeout.

Final feature folder:

- `planning-mds/features/archive/F0021-communication-hub-and-activity-capture/`

Latest-run pointer:

- `planning-mds/operations/evidence/features/F0021-communication-hub-and-activity-capture/latest-run.json`

Trackers touched during plan/build/closeout:

- `planning-mds/features/ROADMAP.md`
- `planning-mds/features/REGISTRY.md`
- `planning-mds/features/STORY-INDEX.md`
- `planning-mds/BLUEPRINT.md`
- `planning-mds/knowledge-graph/*`

ROADMAP final state: F0021 is in Completed:

> Done and archived (2026-07-02, feature run `2026-07-01-9cee64f0`) — 5 stories: structured communication capture, contextual history, related-record/participant links, follow-up task linkage, correction/redaction audit.

## Defect Runs After Feature Closeout

These were separate `defect-bugfix` runs, not feature evidence, and did not update archived F0021 status.

| Run | Defect | Roles | Fix | Status |
| --- | --- | --- | --- | --- |
| `2026-07-02-f334b465` | Local auth/proxy setup blocked app login/runtime | Architect, Frontend, QE | Added/used local dev auth/proxy configuration and Vite env loading | Closed |
| `2026-07-02-965c66a1` | Communications panel showed `Unable to load communications` | Architect, Frontend, QE | Added `/communications` to Vite proxy paths | Closed |
| `2026-07-02-09589802` | Follow-up assignee search could not load `/users` suggestions | Architect, Frontend, QE | Added `/users` to Vite proxy paths | Closed |
| `2026-07-02-314d22e1` | Correction/redaction E2E returned HTTP 500 | Architect, Backend, QE | Explicitly staged `CommunicationCorrection` insert through repository | Closed |

Defect evidence:

- `planning-mds/operations/evidence/runs/2026-07-02-f334b465/`
- `planning-mds/operations/evidence/runs/2026-07-02-965c66a1/`
- `planning-mds/operations/evidence/runs/2026-07-02-09589802/`
- `planning-mds/operations/evidence/runs/2026-07-02-314d22e1/`

## Standalone E2E Test Run

Run folder: `planning-mds/operations/evidence/runs/2026-07-02-ddeb8492/`

Harness mode:

- Action: `test`
- Mode: `standalone`
- Test scope: `e2e`
- Context feature: `F0021`
- Did not create feature evidence.
- Did not mutate `latest-run.json`.
- Did not change archived F0021 status.

Test gates:

| Gate | Result | Evidence |
| --- | --- | --- |
| T0 TEST PLAN | PASS | `test-plan.md` |
| T1 TEST EXECUTION | PASS | `test-execution-report.md`, `artifacts/test-results/f0021-communications-results.json` |
| T2 COVERAGE | PASS | `coverage-report.md` |
| T3 SELF-REVIEW GATE | PASS | `quality-report.md` |
| T4 QUALITY GATE | PASS | `quality-report.md` |

E2E scenarios:

- Unauthenticated `/communications` returns `401`.
- Empty history renders without `Unable to load communications`.
- UI capture creates communication with participant and follow-up.
- History renders on Account, Broker, Policy, Submission, and Renewal contextual surfaces.
- Correction persists and Admin redaction masks content with redacted indicator.

Screenshots:

- `artifacts/screenshots/f0021-empty-state.png`
- `artifacts/screenshots/f0021-created-with-follow-up.png`
- `artifacts/screenshots/f0021-contextual-history.png`
- `artifacts/screenshots/f0021-redacted-state.png`

## Validation Commands

Final successful validation set:

| Command | Result | Notes |
| --- | --- | --- |
| `docker compose ps` | PASS | Local stack running. |
| `curl -fsS http://127.0.0.1:8080/healthz` | PASS | API healthy. |
| `curl -fsS http://127.0.0.1:5174/healthz` | PASS | Frontend healthy. |
| `pnpm --dir experience exec playwright test tests/e2e/f0021-communications.spec.ts --config=playwright.f0021.config.ts` | PASS | 5/5 E2E scenarios passed. |
| `pnpm --dir experience test src/features/communications/components/__tests__/CommunicationPanel.test.tsx` | PASS | 2/2 tests passed. |
| `pnpm --dir experience lint` | PASS | Exit 0 with existing warnings. |
| `pnpm --dir experience lint:theme` | PASS | Theme guard passed. |
| `pnpm --dir experience build` | PASS | Build passed with Vite chunk-size warning. |
| `docker compose up -d --build api` | PASS | API rebuilt after backend correction persistence fix. |

Known non-blocking notes:

- Existing lint warnings remain outside the F0021 E2E scope.
- Vite build reports a chunk-size warning.
- Docker API build reported an existing `Microsoft.OpenApi` package advisory.

## Final Status

F0021 is implemented end to end and ready for local user testing.

Use the running local stack:

- Frontend: `http://127.0.0.1:5174`
- API: `http://127.0.0.1:8080`

Primary evidence packages:

- Plan: `planning-mds/operations/evidence/runs/2026-07-01-c1726908/`
- Feature: `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/`
- Standalone E2E: `planning-mds/operations/evidence/runs/2026-07-02-ddeb8492/`

## Re-Run Quick Reference

From product root:

```bash
docker compose ps
curl -fsS http://127.0.0.1:8080/healthz
curl -fsS http://127.0.0.1:5174/healthz
pnpm --dir experience exec playwright test tests/e2e/f0021-communications.spec.ts --config=playwright.f0021.config.ts
pnpm --dir experience test src/features/communications/components/__tests__/CommunicationPanel.test.tsx
pnpm --dir experience lint
pnpm --dir experience lint:theme
pnpm --dir experience build
```

If backend source changes need to be active in the local container:

```bash
docker compose up -d --build api
```
