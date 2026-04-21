# F0012-S0004: Preserve Collapsible Left Nav and Right Neuron Rail with Adaptive Canvas Width

**Story ID:** F0012-S0004
**Feature:** F0012 — Dashboard Storytelling Infographic Canvas (Flat Canvas + Collapsible Rails)
**Title:** Preserve collapsible left nav and right Neuron rail with adaptive canvas width
**Priority:** High
**Phase:** MVP

## User Story

**As a** dashboard user
**I want** both left navigation and right Neuron rail to remain collapsible while the story canvas adapts its width
**So that** I can choose focus mode without losing core navigation or AI assistance controls

## Context & Background

The storytelling canvas should use maximum available width, but the product shell still requires collapsible rails on both sides. This story formalizes adaptive behavior for all rail state combinations.

## Acceptance Criteria

**Happy Path:**
- **Given** dashboard shell has left nav and right Neuron rail
- **When** either rail is collapsed or expanded
- **Then** story canvas width recalculates without overlap, clipping, or horizontal jitter
- **And** both rail toggles remain accessible and discoverable
- **And** if both rails collapse, canvas expands to maximum content width

**Interaction + Permission:**
- **Given** user role determines dashboard data visibility
- **When** rail state changes
- **Then** data scope does not change
- **And** no additional unauthorized controls become visible
- **And** rail toggles are read-only layout controls, so no domain audit/timeline mutation events are emitted

**Alternative Flows / Edge Cases:**
- Rail state persisted from previous session -> dashboard restores prior layout safely.
- Very narrow viewport + both rails expanded -> canvas switches to compact rendering mode without clipping controls.
- Right rail unavailable by product config -> canvas still renders without overlap, clipping, or horizontal scroll regression.
- Read-only guard -> this story does not mutate business entities.

**Checklist:**
- [ ] Left nav collapse/expand behavior preserved
- [ ] Right Neuron rail collapse/expand behavior preserved
- [ ] Canvas width adapts smoothly across all rail-state combinations
- [ ] Compact fallback behavior defined for constrained widths

## Data Requirements

**Required Fields:**
- Layout state flags: `leftRailCollapsed`, `rightRailCollapsed`
- Viewport/breakpoint indicators

**Optional Fields:**
- Per-user persisted layout preference key/value

**Validation Rules:**
- Rail states must remain valid booleans.
- Invalid or missing persisted state falls back to default shell state.
- Rail state transitions cannot break keyboard focus order.

## Role-Based Visibility

**Roles that can use collapsible rails:**
- DistributionUser — Read
- DistributionManager — Read
- Underwriter — Read
- RelationshipManager — Read
- ProgramManager — Read
- Admin — Read

**Data Visibility:**
- InternalOnly content: dashboard narrative and Neuron rail interactions
- ExternalVisible content: none

## Non-Functional Expectations

- Performance: rail toggle interaction p95 < 120ms for visible layout response.
- Security: rail toggles are UI-only and must not alter data authorization context.
- Reliability: repeated rail toggles should not cause layout desynchronization or render crashes.

## Dependencies

**Depends On:**
- F0012-S0001 — unified story canvas foundation

**Related Stories:**
- F0012-S0005 — responsive/accessibility parity for rail-interactive layout

## Out of Scope

- Neuron rail feature content redesign
- Navigation IA changes
- User role management changes

## Questions & Assumptions

**Open Questions:**
- [ ] Should right Neuron rail default collapsed for tablet portrait widths?

**Assumptions (to be validated):**
- Existing shell components already support collapse state and can expose consistent width signals to dashboard content.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: N/A (layout behavior)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0012-S0004-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
