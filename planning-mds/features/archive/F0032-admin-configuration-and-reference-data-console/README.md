# F0032 — Admin Configuration & Reference Data Console

**Status:** Done and archived
**Priority:** High
**Phase:** Platform Operations

## Overview

Give administrators a governed surface to discover, draft, validate, publish, roll back, and audit supported runtime business configuration as Nebula grows more configurable. The first release focuses on queue/routing governance, workflow SLA thresholds, saved-view/report defaults, and template metadata boundaries without replacing the module-owned implementations.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Product scope and business outcomes |
| [STATUS.md](./STATUS.md) | Planning and implementation tracker |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Setup and refinement notes |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Phase B architecture baseline |

## Stories

| ID | Title | Status |
|----|-------|--------|
| [F0032-S0001](./F0032-S0001-admin-configuration-catalog.md) | Admin configuration catalog | Done |
| [F0032-S0002](./F0032-S0002-draft-reference-and-sla-configuration.md) | Draft reference data and workflow SLA configuration | Done |
| [F0032-S0003](./F0032-S0003-govern-queue-routing-configuration.md) | Govern queue and routing configuration drafts | Done |
| [F0032-S0004](./F0032-S0004-validate-and-compare-configuration.md) | Validate and compare configuration before publish | Done |
| [F0032-S0005](./F0032-S0005-publish-and-rollback-configuration.md) | Publish and roll back configuration sets | Done |
| [F0032-S0006](./F0032-S0006-audit-and-permission-safe-admin-configuration.md) | Audit and permission-safe configuration behavior | Done |

**Total Stories:** 6
**Completed:** 6 / 6

## Planning Notes

- Feature action run `2026-07-06-f0ef8526` passed G0-G8 with accepted non-blocking recommendations.
