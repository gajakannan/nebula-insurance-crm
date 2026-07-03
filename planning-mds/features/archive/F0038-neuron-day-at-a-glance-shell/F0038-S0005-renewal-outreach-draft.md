---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0038-S0005 — Renewal Outreach Draft (Proactive CTA → Generate-On-Click → Persist + Edit In-Chat)

## Story Header

**Story ID:** F0038-S0005
**Feature:** F0038 — Neuron Day-at-a-Glance Shell
**Title:** Generate and persist an editable renewal outreach draft
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** renewal-owning Underwriter
**I want** the companion to offer me a ready-to-edit broker outreach draft for a renewal flagged for attention
**So that** I can act on the renewal in one click instead of hand-writing the email myself

## Context & Background

This is the **one write** that proves the companion's assisted-authoring chain. The companion **proactively suggests** a draft (nudge/CTA) on needs-attention renewals, but the draft is **generated only on the user's click** — never auto-generated, never auto-sent. The generated draft is persisted as a renewal `ActivityTimelineEvent` (mandatory audit) and surfaced **editable in-chat**, labelled an AI-generated draft and `InternalOnly` until a human edits/approves. Generating a draft does **not** transition the renewal — the workflow transition fires only on mock-send (F0038-S0006).

## Acceptance Criteria

**Happy Path:**
- **Given** a renewal needing attention (from F0038-S0003) and the Underwriter has draft rights
- **When** the Underwriter clicks the proactive "Draft outreach" CTA on that renewal
- **Then** the companion generates a draft referencing the upcoming renewal (account, expiry date, broker contact) and requesting engagement, renders it **editable in-chat**, labels it **"AI-generated draft"**, marks it **`InternalOnly`**, and persists it as a renewal `ActivityTimelineEvent`

- **Given** a generated draft
- **When** the Underwriter edits the text in-chat
- **Then** the edited content is retained for the subsequent mock-send step (F0038-S0006) and the edit is reflected on reload of the thread/timeline

**Behavior / Constraints:**
- **Given** draft generation
- **Then** the content **must not** state or imply premium, quote figures, coverage terms, or any binding commitment
- **Given** any draft generation event
- **Then** an `ActivityTimelineEvent` is emitted with actor, timestamp, model, prompt id/version, and content hash (provenance)

**Alternative Flows / Edge Cases:**
- A user **without** `renewal:draft_outreach` (e.g., Distribution) clicks/calls draft → the engine returns forbidden/denied (403); no draft is generated or persisted.
- Renewal not in a draftable state (not `Identified`/`Outreach`, or outside the needs-attention scope) → CTA is not offered / request is rejected with a typed reason.
- Generation backend failure → typed error in-chat; no partial/empty draft is persisted as a timeline event.
- Draft is never auto-sent and never transitions the renewal on its own.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Renewals zone needs-attention row / drill context → proactive "Draft outreach" CTA | Click CTA (generate); then edit draft text in-chat | Draft is editable in-chat after generation; read-only label "AI-generated draft" + `InternalOnly` badge remain | A renewal `ActivityTimelineEvent` (the draft) is created; edits update the in-chat draft content used by mock-send | Reload the thread / renewal timeline → the draft `ActivityTimelineEvent` (with provenance) is present and reflects the latest edited content | Underwriter **only** (Casbin `renewal:draft_outreach`); renewal in `Identified`/`Outreach` within needs-attention scope |

Required checks:
- [ ] Render-only behavior cannot satisfy this story — generation persists a real `ActivityTimelineEvent` and the edited content survives reload.
- [ ] The save path has validation (content constraints) and error behavior (typed failure, no partial persist).
- [ ] A successful generation emits an audit/timeline event with full provenance.
- [ ] Tests prove the Underwriter can generate + edit from the named entry point and observe the persisted timeline event after reload; a non-Underwriter is denied.

## Data Requirements

**Required Fields (draft / timeline event):**
- Renewal reference, account, expiry date, broker contact (inputs to the draft).
- Draft body (generated, then user-edited).
- Provenance: actor, timestamp, model, prompt id/version, content hash.
- Flags: `InternalOnly = true`, label `AI-generated draft`.

**Validation Rules:**
- Content must not include premium/quote/coverage-terms/binding-commitment language.
- A draft is generated only on explicit user action (no auto-generation).

## Role-Based Visibility

**Roles that can draft:**
- Underwriter — holds `renewal:draft_outreach` (new Casbin permission on the Underwriter role, authored by the architect in Phase B).
- Distribution — **cannot** draft in v1 (no draft rights); "refer to Distribution for review" is a future feature.

**Data Visibility:**
- InternalOnly content: the draft is `InternalOnly` until a human edits/approves; never externally visible automatically.
- ExternalVisible content: none in v1 (no real send).

## Non-Functional Expectations

- Performance: draft generation returns within an interactive latency budget; failures surface promptly as typed errors.
- Security: authorization (`renewal:draft_outreach`) enforced engine-side via the forwarded user token; unauthorized callers are denied (403); no prompt-injected content escapes the content constraints / component-render guard.
- Reliability: no draft persisted on generation failure; provenance is always recorded for a persisted draft.

## Dependencies

**Depends On:**
- F0038-S0003 — needs-attention row / drill context the CTA launches from.
- F0038-S0001 — provenance/operation store + model/prompt config.

**Related Stories:**
- F0038-S0006 — mock-send acts on the (edited) draft and commits the workflow transition.

## Business Rules

1. **Generate-on-click only (intake decision A):** proactive CTA, but never auto-generated, never auto-sent.
2. **Underwriter-only (intake decision C):** `renewal:draft_outreach`; Distribution excluded in v1.
3. **Content constraints (intake §3.3):** no premium/quote/terms/binding language; labelled AI-generated; `InternalOnly`.
4. **Mandatory audit (intake §3.3):** the draft lands as a renewal `ActivityTimelineEvent` with full provenance.
5. **Drafting does not transition (intake §6):** the `Identified → Outreach` transition fires only on mock-send.

## Out of Scope

- Real email send (mock-send only — F0038-S0006).
- "Refer draft to Distribution for review" flow (future feature).
- Any write other than this draft.
- Communication Hub (F0021) as the draft home (interim timeline-event home now; not gated on F0021).

## UI/UX Notes

- Screens involved: Renewals zone CTA + in-chat draft editor (see PRD `## Screen Layouts (ASCII)`).
- Key interactions: proactive nudge/CTA → click → editable in-chat draft with AI-generated + InternalOnly labels.

## Questions & Assumptions

**Open Questions:**
- [ ] (Architect/AI Engineer, Phase B) Draft prompt template + the persist-draft engine endpoint shape.

**Assumptions (to be validated):**
- The interim `ActivityTimelineEvent` home is acceptable until a real Communication Hub draft home ships (Later).

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (unauthorized denied, non-draftable state, generation failure, no auto-send)
- [ ] Permissions enforced (`renewal:draft_outreach`, engine-side)
- [ ] Audit/timeline logged (draft `ActivityTimelineEvent` with model/prompt/version/hash)
- [ ] Tests prove generate + edit + persisted-after-reload, content constraints, and non-Underwriter denial
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
