# F0025-S0001: Commission workspace search and policy context

**Story ID:** F0025-S0001
**Feature:** F0025 — Commission, Producer Splits & Revenue Tracking
**Title:** Commission workspace search and policy context
**Priority:** Critical
**Phase:** Brokerage Platform Expansion

## User Story

**As a** distribution leader
**I want** to search authorized commission records by policy, producer, broker, carrier, status, and exception state
**So that** I can find revenue context tied to production activity before reviewing economics

## Context & Background

F0025 starts with read-only visibility. Users need one workspace to find commission context linked to policy, producer ownership, broker, territory, and carrier or market context before any schedule or adjustment work is attempted.

## Acceptance Criteria

**Happy Path:**
- **Given** I am an authenticated internal user with commission read permission
- **When** I search by policy number, account name, producer, broker, carrier or market, status, or exception state
- **Then** I see only authorized commission records
- **And** each result shows policy reference, account, producer, broker, carrier or market, policy period, expected commission status, split status, and exception count
- **And** selecting a result opens the commission detail context for that record

**Alternative Flows / Edge Cases:**
- Query shorter than 2 non-space characters -> no search request is sent.
- No authorized matches -> show "No commission records match this search" without implying hidden records exist.
- Unauthorized records -> omit row, count, snippet, and suggestion.
- Source policy no longer active -> show policy status and keep the commission record visible if the user is authorized.

**Checklist:**
- [ ] Filters include status, exception state, producer, broker, carrier or market, territory, and policy period.
- [ ] Detail context shows source policy, premium basis, producer ownership snapshot, carrier or market context, and audit summary.
- [ ] The workspace has Desktop and narrow layouts described in the PRD.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A — read-only story. Search, filter, and open actions do not mutate CRM data.

## Data Requirements

**Required Fields:**
- Search term: trimmed text, minimum 2 characters, maximum 100 characters.
- Policy reference: policy identifier and policy period.
- Account label: account display name.
- Producer label: attributed producer display name.
- Status: expected commission status.
- Exception count: count of unresolved commission exceptions.

**Optional Fields:**
- Broker, territory, carrier or market, expected amount, split status, last reviewed date.

**Validation Rules:**
- Queries shorter than 2 non-space characters do not execute.
- Queries over 100 characters show validation feedback and do not execute.

## Role-Based Visibility

**Roles that can search:**
- Distribution leader, finance operations user, producer manager, admin — may search commission records within ABAC scope.

**Data Visibility:**
- InternalOnly content: commission records, expected amounts, split context, exception details, and audit summary.
- ExternalVisible content: none in F0025.

## Non-Functional Expectations

- Performance: first page of authorized records returns within the product search target set by Phase B.
- Security: unauthorized rows, counts, snippets, and suggestions are not materialized.
- Reliability: typed query and filters remain available after retryable errors.

## Dependencies

**Depends On:**
- F0017 — producer ownership and hierarchy context.
- F0018 — policy lifecycle and policy period context.
- F0028 — carrier and market context.

**Related Stories:**
- F0025-S0004 — opened record shows expected commission calculation review.
- F0025-S0006 — records contribute to revenue attribution rollups.

## Out of Scope

- Editing commission schedules.
- Editing producer splits.
- Approving adjustments.
- Exporting statements or ledger entries.

## UI/UX Notes

- Screens involved: Commission Workspace, Commission Detail.
- Key interactions: search, filter, clear filters, open source commission record.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- F0025 is internal-only for the first release.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A — read-only search; no CRM mutation occurs)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0025-S0001-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
