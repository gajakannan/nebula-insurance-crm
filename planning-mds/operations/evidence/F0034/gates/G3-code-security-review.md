# F0034 G3 Code + Security Review Evidence

**Date:** 2026-05-07
**Run ID:** 913f3e51-9bb5-4af9-80ba-d7778abbd304

## Code Review

- Verdict: PASS
- Critical findings: 0
- High findings: 0
- Medium findings: 0
- Evidence paths:
  - `engine/src/Nebula.Application/Services/LobAttributeService.cs`
  - `engine/src/Nebula.Application/Services/PolicyService.cs`
  - `engine/src/Nebula.Application/Services/RenewalService.cs`
  - `engine/src/Nebula.Application/Services/SubmissionService.cs`
  - `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260507030000_F0034_ProductSchemaRegistryAndLobAttributes.cs`
  - `experience/src/features/lob-attributes/`
- Review notes:
  - Backend and frontend Cyber validators use the same nested control and money-minor shape.
  - Lifecycle services persist validated envelopes and carry current policy-version attributes into renewal/reinstatement flows.
  - Repository and endpoint routes now expose the OpenAPI product-version/stage resolver.
  - `git diff --check` passed.

## Security Review

- Verdict: PASS
- Critical findings: 0
- High findings: 0
- Medium findings: 0
- Evidence paths:
  - `engine/src/Nebula.Api/Endpoints/LobSchemaEndpoints.cs`
  - `engine/src/Nebula.Api/Helpers/ProblemDetailsHelper.cs`
  - `planning-mds/security/policies/policy.csv`
  - `planning-mds/security/authorization-matrix.md`
- Review notes:
  - `/lob-schemas/**` routes require authentication and explicit `lob_schema` read, resolve, or activate authorization.
  - Lifecycle writes fail closed through `LobAttributeService` validation and structured `lobErrors` problem details.
  - DB trigger `trg_lob_carrier_consistency` enforces product/LOB mismatch protection and product-version immutability outside migration allowances.
  - No secrets, external network calls, or unauthenticated product-schema mutation paths were introduced.
