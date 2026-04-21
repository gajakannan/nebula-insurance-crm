# F0011-S0005: Ensure Responsive and Accessibility Parity for New Opportunities Flow

**Story ID:** F0011-S0005
**Feature:** F0011 — Dashboard Opportunities Flow-First Modernization (Connected Pipeline + Terminal Outcomes)
**Title:** Ensure responsive and accessibility parity for new opportunities flow
**Priority:** High
**Phase:** MVP

## User Story

**As a** dashboard user on desktop, tablet, or phone
**I want** the same core opportunities actions available with device-appropriate layouts
**So that** I can triage opportunities and outcomes reliably on any supported viewport

## Context & Background

The new connected flow introduces richer visual structures that must remain usable on iPad and iPhone layouts. Dense matrix/radial interactions that work on desktop can degrade quickly on smaller viewports.

## Acceptance Criteria

**Happy Path:**
- **Given** a user views opportunities on desktop
- **When** the widget renders
- **Then** full connected flow, milestones, outcomes rail, and secondary mini-views are visible and interactive

- **Given** a user views opportunities on tablet
- **When** the widget renders
- **Then** flow and outcomes stack in readable sections without interaction loss

- **Given** a user views opportunities on phone
- **When** the widget renders
- **Then** primary interactions are simplified to stacked stage cards plus bottleneck/outcome summary list
- **And** stage and outcome drilldowns remain available

**Accessibility + Permission:**
- Keyboard supports period selector, stage/outcome target selection, and drilldown open/close actions.
- Screen reader labels expose entity type, stage/outcome label, count, period, and selection state.
- Focus order is deterministic through flow, outcomes, mini-views, and drilldown controls.
- All data remains ABAC scoped and role-authorized.

**Alternative Flows / Edge Cases:**
- Small viewport overflow -> horizontal containers provide visible affordance and preserve target hit areas.
- Drilldown open on viewport edge -> panel repositions without clipping interactive controls.
- Any one data source fails -> user can still navigate to other views and controls.
- Read-only guard -> this story does not create, update, delete, or transition domain records; audit/timeline mutation events are not required.

**Checklist:**
- [ ] Desktop/tablet/phone layouts are explicitly defined and testable
- [ ] Keyboard and screen reader flows are covered for stage and outcome interactions
- [ ] Error/empty/loading handling is non-blocking
- [ ] ABAC and role boundaries are preserved
- [ ] Visual and interaction parity acceptance tests are documented

## Data Requirements

**Required Fields:**
- Stage/outcome labels and counts
- Selected period
- Drilldown target metadata

**Optional Fields:**
- Responsive breakpoint-specific display hints

**Validation Rules:**
- Drilldown targets remain valid across breakpoints.
- Period selection is preserved when layout changes.
- Accessibility labels include count and context metadata.

## Role-Based Visibility

**Roles that can use responsive opportunities workflows:**
- DistributionUser
- DistributionManager
- Underwriter
- RelationshipManager
- ProgramManager
- Admin

**Data Visibility:**
- All opportunities data remains InternalOnly and ABAC scoped.

## Non-Functional Expectations

- Performance: layout adaptation and interaction response p95 < 300ms after initial render.
- Accessibility: keyboard-only and screen-reader parity validated for core workflows.
- Reliability: no breakpoint-specific blocker prevents opening stage/outcome drilldowns.

## Dependencies

**Depends On:**
- F0011-S0001
- F0011-S0002
- F0011-S0003
- F0011-S0004

**Related Stories:**
- F0010-S0005 — Prior responsive/accessibility baseline

## Out of Scope

- Native mobile app redesign
- Offline opportunities mode

## Questions & Assumptions

**Open Questions:**
- [ ] Confirm whether phone layout should default to a single entity tab (submission or renewal) or keep both stacked by default.

**Assumptions (to be validated):**
- Existing dashboard breakpoints and accessibility patterns can be reused for the new flow components.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: N/A (read-only view)
- [ ] Tests pass
- [ ] Documentation updated
