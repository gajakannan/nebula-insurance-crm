# F0009 — Authentication + Role-Based Login

**Status:** Done
**Priority:** Critical
**Phase:** Phase 1

## Overview

Introduce real login, callback, and role-based entry flows for DistributionUser, Underwriter, and BrokerUser personas.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Full product requirements (why + what + boundaries) |
| [IMPLEMENTATION-CONTRACT.md](./IMPLEMENTATION-CONTRACT.md) | Mandatory architecture "How" decisions for implementation |
| [BROKER-VISIBILITY-MATRIX.md](./BROKER-VISIBILITY-MATRIX.md) | BrokerUser field-level data boundary requirements |
| [../security/F0009-security-review-checklist.md](../../security/F0009-security-review-checklist.md) | Security handoff checklist for review evidence |
| [STATUS.md](./STATUS.md) | Completion checklist and progress tracking |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Setup and verification guide for test users |
| [RELEASE-CUT-MOSCOW.md](./RELEASE-CUT-MOSCOW.md) | Must/Should/Could/Won't release scope decision |

## Stories

| ID | Title | Status |
|----|-------|--------|
| [F0009-S0001](./F0009-S0001-login-screen-and-oidc-redirect.md) | Login Screen and OIDC Redirect | Done |
| [F0009-S0002](./F0009-S0002-oidc-callback-and-session-bootstrap.md) | OIDC Callback and Session Bootstrap | Done |
| [F0009-S0003](./F0009-S0003-role-based-entry-and-protected-navigation.md) | Role-Based Entry and Protected Navigation | Done |
| [F0009-S0004](./F0009-S0004-broker-user-access-boundaries.md) | BrokerUser Access Boundaries | Done |
| [F0009-S0005](./F0009-S0005-seeded-user-access-validation-matrix.md) | Seeded User Access Validation Matrix | Done |

**Total Stories:** 5
**Completed:** 5 / 5
