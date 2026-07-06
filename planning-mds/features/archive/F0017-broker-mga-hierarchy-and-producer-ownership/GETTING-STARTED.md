# F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management — Getting Started

## Prerequisites

- [ ] Read the current release framing in [ROADMAP.md](../ROADMAP.md)
- [ ] Review the Phase A/B approved plan package: [PRD.md](./PRD.md), story files, [STATUS.md](./STATUS.md), and [ADR-026](../../architecture/decisions/ADR-026-broker-mga-hierarchy-producer-ownership-and-territory.md)
- [ ] Review the F0017 API and schema contracts in [nebula-api.yaml](../../api/nebula-api.yaml) and [schemas](../../schemas/README.md)
- [ ] Confirm `scripts/kg/lookup.py F0017` resolves the F0017 feature, stories, endpoints, schemas, and ADR before creating the feature assembly plan

## How to Verify

1. Confirm the feature addresses hierarchy, ownership, and territory together as one distribution model.
2. Confirm hierarchy-aware access enforcement and distribution rollups remain deferred to F0037; F0017 only persists the structural/effective-dated model and emits audit events.
3. Validate story, tracker, API contract, and KG checks before starting implementation.

## Implementation Key Files (feature run 2026-06-07-771a5ef6)

Backend (`engine/`) — DistributionNode hierarchy vertical is implemented and tested; ownership/territory entities + configs exist but their services/endpoints are pending:

- Entities: `Nebula.Domain/Entities/{DistributionNode,ProducerOwnership,Territory,TerritoryAssignment}.cs`
- EF configs: `Nebula.Infrastructure/Persistence/Configurations/{DistributionNode,ProducerOwnership,Territory,TerritoryAssignment}Configuration.cs`
- Migration: `Nebula.Infrastructure/Persistence/Migrations/20260608033854_F0017_DistributionHierarchyOwnershipTerritory.cs` (4 tables + 3 filtered-unique indexes)
- Hierarchy: `Nebula.Application/{DTOs/DistributionNode*,Interfaces/IDistributionNodeRepository,Services/DistributionNodeService,Validators/DistributionNodeParentRequestValidator}.cs`, `Nebula.Infrastructure/Repositories/DistributionNodeRepository.cs`, `Nebula.Api/Endpoints/DistributionEndpoints.cs`
- Casbin: F0017 rows already present in `planning-mds/security/policies/policy.csv` §2.1a (embedded into Nebula.Infrastructure)
- Tests: `engine/tests/Nebula.Tests/Integration/DistributionEndpointTests.cs`

### Verify the backend vertical

```bash
# from engine/ (sdk 10.0; Testcontainers spins up its own postgres:16, Docker required)
dotnet test Nebula.slnx --filter "FullyQualifiedName~DistributionEndpointTests"
# expect: 8/8 passed
```

### Resume points (next session)
ProducerOwnershipService + endpoints (S0003), TerritoryService + endpoints (S0004), audit assertions (S0005), frontend `experience/src/features/distribution/**` (CI-validated), then evidence gates G2–G8 per `feature-assembly-plan.md`.
