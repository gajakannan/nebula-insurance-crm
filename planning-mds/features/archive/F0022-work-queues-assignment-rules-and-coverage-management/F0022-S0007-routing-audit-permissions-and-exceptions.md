# F0022-S0007: Routing audit, permissions, and exceptions

**Story ID:** F0022-S0007
**Feature:** F0022 — Work Queues, Assignment Rules & Coverage Management
**Title:** Routing audit, permissions, and exceptions
**Priority:** High
**Phase:** MVP

## User Story

**As a** Compliance-aware Operations Manager
**I want** routing decisions, permission boundaries, and exceptions to be traceable
**So that** queue operations can be reviewed without exposing unauthorized work

## Context & Background

Routing and reassignment can change workload ownership and visibility. F0022 therefore requires Security Reviewer participation and explicit audit expectations.

## Acceptance Criteria

**Happy Path:**
- **Given** routing, coverage, reassignment, or rebalance occurs
- **When** an authorized reviewer opens the routing/audit view
- **Then** they can see actor/system, source item, queue, assignee, rule version, decision reason, prior value, new value, and timestamp
- **And** unauthorized records are excluded from both audit rows and aggregate counts

**Alternative Flows / Edge Cases:**
- Routing exception occurs -> record exception type and safe message.
- User lacks permission -> deny audit/detail access without revealing hidden record existence.
- Source record deleted/archived -> keep audit row with safe source reference and no editable action.
- Rule later changes -> historical decision still points to the version used at the time.

## Interaction Contract

N/A — read-only audit story. Mutations are owned by F0022-S0001, F0022-S0002, F0022-S0004, and F0022-S0006.

## Data Requirements

**Required Fields:**
- Event type, source type, source ID, queue ID, assignee ID, rule version, decision reason, actor/system, timestamp, exception code when applicable

**Validation Rules:**
- Audit rows are append-only.
- Historical rows remain readable even when current rule configuration changes.

## Role-Based Visibility

**Roles that can view routing audit:**
- Authorized managers, Compliance/Admin roles

**Data Visibility:**
- Audit rows and counts must honor the same source-record authorization boundaries as worklists.
- InternalOnly data remains hidden from unauthorized users.

## Non-Functional Expectations

- Security: audit rows must not leak hidden source records through counts, labels, or exception messages.
- Reliability: historical decisions remain readable after rule changes.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Routing audit is internal-only for MVP.

## Dependencies

**Depends On:**
- F0022-S0002 — rule versions
- F0022-S0003 — routing outcomes
- F0022-S0006 — reassignment actions

## Out of Scope

- External audit portal
- Export scheduling
- Long-term compliance retention policy changes

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Story filename matches `Story ID` prefix

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
