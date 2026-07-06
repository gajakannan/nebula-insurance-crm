# F0021 — Communication Hub & Activity Capture

**Status:** Done — archived 2026-07-02 via feature run `2026-07-01-9cee64f0`
**Priority:** High
**Phase:** CRM Release MVP

## Overview

Capture notes, calls, meetings, email-linked references, and follow-up history so communication becomes part of the CRM record instead of living only in inboxes or private memory.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Product scope and business outcomes |
| [STATUS.md](./STATUS.md) | Planning and implementation tracker |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Setup and refinement notes |

## Stories

| ID | Title | Status |
|----|-------|--------|
| [F0021-S0001](./F0021-S0001-capture-communication-event.md) | Capture a structured communication event | Done |
| [F0021-S0002](./F0021-S0002-view-contextual-communication-history.md) | View contextual communication history | Done |
| [F0021-S0003](./F0021-S0003-link-communications-to-related-records-and-participants.md) | Link communications to related records and participants | Done |
| [F0021-S0004](./F0021-S0004-create-follow-up-task-from-communication.md) | Create a follow-up task from a communication | Done |
| [F0021-S0005](./F0021-S0005-correct-or-redact-communication-with-audit.md) | Correct or redact communication content with audit | Done |

**Total Stories:** 5
**Completed:** 5 / 5

## Architecture Review

**Phase B status:** Complete

### Key Findings

- F0021 reuses activity timeline and task follow-up patterns but introduces a user-authored communication business record.
- Security review is required because free-text communication content and email-linked metadata can contain sensitive information.
- Full outbound send and external connector ingestion remain out of scope for this MVP slice.

### Architecture Artifacts

| Artifact | Status |
|----------|--------|
| Data model / ERD | Complete |
| API contract (OpenAPI) | Complete |
| Workflow state machine | N/A — follow-up uses existing task workflow |
| Casbin policy | Complete |
| JSON schemas | Complete |
| C4 diagrams | N/A for MVP closeout |
| ADRs | Complete — ADR-029 |
| Assembly plan | Complete |

**Archived:** 2026-07-02
