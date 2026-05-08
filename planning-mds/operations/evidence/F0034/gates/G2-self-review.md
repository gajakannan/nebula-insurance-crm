# F0034 G2 Self-Review Evidence

**Date:** 2026-05-07
**Run ID:** 913f3e51-9bb5-4af9-80ba-d7778abbd304

## Backend Role

- Verdict: PASS
- Evidence:
  - `engine/src/Nebula.Api/Endpoints/LobSchemaEndpoints.cs`
  - `engine/src/Nebula.Application/Services/LobAttributeService.cs`
  - `engine/src/Nebula.Application/Services/LobSchemaService.cs`
  - `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260507030000_F0034_ProductSchemaRegistryAndLobAttributes.cs`
  - `engine/tests/Nebula.Tests/Unit/LobAttributes/LobAttributeServiceTests.cs`
- Review notes:
  - Carrier pinning reconciled to non-null `LobProductVersionId` plus `{}` default JSON.
  - Approved sentinel IDs are used for `_unspecified/0.0.0`, `cyber/1.0.0`, and `_legacy_cyber/0.0.0`.
  - Migration seeds per-LOB legacy sentinels and installs `trg_lob_carrier_consistency`.
  - OpenAPI contract routes `/lob-schemas/{productVersionId}/{stage}` and `/lob-schemas/{productVersionId}/activate` are implemented.

## Frontend Role

- Verdict: PASS
- Evidence:
  - `experience/src/features/lob-attributes/components/DynamicAttributePanel.tsx`
  - `experience/src/features/lob-attributes/lib/cyber.ts`
  - `experience/src/features/lob-attributes/hooks/useLobSchemaBundle.ts`
  - `experience/src/pages/CreateSubmissionPage.tsx`
  - `experience/src/pages/CreatePolicyPage.tsx`
- Review notes:
  - Cyber panel emits the nested controls and money-minor envelope used by backend validation and seeded schema bundles.
  - Frontend schema resolver now calls the product-version/stage route from the planning OpenAPI.
  - The UI build passed in a Node 22 container with the repo pnpm virtual store mounted.

## Quality Engineer Role

- Verdict: PASS
- Evidence:
  - Container backend build: `docker run ... mcr.microsoft.com/dotnet/sdk:10.0 dotnet build engine/src/Nebula.Api/Nebula.Api.csproj -p:BaseOutputPath=/tmp/nebula-f0034-container-build/`
  - Container backend unit tests: `docker run ... mcr.microsoft.com/dotnet/sdk:10.0 dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter LobAttributeServiceTests -p:BaseOutputPath=/tmp/nebula-f0034-container-test/`
  - Container frontend build: `docker run ... node:22 corepack pnpm --dir experience build`
- Result summary:
  - Backend build passed with 3 pre-existing nullable warnings in `DashboardRepository` and `SubmissionRepository`.
  - `LobAttributeServiceTests`: 4 passed, 0 failed.
  - Frontend build passed with the existing Vite chunk-size warning.
  - Full frontend `pnpm test` in the ad hoc Node 22 container failed on an out-of-scope `src/services/api.test.ts` cross-realm `Blob instanceof Blob` assertion. `hint.py` maps that test to document features outside F0034, so it was not edited.

## DevOps Role

- Verdict: PASS
- Evidence:
  - `docker compose ps`
  - `docker exec nebula-api bash -lc 'exec 3<>/dev/tcp/127.0.0.1/8080; printf "GET /healthz HTTP/1.1\r\nHost: localhost\r\n\r\n" >&3; head -n 1 <&3'`
- Result summary:
  - Runtime containers remain up; DB and Authentik services are healthy.
  - API container health probe returned `HTTP/1.1 200 OK`.
  - The running API image is runtime-only and has no SDK; compile/test validation used the matching SDK container.

## Architect Role

- Verdict: PASS
- Evidence:
  - `planning-mds/features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/feature-assembly-plan.md`
  - `planning-mds/api/nebula-api.yaml`
  - `planning-mds/security/policies/policy.csv`
  - `planning-mds/lob-schemas/cyber/1.0.0/`
- Review notes:
  - Code follows the approved carrier set: Submission, Renewal, PolicyVersion, PolicyEndorsement.
  - Code reconciled route shape to the planning OpenAPI.
  - No canonical node edits were made during implementation.
