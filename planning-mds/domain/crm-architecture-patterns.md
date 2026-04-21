# Insurance CRM Architecture Patterns

Architectural patterns, module boundaries, and design considerations specific to insurance CRM systems. This guide helps architects design systems that align with industry best practices and avoid common pitfalls.

---

## Purpose

When architecting an insurance CRM, you need to understand:
- **Domain-Specific Patterns**: Broker hierarchies, submission workflows, renewal pipelines
- **Data Model Challenges**: Temporal data, hierarchical relationships, audit requirements
- **Module Boundaries**: How to decompose the system into cohesive, loosely-coupled modules
- **Anti-Patterns**: Common mistakes to avoid based on insurance industry experience

This document is the architectural counterpart to the Product Manager's competitive analysis and insurance glossary. Use it to design technical solutions that fit the insurance domain.

---

## 1. Insurance Domain Architecture Patterns

### 1.1 Broker/MGA Hierarchy Architecture

**Challenge:** Insurance brokers often have hierarchical relationships (MGA → sub-broker → producer), requiring recursive data structures and queries.

**Entity Design Pattern:**

```csharp
public class Broker : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string LicenseNumber { get; set; }

    // Self-referencing for hierarchy
    public Guid? ParentBrokerId { get; set; }
    public Broker? ParentBroker { get; set; }
    public ICollection<Broker> SubBrokers { get; set; } = new List<Broker>();

    // Hierarchy level (0 = MGA, 1 = sub-broker, 2 = producer)
    public int HierarchyLevel { get; set; }

    // Commission split percentage (if sub-broker)
    public decimal? CommissionSplitPercent { get; set; }
}
```

**Recursive Query Pattern (EF Core):**

```csharp
// Get entire broker tree starting from root
var brokerTree = await context.Brokers
    .Where(b => b.ParentBrokerId == null) // Root brokers (MGAs)
    .Include(b => b.SubBrokers)
        .ThenInclude(sb => sb.SubBrokers) // Recurse 2 levels deep
    .ToListAsync();

// Find all ancestors of a broker (bottom-up)
var ancestors = new List<Broker>();
var currentBroker = broker;
while (currentBroker.ParentBrokerId != null)
{
    currentBroker = await context.Brokers.FindAsync(currentBroker.ParentBrokerId);
    ancestors.Add(currentBroker);
}
```

**Database Optimization:**
- Index on `ParentBrokerId` for efficient hierarchy traversal
- Consider materialized path or nested set model for deep hierarchies (> 3 levels)
- Cache broker hierarchy in memory (changes infrequently)

**Authorization Considerations:**
- Sub-brokers can view only their own accounts + child brokers
- MGAs can view all sub-broker accounts
- Recursive authorization: Check user's broker + all parent brokers

---

### 1.2 Multi-Policy Account Architecture

**Challenge:** One insured (Account) can have multiple policies across different coverage types (GL, WC, Property, Auto).

**Entity Relationship Pattern:**

```
Account (1) → (many) Policies
  - One insured can have multiple policies
  - Policies can have different coverage types
  - Policies can have different effective/expiration dates
  - Policies can be with different carriers
```

**Data Model:**

```csharp
public class Account : BaseEntity
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; }
    public string Name { get; set; }
    public Guid BrokerId { get; set; }

    // Relationships
    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}

public class Policy : BaseEntity
{
    public Guid Id { get; set; }
    public string PolicyNumber { get; set; }
    public Guid AccountId { get; set; }
    public string CoverageType { get; set; } // GL, WC, Property, Auto
    public Date EffectiveDate { get; set; }
    public Date ExpirationDate { get; set; }
    public decimal Premium { get; set; }
    public string CarrierName { get; set; }

    // Renewal tracking
    public Guid? RenewalSubmissionId { get; set; }
    public Submission? RenewalSubmission { get; set; }
}
```

**Aggregation Pattern (Account Premium YTD):**

```csharp
// Calculate total premium for account across all active policies
var accountMetrics = await context.Accounts
    .Where(a => a.Id == accountId)
    .Select(a => new AccountMetrics
    {
        TotalPremiumYTD = a.Policies
            .Where(p => p.Status == "Active" && p.EffectiveDate.Year == DateTime.UtcNow.Year)
            .Sum(p => p.Premium),
        ActivePoliciesCount = a.Policies.Count(p => p.Status == "Active"),
        CoverageTypes = a.Policies.Select(p => p.CoverageType).Distinct().ToList()
    })
    .FirstOrDefaultAsync();
```

**Performance Considerations:**
- Use indexed computed columns for frequently-accessed aggregations
- Consider denormalization: Store `TotalPremiumYTD` on Account table, update via triggers or app logic
- Use read-side projections (CQRS pattern) for complex account summaries

---

### 1.3 Submission Workflow Architecture

**Challenge:** Submission workflow has complex state transitions with validation gates, authorization rules, and side effects.

**State Machine Pattern:**

```csharp
public class SubmissionWorkflow
{
    private readonly Dictionary<string, List<string>> _allowedTransitions = new()
    {
        ["Received"] = new() { "Triaging" },
        ["Triaging"] = new() { "WaitingOnBroker", "ReadyForUWReview", "Declined", "Withdrawn" },
        ["WaitingOnBroker"] = new() { "ReadyForUWReview", "Declined", "Withdrawn" },
        ["ReadyForUWReview"] = new() { "InReview", "WaitingOnBroker", "Declined", "Withdrawn" },
        ["InReview"] = new() { "Quoted", "WaitingOnBroker", "Declined", "Withdrawn" },
        ["Quoted"] = new() { "BindRequested", "Declined", "Withdrawn" },
        ["BindRequested"] = new() { "Bound", "Declined" },
        ["Bound"] = new(), // Terminal
        ["Declined"] = new(), // Terminal
        ["Withdrawn"] = new() // Terminal
    };

    public bool IsValidTransition(string fromStatus, string toStatus)
    {
        return _allowedTransitions.TryGetValue(fromStatus, out var allowed)
            && allowed.Contains(toStatus);
    }

    public async Task<Result> TransitionAsync(Submission submission, string toStatus, TransitionContext context)
    {
        // 1. Validate transition is allowed
        if (!IsValidTransition(submission.Status, toStatus))
            return Result.Failure("Invalid status transition");

        // 2. Check prerequisites
        var validation = await ValidatePrerequisites(submission, toStatus);
        if (!validation.IsValid)
            return Result.Failure("Prerequisites not met", validation.Errors);

        // 3. Check authorization
        var authorized = await _authService.CanTransition(context.User, submission, toStatus);
        if (!authorized)
            return Result.Failure("User not authorized");

        // 4. Execute transition
        var oldStatus = submission.Status;
        submission.Status = toStatus;
        submission.UpdatedAt = DateTime.UtcNow;
        submission.UpdatedBy = context.UserId;

        // 5. Record workflow transition (immutable audit)
        var transition = new WorkflowTransition
        {
            Id = Guid.NewGuid(),
            WorkflowType = "Submission",
            EntityId = submission.Id,
            FromState = oldStatus,
            ToState = toStatus,
            OccurredAt = DateTime.UtcNow,
            ActorUserId = context.UserId,
            Reason = context.Reason
        };
        await _context.WorkflowTransitions.AddAsync(transition);

        // 6. Execute side effects
        await ExecuteSideEffects(submission, oldStatus, toStatus, context);

        await _context.SaveChangesAsync();
        return Result.Success();
    }
}
```

**Integration with Temporal (Workflow Engine):**
- Use Temporal for long-running workflows (renewal reminders, escalations)
- Use EF Core state machine for immediate transitions (user-initiated)
- Temporal workflows can trigger state transitions via API calls
- Store Temporal workflow ID on Submission for correlation

---

### 1.4 Renewal Pipeline Architecture

**Challenge:** Renewals require scheduled reminders (60/90/120 days before expiration) and automated workflow triggers.

**Temporal Workflow Pattern:**

```csharp
[Workflow]
public class RenewalWorkflow
{
    [WorkflowRun]
    public async Task RunAsync(RenewalWorkflowInput input)
    {
        var policy = input.Policy;

        // Schedule reminder 120 days before expiration
        await Workflow.DelayAsync(policy.ExpirationDate.AddDays(-120) - DateTime.UtcNow);
        await Workflow.ExecuteActivityAsync<SendRenewalReminderActivity>(
            new ReminderInput { PolicyId = policy.Id, DaysOut = 120 });

        // Schedule reminder 90 days before expiration
        await Workflow.DelayAsync(TimeSpan.FromDays(30));
        await Workflow.ExecuteActivityAsync<SendRenewalReminderActivity>(
            new ReminderInput { PolicyId = policy.Id, DaysOut = 90 });

        // Schedule reminder 60 days before expiration
        await Workflow.DelayAsync(TimeSpan.FromDays(30));
        await Workflow.ExecuteActivityAsync<SendRenewalReminderActivity>(
            new ReminderInput { PolicyId = policy.Id, DaysOut = 60 });

        // Check if renewal submission created
        var renewalCreated = await Workflow.ExecuteActivityAsync<CheckRenewalSubmissionActivity>(policy.Id);

        if (!renewalCreated)
        {
            // Escalate to manager if no renewal 30 days before expiration
            await Workflow.DelayAsync(TimeSpan.FromDays(30));
            await Workflow.ExecuteActivityAsync<EscalateRenewalActivity>(policy.Id);
        }
    }
}
```

**Database Design:**
- `Renewals` table tracks renewal status for each expiring policy
- Link Policy → RenewalSubmission (foreign key)
- Temporal stores workflow execution state (reminder schedule, escalations)

---

### 1.5 Commission Tracking Architecture

**Challenge:** Commission calculations involve splits (MGA, sub-broker, producer), carrier rates, and payment tracking.

**Data Model:**

```csharp
public class CommissionSchedule : BaseEntity
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public string CarrierName { get; set; }
    public string CoverageType { get; set; }
    public decimal CommissionRate { get; set; } // Percentage (e.g., 15.00 = 15%)
}

public class CommissionSplit : BaseEntity
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public Guid BrokerId { get; set; }
    public decimal SplitPercentage { get; set; } // Percentage of total commission
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime? PaidAt { get; set; }
}
```

**Calculation Pattern:**

```csharp
public async Task<IEnumerable<CommissionSplit>> CalculateCommissions(Policy policy)
{
    var splits = new List<CommissionSplit>();

    // 1. Get commission rate for carrier + coverage type
    var schedule = await _context.CommissionSchedules
        .FirstAsync(cs => cs.BrokerId == policy.BrokerId
            && cs.CarrierName == policy.CarrierName
            && cs.CoverageType == policy.CoverageType);

    var totalCommission = policy.Premium * (schedule.CommissionRate / 100);

    // 2. Get broker hierarchy for splits
    var broker = await _context.Brokers
        .Include(b => b.ParentBroker)
        .FirstAsync(b => b.Id == policy.BrokerId);

    // 3. Split commission across hierarchy
    splits.Add(new CommissionSplit
    {
        BrokerId = broker.Id,
        SplitPercentage = broker.CommissionSplitPercent ?? 100m,
        AmountDue = totalCommission * ((broker.CommissionSplitPercent ?? 100m) / 100)
    });

    if (broker.ParentBrokerId != null)
    {
        var parentSplit = 100m - (broker.CommissionSplitPercent ?? 100m);
        splits.Add(new CommissionSplit
        {
            BrokerId = broker.ParentBrokerId.Value,
            SplitPercentage = parentSplit,
            AmountDue = totalCommission * (parentSplit / 100)
        });
    }

    return splits;
}
```

---

### 1.6 Document Management Architecture

**Challenge:** Insurance requires document storage (ACORD forms, loss runs, financial statements) with metadata, versioning, and access control.

**Blob Storage Pattern:**

```csharp
public class SubmissionDocument : BaseEntity
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; } // application/pdf, image/png
    public long FileSize { get; set; }
    public string BlobStorageKey { get; set; } // S3 key or Azure Blob path
    public string DocumentType { get; set; } // ACORD125, LossRuns, FinancialStatement
    public int Version { get; set; } // Document version (for replacements)
    public bool IsLatestVersion { get; set; }
    public DateTime UploadedAt { get; set; }
    public Guid UploadedBy { get; set; }
}
```

**Storage Architecture:**
- Application stores metadata in PostgreSQL
- Actual files stored in blob storage (AWS S3, Azure Blob, MinIO)
- Blob keys organized by entity: `submissions/{submissionId}/documents/{documentId}.pdf`
- Presigned URLs for secure temporary access (no direct public access)

**Versioning Pattern:**
- When document replaced, mark old version `IsLatestVersion = false`
- Create new document record with `Version++`
- Both versions retained in blob storage for audit
- API returns only latest version by default

---

## 2. CRM Module Boundaries

### 2.1 Module Decomposition Principles

**Modular Monolith Approach:**
- Logical modules within single deployable
- Clear interfaces between modules
- Shared database with table prefixes or schemas
- Future microservices extraction possible

**Module Identification:**
- High cohesion: Related functionality grouped together
- Low coupling: Minimal dependencies between modules
- Business alignment: Modules match domain concepts

---

### 2.2 Broker Module

**Responsibilities:**
- Broker and MGA relationship management
- Broker hierarchy (parent/child relationships)
- Broker contacts (people at brokerage)
- Broker licensing and status

**Entities:**
- Broker
- Contact
- BrokerHierarchy (or self-referencing on Broker)

**API Endpoints:**
- `/brokers` (CRUD)
- `/brokers/{id}/contacts` (list contacts for broker)
- `/brokers/{id}/hierarchy` (get broker tree)

**Dependencies:**
- Identity Module (created/updated by users)
- Timeline Module (audit events)

---

### 2.3 Account Module

**Responsibilities:**
- Insured account management
- Account contacts (people at insured company)
- Account classification (industry, size, risk profile)

**Entities:**
- Account
- AccountContact
- AccountClassification

**API Endpoints:**
- `/accounts` (CRUD)
- `/accounts/{id}/360` (360-degree view with policies, submissions)

**Dependencies:**
- Broker Module (account belongs to broker)
- Policy Module (account has policies)
- Submission Module (account has submissions)

---

### 2.4 Submission Module

**Responsibilities:**
- Submission intake and workflow
- Underwriter assignment
- Document management (ACORD forms, loss runs)
- Quoting process

**Entities:**
- Submission
- SubmissionDocument
- Quote

**API Endpoints:**
- `/submissions` (CRUD)
- `/submissions/{id}/transition` (workflow transitions)
- `/submissions/{id}/documents` (upload/download documents)

**Dependencies:**
- Broker Module (submission from broker)
- Account Module (submission for account)
- Workflow Engine (Temporal integration)
- Timeline Module (workflow transitions logged)

---

### 2.5 Renewal Module

**Responsibilities:**
- Renewal pipeline management
- Renewal reminders and escalations
- Renewal status tracking

**Entities:**
- Renewal
- RenewalReminder

**API Endpoints:**
- `/renewals` (list upcoming renewals)
- `/renewals/{id}` (renewal details)

**Dependencies:**
- Policy Module (renewals for expiring policies)
- Submission Module (renewal creates new submission)
- Workflow Engine (Temporal for scheduled reminders)

---

### 2.6 Timeline Module (Shared)

**Responsibilities:**
- Audit trail for all entities
- Activity timeline (user actions, system events)
- Workflow transition history

**Entities:**
- ActivityTimelineEvent
- WorkflowTransition

**API Endpoints:**
- `/timeline?entityType=Broker&entityId={id}` (query timeline for entity)

**Design Pattern:**
- Append-only tables (no updates or deletes)
- No foreign key constraints (timeline records retained even if entity deleted)
- JSONB payload for flexible event data

**Dependencies:**
- None (all modules depend on Timeline, but Timeline depends on nothing)

---

### 2.7 Identity Module

**Responsibilities:**
- User profiles (Keycloak user → Nebula user mapping)
- User preferences (theme, notifications)
- Role management (Distribution, Underwriter, Admin)

**Entities:**
- UserProfile
- UserPreference

**API Endpoints:**
- `/users/me` (current user profile)
- `/users/{id}/preferences` (user preferences)

**Dependencies:**
- Keycloak (external auth provider)

---

## 3. Common Data Model Challenges

### 3.1 Hierarchical Data

**Challenge:** Broker hierarchies, account hierarchies (parent company → subsidiaries), organizational structures.

**Solutions:**
- **Self-Referencing FK**: Simple, works for shallow hierarchies (< 3 levels)
- **Materialized Path**: Store full path as string (e.g., "/MGA-123/Broker-456/Producer-789"), efficient queries
- **Nested Sets**: Store left/right boundaries, complex updates but efficient queries
- **Closure Table**: Separate table storing all ancestor/descendant pairs, flexible but more storage

**Recommendation:** Start with self-referencing FK, add materialized path if deep hierarchies emerge.

---

### 3.2 Temporal Data

**Challenge:** Policies have effective/expiration dates, renewals are time-based, audit logs are timestamped.

**Solutions:**
- **Date Range Queries**: Index on EffectiveDate, ExpirationDate for "active as of date" queries
- **Temporal Tables**: System-versioned tables (PostgreSQL 16+ or triggers)
- **Event Sourcing**: Store all state changes as events, rebuild current state

**Common Queries:**
```sql
-- Policies active as of specific date
SELECT * FROM Policies
WHERE EffectiveDate <= '2026-01-31' AND ExpirationDate > '2026-01-31';

-- Policies expiring in next 90 days
SELECT * FROM Policies
WHERE ExpirationDate BETWEEN NOW() AND NOW() + INTERVAL '90 days';
```

**Indexes:**
```sql
CREATE INDEX IX_Policies_EffectiveDate ON Policies(EffectiveDate);
CREATE INDEX IX_Policies_ExpirationDate ON Policies(ExpirationDate);
CREATE INDEX IX_Policies_DateRange ON Policies(EffectiveDate, ExpirationDate);
```

---

### 3.3 Audit Requirements

**Challenge:** Regulatory compliance requires immutable audit trails (who, what, when, why).

**Solutions:**
- **Audit Fields on Entities**: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
- **Timeline Events**: Append-only table with JSONB payload for all mutations
- **Workflow Transitions**: Separate table for state changes (FromStatus, ToStatus, Reason)
- **Database Triggers**: Automatically log changes (fallback if app logic fails)

**Design Principles:**
- Timeline tables are append-only (no UPDATE or DELETE)
- Timeline records never deleted, even if entity deleted
- Store before/after state in JSONB for full audit
- Use UTC timestamps always

---

### 3.4 Document Relationships

**Challenge:** Submissions have many documents (ACORD forms, loss runs), documents can be versioned, large files.

**Solutions:**
- **Metadata in Database**: Store document metadata (filename, size, type) in PostgreSQL
- **Content in Blob Storage**: Store actual files in S3/Azure Blob
- **Versioning**: Mark old versions as `IsLatestVersion = false`, keep all versions
- **Access Control**: Generate presigned URLs for temporary access

**Anti-Pattern:** Storing documents as BYTEA in PostgreSQL (bloats database, slow backups)

---

### 3.5 Reference Data

**Challenge:** Coverage types, states, carriers, programs are relatively static lookup data.

**Solutions:**
- **Database Tables**: `States`, `CoverageTypes`, `Carriers` with seed data
- **Deterministic Seeding**: Migrations create reference data with known IDs
- **Caching**: Cache reference data in memory (MemoryCache or Redis)
- **Change Tracking**: Reference data changes rare, but audit when they do

**Pattern:**
```csharp
// Seed states with deterministic IDs
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.Entity<State>().HasData(
        new State { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Code = "CA", Name = "California" },
        new State { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Code = "NY", Name = "New York" },
        // ... all 50 states
    );
}
```

---

### 3.6 Role-Based Visibility

**Challenge:** Some data is internal-only (InternalNotes), other data visible to brokers (BrokerVisible).

**Solutions:**
- **Field-Level Attribute**: Mark fields with `[InternalOnly]` attribute
- **Separate DTOs**: BrokerView vs InternalView response models
- **Authorization Policies**: Check user role before including internal fields
- **Database Views**: Create views that filter internal fields (for reporting)

**Pattern:**
```csharp
public class BrokerResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    [InternalOnly] // Custom attribute
    public string InternalNotes { get; set; }

    [InternalOnly]
    public decimal CommissionRate { get; set; }
}

// Mapper filters based on user role
public BrokerResponse MapToBrokerResponse(Broker broker, User user)
{
    var response = _mapper.Map<BrokerResponse>(broker);

    if (!user.IsInternalUser())
    {
        // Strip internal-only fields for external users
        response.InternalNotes = null;
        response.CommissionRate = 0;
    }

    return response;
}
```

---

## 4. Architectural Anti-Patterns to Avoid

### 4.1 Mutable Audit Trails

**Anti-Pattern:** Allowing UPDATE or DELETE on timeline/audit tables.

**Why It's Bad:**
- Violates regulatory compliance (audit logs must be immutable)
- Loses historical state for troubleshooting
- Can't prove data integrity

**Correct Pattern:**
- Timeline tables are append-only (INSERT only)
- Use database constraints to prevent updates: `CREATE TRIGGER prevent_timeline_update ...`
- If correction needed, insert compensating event (don't update original)

---

### 4.2 Missing Authorization Checks

**Anti-Pattern:** Assuming authorization is frontend-only, or checking auth in some endpoints but not all.

**Why It's Bad:**
- Security vulnerability (API can be called directly, bypassing UI)
- Inconsistent behavior
- Regulatory risk (unauthorized data access)

**Correct Pattern:**
- Middleware enforces authorization on every API request
- No endpoint bypasses authorization (including health checks if they expose data)
- Log all authorization decisions (approved and denied)

---

### 4.3 N+1 Query Problems

**Anti-Pattern:** Loading related entities in loops instead of eager loading.

**Why It's Bad:**
- 1 query to get brokers + N queries to get contacts for each broker = N+1 queries
- Slow performance, database overload
- Doesn't scale

**Correct Pattern:**
```csharp
// BAD
var brokers = await context.Brokers.ToListAsync();
foreach (var broker in brokers)
{
    broker.Contacts = await context.Contacts.Where(c => c.BrokerId == broker.Id).ToListAsync();
}

// GOOD
var brokers = await context.Brokers
    .Include(b => b.Contacts)
    .ToListAsync();
```

---

### 4.4 God Entities

**Anti-Pattern:** Broker entity with 50+ fields including commission rates, tax info, preferences, statistics, etc.

**Why It's Bad:**
- Violates Single Responsibility Principle
- Hard to maintain, test, understand
- Loads unnecessary data (performance impact)

**Correct Pattern:**
- Split into multiple entities: Broker (core), BrokerPreferences, BrokerStatistics
- Use projections to load only needed fields
- Use related tables for optional data

---

### 4.5 Missing Indexes on Foreign Keys

**Anti-Pattern:** Defining foreign keys without indexes.

**Why It's Bad:**
- Queries like `SELECT * FROM Submissions WHERE BrokerId = ?` do full table scans
- Slow performance as data grows
- Joins are slow

**Correct Pattern:**
- Always create index on foreign key columns
- EF Core doesn't auto-create FK indexes (must be explicit)
```csharp
builder.Entity<Submission>()
    .HasIndex(s => s.BrokerId)
    .HasDatabaseName("IX_Submissions_BrokerId");
```

---

### 4.6 Ignoring Soft Delete Patterns

**Anti-Pattern:** Hard deleting records from database.

**Why It's Bad:**
- Loses audit trail
- Breaks foreign key relationships
- Can't undo accidental deletes

**Correct Pattern:**
- Add `DeletedAt` DateTime? field
- Global query filter: `WHERE DeletedAt IS NULL`
- DELETE operation sets DeletedAt timestamp
- Separate "Restore" operation clears DeletedAt

---

### 4.7 Hard-Coding Business Rules in Database

**Anti-Pattern:** Using database triggers or stored procedures for complex business logic.

**Why It's Bad:**
- Business logic scattered across application code and database
- Hard to test (can't unit test triggers)
- Hard to version control and deploy
- Difficult to debug

**Correct Pattern:**
- Business logic in application layer (C# code)
- Database enforces data integrity only (FK constraints, NOT NULL)
- Use database for what it's good at (data storage, querying, transactions)

---

### 4.8 Missing State Transition Validation

**Anti-Pattern:** Allowing any status change without validation (e.g., Bound → Triaging).

**Why It's Bad:**
- Invalid data states
- Breaks business rules
- Audit trail inconsistencies

**Correct Pattern:**
- Explicit state machine defining allowed transitions
- Validation logic checks prerequisites before transition
- Return 409 Conflict for invalid transitions
- Log all transition attempts (successful and failed)

---

## Version History

**Version 1.0** - 2026-01-31 - Initial insurance CRM architecture patterns guide
