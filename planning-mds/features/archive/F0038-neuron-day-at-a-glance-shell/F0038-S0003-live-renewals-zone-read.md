---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0038-S0003 — Live Renewals Zone (Read: Needs-Attention List + Per-Renewal Drill Context)

## Story Header

**Story ID:** F0038-S0003
**Feature:** F0038 — Neuron Day-at-a-Glance Shell
**Title:** Live Renewals zone — needs-attention list and drill context
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** renewal-owning Underwriter
**I want** the Renewals zone to show me the renewals that need my attention, with enough context to act
**So that** I can immediately see "what needs me today" instead of hunting through the pipeline by hand

## Context & Background

The Renewals zone is the one **live** zone in F0038. It is read-only and permission-respecting: Neuron calls the .NET engine **as the user** (forwarded authentik token); the engine enforces existing Casbin ABAC and remains source of truth. This story delivers the needs-attention list and the per-renewal drill context that the outreach-draft action (F0038-S0005) launches from.

## Acceptance Criteria

**Happy Path:**
- **Given** the Underwriter opens the glance
- **When** the Renewals zone loads
- **Then** it shows the user's renewals **needing attention**: those in `Identified` or `Outreach` workflow state within **90 days** of expiry (configurable default), each row showing account name, expiry date, days-to-expiry, workflow state, and a last-broker-contact signal

- **Given** the needs-attention list
- **Then** rows are ordered by urgency (soonest expiry / most overdue first) and any renewal with **no broker contact in 30+ days** is flagged; ordering is intra-zone only (no cross-zone ranking)

- **Given** the Underwriter drills into a specific renewal
- **When** the drill context loads
- **Then** it shows the account, broker/contact, and the renewal timeline for that record

**Alternative Flows / Edge Cases:**
- No renewals need attention → the zone shows an explicit empty state ("nothing needs you in the next 90 days"), not a blank slot.
- A renewal the user is not authorized to see → it is excluded by the engine; the zone never shows data the forwarded token is not authorized for (authorization enforced engine-side; a 403/forbidden from the engine is surfaced as a typed state, not leaked rows).
- Engine read failure/timeout → the zone shows a typed error state and a retry affordance; other zones are unaffected.
- A renewal outside the 90-day window or in a non-`Identified`/`Outreach` state → excluded from needs-attention.

## Interaction Contract

N/A — read-only story. No CRM mutation. (The first mutation is the draft in F0038-S0005.)

## Data Requirements

**Read (via engine, as the user):**
- Needs-attention list item: account name, expiry date, days-to-expiry, workflow state (`Identified`/`Outreach`), last-broker-contact signal/date.
- Per-renewal drill: account, broker/contact, renewal timeline (activity/workflow history).

**Parameters:**
- Needs-attention window: default 90 days, configurable.
- No-contact flag threshold: 30 days.

**Validation Rules:**
- Only `Identified`/`Outreach` renewals within the window qualify.
- All reads are permission-scoped to the requesting user by the engine; Neuron applies no independent authorization logic.

## Role-Based Visibility

**Roles that can view:**
- Underwriter (primary) — sees the renewals they own/are authorized for.
- Distribution (assist, secondary) — sees what their authorization permits (engine-enforced).

**Data Visibility:**
- InternalOnly: renewal/account/broker context is internal CRM data; visibility is governed by the engine's Casbin ABAC for the forwarded token.
- ExternalVisible: none.

## Non-Functional Expectations

- Performance: the needs-attention list renders without blocking other zones; pagination/limit applied if the list is large.
- Security: authorization is enforced by the engine (Casbin ABAC) via the forwarded user token; Neuron never broadens scope.
- Reliability: typed empty/error states; no partial/unauthorized data leakage.

## Dependencies

**Depends On:**
- F0038-S0001 — runtime + engine call path (forwarded token).
- F0038-S0002 — the shell slot that renders this zone's content payload.
- Builds on the completed F0007 Renewal Pipeline (renewal records, `Identified`/`Outreach` states).

**Related Stories:**
- F0038-S0005 — the outreach draft launches from a needs-attention row / drill context.

## Business Rules

1. **Needs-attention rule (intake §3.5, decision D):** `Identified`/`Outreach` AND within 90 days of expiry (configurable), ordered by urgency, flag no-contact-30d. Intra-zone ranking only.
2. **Permission-respecting via engine (intake L2):** reads go through the engine as the user; ABAC enforced unchanged.
3. **Read-only zone:** the Renewals zone reads and surfaces; it performs no write (writes are S0005/S0006).
4. **New engine read endpoints** for (a) the needs-attention list and (b) per-renewal context are architect-defined in Phase B (additive to `nebula-api.yaml`).

## Out of Scope

- Any write/mutation from the read zone.
- Cross-zone ranking or merging with other domains.
- Live data in any zone other than Renewals.
- Configurable-window admin UI (only the default + config hook are in scope; admin surface is later).

## UI/UX Notes

- Screens involved: Renewals zone within the Day-at-a-Glance shell + per-renewal drill (see PRD `## Screen Layouts (ASCII)`).
- Key interactions: scan needs-attention rows → drill a renewal → (proactive draft CTA appears, handled in S0005).

## Questions & Assumptions

**Open Questions:**
- [ ] (Architect, Phase B) Source/shape of the "last-broker-contact signal" and where the 90-day default is configured.

**Assumptions (to be validated):**
- F0007 renewal records expose expiry date and workflow state sufficient for the needs-attention computation.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (empty, unauthorized exclusion, engine error, out-of-window)
- [ ] Permissions enforced (engine Casbin ABAC via forwarded token)
- [ ] Audit/timeline logged — N/A (read-only)
- [ ] Tests prove the 90-day/`Identified`+`Outreach` rule, urgency ordering, no-contact-30d flag, and unauthorized exclusion
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
