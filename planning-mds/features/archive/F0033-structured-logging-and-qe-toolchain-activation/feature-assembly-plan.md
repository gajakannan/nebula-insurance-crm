# Feature Assembly Plan — F0033: Structured Logging and QE Toolchain Activation

**Created:** 2026-03-28
**Author:** Architect Agent
**Status:** Approved

> **Purpose:** Implementation execution plan for F0033. This is an infrastructure and release-enablement feature, so the plan focuses on runtime wiring, scripts, CI workflows, and representative verification slices rather than new domain entities or API resources.

## Overview

F0033 activates the cross-cutting stack Nebula has already approved in architecture docs but not yet wired into the working solution: Serilog for structured API logging, Bruno for repo-native API collections, Lighthouse CI for frontend performance validation, Pact for one representative consumer/provider contract, and SonarQube Community for solution-wide code-quality reporting.

The implementation must stay solution-scoped. No generic `agents/**` work is required. The heavy QE services should be isolated behind an opt-in compose overlay so normal local app development does not inherit unnecessary runtime weight.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Serilog structured logging baseline | S0001 | Establishes the observability contract used by later QE/debugging work. |
| 2 | Bruno API validation path | S0002 | Fastest repo-native QE activation with low dependency cost. |
| 3 | Lighthouse CI performance gate | S0003 | Extends frontend validation without changing existing production auth guardrails. |
| 4 | Pact contract slice + broker workflow | S0004 | Adds a deeper consumer/provider guarantee once representative API validation is live. |
| 5 | SonarQube Community reporting | S0005 | Aggregates backend/frontend quality signals and coverage once the other layers are executable. |

## Existing Code (Must Be Modified)

| File | Current State | F0033 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Api/Nebula.Api.csproj` | No Serilog packages. | **Expand** — add Serilog runtime/configuration packages. |
| `engine/src/Nebula.Api/Program.cs` | Default ASP.NET logging pipeline and ProblemDetails trace handling only. | **Rewrite** — bootstrap Serilog, add request/user context enrichment, and keep `traceId` parity. |
| `engine/src/Nebula.Api/appsettings.json` | Minimal `Logging` section only. | **Rewrite** — define `Serilog` configuration root and default sink/level policy. |
| `engine/src/Nebula.Api/appsettings.Development.json` | Development logging levels only. | **Expand** — add dev overrides for structured logging verbosity and sink behavior. |
| `engine/tests/Nebula.Tests/Nebula.Tests.csproj` | No PactNet or Serilog test-sink dependency. | **Expand** — add packages needed for provider verification and logging assertions. |
| `experience/package.json` | No Lighthouse CI or Pact scripts/dependencies. | **Expand** — add `lhci`/Pact scripts and supporting dev dependencies. |
| `docker/postgres/init-databases.sh` | Creates `authentik`, `temporal`, and `temporal_visibility` databases only. | **Expand** — add optional `pactbroker` and `sonarqube` databases for QE overlay services. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Api/Logging/RequestLogContextMiddleware.cs` | Backend | Push `TraceId`, route, and user context into Serilog `LogContext`. |
| `engine/tests/Nebula.Tests/Integration/Logging/StructuredLoggingTests.cs` | Backend Tests | Assert structured log events and `traceId` correlation behavior. |
| `bruno/nebula/nebula.json` | Tooling | Root Bruno collection metadata. |
| `bruno/environments/local.bru` | Tooling | Local Bruno environment template. |
| `bruno/environments/ci.bru` | Tooling | CI Bruno environment template. |
| `bruno/nebula/auth/token.bru` | Tooling | Auth token acquisition request. |
| `bruno/nebula/system/healthz.bru` | Tooling | Health endpoint validation. |
| `bruno/nebula/brokers/list-brokers.bru` | Tooling | Representative broker list validation. |
| `bruno/nebula/tasks/list-my-tasks.bru` | Tooling | Representative task read validation. |
| `scripts/run-bruno.sh` | Tooling | Repo-standard Bruno execution entry point. |
| `experience/lighthouserc.json` | Frontend | Route list, assertions, and artifact paths for Lighthouse CI. |
| `scripts/run-lhci.sh` | Tooling | Repo-standard Lighthouse CI entry point. |
| `experience/tests/contracts/broker-list.contract.spec.ts` | Frontend Tests | Pact consumer contract for broker list. |
| `engine/tests/Nebula.Tests/Contracts/BrokerListProviderPactTests.cs` | Backend Tests | Pact provider verification for broker list. |
| `scripts/run-pact-provider.sh` | Tooling | Repo-standard Pact provider verification and optional broker publication. |
| `docker-compose.qe.yml` | DevOps | Optional QE services overlay (`pact-broker`, `sonarqube`). |
| `scripts/run-sonar.sh` | Tooling | Repo-standard SonarQube Community analysis entry point. |
| `.github/workflows/qe-api.yml` | CI/CD | Bruno API validation workflow. |
| `.github/workflows/frontend-performance.yml` | CI/CD | Lighthouse CI workflow. |
| `.github/workflows/pact-contract.yml` | CI/CD | Pact consumer/provider verification workflow. |
| `.github/workflows/sonarqube.yml` | CI/CD | SonarQube Community analysis workflow. |

---

## Step 1 — Serilog Structured Logging Baseline (F0033-S0001)

### New Files

| File | Layer |
|------|-------|
| `engine/src/Nebula.Api/Logging/RequestLogContextMiddleware.cs` | Backend |
| `engine/tests/Nebula.Tests/Integration/Logging/StructuredLoggingTests.cs` | Backend Tests |

### Modified Files

| File | Change |
|------|--------|
| `engine/src/Nebula.Api/Nebula.Api.csproj` | Add `Serilog.AspNetCore`, `Serilog.Settings.Configuration`, `Serilog.Sinks.Console`, and any chosen enrichment package. |
| `engine/src/Nebula.Api/Program.cs` | Replace default host logger bootstrap with `builder.Host.UseSerilog(...)`, add request logging, add context middleware, preserve current exception/ProblemDetails behavior. |
| `engine/src/Nebula.Api/appsettings.json` | Add canonical `Serilog` section. |
| `engine/src/Nebula.Api/appsettings.Development.json` | Add dev sink/level overrides. |
| `engine/tests/Nebula.Tests/Nebula.Tests.csproj` | Add test dependencies required to capture or assert structured log events. |

### Code / Config Contract

```csharp
// engine/src/Nebula.Api/Logging/RequestLogContextMiddleware.cs
using Serilog.Context;
using System.Diagnostics;

namespace Nebula.Api.Logging;

public sealed class RequestLogContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var traceId = LogContext.PushProperty(
            "TraceId",
            Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);
        using var requestPath = LogContext.PushProperty("RequestPath", context.Request.Path.Value ?? "/");
        using var requestMethod = LogContext.PushProperty("RequestMethod", context.Request.Method);

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var subject = context.User.FindFirst("sub")?.Value;
            var roles = context.User.FindAll("nebula_roles").Select(c => c.Value).ToArray();

            using var userId = LogContext.PushProperty("IdpSubject", subject ?? string.Empty);
            using var userRoles = LogContext.PushProperty("UserRoles", roles, destructureObjects: true);
            await _next(context);
            return;
        }

        await _next(context);
    }
}
```

```csharp
// engine/src/Nebula.Api/Program.cs — logging bootstrap and request logging shape
using Serilog;
using Serilog.Events;
using Nebula.Api.Logging;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

app.UseMiddleware<RequestLogContextMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
        ex is not null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", System.Diagnostics.Activity.Current?.TraceId.ToString());
        diagnosticContext.Set("StatusCode", httpContext.Response.StatusCode);
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
    };
});
```

```json
// engine/src/Nebula.Api/appsettings.json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "Enrich": [ "FromLogContext" ],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.RenderedCompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ]
  }
}
```

### Logic Flow

`API request` → returns `HTTP response + Serilog log events`

1. `Program.cs` bootstraps Serilog before the app host is built.
2. `RequestLogContextMiddleware` pushes `TraceId`, request metadata, and authenticated user context into `LogContext`.
3. `UseSerilogRequestLogging()` emits the completion log with status code and elapsed timing.
4. Existing exception and status-code handlers continue returning ProblemDetails with `traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier`.
5. `StructuredLoggingTests` capture emitted events and assert required properties exist and match the request/error response correlation contract.

### Verification Contract

| Signal | Expected Result |
|--------|-----------------|
| API startup | Serilog becomes the active host logger without startup regression |
| Successful request | Structured completion log includes `TraceId`, `RequestPath`, and `StatusCode` |
| ProblemDetails response | `traceId` maps to the same request correlation context |
| Sensitive headers | Not present in emitted baseline log payload |

### Integration Checkpoint — After Step 1

- [ ] API starts and serves requests with Serilog active
- [ ] Structured logging test passes
- [ ] ProblemDetails `traceId` remains intact
- [ ] No raw auth token or authorization header appears in baseline logs

---

## Step 2 — Bruno API Validation Path (F0033-S0002)

### New Files

| File | Layer |
|------|-------|
| `bruno/nebula/nebula.json` | Tooling |
| `bruno/environments/local.bru` | Tooling |
| `bruno/environments/ci.bru` | Tooling |
| `bruno/nebula/auth/token.bru` | Tooling |
| `bruno/nebula/system/healthz.bru` | Tooling |
| `bruno/nebula/brokers/list-brokers.bru` | Tooling |
| `bruno/nebula/tasks/list-my-tasks.bru` | Tooling |
| `scripts/run-bruno.sh` | Tooling |
| `.github/workflows/qe-api.yml` | CI/CD |

### Implementation Specification

```bash
# scripts/run-bruno.sh
#!/usr/bin/env bash
set -euo pipefail

BRUNO_ENV="${BRUNO_ENV:-local}"
REPORT_DIR="${REPORT_DIR:-planning-mds/operations/evidence/f0033/artifacts/bruno}"
mkdir -p "$REPORT_DIR"

pnpm dlx @usebruno/cli run bruno/nebula \
  --env "$BRUNO_ENV" \
  --reporter-junit "$REPORT_DIR/bruno-junit.xml" \
  --reporter-json "$REPORT_DIR/bruno-report.json"
```

### Logic Flow

`run-bruno.sh` → returns `collection exit code + JUnit/JSON reports`

1. Resolve base URL and auth variables from `local.bru` or `ci.bru`.
2. Execute the token request first and persist the access token into Bruno runtime variables.
3. Run representative requests in order: `/healthz`, `/brokers`, `/my/tasks`.
4. Assert status codes and key response-shape expectations.
5. Emit JUnit/JSON output under `planning-mds/operations/evidence/f0033/artifacts/bruno/`.
6. Exit non-zero on any failing request or assertion.

### Integration Checkpoint — After Step 2

- [ ] Local Bruno run succeeds against the seeded stack
- [ ] CI workflow can run the same collection with env substitution only
- [ ] Reports are written to a stable artifact path
- [ ] Representative failures are actionable from Bruno output alone

---

## Step 3 — Lighthouse CI Performance Gate (F0033-S0003)

### New Files

| File | Layer |
|------|-------|
| `experience/lighthouserc.json` | Frontend |
| `scripts/run-lhci.sh` | Tooling |
| `.github/workflows/frontend-performance.yml` | CI/CD |

### Modified Files

| File | Change |
|------|--------|
| `experience/package.json` | Add `test:performance` or equivalent `lhci` script and dependency. |

### Code / Config Contract

```json
// experience/lighthouserc.json
{
  "ci": {
    "collect": {
      "url": [
        "http://127.0.0.1:4173/login",
        "http://127.0.0.1:4173/",
        "http://127.0.0.1:4173/brokers"
      ],
      "numberOfRuns": 3
    },
    "assert": {
      "assertions": {
        "categories:performance": ["warn", { "minScore": 0.70 }],
        "categories:accessibility": ["error", { "minScore": 0.90 }],
        "first-contentful-paint": ["warn", { "maxNumericValue": 2500 }]
      }
    },
    "upload": {
      "target": "filesystem",
      "outputDir": "planning-mds/operations/evidence/f0033/artifacts/lighthouse"
    }
  }
}
```

```bash
# scripts/run-lhci.sh
#!/usr/bin/env bash
set -euo pipefail

export VITE_AUTH_MODE="${VITE_AUTH_MODE:-dev}"
export NEBULA_API_PROXY_TARGET="${NEBULA_API_PROXY_TARGET:-http://localhost:8080}"

pnpm --dir experience install --frozen-lockfile
pnpm --dir experience exec lhci autorun --config=./lighthouserc.json
```

### Logic Flow

`run-lhci.sh` → returns `LHCI exit code + HTML/JSON report artifacts`

1. Start the approved frontend performance runtime profile.
2. Use a non-production auth mode only if required for protected routes and only within this perf-only execution path.
3. Run Lighthouse against `/login`, `/`, and `/brokers`.
4. Write reports to `planning-mds/operations/evidence/f0033/artifacts/lighthouse/`.
5. Fail CI when committed assertions regress.

### Integration Checkpoint — After Step 3

- [ ] Lighthouse produces reports for the committed route set
- [ ] Protected-route auditing works without weakening the production build guard
- [ ] Report artifacts are uploadable from CI
- [ ] Thresholds are committed and reviewable

---

## Step 4 — Broker List Contract Testing with Pact (F0033-S0004)

### New Files

| File | Layer |
|------|-------|
| `experience/tests/contracts/broker-list.contract.spec.ts` | Frontend Tests |
| `engine/tests/Nebula.Tests/Contracts/BrokerListProviderPactTests.cs` | Backend Tests |
| `scripts/run-pact-provider.sh` | Tooling |
| `.github/workflows/pact-contract.yml` | CI/CD |
| `docker-compose.qe.yml` | DevOps |

### Modified Files

| File | Change |
|------|--------|
| `experience/package.json` | Add Pact JS dependency/script for consumer contract generation. |
| `engine/tests/Nebula.Tests/Nebula.Tests.csproj` | Add `PactNet` package for provider verification. |
| `docker/postgres/init-databases.sh` | Add `pactbroker` database creation. |

### Code / Config Contract

```typescript
// experience/tests/contracts/broker-list.contract.spec.ts
import { PactV4, MatchersV3 } from '@pact-foundation/pact'

const provider = new PactV4({
  consumer: 'nebula-experience',
  provider: 'nebula-api',
  dir: 'experience/pacts',
})

provider
  .addInteraction()
  .uponReceiving('a request for the first broker list page')
  .withRequest('GET', '/brokers?page=1&pageSize=10')
  .willRespondWith(200, (builder) => {
    builder.jsonBody({
      data: MatchersV3.eachLike({
        id: MatchersV3.uuid(),
        legalName: MatchersV3.string('Acme Brokerage'),
        status: MatchersV3.string('Active'),
      }),
      page: 1,
      pageSize: 10,
      totalCount: MatchersV3.integer(),
      totalPages: MatchersV3.integer(),
    })
  })
```

```csharp
// engine/tests/Nebula.Tests/Contracts/BrokerListProviderPactTests.cs
using PactNet;
using PactNet.Verifier;

public sealed class BrokerListProviderPactTests : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public void VerifyBrokerListContract()
    {
        using var pactVerifier = new PactVerifier("nebula-api");
        pactVerifier
            .WithHttpEndpoint(_factory.Server.BaseAddress!)
            .WithFileSource(new FileInfo("experience/pacts/nebula-experience-nebula-api.json"))
            .Verify();
    }
}
```

### Logic Flow

`consumer pact generation` → `provider verification` → optional `broker publication`

1. Frontend contract test generates the broker list pact file into `experience/pacts/`.
2. Backend provider verification reads that pact file and verifies it against the real in-memory test host.
3. `run-pact-provider.sh` optionally publishes the pact and verification result to `PACT_BROKER_BASE_URL` when configured.
4. CI fails on consumer or provider drift.

### Integration Checkpoint — After Step 4

- [ ] Consumer test generates pact file successfully
- [ ] Provider verification passes against the real API host
- [ ] Pact Broker remains optional locally but first-class when configured
- [ ] Broker list is documented as the representative first slice, not full coverage

---

## Step 5 — SonarQube Community Quality Reporting (F0033-S0005)

### New Files

| File | Layer |
|------|-------|
| `scripts/run-sonar.sh` | Tooling |
| `.github/workflows/sonarqube.yml` | CI/CD |

### Modified Files

| File | Change |
|------|--------|
| `docker-compose.qe.yml` | Add `sonarqube` service and wiring to use the shared Postgres service. |
| `docker/postgres/init-databases.sh` | Add `sonarqube` database creation. |

### Code / Config Contract

```bash
# scripts/run-sonar.sh
#!/usr/bin/env bash
set -euo pipefail

SONAR_HOST_URL="${SONAR_HOST_URL:-http://localhost:9001}"
SONAR_PROJECT_KEY="${SONAR_PROJECT_KEY:-nebula-crm}"

docker compose -f docker-compose.yml -f docker-compose.qe.yml up -d sonarqube

dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --collect:"XPlat Code Coverage"
pnpm --dir experience test:coverage

dotnet sonarscanner begin \
  /k:"$SONAR_PROJECT_KEY" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.cs.vstest.reportsPaths="**/TestResults/*.trx" \
  /d:sonar.javascript.lcov.reportPaths="experience/coverage/lcov.info"

dotnet build engine/Nebula.slnx

dotnet sonarscanner end
```

### Logic Flow

`run-sonar.sh` → returns `SonarQube analysis + imported coverage`

1. Start SonarQube from the QE overlay if it is not already running.
2. Run backend tests and frontend coverage generation with explicit output paths.
3. Start Sonar analysis with committed project metadata and report paths.
4. Build the solution and finish analysis upload.
5. Expose the result via CI log/artifact and fail when the configured quality gate fails.

### Integration Checkpoint — After Step 5

- [ ] SonarQube starts successfully from the optional QE overlay
- [ ] Backend and frontend coverage are both imported
- [ ] Analysis is reproducible in local and CI contexts
- [ ] Quality criteria are explicit and reviewable

---

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`engine/`) | Serilog bootstrap, request enrichment, logging tests, Pact provider verification | Backend Developer | Planned |
| Frontend (`experience/`) | Lighthouse config/runtime, Pact consumer test, frontend coverage handoff | Frontend Developer | Planned |
| Quality | Bruno collections, report conventions, evidence mapping | Quality Engineer | Planned |
| DevOps/Runtime | QE overlay compose file, workflows, scanner/runtime scripts | DevOps | Planned |
| Security | Log redaction review, service exposure review | Security Reviewer | Planned |

## Dependency Order

```text
Step 0 (Architect):   feature planning + tracker sync ← DONE
Step 1 (Backend):     Serilog baseline and logging verification
  ──── Observability checkpoint: structured request logs verified ────
Step 2 (QE/DevOps):   Bruno collection and CI API validation path
Step 3 (Frontend):    Lighthouse CI runtime and route thresholds
Step 4 (Frontend+Backend): Pact consumer/provider contract slice
  ──── Contract checkpoint: broker list pact generated + verified ────
Step 5 (DevOps+QE):   SonarQube analysis and coverage import
  ──── Quality checkpoint: backend + frontend quality report visible ────
```

## Integration Checklist

- [ ] Serilog activation verified in the running API
- [ ] Bruno reports written to a stable artifact path
- [ ] Lighthouse reports written to a stable artifact path
- [ ] Pact contract file and provider verification both succeed
- [ ] SonarQube imports both backend and frontend coverage
- [ ] Required runtime evidence artifacts identified under `planning-mds/operations/evidence/f0033/artifacts/`
- [ ] Framework vs solution boundary reviewed (no accidental `agents/**` scope creep)
- [ ] Run/deploy instructions updated for optional QE services

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Serilog redaction is too permissive or too noisy | High | Keep baseline request metadata narrow; require Security Reviewer signoff | Backend + Security |
| Lighthouse auth runtime path undermines build guardrails | High | Keep perf-only runtime separate from production build path; no production `VITE_AUTH_MODE=dev` | Frontend + Security |
| Pact Broker and SonarQube make local setup heavy | Medium | Use `docker-compose.qe.yml` so services are opt-in | DevOps |
| SonarQube coverage wiring is brittle across backend/frontend outputs | Medium | Centralize report path ownership in `run-sonar.sh` and workflow config | DevOps + QE |
| Representative slice expands into too much scope | Medium | Keep Pact to broker list only in F0033; defer broader rollout | PM + Architect |

## JSON Serialization Convention

No new serialization rules are introduced. Pact and Bruno must consume the current API JSON as implemented. If the broker list response shape changes during implementation, update the contract and Bruno assertions together in the same change.

## DI Registration Changes

None expected beyond the Serilog host bootstrap and middleware registration in `Program.cs`.

## Casbin Policy Sync

None. F0033 activates observability and QE tooling around existing routes and policies; it does not add or modify authorization rules.
