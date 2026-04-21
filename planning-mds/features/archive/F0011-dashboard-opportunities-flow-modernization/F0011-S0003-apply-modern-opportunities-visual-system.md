# F0011-S0003: Apply Modern Opportunities Visual System (Dark Depth + Stage Emphasis)

**Story ID:** F0011-S0003
**Feature:** F0011 — Dashboard Opportunities Flow-First Modernization (Connected Pipeline + Terminal Outcomes)
**Title:** Apply modern opportunities visual system (dark depth + stage emphasis)
**Priority:** Medium
**Phase:** MVP

## User Story

**As a** dashboard user
**I want** the opportunities area to use a cleaner modern visual hierarchy
**So that** I can focus on pipeline flow and exception states without panel noise

## Context & Background

The current dark dashboard theme is functional but visually dense, with many borders competing for attention. Inspiration references show stronger rhythm and hierarchy through cleaner containers, selective emphasis, and controlled color transitions.

## Acceptance Criteria

**Happy Path:**
- **Given** the user views opportunities flow
- **When** the widget renders
- **Then** border density is reduced and content groups rely on spacing, elevation, and typography hierarchy
- **And** stage progression follows a warm-to-cool color rhythm from left to right
- **And** outcomes retain distinct semantic contrast from open stages

**Interaction + Permission:**
- **Given** active or blocked stages are identified
- **When** those stages are rendered
- **Then** selective glow/emphasis cues are visible
- **And** these cues do not alter authorization behavior or expose hidden records

**Alternative Flows / Edge Cases:**
- User has reduced-motion preference -> emphasis uses static cues and no non-essential animations.
- High contrast requirement -> text and key labels remain readable with AA-level contrast.
- Color perception limitations -> labels and numeric values remain sufficient without color-only dependency.
- Read-only guard -> this story does not create, update, delete, or transition domain records; audit/timeline mutation events are not required.

**Checklist:**
- [ ] Border noise is reduced and visual hierarchy is clearer
- [ ] Warm-to-cool stage progression is consistently applied
- [ ] Active/blocked emphasis is available and non-intrusive
- [ ] Readability and contrast requirements are documented and testable
- [ ] Empty/error/loading visuals align with the updated style system

## Data Requirements

**Required Fields:**
- Stage order index
- Stage visual token key
- Stage emphasis hint (if available)

**Optional Fields:**
- Theme token overrides for stage/outcome emphasis

**Validation Rules:**
- Stage-to-color mapping is deterministic by stage order.
- Emphasis states map only to approved token set.
- Color assignments must not be the sole information carrier.

## Role-Based Visibility

**Roles that can view updated opportunities visual system:**
- DistributionUser
- DistributionManager
- Underwriter
- RelationshipManager
- ProgramManager
- Admin

**Data Visibility:**
- Visual treatment does not change data visibility boundaries; all opportunities data remains InternalOnly and ABAC scoped.

## Non-Functional Expectations

- Performance: visual styling changes do not regress render performance by more than 10% compared to F0010 baseline.
- Accessibility: contrast and keyboard focus indicators remain compliant.
- Reliability: theme rendering remains stable across supported breakpoints.

## Dependencies

**Depends On:**
- F0011-S0001 (connected flow baseline)

**Related Stories:**
- F0011-S0002 — Add terminal outcomes rail and drilldowns
- F0011-S0005 — Ensure responsive and accessibility parity

## Out of Scope

- Full dashboard typography redesign outside opportunities widget
- New global design-token architecture overhaul

## Questions & Assumptions

**Open Questions:**
- [ ] Confirm whether active/blocked stage emphasis should derive from existing aging thresholds or a new explicit signal from backend.

**Assumptions (to be validated):**
- Existing theme token system can support additional stage/outcome emphasis tokens.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: N/A (read-only view)
- [ ] Tests pass
- [ ] Documentation updated
