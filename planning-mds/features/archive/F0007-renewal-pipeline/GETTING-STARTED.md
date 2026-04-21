# F0007 — Renewal Pipeline — Getting Started

## Prerequisites

- [ ] Read the [PRD](./PRD.md) — scope, personas, workflow states, screens, and business rules
- [ ] Read the [COMMERCIAL-PC-CRM-RELEASE-PLAN](../COMMERCIAL-PC-CRM-RELEASE-PLAN.md) — F0007 sequencing and release context
- [ ] Review [ADR-009](../../architecture/decisions/ADR-009-lob-classification-and-sla-configuration.md) — WorkflowSlaThreshold pattern for renewal timing windows
- [ ] Review [ADR-011](../../architecture/decisions/ADR-011-crm-workflow-state-machines-and-transition-history.md) — State machine and transition history pattern
- [ ] Review the F0007 implementation note in [feature-assembly-plan.md](./feature-assembly-plan.md) — the current contract includes a Policy stub and does not yet expose a policy search/read picker endpoint

## Dependencies (required for implementation and validation)

| Dependency | Feature | What F0007 Needs |
|------------|---------|------------------|
| Policy entity with expiration dates | F0018 surface delivered early by F0007 | PolicyId, ExpirationDate, EffectiveDate, Carrier, LOB, AccountId, BrokerId |
| Account entity | F0016 | AccountId, Name, Industry, PrimaryState |
| Broker entity | F0002 (done) | BrokerId, LegalName, LicenseNumber, State |
| User search API | F0004-S0002 (done) | Assignee picker for renewal ownership |
| Task linked entity | F0003/F0004 (done) | LinkedEntityType=Renewal on Task entity |
| WorkflowSlaThreshold table | ADR-009 (done) | Renewal-specific timing threshold seed data |

## Key Domain Concepts

- **Renewal workflow:** Identified → Outreach → InReview → Quoted → Completed / Lost
- **Overdue:** `current_date > (PolicyExpirationDate - LOB outreach target days)` AND `CurrentStatus = Identified`
- **Ownership handoff:** Distribution owns Identified/Outreach; underwriting owns InReview/Quoted
- **One active renewal per policy:** Enforced by unique constraint (excludes terminal + deleted)

## Current Contract Caveats

- F0007 ships the minimal Policy stub needed for renewal workflows because the broader F0018 Policy Lifecycle feature is not yet planned.
- The renewal create experience currently accepts a direct `PolicyId`; the OpenAPI contract does not yet provide a policy search/read picker endpoint.
- A richer Policy 360 selection flow remains future F0018 work and is not part of the delivered F0007 scope.

## Seed Data Required

- WorkflowSlaThreshold entries for EntityType="renewal", Status="Identified", per-LOB outreach target and warning days
- ReferenceRenewalStatus entries for: Identified, Outreach, InReview, Quoted, Completed, Lost
- Sample policies with future expiration dates for development testing

## Verification Commands

```bash
docker compose ps
dotnet build engine/src/Nebula.Api/Nebula.Api.csproj
dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj
dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter "Renewal|Workflow"
pnpm --dir experience build
pnpm --dir experience lint
pnpm --dir experience lint:theme
pnpm --dir experience test
pnpm --dir experience exec vitest run src/pages/tests/RenewalsPage.integration.test.tsx src/pages/tests/RenewalDetailPage.integration.test.tsx src/pages/tests/DashboardPage.integration.test.tsx
python3 agents/product-manager/scripts/validate-trackers.py
python3 scripts/kg/validate.py
```

## How to Verify

1. Create a renewal from an expiring policy → confirm status = Identified, fields inherited from policy
2. Transition through the full workflow → confirm each transition is validated and recorded
3. Assign/reassign ownership → confirm timeline records the change
4. Create a renewal past its outreach target date → confirm overdue flag appears in pipeline list
5. View dashboard → confirm renewal nudge card shows overdue/approaching counts
6. Attempt invalid transition → confirm HTTP 409 with appropriate error code
7. Attempt duplicate renewal for same policy → confirm HTTP 409
8. Run tracker validation → `python3 agents/product-manager/scripts/validate-trackers.py`
9. Run KG validation when graph files change → `python3 scripts/kg/validate.py`

## Validation Snapshot — 2026-04-11

- Backend builds passed for `Nebula.Api` and `Nebula.Tests`.
- Targeted backend validation passed: `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter "Renewal|Workflow"` -> 53 passed, 0 failed.
- Frontend build, lint, theme lint, and full `pnpm --dir experience test` run passed.
- Targeted F0007 frontend integration coverage passed for renewal list, renewal detail, and dashboard nudge flows.
- The broader `pnpm --dir experience test:integration` suite still has an unrelated existing failure in `src/pages/tests/SubmissionDetailPage.integration.test.tsx`; treat that as a separate issue outside F0007 scope.
