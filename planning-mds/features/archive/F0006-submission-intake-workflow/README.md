# F0006 — Submission Intake Workflow

**Status:** Done
**Archived:** 2026-04-04
**Priority:** Critical
**Phase:** CRM Release MVP

## Overview

Establish Nebula as the system of record for new business intake, triage, completeness checks, and early underwriting routing so submissions stop living across inboxes and spreadsheets. F0006 owns the first half of the submission lifecycle (Received → Triaging → WaitingOnBroker → ReadyForUWReview); the downstream quoting and bind workflow is owned by F0019.
F0006 closeout does not include submission end-of-life actions or deleted-account fallback behavior on linked views; those concerns are explicitly deferred to F0019 and F0016 respectively.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Full product requirements (why + what + how) |
| [STATUS.md](./STATUS.md) | Completion checklist and progress tracking |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Developer/agent setup guide |

## Stories

| ID | Title | Status |
|----|-------|--------|
| [F0006-S0001](./F0006-S0001-submission-pipeline-list-with-intake-status-filtering.md) | Submission pipeline list with intake status filtering | Done |
| [F0006-S0002](./F0006-S0002-create-submission-for-new-business-intake.md) | Create submission for new business intake | Done |
| [F0006-S0003](./F0006-S0003-submission-detail-view-with-intake-context.md) | Submission detail view with intake context | Done |
| [F0006-S0004](./F0006-S0004-submission-intake-status-transitions.md) | Submission intake status transitions | Done |
| [F0006-S0005](./F0006-S0005-submission-completeness-evaluation.md) | Submission completeness evaluation | Done |
| [F0006-S0006](./F0006-S0006-submission-ownership-assignment-and-underwriting-handoff.md) | Submission ownership assignment and underwriting handoff | Done |
| [F0006-S0007](./F0006-S0007-submission-activity-timeline-and-audit-trail.md) | Submission activity timeline and audit trail | Done |
| [F0006-S0008](./F0006-S0008-stale-submission-visibility-and-follow-up-flags.md) | Stale submission visibility and follow-up flags | Done |

**Total Stories:** 8
**Completed:** 8 / 8
