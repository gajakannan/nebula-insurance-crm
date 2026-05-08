# F0034-S0004 - Prove frontend and backend validator equivalence

**Story ID:** F0034-S0004
**Feature:** F0034 - Product Schema Registry and Dynamic LOB Attributes
**Title:** Prove frontend and backend validator equivalence
**Priority:** Critical
**Phase:** Platform Foundation

## User Story

**As a** quality engineer
**I want** frontend and backend validation to agree on every Cyber fixture
**So that** users receive consistent product-attribute decisions regardless of where validation runs

## Context & Background

AJV in the browser and the backend JSON Schema validator are separate engines. F0034 acceptance requires engineered parity: the same schema profile, shared fixtures, stable normalized error codes, JSON pointers, and rule-case evidence.

## Acceptance Criteria

**Happy Path:**
- **Given** the Cyber `1.0.0` bundle has valid, invalid, and rule-case fixtures
- **When** the parity harness runs
- **Then** FE and backend validators accept the same valid fixtures
- **And** they reject the same invalid fixtures
- **And** rejected schema fixtures produce identical `(code, pointer)` multisets
- **And** rule-case fixtures produce identical pass/fail outcomes
- **And** the harness reports side-by-side failure detail when cardinality differs

**Alternative Flows / Edge Cases:**
- FE accepts a fixture and backend rejects it -> parity harness fails
- Backend accepts a fixture and FE rejects it -> parity harness fails
- Both layers reject but emit different `(code, pointer)` multisets -> parity harness fails
- Rule expression exceeds the approved operation set -> activation is rejected
- Existing ProblemDetails `errors` field is altered for non-LOB failures -> regression test fails

**Checklist:**
- [ ] At least 5 valid Cyber examples exist
- [ ] At least 5 invalid Cyber examples exist
- [ ] Passing and failing rule-case examples exist
- [ ] Normalized error envelope uses stable LOB error codes
- [ ] Error comparison ignores message text
- [ ] Error comparison includes cardinality, `code`, and `pointer`
- [ ] Non-LOB ProblemDetails behavior remains unchanged
- [ ] Authorization-sensitive hidden fields are not exposed through validation error payloads
- [ ] Audit/timeline logging is N/A because the parity harness does not mutate domain records

## Data Requirements

**Required Fields:**
- Fixture id
- Product version id
- Stage
- Expected decision: accept or reject
- Expected error code
- Expected JSON pointer

**Optional Fields:**
- Constraint value
- Schema path
- Rule id

**Validation Rules:**
- Message text is not a parity primitive
- `(code, pointer)` is the parity primitive for rejected schema fixtures
- Rule cases compare pass/fail result and pointer-mapped errors
- Both validators run in collect-all-errors mode

## Role-Based Visibility

**Roles that can approve or operate this story:**
- Quality Engineer - owns parity evidence
- Schema Steward - reviews fixtures before activation
- Backend Developer - implements backend normalization
- Frontend Developer - implements frontend normalization
- Security Reviewer - reviews error payload leakage
- Authorization review confirms fixture execution does not grant product-attribute read access to unauthorized users

**Data Visibility:**
- InternalOnly content: fixture payloads, parity reports, schema activation evidence
- ExternalVisible content: none in MVP

## Non-Functional Expectations

- Performance: warm FE validation p95 target is at most 5 ms for representative Cyber payloads
- Performance: warm backend validation p95 target is at most 10 ms for representative Cyber payloads
- Reliability: parity harness fails closed on missing fixtures, unknown rule operations, or divergent errors
- Security: error payloads expose codes and pointers without leaking hidden product data

## Dependencies

**Depends On:**
- F0034-S0001 - validator equivalence decision
- F0034-S0006 - Cyber bundle and fixtures

**Related Stories:**
- F0034-S0005 - dynamic panel displays normalized errors
- F0034-S0007 - E2E flow relies on parity evidence

## Business Rules

1. **Parity is a release gate:** Cyber activation cannot pass without FE/BE parity evidence.
2. **Codes beat messages:** Error text can differ by UI language; stable codes and pointers define equivalence.
3. **Global ProblemDetails is preserved:** LOB validation adds a sibling error shape rather than changing the existing global `errors` field semantics.

## Out of Scope

- Full OpenAPI 3.1 migration
- Replacing non-LOB FluentValidation paths
- Comparing localized human-readable messages

## UI/UX Notes

- Screens involved: Dynamic Attribute Panel error display
- Key interactions: field-level errors map from normalized JSON pointers to form fields

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- Phase B pins exact validator package versions and options.
- Representative Cyber payloads are defined by the Cyber examples.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - validation harness story)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
