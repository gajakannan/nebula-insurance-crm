# Story F0036-S0005: Replace Hardcoded Cyber DynamicAttributePanel (Five-Screen Regression)

## Story Header

**Story ID:** F0036-S0005
**Feature:** F0036 — Form Engine and Form-State Preservation
**Title:** Replace Hardcoded Cyber DynamicAttributePanel With the Engine (Five-Screen Regression)
**Priority:** High
**Phase:** MVP
**Workstream:** A — Dynamic product-attribute engine

## User Story

**As a** Cyber Underwriter
**I want** the Cyber attributes panel on every screen I use to keep working exactly as it does today
**So that** the move to a governed engine is invisible to me — same fields, same behavior, no lost work

## Context & Background

`experience/src/features/lob-attributes/components/DynamicAttributePanel.tsx` is currently a hardcoded Cyber panel: typed `CyberLobAttributeValues`, fixed JSX, option constants, lifted parent state, and a `useCyberSchemaBundle` call used only for a status string. This story swaps the panel's internals for the schema-driven engine (S0003) while keeping its public placement and props contract stable across the five consuming screens. It is the highest-regression-risk story in Workstream A, so it is gated by a per-screen regression suite written before the swap.

## Acceptance Criteria

**Happy Path — Drop-in replacement:**
- **Given** the engine from S0001–S0004
- **When** `DynamicAttributePanel` is reimplemented on the engine
- **Then** its placement and external props contract on the five consuming screens are preserved: `CreateSubmissionPage`, `CreatePolicyPage`, `PolicyDetailPage`, `RenewalDetailPage`, `SubmissionDetailPage`
- **And** Cyber field semantics (revenue band, records held, requested limit/retention, MFA enabled, MFA maturity, EDR, offline backups, training frequency) are expressed through the schema bundle, not hardcoded JSX or option constants

**Happy Path — Per-screen behavior preserved:**
- **Given** a regression test per consuming screen, captured against the current panel before the swap
- **When** the engine-backed panel replaces it
- **Then** each screen's create, edit, and read-only attribute behavior matches the pre-swap baseline (same visible fields, same en/disabled states, same submit behavior)
- **And** the existing entity-update timeline event still fires on save (regression-asserted; no new event class)
- **And** no Cyber-specific field list or option constant remains in the rendering path

**Edge Case — Read-only contexts:**
- **Given** a screen that shows attributes read-only (e.g. a detail page for a terminal/locked record)
- **When** the engine renders
- **Then** it renders read-only (no editable controls, no enabled Save), matching the prior panel's read-only behavior

**Edge Case — Conditional gating preserved:**
- **Given** the prior panel's conditional behavior (MFA maturity is only meaningful when MFA is enabled)
- **When** the engine renders the controls section
- **Then** the same enable/disable relationship is preserved via the engine's conditional mechanism (decided in Phase B), not ad-hoc component logic
- **And** clearing `mfaEnabled` clears/disables `mfaMaturity` consistently with today's behavior

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Each of the five screens hosting `DynamicAttributePanel` | Create / edit Cyber attributes, then save via the host screen | Editable in create/edit contexts; read-only in read-only contexts | The host save calls the existing F0034 backend write path; attributes persist and the entity timeline records the change (existing behavior, unchanged) | Per-screen regression test: edit an attribute, save, reload → persisted value shown and timeline event present; read-only screen shows no editable control | Roles/lifecycle states each screen already enforces (unchanged) |

Required checks:
- [x] Render-only behavior cannot satisfy: edit→save→reload must show persisted change on the editable screens; this is asserted, not snapshot-only.
- [x] Save path validation: AJV (S0003) blocks invalid submit; backend authoritative.
- [x] Audit/timeline: the existing entity-update timeline event must still fire on save (regression-asserted), not a new event class.
- [x] Tests prove parity with the pre-swap baseline on all five screens.

## Data Requirements

**Required fields:** unchanged from the current Cyber panel — driven by the Cyber `data-schema.json` (see S0003). The engine consumes the same value object the screens already pass; no new persisted fields.

**Validation Rules:**
- Behavior parity is the contract: the same fields, options, enable/disable, and submit gating as the pre-swap panel.
- Any intentional difference must be justified in the PR and reflected in the regression baseline update.

## Role-Based Visibility

**Roles that can edit:**
- Exactly the roles each of the five screens already authorizes for create/edit; read-only screens stay read-only for all roles. This story changes the panel internals, not authorization. Unauthorized users are still blocked by each screen's existing route auth (HTTP 401/403 upstream).

**Data Visibility:**
- No change. Attribute visibility follows each host screen's existing InternalOnly/ExternalVisible rules.

## Non-Functional Expectations

- **Performance:** Panel render and interaction latency are at or below the pre-swap baseline on all five screens; no added full-form re-renders per keystroke.
- **Security:** No new network surface or auth change. The `useCyberSchemaBundle` status-string usage is replaced by real bundle consumption; no tokens/PII handled.
- **Reliability:** A bundle-load failure renders a controlled error in the panel region rather than breaking the host screen.

## Dependencies

**Depends On:**
- F0036-S0003 — the validated, schema-driven form being dropped in.
- F0036-S0004 — pin-during-edit binding the panel uses.

**Related Stories:**
- F0036-S0006 — adds preservation to this now-engine-backed panel.

## Business Rules

1. **No behavior regression.** The five-screen baseline is the acceptance bar; engine adoption is invisible to underwriters in the happy path.
2. **No hardcoded Cyber rendering.** Field list, options, and layout come from the bundle after this story.
3. **Read-only stays read-only.** Read-only host contexts must not become editable.

## Out of Scope

- F0035 forced-re-auth preservation of this panel (S0006).
- Workstream B CRUD forms (S0007/S0008).
- Visual redesign — the goal is parity, not a new look.

## UI/UX Notes

- Screens involved: the five consuming screens above. Visual output should match the current panel closely enough that underwriters notice no change.
- Inline validation now comes from AJV (S0003) rather than the prior bespoke checks.

## Questions & Assumptions

**Resolved (Phase B):**
- The conditional MFA-maturity gating is not encoded in the shipped bundle (`ui-schema.json` is layout-only; `rules.json` has `mfa_required_for_high_record_count` but not a UI enable rule). Phase B decides the gating mechanism (engine conditional convention vs a bundle extension) and records it in the ADR; this story preserves today's observable behavior regardless of mechanism.

**Assumptions (to be validated):**
- The pre-swap panel's exact behavior can be captured as a regression baseline before the swap (it can — the component is self-contained).

## Definition of Done

- [ ] Acceptance criteria met (drop-in swap, five-screen parity, read-only, conditional gating)
- [ ] Edge cases handled (read-only contexts, bundle-load failure, conditional clearing)
- [ ] Permissions enforced (per-screen auth unchanged; verified by regression tests)
- [ ] Audit/timeline logged (existing entity-update event still fires on save)
- [ ] Tests pass (per-screen regression suite written pre-swap and green post-swap; Playwright on at least one create + one edit screen)
- [ ] Documentation updated (GETTING-STARTED notes the engine-backed panel + the five screens)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
