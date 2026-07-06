# PM Closeout — F0021-communication-hub-and-activity-capture run 2026-07-01-9cee64f0

> Required at G8/closeout per `agents/actions/feature.md`. PM-owned final approval artifact.

## Final Story Status

| Story | Final Status | Evidence | Notes |
|-------|--------------|----------|-------|
| F0021-S0001 | Done | `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/test-execution-report.md`, `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/code-review-report.md`, `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/security-review-report.md` | Structured communication capture delivered and signed off. |
| F0021-S0002 | Done | `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/test-execution-report.md`, `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/code-review-report.md`, `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/security-review-report.md` | Contextual communication history delivered and signed off. |
| F0021-S0003 | Done | `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/test-execution-report.md`, `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/code-review-report.md`, `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/security-review-report.md` | Related-record and participant linkage delivered and signed off. |
| F0021-S0004 | Done | `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/test-execution-report.md`, `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/code-review-report.md`, `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/deployability-check.md` | Follow-up task linkage delivered and signed off. |
| F0021-S0005 | Done | `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/test-execution-report.md`, `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/code-review-report.md`, `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/security-review-report.md` | Correction/redaction audit delivered and signed off. |

## Archive Decision

F0021 is `Done` and archived on 2026-07-02.

- Active path before closeout: `planning-mds/features/F0021-communication-hub-and-activity-capture/`
- Archived path after closeout: `planning-mds/features/archive/F0021-communication-hub-and-activity-capture/`
- Approved run: `planning-mds/operations/evidence/runs/2026-07-01-9cee64f0/`

## Deferred Follow-ups

| Follow-up | Why deferred | Tracking link | Owner |
|-----------|--------------|---------------|-------|
| Real outbound send | F0021 MVP is capture/reference only; outbound send belongs to later Neuron/Communication Hub integration work. | Future feature | Product Manager |
| External connector ingestion | Requires integration contracts, deduplication, and security rules beyond MVP capture. | F0030 or future integration feature | Product Manager |
| AI-generated summaries | AI scope is intentionally excluded from F0021 MVP. | Future Neuron feature | Product Manager |
| Broader browser/integration coverage | Focused backend/frontend tests passed; expanded browser journeys remain quality hardening. | Future QA hardening | Quality Engineer |
| Dependency advisory triage | Security dependency scan produced findings but no G3 critical/high F0021 findings; remediation/risk acceptance remains a platform security follow-up. | Security backlog | Security Reviewer |

## Recommendation Acceptances

- Accepted: Add API integration coverage for authorization, persistence, and timeline emission before final closeout - accepted as non-blocking carry-forward; focused validator, frontend component, build, runtime, and signoff evidence passed.
- Accepted: Review the F0019 idempotent migration repair before final signoff - accepted as non-blocking carry-forward; code review and deployability review both preserved the risk and final signoff passed.
- Accepted: Run broader backend and frontend suites before final closeout if schedule permits - accepted as non-blocking carry-forward; focused F0021 suites and build evidence passed.
- Accepted: Track sandbox MSBuild named-pipe failure as an environment limitation - accepted as non-blocking environment limitation; escalated host execution passed.
- Accepted: Add end-to-end browser coverage for creating a communication from at least one contextual record page - accepted as non-blocking QA hardening; component and contextual integration evidence passed.
- Accepted: Add API integration coverage for create, correction, redaction, and follow-up task linkage - accepted as non-blocking QA hardening; validator and service-level evidence passed.
- Accepted: Review deployment migration behavior for the hand-authored F0021 migration and F0019 drift repair before final signoff - accepted as non-blocking carry-forward; migration/runtime evidence passed and drift repair was reviewed in later gates.
- Accepted: Resolve or formally accept the existing `Microsoft.OpenApi` advisory before security signoff - accepted as non-blocking security backlog item; no critical/high F0021 finding was recorded.
- Accepted: `CommunicationPanel.tsx` has a React hook dependency warning because `communications` is initialized with a new fallback array each render; this does not break behavior, but it should be stabilized to keep lint clean - accepted as non-blocking frontend cleanup.
- Accepted: The legacy F0019 migration drift repair changes an older migration to tolerate pre-existing archive columns/index; this was necessary for the local runtime but should receive focused reviewer attention before signoff - accepted as non-blocking; focused review occurred through G5 signoff.
- Accepted: The lint wrapper expects a `format` script that the frontend package does not provide, so the wrapper exits 2 after ESLint succeeds; align harness lint expectations with the current package scripts - accepted as non-blocking harness/package-script alignment.
- Accepted: Add API integration tests for create/list/correct/redact/follow-up behavior to complement validator-unit coverage - accepted as non-blocking QA hardening.
- Accepted: Add at least one browser-level create-flow test from a contextual record page before final closeout - accepted as non-blocking QA hardening.
- Accepted: Stabilize the `communications` fallback array in `CommunicationPanel.tsx` to remove the F0021 ESLint warning - accepted as non-blocking frontend cleanup.
- Accepted: Upgrade or explicitly risk-accept vulnerable dependencies before final security signoff, with special attention to `Microsoft.OpenApi` and frontend dev server/tooling advisories - accepted as non-blocking security backlog item; no critical/high F0021 finding was recorded.
- Accepted: Install or provide `gitleaks`, `semgrep`, and OWASP ZAP scanner access before closeout so full scanner artifacts can replace local fallback evidence - accepted as non-blocking security tooling follow-up; fallback evidence was recorded.
- Accepted: Add API authorization integration tests for all F0021 communication actions and denied roles - accepted as non-blocking QA/security hardening.

## Tracker Updates

- `planning-mds/features/REGISTRY.md` moved F0021 from Planned to Archived Features.
- `planning-mds/features/ROADMAP.md` moved F0021 from Now to Completed.
- `planning-mds/BLUEPRINT.md` marks F0021 Done and archived.
- `planning-mds/features/STORY-INDEX.md` is regenerated during G8 after the archive move.
- `planning-mds/knowledge-graph/feature-mappings.yaml` updates F0021 feature/story paths to the archive path and marks the feature `archived-done`.
- `planning-mds/knowledge-graph/coverage-report.yaml` is regenerated during G8 after the archive move.

## Validator Results

| Validator | Command | Exit Code | Result |
|-----------|---------|-----------|--------|
| G7 evidence | `.venv/bin/python agents/product-manager/scripts/validate-feature-evidence.py --product-root ../nebula-insurance-crm --feature F0021 --run-id 2026-07-01-9cee64f0 --stage G7` | 0 | PASS |
| G8 story index | `.venv/bin/python agents/product-manager/scripts/generate-story-index.py ../nebula-insurance-crm/planning-mds/features/` | 0 | PASS |
| KG coverage initial | `python3 scripts/kg/validate.py --write-coverage-report` | 1 | REWORK — stale post-archive F0021 source-doc paths found and repaired |
| KG coverage final | `python3 scripts/kg/validate.py --write-coverage-report` | 0 | PASS |
| KG drift | `python3 scripts/kg/validate.py --check-drift` | 0 | PASS |
| tracker validation initial | `.venv/bin/python agents/product-manager/scripts/validate-trackers.py --product-root ../nebula-insurance-crm --feature F0021` | 1 | REWORK — stale commands.log artifact paths repaired |
| tracker validation final | `.venv/bin/python agents/product-manager/scripts/validate-trackers.py --product-root ../nebula-insurance-crm --feature F0021` | 0 | PASS |
| closeout evidence initial | `.venv/bin/python agents/product-manager/scripts/validate-feature-evidence.py --product-root ../nebula-insurance-crm --feature F0021 --stage closeout` | 1 | REWORK — PM acceptance identifiers repaired |
| closeout evidence final | `.venv/bin/python agents/product-manager/scripts/validate-feature-evidence.py --product-root ../nebula-insurance-crm --feature F0021 --stage closeout` | 0 | PASS |
