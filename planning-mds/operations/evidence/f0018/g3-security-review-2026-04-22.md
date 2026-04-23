# F0018 G3 Security Review Evidence

Date: 2026-04-22  
Reviewer: Codex feature runner  
Verdict: PASS

## Reviewed Artifacts

- `planning-mds/security/policies/policy.csv`
- `planning-mds/security/authorization-matrix.md`
- `engine/src/Nebula.Api/Endpoints/PolicyEndpoints.cs`
- `engine/src/Nebula.Application/Services/PolicyService.cs`
- `engine/src/Nebula.Infrastructure/Repositories/PolicyRepository.cs`

## Authorization Contract

- Policy actions are role-gated through `policy.csv` entries for `policy:read`, `policy:create`, `policy:update`, `policy:issue`, `policy:endorse`, `policy:cancel`, `policy:reinstate`, `policy:coverage:manage`, and `policy:import`.
- Read scope is enforced by `PolicyRepository.GetScopedQuery` with Admin, regional distribution roles, Underwriter, RelationshipManager, ProgramManager, and BrokerUser handling.
- Create and lifecycle mutation scope now checks the same account/broker visibility before committing changes.

## Findings

| Severity | Finding | Resolution |
|----------|---------|------------|
| Critical | None remaining. | N/A |
| High | Mutation endpoints loaded policies for update by ID after only role/action authorization, allowing scoped write roles to mutate out-of-scope policies. | Fixed by routing update, issue, endorse, cancel, and reinstate through an accessible-by-id check before loading the tracked row. |
| High | Create/import/from-bind accepted arbitrary account and broker IDs for scoped create roles after only role/action authorization. | Fixed by validating create scope against account region, managed broker, or broker-user tenant scope before creating the policy. |

## Verification

- `dotnet test engine/Nebula.slnx` => PASS, 395 passed, 1 skipped
- Runtime preflight evidence: `planning-mds/operations/evidence/f0018/runtime-preflight-2026-04-22.md`

## Residual Risk

- Dedicated negative authorization tests for policy create/update scope should be added to lock the repaired behavior.
