# Architecture Examples

Real-world examples from Nebula showing complete architectural specifications. These examples demonstrate best practices for entity design, API contracts, workflows, authorization, and architectural decisions.

---

## Example 1: Complete Broker Module Specification

This example shows a complete module specification including entity design, API contracts, authorization policies, and audit requirements.

### Broker Entity Specification

**Table Name:** `Brokers`

**Description:** Represents an insurance broker or brokerage firm that submits business to carriers.

#### Fields

| Field | Type | Constraints | Default | Description |
|-------|------|-------------|---------|-------------|
| Id | Guid | PK, NOT NULL | NewGuid() | Unique identifier |
| Name | string(255) | NOT NULL, INDEX | - | Broker legal name (DBA or registered name) |
| LicenseNumber | string(50) | NOT NULL, UNIQUE | - | State-issued broker license number |
| State | string(2) | NOT NULL, FK → States | - | Licensed state (US state code: CA, NY, TX, etc.) |
| TaxId | string(20) | NULL | - | Federal Tax ID (EIN) - encrypted at rest |
| Email | string(255) | NULL | - | Primary contact email |
| Phone | string(20) | NULL | - | Primary contact phone (format: +1-555-555-5555) |
| Address | string(500) | NULL | - | Physical mailing address |
| Website | string(255) | NULL | - | Broker website URL |
| ParentBrokerId | Guid | NULL, FK → Brokers | - | For broker hierarchies (MGA → sub-broker) |
| Status | string(20) | NOT NULL | 'Active' | Active, Inactive, Suspended |
| InternalNotes | string(2000) | NULL | - | Internal-only notes (not visible to brokers) |
| CreatedAt | DateTime | NOT NULL | UtcNow | UTC timestamp of creation |
| CreatedBy | Guid | NOT NULL, FK → Users | - | User who created the record |
| UpdatedAt | DateTime | NOT NULL | UtcNow | UTC timestamp of last update |
| UpdatedBy | Guid | NOT NULL, FK → Users | - | User who last updated the record |
| DeletedAt | DateTime | NULL | - | Soft delete timestamp (NULL = active) |

#### Relationships

- **One-to-Many:** Broker → Contacts (one broker has many contact persons)
  - Cascade: Soft delete (when broker soft-deleted, contacts also soft-deleted)
- **One-to-Many:** Broker → Submissions (one broker submits many submissions)
  - Cascade: Restrict (cannot delete broker with active submissions)
- **One-to-Many:** Broker → Accounts (one broker manages many accounts/insureds)
  - Cascade: Restrict (cannot delete broker with active accounts)
- **Self-Referencing:** Broker → ParentBroker (for MGA/sub-broker hierarchies)
  - Cascade: Set NULL (if parent deleted, child becomes top-level broker)
- **Many-to-One:** Broker → States (reference data)

#### Indexes

- `PK_Brokers_Id` (PRIMARY KEY, clustered)
- `IX_Brokers_LicenseNumber` (UNIQUE, non-clustered)
- `IX_Brokers_Name` (non-clustered, for search/filter)
- `IX_Brokers_State` (non-clustered, for filtering by state)
- `IX_Brokers_Status` (non-clustered, for filtering active/inactive)
- `IX_Brokers_ParentBrokerId` (non-clustered, for hierarchy queries)
- `IX_Brokers_DeletedAt` (partial index WHERE DeletedAt IS NULL, for active records)

#### Audit Requirements

All mutations (Create, Update, Soft Delete) must generate `ActivityTimelineEvent`:

- **BrokerCreated**: When new broker added
- **BrokerUpdated**: When any field modified (include changed fields in payload)
- **BrokerDeleted**: When soft-deleted (sets DeletedAt timestamp)
- **BrokerRestored**: When un-deleted (clears DeletedAt)

Timeline events include:
```json
{
  "entityType": "Broker",
  "entityId": "guid-here",
  "eventType": "BrokerCreated",
  "userId": "user-guid",
  "timestamp": "2026-01-31T10:30:00Z",
  "payload": {
    "name": "Acme Insurance Brokers",
    "licenseNumber": "CA-12345",
    "state": "CA"
  }
}
```

#### Seed Data Strategy

- **No broker seed data** (all broker records are user-created)
- **Reference data seeded**: `States` table populated with 50 US states + DC
- **Test data**: Faker-generated brokers for development/testing environments only

---

### Broker API Contracts

Complete OpenAPI specification for all Broker CRUD operations.

```yaml
openapi: 3.0.0
info:
  title: Broker Management API
  version: 1.0.0
  description: API for managing insurance brokers and brokerage firms

servers:
  - url: https://api.nebula.example.com
    description: Production API
  - url: http://localhost:5000
    description: Development API

security:
  - bearerAuth: []

paths:
  /brokers:
    post:
      summary: Create a new broker
      description: Creates a new broker record. Requires CreateBroker permission.
      operationId: createBroker
      tags: [Brokers]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [name, licenseNumber, state]
              properties:
                name:
                  type: string
                  maxLength: 255
                  description: Legal name of the broker or brokerage firm
                  example: "Acme Insurance Brokers"
                licenseNumber:
                  type: string
                  maxLength: 50
                  description: State-issued broker license number
                  example: "CA-12345"
                state:
                  type: string
                  pattern: "^[A-Z]{2}$"
                  description: Two-letter US state code
                  example: "CA"
                taxId:
                  type: string
                  maxLength: 20
                  nullable: true
                  description: Federal Tax ID (EIN)
                  example: "12-3456789"
                email:
                  type: string
                  format: email
                  maxLength: 255
                  nullable: true
                phone:
                  type: string
                  maxLength: 20
                  nullable: true
                address:
                  type: string
                  maxLength: 500
                  nullable: true
                website:
                  type: string
                  format: uri
                  maxLength: 255
                  nullable: true
                parentBrokerId:
                  type: string
                  format: uuid
                  nullable: true
                  description: Parent broker ID for sub-broker relationships
                internalNotes:
                  type: string
                  maxLength: 2000
                  nullable: true
                  description: Internal-only notes (not visible to brokers)
      responses:
        '201':
          description: Broker created successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/BrokerResponse'
        '400':
          description: Validation error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              examples:
                validationError:
                  value:
                    type: "https://api.nebula.example/errors/validation"
                    title: "Validation failed"
                    status: 400
                    code: "validation_error"
                    detail: "Invalid request data"
                    errors:
                      - field: "licenseNumber"
                        message: "License number is required"
        '403':
          description: Forbidden - user lacks CreateBroker permission
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
        '409':
          description: Conflict - duplicate license number
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              examples:
                duplicateLicense:
                  value:
                    type: "https://api.nebula.example/errors/duplicate-license"
                    title: "Duplicate broker license"
                    status: 409
                    code: "duplicate_license"
                    detail: "A broker with this license number already exists"
                    errors:
                      - field: "licenseNumber"
                        message: "License number CA-12345 is already in use"

    get:
      summary: List all brokers with pagination and filtering
      description: Returns paginated list of brokers. Requires ReadBroker permission.
      operationId: listBrokers
      tags: [Brokers]
      parameters:
        - name: page
          in: query
          schema:
            type: integer
            minimum: 1
            default: 1
        - name: pageSize
          in: query
          schema:
            type: integer
            minimum: 1
            maximum: 100
            default: 20
        - name: status
          in: query
          schema:
            type: string
            enum: [Active, Inactive, Suspended]
        - name: state
          in: query
          schema:
            type: string
            pattern: "^[A-Z]{2}$"
        - name: search
          in: query
          description: Search by broker name or license number
          schema:
            type: string
        - name: sort
          in: query
          description: Sort field and direction (e.g., name:asc, createdAt:desc)
          schema:
            type: string
            default: "name:asc"
      responses:
        '200':
          description: List of brokers
          content:
            application/json:
              schema:
                type: object
                properties:
                  data:
                    type: array
                    items:
                      $ref: '#/components/schemas/BrokerListItem'
                  page:
                    type: integer
                    example: 1
                  pageSize:
                    type: integer
                    example: 20
                  totalCount:
                    type: integer
                    example: 156
                  totalPages:
                    type: integer
                    example: 8

  /brokers/{id}:
    get:
      summary: Get broker by ID
      description: Returns detailed broker information. Requires ReadBroker permission.
      operationId: getBroker
      tags: [Brokers]
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '200':
          description: Broker details
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/BrokerResponse'
        '404':
          description: Broker not found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'

    put:
      summary: Update broker
      description: Updates broker information. Requires UpdateBroker permission.
      operationId: updateBroker
      tags: [Brokers]
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/BrokerUpdateRequest'
      responses:
        '200':
          description: Broker updated successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/BrokerResponse'
        '400':
          description: Validation error
        '403':
          description: Forbidden
        '404':
          description: Broker not found
        '409':
          description: Conflict (e.g., duplicate license number)

    delete:
      summary: Soft delete broker
      description: Soft deletes broker (sets DeletedAt timestamp). Requires DeleteBroker permission.
      operationId: deleteBroker
      tags: [Brokers]
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '204':
          description: Broker deleted successfully
        '403':
          description: Forbidden
        '404':
          description: Broker not found
        '409':
          description: Conflict - cannot delete broker with active submissions/accounts

components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT

  schemas:
    BrokerResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
        name:
          type: string
        licenseNumber:
          type: string
        state:
          type: string
        email:
          type: string
          nullable: true
        phone:
          type: string
          nullable: true
        address:
          type: string
          nullable: true
        website:
          type: string
          nullable: true
        parentBrokerId:
          type: string
          format: uuid
          nullable: true
        status:
          type: string
          enum: [Active, Inactive, Suspended]
        internalNotes:
          type: string
          nullable: true
          description: Only visible to internal users (not brokers)
        createdAt:
          type: string
          format: date-time
        updatedAt:
          type: string
          format: date-time

    BrokerListItem:
      type: object
      properties:
        id:
          type: string
          format: uuid
        name:
          type: string
        licenseNumber:
          type: string
        state:
          type: string
        status:
          type: string
          enum: [Active, Inactive, Suspended]

    BrokerUpdateRequest:
      type: object
      properties:
        name:
          type: string
          maxLength: 255
        email:
          type: string
          format: email
        phone:
          type: string
        address:
          type: string
        website:
          type: string
        status:
          type: string
          enum: [Active, Inactive, Suspended]
        internalNotes:
          type: string

    ProblemDetails:
      type: object
      required: [type, title, status, code]
      properties:
        type:
          type: string
          format: uri
          description: URI identifier for the error category
        title:
          type: string
          description: Short, human-readable error summary
        status:
          type: integer
          description: HTTP status code for this error
        code:
          type: string
          description: Machine-readable error code
        traceId:
          type: string
          description: Correlation identifier for diagnostics
        detail:
          type: string
          description: Optional detailed explanation
        errors:
          type: array
          items:
            type: object
            properties:
              field:
                type: string
              message:
                type: string
```

---

### Broker Authorization Policies

Casbin ABAC policies for Broker module.

```csv
# Distribution users can create, read, update brokers
p, Distribution, Broker, Create, allow
p, Distribution, Broker, Read, allow
p, Distribution, Broker, Update, allow

# Distribution users can soft-delete brokers (but not hard delete)
p, Distribution, Broker, Delete, allow

# Underwriters can read brokers but not modify
p, Underwriter, Broker, Read, allow

# Admins can do everything with brokers
p, Admin, Broker, *, allow

# System administrators can restore soft-deleted brokers
p, Admin, Broker, Restore, allow
```

**Authorization Enforcement:**
- All API endpoints check Casbin policies before executing operations
- Subject attributes extracted from JWT token (roles claim)
- Resource type = "Broker"
- Action = Create, Read, Update, Delete

---

## Example 2: Complete Submission Workflow Specification

This example shows a complete workflow with state machine, transitions, validation, authorization, and error handling.

### Submission Entity

**Table Name:** `Submissions`

#### Key Fields

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique identifier |
| SubmissionNumber | string(50) | UNIQUE, NOT NULL | Human-readable ID (e.g., SUB-2026-001234) |
| BrokerId | Guid | FK → Brokers, NOT NULL | Broker who submitted |
| AccountId | Guid | FK → Accounts, NULL | Linked account/insured |
| InsuredName | string(255) | NOT NULL | Name of insured entity |
| CoverageType | string(50) | NOT NULL | GL, WC, Property, Auto, etc. |
| ProgramId | Guid | FK → Programs, NULL | Insurance program |
| EffectiveDate | Date | NOT NULL | Requested policy effective date |
| ExpirationDate | Date | NOT NULL | Requested policy expiration date |
| EstimatedPremium | decimal(18,2) | NULL | Broker's estimated premium |
| Status | string(50) | NOT NULL | Current workflow status |
| AssignedUnderwriterId | Guid | FK → Users, NULL | Underwriter assigned |
| CreatedAt | DateTime | NOT NULL | Submission received timestamp |
| CreatedBy | Guid | FK → Users, NOT NULL | User who created |
| UpdatedAt | DateTime | NOT NULL | Last update timestamp |
| UpdatedBy | Guid | FK → Users, NOT NULL | User who last updated |

### Submission Workflow State Machine

**States:**
1. **Received** - Initial state when submission created
2. **Triaging** - Distribution team reviewing for completeness
3. **WaitingOnBroker** - Missing information, awaiting broker response
4. **ReadyForUWReview** - Complete, ready for underwriter assignment
5. **InReview** - Underwriter actively reviewing
6. **Quoted** - Quote generated, awaiting broker acceptance
7. **BindRequested** - Broker requested to bind policy
8. **Bound** - Policy issued (terminal state)
9. **Declined** - Submission declined by underwriter (terminal state)
10. **Withdrawn** - Broker withdrew submission (terminal state)

**Allowed Transitions:**

| From State | To State(s) | Prerequisites | Authorization |
|------------|-------------|---------------|---------------|
| Received | Triaging | None (automatic) | System |
| Triaging | WaitingOnBroker | Missing info documented | Distribution, Admin |
| Triaging | ReadyForUWReview | All required fields populated | Distribution, Admin |
| Triaging | Declined | Decline reason | Distribution, Admin |
| Triaging | Withdrawn | Withdrawal reason | Distribution, Admin |
| WaitingOnBroker | ReadyForUWReview | Missing info received | Distribution, Admin |
| WaitingOnBroker | Declined | Decline reason | Distribution, Admin |
| WaitingOnBroker | Withdrawn | Withdrawal reason | Distribution, Admin |
| ReadyForUWReview | InReview | Underwriter assigned | Underwriter, Admin |
| ReadyForUWReview | WaitingOnBroker | Additional info needed | Underwriter, Admin |
| ReadyForUWReview | Declined | Decline reason | Underwriter, Admin |
| ReadyForUWReview | Withdrawn | Withdrawal reason | Distribution, Admin |
| InReview | Quoted | Quote generated | Underwriter, Admin |
| InReview | WaitingOnBroker | Additional info needed | Underwriter, Admin |
| InReview | Declined | Decline reason | Underwriter, Admin |
| InReview | Withdrawn | Withdrawal reason | Distribution, Admin |
| Quoted | BindRequested | Broker acceptance + payment info | Distribution, Admin |
| Quoted | Declined | Insured declined quote | Distribution, Admin |
| Quoted | Withdrawn | Withdrawal reason | Distribution, Admin |
| BindRequested | Bound | Payment confirmed, policy issued | Underwriter, Admin |
| BindRequested | Declined | Decline reason (rare) | Underwriter, Admin |

**Terminal States (No transitions allowed):**
- Bound
- Declined
- Withdrawn

### Transition Validation Rules

#### Triaging → ReadyForUWReview

**Required Fields:**
- InsuredName (not empty)
- CoverageType (valid enum value)
- ProgramId (must exist in Programs table)
- BrokerId (must exist and be Active status)
- EffectiveDate (must be future date)
- ExpirationDate (must be after EffectiveDate)

**Business Rules:**
- Effective date cannot be more than 90 days in the past
- Expiration date must be 1 year from effective date (typical policy term)
- Program must be active and accepting new submissions

**Authorization:**
- User must have "TransitionSubmission" permission
- User role must be Distribution or Admin

**Side Effects:**
- WorkflowTransition event created (immutable)
- ActivityTimelineEvent logged (SubmissionReadyForReview)
- Email notification sent to underwriting team
- Submission appears in underwriter queue

**Error Response (Missing Fields):**
```json
{
  "code": "MISSING_REQUIRED_FIELDS",
  "message": "Cannot transition to ReadyForUWReview. Missing required fields.",
  "details": [
    {
      "field": "program",
      "message": "Program must be selected"
    },
    {
      "field": "coverageType",
      "message": "Coverage type is required"
    }
  ]
}
```

**Error Response (Invalid Transition):**
```json
{
  "code": "INVALID_TRANSITION",
  "message": "Cannot transition from Bound to ReadyForUWReview",
  "details": {
    "currentStatus": "Bound",
    "attemptedStatus": "ReadyForUWReview",
    "allowedStatuses": []
  }
}
```

### Submission API Workflow Endpoint

```yaml
/submissions/{id}/transition:
  post:
    summary: Transition submission to new status
    operationId: transitionSubmission
    tags: [Submissions]
    parameters:
      - name: id
        in: path
        required: true
        schema:
          type: string
          format: uuid
    requestBody:
      required: true
      content:
        application/json:
          schema:
            type: object
            required: [toStatus]
            properties:
              toStatus:
                type: string
                enum: [Triaging, WaitingOnBroker, ReadyForUWReview, InReview, Quoted, BindRequested, Bound, Declined, Withdrawn]
              reason:
                type: string
                description: Required for Declined/Withdrawn transitions
              assignedUnderwriterId:
                type: string
                format: uuid
                description: Required for ReadyForUWReview → InReview
    responses:
      '200':
        description: Transition successful
        content:
          application/json:
            schema:
              type: object
              properties:
                id:
                  type: string
                  format: uuid
                status:
                  type: string
                transitionedAt:
                  type: string
                  format: date-time
      '400':
        description: Missing required fields or invalid transition
      '403':
        description: User lacks TransitionSubmission permission
      '409':
        description: Invalid state transition
```

### Submission Authorization Policies

```csv
# Distribution users can create and transition submissions (Triaging → ReadyForUWReview)
p, Distribution, Submission, Create, allow
p, Distribution, Submission, Read, allow
p, Distribution, Submission, Update, allow
p, Distribution, Submission, Transition, allow, res.status in ["Received", "Triaging", "WaitingOnBroker"]

# Underwriters can read all submissions
p, Underwriter, Submission, Read, allow

# Underwriters can update submissions assigned to them
p, Underwriter, Submission, Update, allow, sub.userId == res.assignedUnderwriter

# Underwriters can transition assigned submissions (ReadyForUWReview → InReview → Quoted → Bound)
p, Underwriter, Submission, Transition, allow, sub.userId == res.assignedUnderwriter && res.status in ["ReadyForUWReview", "InReview", "Quoted", "BindRequested"]

# Admins can do everything
p, Admin, Submission, *, allow
```

---

## Example 3: Account 360 View Specification

This example shows how to design a 360-degree view with optimized queries and relationship loading.

### Account Entity Design

**Table Name:** `Accounts`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Account identifier |
| AccountNumber | string(50) | UNIQUE, NOT NULL | Human-readable account number |
| Name | string(255) | NOT NULL | Insured company/person name |
| BrokerId | Guid | FK → Brokers, NOT NULL | Managing broker |
| Industry | string(100) | NULL | Industry classification |
| Status | string(20) | NOT NULL | Active, Inactive, Suspended |
| CreatedAt | DateTime | NOT NULL | Account created timestamp |

**Related Entities:**
- AccountContacts (one-to-many)
- Policies (one-to-many)
- Submissions (one-to-many)
- Renewals (one-to-many)
- ActivityTimelineEvents (one-to-many)

### Account 360 API Endpoint

```yaml
/accounts/{id}/360:
  get:
    summary: Get complete account 360 view
    description: Returns account with all related entities in a single optimized query
    operationId: getAccount360
    tags: [Accounts]
    parameters:
      - name: id
        in: path
        required: true
        schema:
          type: string
          format: uuid
      - name: include
        in: query
        description: Comma-separated list of relationships to include
        schema:
          type: string
          default: "contacts,policies,submissions,timeline"
    responses:
      '200':
        description: Account 360 view
        content:
          application/json:
            schema:
              type: object
              properties:
                account:
                  $ref: '#/components/schemas/AccountDetail'
                contacts:
                  type: array
                  items:
                    $ref: '#/components/schemas/Contact'
                policies:
                  type: array
                  items:
                    $ref: '#/components/schemas/PolicySummary'
                submissions:
                  type: array
                  items:
                    $ref: '#/components/schemas/SubmissionSummary'
                timeline:
                  type: array
                  items:
                    $ref: '#/components/schemas/TimelineEvent'
                metrics:
                  type: object
                  properties:
                    totalPremiumYTD:
                      type: number
                      format: decimal
                    activePoliciesCount:
                      type: integer
                    openSubmissionsCount:
                      type: integer
                    upcomingRenewalsCount:
                      type: integer
```

### Query Optimization for 360 View

**EF Core Implementation (C#):**

```csharp
// BAD: N+1 query problem
var account = await context.Accounts.FindAsync(accountId);
var contacts = await context.Contacts.Where(c => c.AccountId == accountId).ToListAsync();
var policies = await context.Policies.Where(p => p.AccountId == accountId).ToListAsync();
// ... multiple separate queries

// GOOD: Single optimized query with eager loading
var account360 = await context.Accounts
    .Where(a => a.Id == accountId)
    .Include(a => a.Contacts.Where(c => c.DeletedAt == null))
    .Include(a => a.Policies.Where(p => p.Status == "Active"))
    .Include(a => a.Submissions.Where(s => s.Status != "Bound" && s.Status != "Declined"))
    .Select(a => new Account360Response
    {
        Account = new AccountDetail
        {
            Id = a.Id,
            Name = a.Name,
            AccountNumber = a.AccountNumber,
            // ... map only needed fields, avoid loading entire entity
        },
        Contacts = a.Contacts.Select(c => new ContactSummary
        {
            Id = c.Id,
            Name = c.FullName,
            Email = c.Email,
            Phone = c.Phone
        }).ToList(),
        Policies = a.Policies.Select(p => new PolicySummary
        {
            Id = p.Id,
            PolicyNumber = p.PolicyNumber,
            CoverageType = p.CoverageType,
            Premium = p.Premium,
            EffectiveDate = p.EffectiveDate,
            ExpirationDate = p.ExpirationDate
        }).ToList(),
        // ... project only needed fields
    })
    .AsSplitQuery() // Use split queries for multiple collections to avoid cartesian explosion
    .FirstOrDefaultAsync();
```

**Performance Considerations:**
- Use `.Select()` projection to load only needed fields (not entire entities)
- Use `.AsSplitQuery()` when including multiple collections to avoid cartesian explosion
- Filter related entities in `.Include()` to reduce payload size
- Use indexed columns in `.Where()` clauses
- Consider caching reference data (coverage types, states)

### Authorization for 360 View

**Casbin Policies:**

```csv
# Distribution users can view full 360 for all accounts
p, Distribution, Account, Read360, allow

# Underwriters can view 360 only for accounts with active submissions assigned to them
p, Underwriter, Account, Read360, allow, exists(sub.assignedSubmissions, s.accountId == res.id)

# Brokers can view 360 only for their own accounts (external portal)
p, Broker, Account, Read360, allow, res.brokerId == sub.brokerId

# Admins can view all
p, Admin, Account, Read360, allow
```

**Field-Level Security:**
- Internal notes: Visible only to Distribution, Underwriter, Admin (not Broker)
- Premium amounts: Visible to all
- Tax ID: Visible only to Admin

---

## Example 4: Architecture Decision Records (ADRs)

Complete ADR examples showing architectural decision documentation.

### ADR-001: Modular Monolith vs Microservices

**Status:** Accepted

**Context:**

We need to decide on the service architecture for Nebula. The primary options are:
1. **Microservices**: Separate deployable services for Broker, Account, Submission, Renewal modules
2. **Modular Monolith**: Single deployable with logical module boundaries

**Factors to consider:**
- Team size: 3-5 developers initially
- Expected scale: 50-500 concurrent users in first year
- Deployment complexity: Limited DevOps resources initially
- Transaction boundaries: Many operations span multiple entities (e.g., create submission → update broker → log timeline)
- Development velocity: Need to ship MVP quickly

**Decision:**

We will implement a **Modular Monolith** architecture for Phase 0 and Phase 1.

**Rationale:**

Pros of Modular Monolith:
- **Simpler deployment**: Single Docker container, easier CI/CD
- **Easier transactions**: ACID transactions across modules (Broker + Submission in same DB transaction)
- **Faster development**: No network calls between modules, easier debugging
- **Lower operational complexity**: One database, one deployment, simpler monitoring
- **Team size appropriate**: Small team benefits from shared codebase

Cons of Modular Monolith:
- **Scaling limitations**: Must scale entire application (but not a concern at 50-500 users)
- **Technology coupling**: All modules use same tech stack (.NET) (acceptable given expertise)

Cons of Microservices (which we're avoiding):
- **Operational overhead**: Multiple deployments, service discovery, distributed tracing
- **Transaction complexity**: Distributed transactions, saga patterns, eventual consistency
- **Development overhead**: API versioning, inter-service communication, testing complexity
- **Team overhead**: Requires more DevOps expertise and tooling

**Consequences:**

- All modules (Broker, Account, Submission, Renewal, Timeline, Identity) deployed as one .NET application
- Logical module boundaries enforced via folder structure and namespaces
- Shared database (PostgreSQL) with tables organized by module
- Future migration path: If scaling requires, can extract modules to microservices (design interfaces as if they were separate services)
- Clear interfaces between modules to enable future decomposition

**Review Date:** 2027-01-31 (after 1 year of production usage)

---

### ADR-002: EF Core as ORM

**Status:** Accepted

**Context:**

We need an Object-Relational Mapping (ORM) tool for the .NET backend. Options considered:
1. **Entity Framework Core 10**: Microsoft's official ORM
2. **Dapper**: Lightweight micro-ORM (SQL-first)
3. **NHibernate**: Mature ORM alternative

**Decision:**

Use **Entity Framework Core 10** as the primary ORM.

**Rationale:**

Pros of EF Core:
- **Code-first migrations**: Easy schema evolution and version control
- **LINQ support**: Strongly-typed queries, compile-time safety
- **Change tracking**: Automatic audit field updates (UpdatedAt, UpdatedBy)
- **Navigation properties**: Easy relationship traversal
- **Microsoft support**: Official support, regular updates, .NET 10 compatibility
- **Ecosystem**: Good tooling, documentation, community

Cons of EF Core:
- **Performance overhead**: Slightly slower than Dapper for read-heavy scenarios
- **Learning curve**: Complex for advanced scenarios (owned entities, TPH, query filters)

Why not Dapper:
- More boilerplate code for CRUD operations
- No change tracking (manual UpdatedAt, UpdatedBy management)
- No migration support (would need FluentMigrator or manual SQL scripts)
- Better for read-heavy, report-style queries (which we can use alongside EF Core)

Why not NHibernate:
- Less active development compared to EF Core
- Smaller community and fewer resources
- Configuration complexity (XML or fluent API)

**Consequences:**

- All entity classes inherit from `BaseEntity` (Id, timestamps, audit fields)
- Migrations managed via `dotnet ef migrations` commands
- Use `.AsNoTracking()` for read-only queries (performance optimization)
- Use Dapper for complex reporting queries where EF Core is suboptimal
- Global query filters for soft delete (WHERE DeletedAt IS NULL)

**Performance Mitigation:**
- Use projections (`.Select()`) to load only needed fields
- Use `.AsSplitQuery()` for multiple included collections
- Use `.AsNoTracking()` for read-only operations
- Consider read replicas for heavy reporting

---

### ADR-003: Casbin for ABAC Authorization

**Status:** Accepted

**Context:**

We need fine-grained authorization that supports:
- Role-based access (Admin, Distribution, Underwriter)
- Resource-based access (user can update submissions assigned to them)
- Attribute-based access (user can view accounts in their region)

Options:
1. **Casbin (ABAC)**: Policy-based authorization framework
2. **Custom RBAC**: Hand-rolled role checks in code
3. **Azure AD / Keycloak built-in**: RBAC via JWT claims

**Decision:**

Use **Casbin with ABAC model** for authorization.

**Rationale:**

Pros of Casbin:
- **Policy-based**: Policies external to code, can be updated without deployment
- **ABAC support**: Can check resource attributes (submission.assignedUnderwriter == user.id)
- **Auditable**: All policies defined in CSV or database, easy to review
- **Testable**: Policies can be unit-tested independently
- **Flexible**: Supports RBAC, ABAC, and hybrid models

Why not custom RBAC:
- Hard-coded role checks scattered in code (`if (user.Role == "Admin")`)
- Hard to audit all authorization logic
- Difficult to add fine-grained rules (attribute-based)

Why not Keycloak-only:
- Keycloak provides authentication and basic RBAC via roles
- Cannot check resource attributes (e.g., "assigned underwriter") in Keycloak
- Would need custom code anyway for fine-grained checks

**Consequences:**

- Casbin policies stored in database (NebulaDbContext)
- Middleware enforces authorization on every API request
- Subject attributes extracted from JWT (roles, userId, region)
- Resource attributes extracted from entity (status, assignedUnderwriter, brokerId)
- Policy format: `p, {role}, {resource}, {action}, {effect}, {condition}`
- Example: `p, Underwriter, Submission, Update, allow, sub.userId == res.assignedUnderwriter`

**Integration:**
- Keycloak for authentication (OIDC/JWT)
- Casbin for authorization (ABAC policies)
- UserProfile table maps Keycloak user to Nebula attributes (region, roles)

---

## Version History

**Version 2.0** - 2026-01-31 - Expanded with complete Broker, Submission, Account 360, and ADR examples (600 lines)
**Version 1.0** - 2026-01-26 - Initial architecture examples (135 lines)
