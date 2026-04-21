---
template: feature
version: 1.1
applies_to: product-manager
---

# F0003: Task Center + Reminders (API-only MVP)

**Feature ID:** F0003  
**Feature Name:** Task Center + Reminders (API-only MVP)  
**Priority:** Medium  
**Phase:** MVP

## Feature Statement

**As a** internal user  
**I want** to create, update, and delete my own tasks via API  
**So that** I can manage follow-ups that power the dashboard task and nudge widgets

## Business Objective

- **Goal:** Enable reliable task creation and maintenance for MVP workflows.  
- **Metric:** Tasks created/updated per active user.  
- **Baseline:** Tasks live outside Nebula.  
- **Target:** Tasks managed in Nebula for all MVP users.

## Problem Statement

- **Current State:** Tasks are tracked in personal tools with no system record.  
- **Desired State:** Tasks are stored in Nebula and visible on the dashboard.  
- **Impact:** Missed deadlines and inconsistent follow-up history.

## Scope & Boundaries

**In Scope:**
- Create task (self-assigned only)
- Update task (self-assigned only)
- Soft delete task (self-assigned only)
- Timeline events for task mutations

**Out of Scope:**
- Task Center UI (API-only in MVP)
- Assigning tasks to other users
- Notifications or reminders
- Automated task creation

## Success Criteria

- Task CRUD endpoints enforce self-assignment and return valid schemas.  
- Task mutations emit ActivityTimelineEvent records.  
- Dashboard widgets can read tasks without policy exceptions.

## Risks & Assumptions

- **Risk:** Scope creep into full Task Center UI.  
- **Assumption:** Self-assigned tasks are sufficient for MVP.  
- **Mitigation:** Keep UI out of MVP and enforce assignment rules server-side.

## Dependencies

- Task entity and indexes (data-model.md)  
- ABAC enforcement and policy definitions  
- ProblemDetails error contract

## Related User Stories

- F0003-S0001 — Create Task  
- F0003-S0002 — Update Task  
- F0003-S0003 — Delete Task
