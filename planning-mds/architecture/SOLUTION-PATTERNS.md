# Nebula CRM - Solution-Specific Patterns

**Purpose:** This document captures architectural decisions, design patterns, and coding conventions specific to the Nebula CRM solution. These patterns ensure consistency across all implementations and serve as institutional memory.

**Last Updated:** 2026-02-17

---

## How to Use This Document

### During Planning (Architect)
- Read patterns before designing architecture
- Apply patterns to new designs
- Reference patterns in ADRs
- Update patterns when new conventions emerge

### During Implementation (Developers)
- Read relevant patterns before coding
- Follow established conventions
- Ask if pattern doesn't fit your use case
- Propose pattern updates via ADR

### During Review (Code Reviewer, Security)
- Validate implementations against patterns
- Check for pattern violations
- Suggest pattern improvements
- Approve pattern updates

---

## 1. Authorization Patterns

### Pattern: Use Casbin ABAC
**Decision:** All authorization uses Casbin with Attribute-Based Access Control (ABAC)
**Rationale:** Need fine-grained, attribute-based permissions beyond simple role-based access
**Applied in:** All API endpoints, domain services
**Enforcement:** Server-side only (never trust client)

**Example:**
```csharp
// Check permission before operation
if (!await _authorizationService.Authorize(user, "broker", "read", broker))
{
    return Forbid();
}
```

### Pattern: JWT Token Authentication
**Decision:** Use JWT tokens issued by **authentik** for authentication
**Rationale:** Standard, stateless, integrates with existing identity provider (authentik). See [ADR-006](../architecture/decisions/ADR-006-authentik-idp-migration.md) for IdP choice rationale.
**Applied in:** All API requests
**Token Location:** Authorization header (Bearer token)
**OIDC Authority:** `http://authentik-server:9000/application/o/nebula/`
**Roles claim:** `nebula_roles` (flat string array, authentik property mapping)

### Pattern: Per-Endpoint Authorization
**Decision:** Every API endpoint must explicitly check authorization
**Rationale:** Defense in depth, fail-secure
**Applied in:** All controllers/endpoints

**Example:**
```csharp
[HttpGet("{id}")]
[Authorize] // Authentication required
public async Task<IActionResult> GetBroker(Guid id)
{
    // Explicit authorization check
    var broker = await _brokerService.GetAsync(id);
    if (!await _authz.CanRead(User, broker))
        return Forbid();

    return Ok(broker);
}
```

---

## 2. Audit & Timeline Patterns

### Pattern: All Mutations Create Timeline Events
**Decision:** Every data mutation creates an immutable ActivityTimelineEvent
**Rationale:** Compliance requires complete audit trail of all changes
**Applied in:** All services that modify data

**Example:**
```csharp
public async Task<Broker> UpdateBrokerAsync(Guid id, UpdateBrokerDto dto)
{
    var broker = await _repo.GetAsync(id);
    broker.Update(dto);

    // MUST create timeline event
    await _timelineService.CreateEventAsync(new ActivityTimelineEvent
    {
        EntityType = "Broker",
        EntityId = id,
        EventType = "BrokerUpdated",
        Description = $"Broker {broker.Name} updated",
        PerformedBy = _currentUser.Id,
        PerformedAt = DateTime.UtcNow,
        Metadata = JsonSerializer.Serialize(dto)
    });

    await _repo.SaveAsync();
    return broker;
}
```

### Pattern: EventDescription is Pre-Rendered on Write
**Decision:** The `EventDescription` field on `ActivityTimelineEvent` is pre-rendered at write time, not computed at query time
**Rationale:** Keeps the activity feed query simple (no template resolution), ensures historical descriptions remain stable even if templates change later
**Applied in:** All timeline event creation
**Templates:** Defined in `planning-mds/schemas/activity-event-payloads.schema.json` under `descriptionTemplate` per event type

**Example:** For `BrokerCreated`, the template is `"New broker \"{legalName}\" added"`. The service resolves placeholders from the event payload and stores the result:
```csharp
var description = $"New broker \"{dto.LegalName}\" added";
event.EventDescription = description; // stored alongside EventPayloadJson
```

**Important:** Both `EventDescription` (human-readable) and `EventPayloadJson` (structured data) are stored. The payload is the source of truth for programmatic use; the description is for display only.

### Pattern: Workflow Transitions are Append-Only
**Decision:** WorkflowTransition table is append-only (immutable history)
**Rationale:** Audit compliance, state machine integrity
**Applied in:** Submission workflow, Renewal workflow

**Example:**
```csharp
// Never update or delete - always append
var transition = new WorkflowTransition
{
    WorkflowType = "Submission",
    EntityId = submission.Id,
    FromState = submission.Status,
    ToState = newStatus,
    ActorUserId = _currentUser.Id,
    OccurredAt = DateTime.UtcNow,
    Reason = reason
};

await _transitionRepo.AddAsync(transition); // Append only!
submission.Status = newStatus;
```

---

## 3. API Design Patterns

### Pattern: REST Resource Conventions
**Decision:** API endpoints follow `/{resource}` and `/{resource}/{id}` pattern
**Rationale:** Simplicity, RESTful conventions, and cleaner gateway/public API paths
**Applied in:** All REST endpoints
**Profile:** `planning-mds/architecture/api-guidelines-profile.md`

**Examples:**
```
GET    /brokers           - List brokers
POST   /brokers           - Create broker
GET    /brokers/{id}      - Get broker detail
PUT    /brokers/{id}      - Update broker
DELETE /brokers/{id}      - Delete broker
```

**Sub-resource vs query-parameter convention:**
Use flat collection endpoints with query-parameter filters (e.g. `GET /contacts?brokerId=...`)
when the child entity may be queried across parents or independently. Use sub-resource URLs
(e.g. `GET /brokers/{id}/submissions`) only when the child is always accessed in the context
of a single parent. For Nebula MVP, contacts use the query-parameter pattern; submissions and
renewals use the sub-resource pattern.

### Pattern: RFC 7807 ProblemDetails for Errors
**Decision:** All API errors return RFC 7807 ProblemDetails format
**Rationale:** Standard, machine-readable error format
**Applied in:** All API error responses
**Error codes:** See `planning-mds/architecture/error-codes.md` for the authoritative list.
**Media type:** `application/problem+json`
**Profile:** `planning-mds/architecture/api-guidelines-profile.md`

**Example:**
```csharp
return Problem(
    title: "Broker not found",
    detail: $"Broker with ID {id} does not exist",
    statusCode: StatusCodes.Status404NotFound,
    type: "https://docs.nebula.com/errors/broker-not-found"
);
```

### Pattern: Pagination for List Endpoints
**Decision:** All list endpoints support pagination with query parameters
**Rationale:** Performance, avoid returning huge datasets
**Applied in:** All GET collection endpoints

**Example:**
```
GET /brokers?page=1&pageSize=20
GET /submissions?page=2&pageSize=50
```

### Pattern: Nullable Field Conventions
**Decision:** Use each specification's native nullable syntax — do not mix them
**Rationale:** OpenAPI 3.0.x and JSON Schema Draft-07 express nullability differently. Mixing conventions causes codegen and validator mismatches.

**Rules:**
- **OpenAPI 3.0.3** (`nebula-api.yaml`): Use `nullable: true` alongside the type.
  ```yaml
  email:
    type: string
    format: email
    nullable: true
  ```
- **JSON Schema Draft-07** (`planning-mds/schemas/*.json`): Use type arrays.
  ```json
  "email": { "type": ["string", "null"], "format": "email" }
  ```
- **C# DTOs:** Map nullable fields to `string?` (nullable reference types).
- **TypeScript:** Map to `string | null`.

**Note:** Both conventions are semantically equivalent. The difference is purely syntactical per spec version. OpenAPI 3.1+ aligns with JSON Schema, but Nebula MVP uses OpenAPI 3.0.3.

---

## 4. Clean Architecture Patterns

### Pattern: Four-Layer Architecture
**Decision:** Domain → Application → Infrastructure → API layers
**Rationale:** Clean Architecture, separation of concerns, testability
**Applied in:** Entire backend codebase

**Layer Rules:**
```
Domain Layer (innermost)
  - Entities, Value Objects, Domain Events
  - No dependencies on outer layers
  - Pure business logic

Application Layer
  - Services, Use Cases, Interfaces
  - Depends only on Domain
  - Defines repository interfaces

Infrastructure Layer
  - EF Core, Repositories, External Services
  - Implements Application interfaces
  - Depends on Application and Domain

API Layer (outermost)
  - Controllers, DTOs, Middleware
  - Depends on Application
  - Thin layer, delegates to Application services
```

### Pattern: Repository Pattern
**Decision:** Use repository pattern for data access
**Rationale:** Abstraction, testability, clean separation
**Applied in:** All data access

**Example:**
```csharp
// Application layer defines interface
public interface IBrokerRepository
{
    Task<Broker> GetAsync(Guid id);
    Task<IEnumerable<Broker>> ListAsync();
    Task AddAsync(Broker broker);
}

// Infrastructure layer implements with EF Core
public class BrokerRepository : IBrokerRepository
{
    private readonly AppDbContext _context;
    // Implementation...
}
```

---

## 5. Frontend Patterns

### Pattern: React Hook Form for Manual Forms
**Decision:** Manual forms (static, custom layouts) use React Hook Form
**Rationale:** Performance, minimal re-renders, great DX, full control over UI
**Applied in:** User-facing forms with custom designs and branding

**Example:**
```tsx
const { register, handleSubmit, formState: { errors } } = useForm<BrokerFormData>({
  resolver: ajvResolver(brokerSchema) // JSON Schema validation
});

<form onSubmit={handleSubmit(onSubmit)}>
  <input {...register("name")} />
  {errors.name && <span className="text-red-500">{errors.name.message}</span>}
</form>
```

### Pattern: RJSF for Dynamic Forms
**Decision:** Dynamic/admin forms use RJSF (React JSON Schema Form)
**Rationale:** Auto-generates forms from JSON Schema, great for admin panels, configurable forms, rapid development
**Applied in:** Admin interfaces, schema-driven forms, configurable workflows
**When to use:** Forms that need to adapt to changing schemas, or when speed > custom design

**Example:**
```tsx
import Form from '@rjsf/core';
import validator from '@rjsf/validator-ajv8';

// Schema loaded from shared location
const schema = {
  type: 'object',
  properties: {
    name: { type: 'string', title: 'Broker Name' },
    email: { type: 'string', format: 'email' },
  },
  required: ['name', 'email']
};

<Form
  schema={schema}
  validator={validator}
  onSubmit={({ formData }) => handleSubmit(formData)}
/>
```

**Custom Widgets:**
```tsx
// Use shadcn/ui components as RJSF widgets
const CustomTextWidget = (props) => (
  <Input
    value={props.value}
    onChange={(e) => props.onChange(e.target.value)}
  />
);

const widgets = { TextWidget: CustomTextWidget };

<Form schema={schema} validator={validator} widgets={widgets} />
```

### Pattern: JSON Schema + AJV for Validation
**Decision:** Use JSON Schema for validation with AJV validator
**Rationale:** Language-agnostic schemas shared between frontend (TypeScript) and backend (C#), consistent validation rules across tiers
**Applied in:** All forms with validation, API request/response validation
**Schema Location:** `planning-mds/schemas/` or `experience/src/schemas/` (shared schemas in planning-mds)

**Why JSON Schema + AJV for this architecture:**
- Backend C# can validate using same schema (NJsonSchema or similar)
- Single source of truth for validation rules
- TypeScript types can be generated from schemas
- Industry standard, works with OpenAPI

**Example:**
```tsx
import { ajvResolver } from '@hookform/resolvers/ajv';
import Ajv from 'ajv';
import addErrors from 'ajv-errors';
import type { JSONSchemaType } from 'ajv';

// Define schema (can be loaded from shared JSON file)
interface BrokerFormData {
  name: string;
  email: string;
  state: string;
}

const brokerSchema: JSONSchemaType<BrokerFormData> = {
  type: 'object',
  properties: {
    name: {
      type: 'string',
      minLength: 1,
      errorMessage: 'Name is required'
    },
    email: {
      type: 'string',
      format: 'email',
      errorMessage: 'Invalid email address'
    },
    state: {
      type: 'string',
      minLength: 2,
      maxLength: 2,
      errorMessage: 'State must be 2 characters'
    }
  },
  required: ['name', 'email', 'state'],
  additionalProperties: false
};

// Use with React Hook Form
const ajv = new Ajv({ allErrors: true });
addErrors(ajv);

const { register, handleSubmit } = useForm<BrokerFormData>({
  resolver: ajvResolver(brokerSchema)
});
```

**Alternative: Load from shared schema file**
```tsx
// Load schema from planning-mds/schemas/broker.schema.json
import brokerSchema from '@/schemas/broker.schema.json';
import { ajvResolver } from '@hookform/resolvers/ajv';

const { register, handleSubmit } = useForm({
  resolver: ajvResolver(brokerSchema)
});
```

### Pattern: TanStack Query for API Calls
**Decision:** All API calls use TanStack Query (React Query)
**Rationale:** Caching, loading states, error handling, refetching
**Applied in:** All data fetching

**Example:**
```tsx
const { data: brokers, isLoading, error } = useQuery({
  queryKey: ['brokers'],
  queryFn: () => api.brokers.list()
});
```

### Pattern: Tailwind + shadcn/ui for Styling
**Decision:** Use Tailwind CSS utility classes + shadcn/ui components
**Rationale:** Consistency, accessibility, customizable, good DX
**Applied in:** All UI components

**Example:**
```tsx
<Button variant="primary" size="lg" className="mt-4">
  Create Broker
</Button>
```

---

## 6. Data Modeling Patterns

### Pattern: Audit Fields on All Entities
**Decision:** All entities include audit fields
**Rationale:** Track who created/modified, when
**Applied in:** All domain entities

**Required Fields:**
```csharp
public class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; } // internal UserId from UserProfile; never raw IdP sub
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; } // internal UserId from UserProfile
}
```

### Pattern: Soft Delete Support
**Decision:** Entities support soft delete (not hard delete)
**Rationale:** Data recovery, audit compliance, referential integrity
**Applied in:** All deletable entities

**Additional Fields:**
```csharp
public bool IsDeleted { get; set; }
public DateTime? DeletedAt { get; set; }
public Guid? DeletedByUserId { get; set; } // internal UserId from UserProfile; never raw IdP sub
```

### Pattern: IgnoreQueryFilters for Admin/Reactivation Access
**Decision:** EF Core global query filters suppress soft-deleted records on all queries by default. Use `IgnoreQueryFilters()` only in the specific repository methods that must see deactivated records.
**Rationale:** Global filters prevent accidental exposure of deleted data; selective bypass gives Admin and reactivation flows the access they need without scattering `WHERE IsDeleted = false` clauses everywhere.
**Applied in:** Admin broker lookup (Broker 360 for deactivated broker), broker reactivation (F0002-S0008)

**Global filter (configured once in DbContext):**
```csharp
modelBuilder.Entity<Broker>().HasQueryFilter(b => !b.IsDeleted);
modelBuilder.Entity<BrokerContact>().HasQueryFilter(c => !c.IsDeleted);
```

**Bypass pattern — use sparingly and with explicit intent:**
```csharp
// Only for admin 360 view and reactivation use cases
public async Task<Broker?> GetIncludingDeactivatedAsync(Guid id)
{
    return await _context.Brokers
        .IgnoreQueryFilters()   // bypasses IsDeleted filter — Admin/reactivation only
        .FirstOrDefaultAsync(b => b.Id == id);
}
```

**Rules:**
- Standard `GetAsync` / `ListAsync` methods must NOT call `IgnoreQueryFilters()`.
- Methods that use `IgnoreQueryFilters()` must have a name suffix or comment making the bypass explicit (e.g., `GetIncludingDeactivatedAsync`).
- ABAC authorization must still be enforced after the bypass — calling `IgnoreQueryFilters()` only bypasses the EF filter, not the authorization layer.

### Pattern: Optimistic Concurrency Control
**Decision:** All mutable entities include a `RowVersion` column (EF Core concurrency token) for optimistic locking
**Rationale:** Multi-user CRM requires safe concurrent edits; optimistic locking detects conflicts without database-level locks
**Applied in:** Brokers, Contacts, Submissions, Renewals, Tasks, Accounts

**Implementation:**
```csharp
// Domain entity
public class BaseEntity
{
    // ... existing fields ...
    [Timestamp]
    public uint RowVersion { get; set; } // PostgreSQL xmin
}
```

**EF Core configuration (PostgreSQL xmin):**
```csharp
modelBuilder.Entity<Broker>()
    .UseXminAsConcurrencyToken();
```

**API pattern:** State-mutating endpoints that update an existing aggregate accept the `If-Match` header with the last returned `RowVersion` value. On stale versions, return HTTP 412 with ProblemDetails (`code=precondition_failed`).

```csharp
// In update handler
try
{
    await _repository.UpdateAsync(entity);
}
catch (DbUpdateConcurrencyException)
{
    return Problem(
        title: "Precondition failed",
        detail: "The resource was modified by another user. Refresh and retry with the latest rowVersion.",
        statusCode: 412,
        extensions: new Dictionary<string, object?> { ["code"] = "precondition_failed" }
    );
}
```

### Pattern: Use GUIDs for Primary Keys
**Decision:** All primary keys are GUIDs (not integers)
**Rationale:** Distributed systems, no conflicts, security (non-sequential)
**Applied in:** All entities

---

## 7. Testing Patterns

**Reference:** See `planning-mds/architecture/TESTING-STRATEGY.md` for comprehensive testing strategy across all tiers.

### Pattern: Test Pyramid (70-20-10 Rule)
**Decision:** 70% unit tests, 20% integration tests, 10% E2E tests
**Rationale:** Fast feedback (unit), contract verification (integration), critical flow validation (E2E)
**Applied in:** All layers (Frontend, Backend, AI/Neuron)

**Distribution:**
- **Unit Tests:** Test business logic in isolation (fast, cheap)
- **Integration Tests:** Test API contracts, database access (medium speed/cost)
- **E2E Tests:** Test critical user flows (slow, expensive)

### Pattern: Testing Tools by Layer

**Frontend (experience/):**
- **Unit/Component:** Vitest + React Testing Library
- **Integration:** Vitest + MSW (Mock Service Worker)
- **E2E:** Playwright (with MCP automation)
- **Accessibility:** @axe-core/playwright, jest-axe
- **Performance:** Lighthouse CI, Web Vitals

**Backend (engine/):**
- **Unit:** xUnit + FluentAssertions
- **Integration:** xUnit + WebApplicationFactory
- **API Collections:** Bruno CLI or curl scripts
- **Database:** xUnit + Testcontainers (real PostgreSQL)
- **Contract:** Pact.NET (consumer-driven contracts)
- **Load:** k6
- **Coverage:** Coverlet + ReportGenerator

**AI/Neuron (neuron/):**
- **Unit:** pytest
- **LLM:** pytest + mocking (fast, no API calls)
- **Evaluation:** pytest + custom metrics (accuracy, hallucination, cost)
- **Integration:** pytest + real API (use sparingly)
- **MCP Server:** pytest + FastAPI TestClient
- **Performance:** pytest-benchmark

### Pattern: ≥80% Unit Test Coverage for Business Logic
**Decision:** Minimum 80% code coverage for domain and application layers
**Rationale:** Quality assurance, regression prevention
**Applied in:** All domain services, application services
**Measurement:** Coverlet (backend), Vitest (frontend), pytest-cov (AI)

### Pattern: Integration Tests for All API Endpoints
**Decision:** Every API endpoint has integration tests
**Rationale:** Verify end-to-end functionality, catch integration issues
**Applied in:** All API controllers
**Tool:** WebApplicationFactory + xUnit (backend), MSW (frontend mocks)

**Example:**
```csharp
[Fact]
public async Task CreateBroker_WithValidData_ReturnsCreated()
{
    // Arrange
    var client = _factory.CreateClient();
    var dto = new CreateBrokerDto { Name = "Test", Email = "test@example.com" };

    // Act
    var response = await client.PostAsJsonAsync("/brokers", dto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

### Pattern: E2E Tests for Critical Workflows Only
**Decision:** All critical user workflows have E2E tests (not every feature)
**Rationale:** Verify complete user journeys, but minimize slow/expensive tests
**Applied in:** Submission workflow, Renewal workflow, Broker CRUD, Login flow
**Tool:** Playwright (cross-browser: Chrome, Firefox, Safari)

### Pattern: Real Database for Integration Tests (Testcontainers)
**Decision:** Use Testcontainers (real PostgreSQL) for database integration tests, not in-memory
**Rationale:** PostgreSQL-specific features (JSONB, triggers, full-text search), production-like environment
**Applied in:** Repository tests, migration tests
**Tool:** Testcontainers.PostgreSql

**Example:**
```csharp
private PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
    .WithImage("postgres:16")
    .Build();

await _postgresContainer.StartAsync();
var connectionString = _postgresContainer.GetConnectionString();
```

### Pattern: Contract Testing (Frontend ↔ Backend)
**Decision:** Use Pact for consumer-driven contract testing
**Rationale:** Verify frontend expectations match backend implementation
**Applied in:** All API integrations
**Tool:** Pact.NET (backend), Pact JS (frontend)

### Pattern: AI Agent Evaluation Metrics
**Decision:** Measure agent quality with custom metrics
**Metrics:**
- **Accuracy:** Compare output to golden dataset (≥85% threshold)
- **Hallucination Rate:** Detect invented information (<5%)
- **Latency:** p95 response time (<2 seconds)
- **Cost:** Token usage per request
- **Success Rate:** % of tasks completed (≥90%)

**Tool:** pytest with custom evaluation framework

### Pattern: Accessibility Testing (WCAG 2.1 AA)
**Decision:** All screens must pass WCAG 2.1 AA compliance
**Rationale:** Legal requirement, inclusive design
**Applied in:** All frontend components
**Tool:** @axe-core/playwright (E2E), jest-axe (component tests)

**Example:**
```typescript
test('broker form has no a11y violations', async ({ page }) => {
  await page.goto('/brokers/new');
  const results = await new AxeBuilder({ page }).analyze();
  expect(results.violations).toEqual([]);
});
```

### Pattern: Performance Testing Thresholds
**Decision:** Define and enforce performance thresholds
**Thresholds:**
- **Frontend:** LCP < 2.5s, FID < 100ms, CLS < 0.1
- **Backend:** p95 response time < 500ms, error rate < 1%
- **Load:** Support 100 concurrent users with <500ms p95 response

**Tools:** Lighthouse CI (frontend), k6 (backend load testing)

---

## 8. Workflow Patterns

### Pattern: Temporal.io for Workflow Engine
**Decision:** Use Temporal for long-running workflows
**Rationale:** Durable execution, retry logic, workflow visibility
**Applied in:** Submission workflow, Renewal workflow

### Pattern: State Machines for Business Workflows
**Decision:** Model workflows as explicit state machines
**Rationale:** Clear transitions, validation, audit trail
**Applied in:** Submission (10 states), Renewal (6 states)

**Example:**
```csharp
public enum SubmissionStatus
{
    Received,
    Triaging,
    WaitingOnBroker,
    ReadyForUWReview,
    InReview,
    Quoted,
    BindRequested,
    Bound,       // Terminal
    Declined,    // Terminal
    Withdrawn    // Terminal
}

// Valid transitions defined
private static readonly Dictionary<SubmissionStatus, List<SubmissionStatus>> ValidTransitions = new()
{
    { SubmissionStatus.Received, new() { SubmissionStatus.Triaging } },
    { SubmissionStatus.Triaging, new() { SubmissionStatus.WaitingOnBroker, SubmissionStatus.ReadyForUWReview } },
    // etc.
};
```

---

## 9. Cross-Cutting Concerns

### Pattern: Structured Logging with Serilog
**Decision:** Use Serilog for structured logging
**Rationale:** Queryable logs, context preservation
**Applied in:** All services, controllers

**Example:**
```csharp
_logger.LogInformation(
    "Broker {BrokerId} updated by {UserId}",
    broker.Id,
    currentUser.Id
);
```

### Pattern: Global Exception Handler
**Decision:** Use global exception handling middleware
**Rationale:** Consistent error responses, avoid scattered try-catch
**Applied in:** API layer

### Pattern: In-Memory-First Caching (Cache-Aside Default)
**Decision:** Use in-memory caching (MemoryCache) for small, low-churn datasets; use external cache (Redis) only when cross-instance consistency or scale requires it.
**Rationale:** Lower operational complexity during early implementation while preserving an upgrade path for scale.
**Applied in:** Reference data, dashboard aggregates, request-scoped ABAC resolution.

**When to use in-memory cache:**
- Data is small and changes infrequently (reference tables).
- Cache is per-process or per-request (ABAC scope resolution).
- Cross-instance consistency is not required.

**When to use external cache (Redis):**
- Multiple app instances must share cache state.
- Cache entry size or volume exceeds in-memory thresholds.
- Cache invalidation must be centralized.

**Pattern choice:**
- **Cache-aside (default):** Read from cache; on miss, load from DB and populate.
- **Write-through (selective):** Admin-managed reference data updates that require immediate consistency.
- **Write-behind:** Not used in MVP.

**Security notes:**
- Avoid caching secrets or raw PII without explicit approval.
- Include tenant/subject identifiers in cache keys for scoped data.

### Pattern: UserProfile Create-on-First-Login (Claims Normalization)
**Decision:** On every authenticated API request, upsert a `UserProfile` row keyed by `(IdpIssuer, IdpSubject)` and return the stable internal `UserId (uuid)`. All downstream code uses `NebulaPrincipal.UserId`, never a raw IdP `sub`.
**Rationale:** Dashboard queries (pipeline mini-cards, activity feed) JOIN `UserProfile` for display names and initials. The `UserId` principal key ensures ownership fields remain valid across IdP migrations. See [ADR-006](decisions/ADR-006-authentik-idp-migration.md).
**Applied in:** Authentication middleware / JWT pipeline via `IClaimsPrincipalNormalizer`
**Fields synced from JWT claims:** `Email`, `DisplayName` (from `name`), `Department`, `Regions` (custom claim), `Roles` (from `nebula_roles` — authentik property mapping)

**Implementation approach:**
```csharp
// Middleware runs after JWT validation
public class NebulaPrincipalMiddleware
{
    public async Task InvokeAsync(HttpContext context, IClaimsPrincipalNormalizer normalizer)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var principal = await normalizer.NormalizeAsync(context.User);
            context.Items["NebulaPrincipal"] = principal;
        }
        await _next(context);
    }
}
```

**Performance note:** `IClaimsPrincipalNormalizer` uses a 5-minute in-memory cache keyed by `(IdpIssuer, IdpSubject)` to avoid a DB upsert on every request.

### Pattern: OWASP Top 10 Compliance
**Decision:** Follow OWASP Top 10 security practices
**Rationale:** Industry standard security baseline
**Applied in:** All code
**Checks:** Input validation, SQL injection prevention, XSS prevention, CSRF protection, etc.

### Pattern: Rate Limiting
**Decision:** Use ASP.NET Core built-in rate limiting middleware (`Microsoft.AspNetCore.RateLimiting`)
**Rationale:** Protect against abuse and DoS without external infrastructure in MVP
**Applied in:** All API endpoints

**Default limits:**
- **Global:** Fixed window, 100 requests/minute per authenticated user (keyed by `sub` claim)
- **Anonymous:** Fixed window, 20 requests/minute per IP (login, health endpoints only)
- **Dashboard endpoints:** Sliding window, 30 requests/minute per user (prevents aggressive polling)

**Configuration:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("authenticated", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });

    options.AddFixedWindowLimiter("anonymous", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
```

**Response on limit exceeded:** HTTP 429 with `Retry-After` header and RFC 7807 ProblemDetails body.

### Pattern: Correlation ID / Request Tracing
**Decision:** Propagate a correlation ID (`X-Request-Id` header) through every request and include it in logs and error responses as `traceId`
**Rationale:** Enables end-to-end request tracing across logs, error responses, and future distributed services
**Applied in:** All API requests, all log entries, ProblemDetails responses
**Profile:** `planning-mds/architecture/api-guidelines-profile.md`

**Implementation:**
- ASP.NET Core's `Activity` provides a built-in `TraceId`. Use this as the correlation ID.
- Serilog enricher `Enrich.FromLogContext()` + `Enrich.WithProperty("TraceId", ...)` attaches it to every log entry.
- If an incoming request has `X-Request-Id`, use it as the parent; otherwise generate one.
- The `traceId` field in ProblemDetails is populated from `Activity.Current?.Id`.

```csharp
// In global exception handler / ProblemDetails factory
var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
problemDetails.Extensions["traceId"] = traceId;
```

**Frontend:** The SPA should log the `traceId` from error responses to help correlate user-reported issues with server logs.

---

## 10. DevOps Patterns

### Pattern: Docker for All Services
**Decision:** All services run in Docker containers
**Rationale:** Consistency, reproducibility, deployment simplicity
**Applied in:** Backend, Frontend, Database, authentik (server + worker), Redis

### Pattern: docker-compose for Local Development
**Decision:** Use docker-compose for local dev environment
**Rationale:** Easy setup, matches production, all services together
**Applied in:** Development workflow

### Pattern: Environment Variables for Configuration
**Decision:** All configuration via environment variables (12-factor app)
**Rationale:** Security (no secrets in code), environment-specific config
**Applied in:** All services

**Example:**
```bash
DATABASE_CONNECTION_STRING=...
Authentication__Authority=http://authentik-server:9000/application/o/nebula/
Authentication__Audience=nebula
Authentication__RolesClaim=nebula_roles
AUTHENTIK_SECRET_KEY=...
```

---

## 11. Dashboard Infographic Canvas Patterns (F0012)

### Pattern: Flat Canvas Layout (No Panels)
**Decision:** The dashboard is a single continuous flat canvas. Content zones are differentiated by spacing, typography, and color weight — never by panel borders, card wrappers, or divider lines.
**Rationale:** Infographic-style storytelling creates a more cohesive visual narrative than traditional panelized dashboards. The eye flows naturally through data without visual interruption.
**Applied in:** Dashboard page (F0012), all content zones (nudge bar, KPI band, connected flow, chapter overlays, activity feed, my tasks)

**CSS architecture:**
- `canvas-section` — full-bleed borderless container for every top-level zone
- `canvas-zone-tight` (12px) — between logically related zones (controls → KPIs → flow)
- `canvas-zone-default` (24px) — between sibling content zones
- `canvas-zone-break` (40px + background shift) — between major sections

**Tokens:** See `planning-mds/screens/design-tokens.md` → "Infographic Canvas Utilities (F0012)"

**Anti-patterns (DO NOT):**
- No `border`, `box-shadow`, or `border-radius` on content zone wrappers
- No `<Card>` or `glass-card` around dashboard sections
- No `<hr>` or `<Separator>` between zones
- No `--border` token usage within the canvas

### Pattern: Chapter Overlay Composition
**Decision:** Multiple data layers are composed onto the same base visualization (connected opportunity flow) via chapter controls. Each chapter activates a different overlay without replacing the underlying flow.
**Rationale:** Traditional tab-switching discards context. Overlay composition lets users maintain spatial orientation while layering new data dimensions (friction, aging, mix) onto the flow they already understand.
**Applied in:** Story canvas chapter controls (Flow, Friction, Outcomes, Aging, Mix)

**Component decomposition:**
```
StoryCanvas.tsx
├── ChapterControls.tsx          ← pill/tab selector for active chapter
├── ConnectedFlow.tsx            ← base visualization (always rendered)
│   └── TerminalOutcomesRail.tsx ← terminal outcome badges at flow end
├── ChapterOverlayManager.tsx    ← mounts/unmounts the active overlay
│   └── overlays/
│       ├── FrictionOverlay.tsx  ← dwell time heatmap on flow nodes
│       ├── OutcomesOverlay.tsx  ← terminal outcome breakdown
│       ├── AgingOverlay.tsx     ← age bucket distribution
│       └── MixOverlay.tsx       ← treemap/sunburst entity mix
└── KpiBand.tsx                  ← static KPI strip above flow
```

**Overlay lifecycle:**
1. Base `ConnectedFlow` renders on mount and stays mounted across chapter switches
2. `ChapterOverlayManager` receives `activeChapter` prop
3. On chapter change: fade-out current overlay (150ms CSS), unmount, mount new overlay, fade-in
4. Each overlay receives the flow node positions and adds its own data layer

**Data loading strategy:**
| Data source        | Load strategy | Trigger              |
|--------------------|---------------|----------------------|
| Nudge items        | Eager         | Mount                |
| KPIs               | Eager         | Mount                |
| Flow nodes + links | Eager         | Mount                |
| Terminal outcomes   | Eager         | Mount                |
| Friction (dwell)   | Lazy          | Chapter = "Friction" |
| Aging buckets      | Lazy          | Chapter = "Aging"    |
| Mix hierarchy      | Lazy          | Chapter = "Mix"      |

**TanStack Query pattern for lazy chapters:**
```tsx
const { data: agingData } = useQuery({
  queryKey: ['dashboard', 'aging', { periodDays, entityType }],
  queryFn: () => api.dashboard.getAgingBuckets({ periodDays, entityType }),
  enabled: activeChapter === 'aging', // only fetch when chapter is active
  staleTime: 5 * 60 * 1000,          // 5-minute stale window
});
```

### Pattern: Per-Widget Dashboard Endpoints
**Decision:** Each dashboard widget fetches from its own dedicated endpoint. No monolithic `/dashboard` endpoint.
**Rationale:** Widget-level failure isolation (one widget error doesn't break the page), independent cache TTLs, parallel fetch on mount, simpler backend query optimization per widget.
**Applied in:** Dashboard data layer
**Reference:** ADR-002 (per-widget endpoints)

**Endpoints:**
```
GET /dashboard/nudges         → nudge items
GET /dashboard/kpis           → KPI summary (accepts periodDays)
GET /dashboard/flow           → connected flow nodes + links
GET /dashboard/activity       → recent activity feed
GET /dashboard/tasks          → user's assigned tasks
```

**Frontend pattern:**
- All eager endpoints fire in parallel on mount using independent `useQuery` hooks
- Each hook has its own Error Boundary wrapper so widget failures are isolated
- Skeleton loading per widget (not a full-page spinner)

### Pattern: Flow Node Emphasis (Server-Computed)
**Decision:** The backend computes an `emphasis` hint for each flow node based on aggregate data. The frontend maps emphasis values to CSS classes without re-computing business logic.
**Rationale:** Keeps business rules (what constitutes a "bottleneck") server-side. Frontend is a pure renderer of the emphasis signal.
**Applied in:** Friction chapter overlay, `OpportunityFlowNodeDto.emphasis` field

**Emphasis values:**
| Value        | Meaning                                | CSS class                    |
|--------------|----------------------------------------|------------------------------|
| `normal`     | Default state                          | `flow-emphasis-normal`       |
| `active`     | Rightmost non-zero, non-terminal node  | `flow-emphasis-active`       |
| `blocked`    | Highest `avgDwellDays` in the flow     | `flow-emphasis-blocked`      |
| `bottleneck` | Highest `currentCount` concentration   | `flow-emphasis-bottleneck`   |
| `null`       | Not applicable (e.g., terminal node)   | No emphasis class applied    |

**Computation (backend):**
```csharp
// Pseudo-code for emphasis assignment
foreach (var node in nodes.Where(n => !n.IsTerminal))
{
    if (node.CurrentCount == maxCount) node.Emphasis = "bottleneck";
    else if (node.AvgDwellDays == maxDwell) node.Emphasis = "blocked";
    else if (node == rightmostNonZero) node.Emphasis = "active";
    else node.Emphasis = "normal";
}
```

---

## Pattern Update Process

When new patterns emerge or existing patterns need updating:

1. **Identify Pattern** - Notice repeated design decisions or conventions
2. **Document Proposal** - Write ADR explaining the pattern and rationale
3. **Review** - Architect reviews and discusses with team
4. **Approve** - Add to this document with clear rationale
5. **Apply** - Use in all new code
6. **Refactor** - Optionally update existing code to match

---

## Pattern Categories

### Must Follow (Critical)
- Authorization patterns (security-critical)
- Audit patterns (compliance-critical)
- Error handling patterns (user experience)
- Testing patterns (quality-critical)

### Should Follow (Recommended)
- API design patterns (consistency)
- Frontend patterns (maintainability)
- Data modeling patterns (integrity)
- Clean architecture patterns (maintainability)

### May Follow (Guideline)
- Code organization conventions
- Naming conventions
- Comment styles

---

## Related Documents

- `planning-mds/architecture/decisions/*.md` - ADRs explaining why patterns were chosen
- `planning-mds/BLUEPRINT.md` Section 4 - Architecture specifications
- Generic (non-project-specific) best practices are maintained in the external agent framework's role references.

---

**Version:** 1.0
**Next Review:** After Phase C completion or when significant patterns emerge
