# F0016 — Account 360 & Insured Management — Getting Started

## Prerequisites

- Read [PRD.md](./PRD.md), [feature-assembly-plan.md](./feature-assembly-plan.md), and [STATUS.md](./STATUS.md)
- Review [ADR-017](../../architecture/decisions/ADR-017-account-merge-tombstone-and-fallback-contract.md) before changing merge or deleted-account behavior
- Review F0002 broker ownership, F0006 submission intake, and F0007 renewal/policy-stub contracts before changing dependent fallback behavior
- Refresh ontology context with `python3 scripts/kg/lookup.py F0016` before broad repository scans

## Key Files

- Backend aggregate and API:
  - `engine/src/Nebula.Domain/Entities/Account.cs`
  - `engine/src/Nebula.Application/Services/AccountService.cs`
  - `engine/src/Nebula.Application/Services/AccountContactService.cs`
  - `engine/src/Nebula.Infrastructure/Repositories/AccountRepository.cs`
  - `engine/src/Nebula.Api/Endpoints/AccountEndpoints.cs`
- Backend schema/runtime support:
  - `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260414120000_F0016_AccountLifecycle.cs`
  - `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260414121000_F0016_AccountContactsAndRelationshipHistory.cs`
  - `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260414122000_F0016_DependentFallbackDenormalization.cs`
  - `engine/src/Nebula.Infrastructure/Persistence/DevSeedData.cs`
  - `planning-mds/security/policies/policy.csv`
- Frontend:
  - `experience/src/features/accounts/**`
  - `experience/src/pages/AccountsPage.tsx`
  - `experience/src/pages/CreateAccountPage.tsx`
  - `experience/src/pages/AccountDetailPage.tsx`

## Verification Commands

1. Backend build:
   - `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj`
2. Backend targeted feature validation:
   - `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter "FullyQualifiedName~AccountEndpointTests|FullyQualifiedName~WorkflowEndpointTests|FullyQualifiedName~DashboardScopeFilteringTests|FullyQualifiedName~NudgePriorityTests"`
3. Frontend validation:
   - `pnpm --dir experience exec tsc -b`
   - `pnpm --dir experience lint`
   - `pnpm --dir experience lint:theme`
   - `pnpm --dir experience test`
   - `pnpm --dir experience build`
4. Tracker and KG validation after closeout changes:
   - `python3 scripts/kg/validate.py --check-drift`
   - `python3 agents/product-manager/scripts/validate-stories.py planning-mds/features/archive/F0016-account-360-and-insured-management`
   - `python3 agents/product-manager/scripts/generate-story-index.py planning-mds/features/`
   - `python3 agents/product-manager/scripts/validate-trackers.py`

## Runtime Notes

- Compose preflight for closeout used:
  - `docker compose up -d db authentik-server authentik-worker api`
  - `docker compose ps`
- Observed result on 2026-04-14:
  - `db` healthy
  - `authentik-server` healthy
  - `api` started
  - `authentik-worker` unhealthy because of an existing blueprint/import failure unrelated to F0016 (`nebula-roles-mapping` / null `authorization_flow`)
- Treat that identity-stack drift as out of scope for F0016 and escalate it back through Architect/PM rather than modifying blueprints during account-slice work.

## Deferred Follow-ups

- Async / Temporal-backed merge for accounts with more than 500 linked records
- Unmerge / undelete recovery flows
- Documents rail ownership once F0020 lands
- Territory hierarchy and rule-based ownership once F0017 lands
