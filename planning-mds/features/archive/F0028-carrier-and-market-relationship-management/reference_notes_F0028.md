# Reference Notes — F0028 Carrier & Market Relationship Management

**Feature:** F0028 — Carrier & Market Relationship Management  
**Harness:** `nebula-agents-sagar`  
**Product repo:** `nebula-insurance-crm-sagar`  
**Feature action run:** `2026-07-02-736e7854`  
**Final status:** Done, archived, and post-closeout runtime fixes pushed  
**Latest commits:** `4677ae4`, `1bad5fb`, `168fa74`, `621d362`, `bdc8c6c`

## 1. Starting Context And Objective

F0028 was moved from later planning into active delivery to add a CRM-side carrier and market relationship workspace.

The intended product outcome was:

- Track carrier/market profiles separately from accounts, brokers, submissions, and policies.
- Manage underwriter/market contacts, appetite notes, appointment context, and related work links.
- Keep carrier API integrations, rating/pricing, reinsurance, and external synchronization out of scope.
- Use the `nebula-agents-sagar` harness strictly for planning, implementation, evidence, validation, archive, and follow-up fixes.

## 2. Dependencies And Preconditions

### Repositories

- `nebula-agents-sagar` — harness, action prompts, templates, evidence validators, story validators, tracker validators.
- `nebula-insurance-crm-sagar` — product repository where F0028 planning artifacts, backend, frontend, tests, KG bindings, and evidence were created.

### Feature Dependencies

- F0019 — submission quoting/proposal/approval context, used as the source for submission-related work links.
- F0023 — global search, saved views, and operational reporting substrate, used for search/reporting alignment.
- Existing auth and authorization foundation — OIDC/dev auth, current-user service, Casbin policy enforcement.
- Existing timeline/audit pattern — `ActivityTimelineEvent` rows for successful mutations.
- Existing clean architecture patterns — Domain, Application, Infrastructure, API, frontend feature slice.

### Runtime/Tooling Preconditions

- Product root resolved as `/Users/wallstreet289/Documents/workspace/nebula-insurance-crm-sagar`.
- Harness root resolved as `/Users/wallstreet289/Documents/workspace/nebula-agents-sagar`.
- Evidence run created under `planning-mds/operations/evidence/runs/2026-07-02-736e7854/`.
- Local services used during validation:
  - PostgreSQL database.
  - Authentik local auth service.
  - Nebula API on `http://127.0.0.1:8080`.
  - Vite frontend on `http://localhost:5173`.
- Frontend local development uses deterministic dev auth for F0028 local testing.

### Dependencies/Packages

- Product frontend dependency set was already managed by `pnpm`.
- Backend dependency set was already managed by .NET project files and restore/build flow.
- Harness Python dependencies were required for validators; template validation used the harness `.venv` because system Python did not include `PyYAML`.
- Existing warnings remained non-blocking:
  - `Microsoft.OpenApi 2.0.0` NU1903 advisory.
  - Existing KG low-confidence/unknown-symbol warnings.
  - Vite chunk-size warning.
  - Node `module.register()` deprecation warning.

## 3. Harness Process Followed

### Planning Action

- Used `agents/actions/plan.md` for F0028 planning.
- Generated a run ID: `2026-07-02-736e7854`.
- Created required evidence files:
  - `README.md`
  - `action-context.md`
  - `artifact-trace.md`
  - `gate-decisions.md`
  - `commands.log`
  - `lifecycle-gates.log`
- Repaired KG freshness by refreshing coverage before proceeding.
- Expanded F0028 from draft PRD into implementation-ready stories and architecture.
- Stopped at Phase A and Phase B approval gates before feature implementation.

### Feature Action

- Used `agents/actions/feature.md` for implementation.
- Created the feature assembly plan at G0.
- Followed feature gates G0 through G8.
- Archived F0028 after G8 Product Manager closeout.
- Performed post-closeout fixes through the same evidence path rather than bypassing the harness.

## 4. Agents And Roles Involved

| Role / Agent | Involvement | Evidence |
|---|---|---|
| Product Manager | Requirements, Phase A, closeout, G8 approval | `STATUS.md`, `gate-decisions.md` |
| Architect | Phase B contract, feature assembly plan, KG reconciliation | `ARCHITECTURE.md`, `feature-assembly-plan.md`, `kg-reconciliation.md` |
| Backend Developer | Domain, EF, repository, service, validators, endpoints, migrations | Backend source and tests |
| Frontend Developer | Markets route, sidebar entry, page, feature hooks/types, UI workflows | Frontend source and tests |
| Quality Engineer | Acceptance coverage and focused validation | `test-execution-report.md` |
| Code Reviewer | Implementation review and recommendations | `code-review-report.md` |
| Security Reviewer | Sensitive market/appetite/appointment data review | `security-review-report.md` |
| DevOps | Runtime, migration, and deployability checks | `deployability-check.md` |
| Codex Operator | Harness execution, fixes, validation, archive support | `commands.log`, commits |

## 5. Main Artifacts Generated

### Planning And Evidence

- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/PRD.md`
- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/ARCHITECTURE.md`
- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/feature-assembly-plan.md`
- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/STATUS.md`
- `planning-mds/features/archive/F0028-carrier-and-market-relationship-management/F0028-S0001-*.md` through `F0028-S0006-*.md`
- `planning-mds/operations/evidence/runs/2026-07-02-736e7854/*`

### API, Schema, Security, And KG

- `planning-mds/api/nebula-api.yaml`
- `planning-mds/schemas/carrier-*.schema.json`
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- `planning-mds/knowledge-graph/canonical-nodes.yaml`
- `planning-mds/knowledge-graph/code-index.yaml`
- `planning-mds/knowledge-graph/coverage-report.yaml`
- `planning-mds/knowledge-graph/feature-mappings.yaml`

### Backend Implementation

- Carrier market domain entities.
- Carrier market DTOs.
- Carrier market repository interface and EF implementation.
- Carrier market service with mutation logic and timeline event creation.
- Carrier market validators.
- Carrier market EF configuration and migration.
- Carrier market API endpoints.
- Dev seed fixtures for deterministic F0028 market examples.

### Frontend Implementation

- Sidebar `Markets` navigation item.
- `/carrier-markets` protected route.
- Carrier Markets page with:
  - Market directory.
  - Profile edit/save.
  - Contacts.
  - Appetite.
  - Appointments.
  - Activity Links / Related Work.
- Carrier market feature types and React Query hooks.
- Local dev auth stabilization for Markets.
- F0028-scoped local dev retry for stale invalid-token responses.

## 6. Actions Performed

### Feature Construction

- Added carrier/market relationship data model.
- Added persistence migration for carrier market tables.
- Added API endpoints under `/carrier-markets`.
- Added Casbin policy rows for carrier market read/search/create/update/manage/link actions.
- Added timeline/audit event creation for successful market mutations.
- Added search/reporting KG bindings and contract artifacts.
- Added frontend directory/detail workspace.
- Added sidebar route so users can open `Markets`.
- Added seed/demo data so local contributors can see market examples consistently.

### Post-Closeout Runtime Fixes

- Fixed Vite proxy config so `/carrier-markets` routes through the local API.
- Added frontend local env example for development auth/proxy values.
- Added centralized frontend auth mode resolver.
- Made local development default to deterministic dev auth when `VITE_AUTH_MODE` is unset.
- Added deterministic carrier market seed fixtures:
  - `NEB-DEMO-ATLANTIC`
  - `NEB-DEMO-PACIFIC`
  - `NEB-DEMO-SUMMIT`
- Added F0028-local retry behavior so `/carrier-markets` retries once with the dev token if a stale invalid browser token is sent.
- Restarted stale Vite dev server so the browser loaded the patched bundle.

## 7. Gate Results

| Gate | Result | Summary |
|---|---|---|
| G0 | PASS | Feature assembly plan created. |
| G1 | PASS | Runtime preflight passed after local env/Auth database repair. |
| G2 | PASS | Focused implementation validation passed. |
| G3 | PASS WITH RECOMMENDATIONS | Code review accepted with non-blocking recommendations. |
| G4 | PASS WITH RECOMMENDATIONS | Security review passed; inherited advisory deferred. |
| G5 | PASS WITH RECOMMENDATIONS | PM accepted evidence and recommendations. |
| G6 | PASS | Implementation completed across backend, frontend, tests, and evidence. |
| G7 | PASS | KG reconciliation passed with documented existing warnings. |
| G8 | APPROVED WITH RECOMMENDATIONS | PM closeout completed; feature archived. |

## 8. Validations Run

### Planning/Harness

- `validate-stories.py`
- `generate-story-index.py`
- `validate-trackers.py`
- `validate-feature-evidence.py`
- `validate_templates.py`

### Knowledge Graph

- `scripts/kg/validate.py --write-coverage-report`
- `scripts/kg/validate.py`
- `scripts/kg/validate.py --check-symbols`
- `scripts/kg/validate.py --check-drift`
- `scripts/kg/lookup.py F0028`
- `scripts/kg/hint.py ...`

### Backend

- .NET builds for API and tests.
- Focused F0028 backend endpoint tests.
- Casbin authorization tests.
- Runtime API health checks.
- `/carrier-markets` smoke checks.

### Frontend

- `pnpm build`
- `pnpm test` focused app/auth/API tests.
- `App.test.tsx`
- `api.test.ts`
- Login and protected-route auth tests.
- Live Vite proxy verification for `/carrier-markets`.

### Runtime

- Docker Compose service inspection.
- API `/healthz` check.
- Frontend dev server startup on `http://localhost:5173`.
- Live proxy call to `/carrier-markets` returned `200` with demo market rows.

## 9. Commits Produced

| Commit | Purpose |
|---|---|
| `4677ae4 feat: add F0028 carrier market relationship management` | Main end-to-end feature implementation. |
| `1bad5fb chore: archive F0028 closeout artifacts` | Archived planning/evidence artifacts after closeout. |
| `168fa74 fix: load frontend local proxy config` | Fixed local Vite proxy/env loading for frontend API access. |
| `621d362 fix: stabilize F0028 local markets auth` | Added local dev auth resolver and deterministic seed data. |
| `bdc8c6c fix: retry F0028 markets dev auth locally` | Added F0028-scoped retry for stale invalid local tokens. |

## 10. Sample Testing Scenario

Use this sample to understand where F0028 changes reflect.

### Create Market

Open `Markets -> Carrier Markets`.

```text
Code: TEST-MKT-001
Name: Test Harbor Specialty
Status: Active
Market: Admitted
NAIC: 99999
AM Best: A
Email: test.harbor@example.local
Phone: +12125550111
Website: https://example.com/test-harbor
Notes: Test market for checking F0028 visibility.
```

Expected:

- Appears in `Market Directory`.
- Opens in the selected market workspace.
- Profile fields persist after refresh.
- Does not automatically appear in Accounts or Dashboard.

### Add Contact

```text
Name: Jordan Blake
Title: Senior Underwriter
Email: jordan.blake@testharbor.example.local
Phone: +12125550122
Roles: Underwriter, RelationshipOwner
Primary: checked
```

Expected:

- Appears in the selected market's `Contacts` section.
- Does not automatically appear in Accounts, Dashboard, Submissions, or Policies.

### Add Appetite Note

```text
Summary: Open to middle-market property schedules
Level: Open
LOB: Property
Region: East
```

Expected:

- Appears in the selected market's `Appetite` section.
- Remains market-side internal relationship intelligence.

### Add Appointment

```text
Status: Appointed
States: NY, NJ, PA
LOB: Property
Number: THS-PROP-001
```

Expected:

- Appears in the selected market's `Appointments` section.
- Does not change policy or submission workflow state.

### Add Activity Link

Copy an existing Submission or Policy UUID from its source screen URL, then return to `Markets`.

```text
Type: Submission
Kind: Marketed
Related ID: <existing submission UUID>
Note: Marketed this submission to Test Harbor Specialty.
```

Expected:

- Appears in the selected market's `Activity Links` section.
- Submission or Policy workflow state does not change.
- F0028 stores this as a related-work link only.

## 11. Where Changes Reflect In The UI

| User action | Reflects in Markets | Reflects in Accounts | Reflects in Dashboard | Reflects in Submissions/Policies |
|---|---:|---:|---:|---:|
| Create market profile | Yes | No | No | No |
| Save profile changes | Yes | No | No | No |
| Add contact | Yes | No | No | No |
| Add appetite note | Yes | No | No | No |
| Add appointment | Yes | No | No | No |
| Add activity link | Yes | No | No | Link references an existing source record but does not mutate it |

## 12. Important Clarification

F0028 is a carrier/market-side CRM module. It is not designed to automatically push saved markets into account detail pages, dashboard cards, submission workflow state, or policy workflow state.

The current visible activity section in the frontend is `Activity Links`, meaning related-work links to submissions or policies. Backend audit/timeline rows are created for successful mutations, but a full rendered timeline feed for `CarrierMarket` events is not currently exposed in the F0028 frontend page.

## 13. Deferred / Out Of Scope

- Carrier API synchronization.
- Rating, pricing, quote comparison, or recommendation logic.
- Reinsurance workflows.
- External broker visibility for market intelligence.
- Automatic dashboard/account rollups for carrier markets.
- Full `CarrierMarket` audit/timeline feed rendering in the frontend.
- Inherited `Microsoft.OpenApi` dependency advisory resolution.
- Full release-wide regression suite.

