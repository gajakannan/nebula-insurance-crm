# F0019 — Submission Quoting, Proposal & Approval Workflow

**Status:** Draft
**Priority:** Critical
**Phase:** CRM Release MVP

## Overview

Complete the commercial P&C submission lifecycle beyond intake by adding quote preparation, proposal generation, approval flow, bind readiness, and final decision states.
F0019 also owns the future submission end-of-life contract that replaces F0006's descoped soft-delete claim, using audit-preserving archive/deactivate behavior rather than implicit CRUD delete.

Boundary guardrail: F0019 does not merely document downstream submission states; it is the feature that explicitly turns them on. F0006 remains the authoritative intake boundary through `ReadyForUWReview`, and the shared submission transition surface must continue rejecting `ReadyForUWReview -> InReview` and later transitions until F0019 stories are refined, implemented, and validated.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Product scope and business outcomes |
| [STATUS.md](./STATUS.md) | Planning and implementation tracker |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Setup and refinement notes |

## Stories

| ID | Title | Status |
|----|-------|--------|

**Total Stories:** 0
**Completed:** 0 / 0
