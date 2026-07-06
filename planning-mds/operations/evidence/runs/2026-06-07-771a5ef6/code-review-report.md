# Code Review Report — F0017 run 2026-06-07-771a5ef6

## Summary

Verdict: APPROVED WITH RECOMMENDATIONS.

Reviewed the F0017 backend hierarchy/ownership/territory services, endpoints, repositories, migration invariants, and integration tests, plus the frontend distribution slice evidence generated at G2.

## Review Scope

- `engine/src/Nebula.Application/Services/DistributionNodeService.cs`
- `engine/src/Nebula.Application/Services/ProducerOwnershipService.cs`
- `engine/src/Nebula.Application/Services/TerritoryService.cs`
- `engine/src/Nebula.Application/Interfaces/ITerritoryAssignmentRepository.cs`
- `engine/src/Nebula.Infrastructure/Repositories/TerritoryAssignmentRepository.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260608033854_F0017_DistributionHierarchyOwnershipTerritory.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Configurations/TerritoryAssignmentConfiguration.cs`
- `engine/src/Nebula.Api/Endpoints/DistributionEndpoints.cs`
- `engine/src/Nebula.Api/Endpoints/ProducerOwnershipEndpoints.cs`
- `engine/src/Nebula.Api/Endpoints/TerritoryEndpoints.cs`
- `engine/tests/Nebula.Tests/Integration/DistributionEndpointTests.cs`
- `engine/tests/Nebula.Tests/Integration/ProducerOwnershipEndpointTests.cs`
- `engine/tests/Nebula.Tests/Integration/TerritoryEndpointTests.cs`
- `experience/src/features/distribution`

## Findings

Resolved during G3:
- Territory open-assignment uniqueness was originally scoped to `(TerritoryId, MemberType, MemberId)`, allowing one member to remain open in multiple territories. This conflicted with F0017 overlap-prevention semantics and could make `/territory-assignments` return an arbitrary active territory. Repair changed the open lookup and filtered unique index to member scope and added cross-territory reassignment coverage.
- Descendant reparent audit payloads reused the moved node's old/new depth instead of each descendant's own before/after depth. Repair records descendant old depth before recompute and emits descendant-specific payload depth values.

No unresolved blocking code-review findings remain after the G3 repair and rerun.

## Verification

Command:
`dotnet test Nebula.slnx --filter "DistributionEndpointTests|ProducerOwnershipEndpointTests|TerritoryEndpointTests" --logger "trx;LogFileName=f0017-backend-after-g3.trx"`

Result:
exit_code=0; 24 passed, 0 failed, 0 skipped.

Artifacts:
- `artifacts/test-results/f0017-backend-after-g3.trx`
- `artifacts/coverage/f0017-backend-after-g3-cobertura.xml`

Regression added:
- `AssignMember_ToDifferentTerritory_ClosesPriorOpenAssignment` proves a member reassignment to a different territory closes the previous open period and preserves correct as-of reads/lists.

## Recommendations

- Remediate or explicitly accept the existing `Microsoft.OpenApi` high severity advisory before final release signoff.
- Consider frontend tests for `OwnershipPanel` and `TerritoriesPanel` before G8 if those are release-critical surfaces.
- Keep migration snapshot reconciliation visible before final closeout because earlier run notes identified branch-level snapshot drift.
