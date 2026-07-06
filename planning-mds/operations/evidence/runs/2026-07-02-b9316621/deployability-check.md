# Deployability Check — F0027-coi-acord-and-outbound-document-generation run 2026-07-02-b9316621

## Runtime / Deployment Config Changes

No Dockerfile, compose file, CI job, startup script, deployment manifest, or environment variable contract was changed. Runtime-bearing code changed in API/application/infrastructure/frontend layers.

## Migrations / Rollback

No EF migration was added. F0027 stores generated artifacts through the existing ADR-012 filesystem sidecar repository, so rollback is code rollback plus preservation/removal of generated sidecar documents according to retention policy.

## Env / Config Contract

No new environment variables or secrets were introduced. Runtime document configuration added a non-secret metadata schema and taxonomy entry:

- `engine/src/Nebula.Api/data/documents/configuration/taxonomy.yaml`
- `engine/src/Nebula.Api/data/documents/configuration/metadata-schemas/registry.yaml`
- `engine/src/Nebula.Api/data/documents/configuration/metadata-schemas/generated-document.v1.schema.json`

## Manifest Boolean Cross-Check

- `runtime_bearing = true`: correct; backend API and frontend runtime surfaces changed.
- `deployment_config_changed = false`: correct; no deployment config or env contract changed.
- `security_sensitive_scope = true`: correct; document generation, authorization checks, and downloadable artifacts are in scope.

## Build / Start / Smoke Results

```text
- start/preflight: docker compose ps -> exit 0; API, DB, authentik-server, and authentik-worker up
- build: dotnet build engine/src/Nebula.Api/Nebula.Api.csproj -> exit 0
- build: pnpm --dir experience build -> exit 0
- unit smoke: dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter OutboundDocumentGenerationServiceTests --no-build -> exit 0
- component smoke: pnpm --dir experience test src/features/documents/tests/ParentDocumentsPanel.test.tsx -> exit 0
```

## Runtime Warnings

- Docker preflight reported unset `AUTHENTIK_SECRET_KEY` and `AUTHENTIK_BOOTSTRAP_PASSWORD` warnings, while existing containers remained healthy.
- `dotnet build` reports existing `Microsoft.OpenApi` NU1903 high-severity advisory. Security review must disposition this at G3.
- Vite build reports an existing large chunk warning.

## Recommendations

- [medium] Security reviewer should disposition the existing `Microsoft.OpenApi` advisory during G3 — owner: Security Reviewer; follow-up: F0027-G3-security

## Result

PASS WITH RECOMMENDATIONS
