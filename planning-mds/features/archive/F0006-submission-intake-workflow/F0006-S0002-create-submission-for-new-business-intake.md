---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0006-S0002: Create Submission for New Business Intake

**Story ID:** F0006-S0002
**Feature:** F0006 — Submission Intake Workflow
**Title:** Create submission for new business intake
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user
**I want** to create a new submission linked to an account and broker with required intake fields
**So that** new business enters Nebula as a structured, trackable record from the start

## Context & Background

Submission creation is the entry point for all new business in Nebula. A distribution user captures the minimum intake information needed to open the record — account, broker, and requested coverage date, with line of business captured when already known — and the system creates the submission in Received status. Region alignment between account and broker is enforced at creation time. The created submission immediately appears in the pipeline list and the creator is initially assigned as the owner.

## Acceptance Criteria

**Happy Path:**
- **Given** a distribution user with `submission:create` permission
- **When** they submit a create request with AccountId, BrokerId, and EffectiveDate
- **Then** a submission is created in `Received` status, `AssignedToUserId` is set to the creator, `CreatedByUserId` and `CreatedAt` are set, an ActivityTimelineEvent (`SubmissionCreated`) is appended, and the submission ID is returned

**With Optional Fields:**
- **Given** valid required fields
- **When** the user also provides ProgramId, PremiumEstimate, Description, and ExpirationDate
- **Then** all optional fields are persisted on the submission record

**Alternative Flows / Edge Cases:**
- Missing required field (AccountId, BrokerId, EffectiveDate) → HTTP 400 with `ProblemDetails` listing missing fields
- Invalid AccountId (not found or soft-deleted) → HTTP 400 with `code=invalid_account`
- Invalid BrokerId (not found, soft-deleted, or inactive) → HTTP 400 with `code=invalid_broker`
- Region mismatch (Account.Region not in broker's BrokerRegion set) → HTTP 400 with `code=region_mismatch`
- Invalid ProgramId (not found or soft-deleted) → HTTP 400 with `code=invalid_program`
- Invalid LineOfBusiness (not in known LOB set) → HTTP 400 with `code=invalid_lob`
- PremiumEstimate is negative → HTTP 400 with validation error
- EffectiveDate in the past → allowed (backdated submissions are valid in insurance)
- User lacks `submission:create` permission → HTTP 403
- Duplicate submission detection is not enforced in MVP (same account+broker+date can create multiple submissions)

**Checklist:**
- [ ] Submission created in `Received` status
- [ ] `AssignedToUserId` defaults to the creator on create
- [ ] `CreatedByUserId` and `CreatedAt` set from authenticated principal
- [ ] Region alignment validated: `Account.Region` must be in broker's `BrokerRegion` set
- [ ] `ExpirationDate` defaults to `EffectiveDate + 12 months` when not provided
- [ ] One `ActivityTimelineEvent` (`SubmissionCreated`) appended atomically
- [ ] One `WorkflowTransition` (null → Received) appended atomically
- [ ] Response returns the created submission with ID
- [ ] All validation errors use RFC 7807 ProblemDetails format

## Data Requirements

**Required Fields:**
- `accountId` (uuid): The insured account
- `brokerId` (uuid): The submitting broker
- `effectiveDate` (date): Requested coverage effective date

**Optional Fields:**
- `programId` (uuid): Associated program
- `lineOfBusiness` (string): LOB classification
- `premiumEstimate` (decimal): Estimated premium amount
- `expirationDate` (date): Coverage expiration (defaults to effectiveDate + 12 months)
- `description` (string): Free-text submission notes

**Validation Rules:**
- AccountId must reference a valid, non-deleted account
- BrokerId must reference a valid, non-deleted, active broker
- Account.Region must be in broker's BrokerRegion set (region alignment)
- ProgramId, when provided, must reference a valid, non-deleted program
- LineOfBusiness, when provided, must be in the known LOB value set
- PremiumEstimate must be >= 0 when provided
- EffectiveDate is required and may be past or future

## Role-Based Visibility

**Roles that can create:**
- Distribution User — Creates submissions and is assigned as initial owner
- Distribution Manager — Creates submissions; initial owner still defaults to the creator and reassignment happens separately
- Admin — Full create access

**Data Visibility:**
- InternalOnly: Submission data is internal-only in MVP
- ExternalVisible: None

## Non-Functional Expectations

- Performance: Create completes in < 500ms
- Security: ABAC authorization enforced; region alignment validated server-side
- Reliability: Create + timeline + transition record creation is atomic (single DB transaction); idempotent-safe for retried requests (client should check for success before retrying)

## Dependencies

**Depends On:**
- F0002 (Broker entity — already complete)
- F0016 (Account entity — must exist, at minimum stub)

**Related Stories:**
- F0006-S0001 — Pipeline list displays created submissions
- F0006-S0003 — Detail view shows created submission
- F0006-S0007 — Timeline records SubmissionCreated event

## Business Rules

1. **Region Alignment:** Account.Region must be included in the broker's BrokerRegion set at creation time. Mismatches return HTTP 400 with `code=region_mismatch`. This prevents submissions from being created against broker-account pairings that violate territorial assignment.
2. **Self-Assignment on Create:** The authenticated creator is automatically assigned as the initial submission owner (`AssignedToUserId = creator`). Reassignment to an underwriter happens separately via F0006-S0006.
3. **Default ExpirationDate:** When ExpirationDate is not provided, it defaults to EffectiveDate + 12 months. This follows standard commercial P&C policy term conventions.
4. **Backdated Submissions Allowed:** EffectiveDate in the past is permitted. Insurance intake frequently processes submissions where coverage start dates have already passed (e.g., broker submits late, retroactive coverage requests).
5. **No Duplicate Detection in MVP:** Multiple submissions for the same account + broker + effective date combination are allowed. Duplicate detection is deferred — the insurance domain legitimately produces multiple submissions for the same insured/broker pair.
6. **LOB Validation:** LineOfBusiness, when provided, must match a value in the known LOB set defined per ADR-009. Invalid values return HTTP 400 with `code=invalid_lob`.
7. **Append-Only Audit on Create:** Creation atomically appends one ActivityTimelineEvent (`SubmissionCreated`) and one WorkflowTransition (null → Received) in the same DB transaction. If either append fails, the entire creation rolls back.
8. **InternalOnly Data:** All submission data is internal-only in MVP. No external broker visibility.

## Out of Scope

- Bulk submission creation or CSV import
- Submission creation from external broker portal
- Automated submission creation from email or document parsing
- Duplicate detection beyond what the database naturally prevents (no business-key uniqueness in MVP)

## UI/UX Notes

- Screens involved: Create Submission dialog or page (accessed from Pipeline List header action)
- Key interactions: Account picker (search-as-you-type), Broker picker (search-as-you-type with region validation feedback), optional LOB dropdown, date picker for effective date, optional premium and description fields
- On successful create → navigate to Submission Detail (F0006-S0003)
- Region mismatch shown as inline validation error near broker picker

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Backdated submissions (EffectiveDate in the past) are allowed per insurance industry practice
- Creator is auto-assigned as initial owner; reassignment happens separately (F0006-S0006)
- No duplicate detection in MVP — multiple submissions for the same account+broker+date are allowed

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (ABAC + region alignment)
- [ ] Audit/timeline logged (SubmissionCreated + WorkflowTransition null→Received)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0006-S0002-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
