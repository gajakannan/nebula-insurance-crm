# F0028-S0001: Market directory search and open

**Story ID:** F0028-S0001
**Feature:** F0028 — Carrier & Market Relationship Management
**Title:** Market directory search and open
**Priority:** Critical
**Phase:** CRM Release MVP+

## User Story

**As a** distribution leader
**I want** to search and filter carrier and market relationship records
**So that** I can find the right market context before making placement decisions

## Context & Background

F0028 starts with a searchable internal market directory. It reuses the F0023 search/reporting foundation where Phase B confirms fit, but this story is about a dedicated market directory and opening the source relationship workspace.

## Acceptance Criteria

**Happy Path:**
- **Given** I am an authenticated internal user with market read permission
- **When** I search by carrier/market name, segment, line of business, geography, contact name, or appointment status
- **Then** I see only authorized carrier and market records
- **And** each row shows name, market type, active/inactive status, primary segment, geography, owner, appetite freshness, and appointment summary
- **And** selecting a row opens the market relationship workspace for that record

**Alternative Flows / Edge Cases:**
- Query shorter than 2 non-space characters -> no search request is sent.
- No authorized matches -> show "No markets match this search" without implying hidden records exist.
- Unauthorized records -> omit row, snippet, count, and suggestion.
- Search service unavailable -> show retry action and preserve the typed query.

**Checklist:**
- [ ] Filters include status, market type, segment, line of business, geography, owner, appetite freshness, and appointment status.
- [ ] Directory rows link to one authoritative market workspace route.
- [ ] Keyboard and screen-reader navigation can submit search and open a result.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A — read-only story. Search criteria do not mutate CRM data.

## Data Requirements

**Required Fields:**
- Search term: trimmed text, minimum 2 characters, maximum 100 characters.
- Market record label: carrier or market display name.
- Market type: admitted carrier, non-admitted carrier, MGA/program market, or wholesaler market.
- Status: active, inactive, monitoring, or archived.
- Source route: authoritative market workspace link.

**Optional Fields:**
- Segment, geography, owner, appetite freshness, appointment summary, primary contact.

**Validation Rules:**
- Queries shorter than 2 non-space characters do not execute.
- Queries over 100 characters show validation feedback and do not execute.

## Role-Based Visibility

**Roles that can search:**
- Distribution leader, distribution manager, relationship manager, underwriter, program manager, admin — may search internal market records within ABAC scope.

**Data Visibility:**
- InternalOnly content: all market relationship results and snippets.
- ExternalVisible content: none in F0028.

## Non-Functional Expectations

- Performance: first page of authorized market results returns within the product search target set in Phase B.
- Security: unauthorized rows, counts, snippets, and suggestions are not materialized.
- Reliability: typed query and filters remain available after retryable errors.

## Dependencies

**Depends On:**
- F0023 — search/reporting substrate.

**Related Stories:**
- F0028-S0002 — opened record lands on the profile workspace.
- F0028-S0006 — related work and activity appear on the opened workspace.

## Out of Scope

- External broker-visible market directory.
- Carrier API search.
- Quote comparison or market recommendation ranking.

## UI/UX Notes

- Screens involved: Market Directory, Market Relationship Workspace.
- Key interactions: search, filter, clear filters, open source record.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Market directory is internal-only for F0028.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A — read-only search; search telemetry is not required for story completion)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0028-S0001-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
