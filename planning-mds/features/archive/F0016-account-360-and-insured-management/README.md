# F0016 — Account 360 & Insured Management

**Status:** Done (Archived)
**Priority:** Critical
**Phase:** CRM Release MVP
**Archived:** 2026-04-14

## Overview

Add insured-centered Account records and a composed Account 360 workspace so underwriters, distribution users, managers, and relationship/program roles can view contacts, submissions, policies, renewals, lifecycle state, and activity in one place.

F0016 also owns the Deleted / Merged Account Fallback Contract so dependent submission, policy, renewal, timeline, and search views continue to render predictably after lifecycle transitions.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Product scope, workflows, stories, and business rules |
| [STATUS.md](./STATUS.md) | Final implementation tracker, signoff ledger, and closeout summary |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Prerequisites, verification commands, and runtime notes |
| [feature-assembly-plan.md](./feature-assembly-plan.md) | Architect build plan used for the slice execution |
| [F0016-QE-REPORT.md](./F0016-QE-REPORT.md) | QE acceptance and validation evidence |
| [F0016-CODE-REVIEW-REPORT.md](./F0016-CODE-REVIEW-REPORT.md) | Code review findings and approval |
| [F0016-SECURITY-REVIEW-REPORT.md](./F0016-SECURITY-REVIEW-REPORT.md) | Security signoff and control review |
| [F0016-DEVOPS-VALIDATION.md](./F0016-DEVOPS-VALIDATION.md) | Migration/runtime deployability evidence |
| [F0016-ARCHITECT-REVIEW.md](./F0016-ARCHITECT-REVIEW.md) | Architecture conformance review |
| [ADR-017](../../architecture/decisions/ADR-017-account-merge-tombstone-and-fallback-contract.md) | Merge, tombstone, and fallback contract |

## Stories

| ID | Title | Status |
|----|-------|--------|
| F0016-S0001 | [Account list with search and filtering](./F0016-S0001-account-list-with-search-and-filtering.md) | Done |
| F0016-S0002 | [Create account (manual and from submission / policy)](./F0016-S0002-create-account.md) | Done |
| F0016-S0003 | [Account detail and profile edit](./F0016-S0003-account-detail-and-profile-edit.md) | Done |
| F0016-S0004 | [Account 360 composed workspace](./F0016-S0004-account-360-composition.md) | Done |
| F0016-S0005 | [Account-scoped contacts management](./F0016-S0005-account-contacts-management.md) | Done |
| F0016-S0006 | [Account relationships (broker / producer / territory)](./F0016-S0006-account-relationships-broker-producer-territory.md) | Done |
| F0016-S0007 | [Account lifecycle (deactivate / reactivate / delete)](./F0016-S0007-account-lifecycle-deactivate-reactivate-delete.md) | Done |
| F0016-S0008 | [Account merge and duplicate handling](./F0016-S0008-account-merge-and-duplicate-handling.md) | Done |
| F0016-S0009 | [Deleted / merged account fallback contract](./F0016-S0009-deleted-merged-account-fallback-contract.md) | Done |
| F0016-S0010 | [Account activity timeline and audit trail](./F0016-S0010-account-activity-timeline-and-audit.md) | Done |
| F0016-S0011 | [Account summary projection](./F0016-S0011-account-summary-projection.md) | Done |

**Total Stories:** 11  
**Completed:** 11 / 11
