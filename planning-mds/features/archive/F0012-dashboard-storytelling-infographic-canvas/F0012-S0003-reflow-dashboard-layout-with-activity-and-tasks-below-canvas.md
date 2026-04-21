# F0012-S0003: Flow Activity and My Tasks as Flat Canvas Sections Below Story Content

**Story ID:** F0012-S0003
**Feature:** F0012 — Dashboard Storytelling Infographic Canvas (Flat Canvas + Collapsible Rails)
**Title:** Flow Activity and My Tasks as flat canvas sections below story content
**Priority:** Medium
**Phase:** MVP

## User Story

**As a** dashboard user
**I want** Activity and My Tasks to appear as continuous flat sections of the same infographic canvas below the story content
**So that** I can scroll from narrative analysis to concrete execution work within one seamless surface

## Context & Background

The infographic canvas philosophy extends to Activity and My Tasks. These are not separate bordered panels — they are content sections of the same flat canvas, differentiated from the story content above by vertical spacing and section headers. No panel borders, card wrappers, or divider lines separate them from the rest of the canvas. This preserves the editorial infographic feel while keeping operational workflows clear.

## Acceptance Criteria

**Happy Path:**
- **Given** dashboard infographic canvas is loaded
- **When** the user scrolls below the story content (nudges + KPIs + flow + chapters)
- **Then** Activity renders as a flat timeline/list section with a section header
- **And** My Tasks renders as a flat task list section with a section header
- **And** no panel borders, card wrappers, or divider lines separate these sections from the story content above or from each other
- **And** vertical spacing and typography size establish section boundaries
- **And** both sections preserve existing core interactions and links

**Interaction + Permission:**
- **Given** role-scoped access applies
- **When** user views Activity and My Tasks sections
- **Then** content remains filtered by existing policy boundaries
- **And** user cannot access records outside their allowed scope

**Alternative Flows / Edge Cases:**
- Empty activity feed → section shows clear empty state with no layout collapse; spacing and header remain.
- Empty task list → section shows clear empty state and action prompt if applicable.
- One section fails to load → other section remains usable and story content is unaffected.
- Read-only guard → this story changes layout behavior only; no workflow mutation requirement.

**Checklist:**
- [ ] Activity section positioned below story content as flat canvas zone
- [ ] My Tasks section positioned below Activity as flat canvas zone
- [ ] No panel borders, card wrappers, or divider lines between sections
- [ ] Spacing and typography hierarchy establish section boundaries
- [ ] Existing interaction affordances preserved (links, sort, filter, deep-links)
- [ ] Empty/error/loading states defined

## Data Requirements

**Required Fields:**
- Activity event summary fields (type, timestamp, actor, description)
- Task list fields (title, due date, status, linked entity)

**Optional Fields:**
- Secondary badges and metadata chips

**Validation Rules:**
- Section data remains role-scoped and sorted using existing business behavior.
- Section loading/error states do not alter story-content state above.

## Role-Based Visibility

**Roles that can view these sections:**
- DistributionUser — Read
- DistributionManager — Read
- Underwriter — Read
- RelationshipManager — Read
- ProgramManager — Read
- Admin — Read

**Data Visibility:**
- InternalOnly content: all activity and task details
- ExternalVisible content: none

## Non-Functional Expectations

- Performance: section render should not delay first story-canvas render (lazy load or deferred render acceptable).
- Security: no broadened data exposure due to layout repositioning.
- Reliability: section errors are isolated and non-blocking to narrative layer above.

## Dependencies

**Depends On:**
- F0012-S0001 — unified flat infographic canvas

**Related Stories:**
- F0012-S0005 — responsive/accessibility parity for stacked canvas layout

## Out of Scope

- Task workflow logic changes
- Activity event model changes
- Cross-section bulk actions

## Questions & Assumptions

**Open Questions:**
- [ ] Should Activity and My Tasks sections be independently collapsible within the canvas?

**Assumptions (to be validated):**
- Existing activity/task APIs and widgets can be repositioned and restyled (border removal) without contract changes.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] No panel borders, card wrappers, or divider lines in rendered output
- [ ] Audit/timeline logged: N/A (layout-only scope)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0012-S0003-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
