# F0012-S0002: Add Interactive Story Chapters and In-Canvas Analytical Overlays

**Story ID:** F0012-S0002
**Feature:** F0012 — Dashboard Storytelling Infographic Canvas (Flat Canvas + Collapsible Rails)
**Title:** Add interactive story chapters and in-canvas analytical overlays
**Priority:** High
**Phase:** MVP

## User Story

**As a** Relationship Manager or Program Manager
**I want** to switch between story chapters inside the same infographic canvas
**So that** I can inspect flow, friction, outcomes, aging, and composition without leaving the canvas or losing context

## Context & Background

F0010's dashboard uses tab-level mode switching between Pipeline/Heatmap/Treemap/Sunburst — each replacing the previous view entirely. This story replaces that with chapter controls that keep one persistent canvas and layer contextual overlays/insets in place. The chapter system also absorbs F0011's visual system concepts (warm-to-cool color progression, selective emphasis, reduced border noise).

## Chapter-to-Data Mapping

| F0010 View Mode | F0012 Chapter | Data Source / Endpoint | Overlay Behavior |
|---|---|---|---|
| Pipeline Board | **Flow** (default) | `GET /dashboard/opportunities` + connected sequence metadata | Base connected flow canvas — no overlay |
| *(new — from F0011)* | **Friction** | Stage aggregates + emphasis hints (`blocked`, `bottleneck`, `stalled`) | Friction annotation overlay; highlights stages with highest dwell time or count concentration |
| *(new — from F0011)* | **Outcomes** | Terminal outcome aggregates (`outcome_type`, `count`, `percent_of_exits`, `avg_days_to_exit`) | Terminal outcomes rail emphasis; branch paths highlighted with semantic styles |
| Heatmap | **Aging** | `GET /dashboard/opportunities/aging` (status x aging-bucket matrix) | Heat intensity overlay on stage nodes; aging bucket visualization |
| Treemap + Sunburst | **Mix** | `GET /dashboard/opportunities/hierarchy` (EntityType → ColorGroup → Status) | Composition blocks and radial mini inset as compact overlay elements |

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated internal user is on the dashboard infographic canvas
- **When** they select a chapter (`Flow`, `Friction`, `Outcomes`, `Aging`, `Mix`)
- **Then** the canvas keeps the same base stage/outcome layout
- **And** only chapter-specific overlays/insets change within the flat canvas
- **And** selected chapter state is visually clear and keyboard reachable
- **And** no mode switch, page transition, or panel swap occurs

**Interaction + Permission:**
- **Given** opportunities access is role-scoped
- **When** chapter overlays show drilldown counts or highlights
- **Then** displayed values remain ABAC scoped for that user
- **And** unauthorized users do not see hidden aggregate categories
- **And** chapter switching is read-only UI behavior, so no domain audit/timeline mutation events are emitted

**Alternative Flows / Edge Cases:**
- Overlay dataset unavailable → chapter shows fallback text and user can switch chapters normally.
- Rapid chapter switching → latest selection wins and stale overlay states do not flash incorrect data.
- Small canvas width → overlays collapse into compact mode while preserving baseline flow readability.
- Read-only guard → no domain mutations are performed by chapter switching.

**Checklist:**
- [ ] Chapter control strip implemented inside infographic canvas (part of story controls zone)
- [ ] `Flow` chapter remains baseline default
- [ ] Overlays/insets render in-place within the flat canvas (no full page mode switch)
- [ ] Chapter state and focus behavior accessible via keyboard
- [ ] Each chapter maps to documented data source per chapter-to-data mapping table

## Data Requirements

**Required Fields:**
- Chapter key (`flow`, `friction`, `outcomes`, `aging`, `mix`)
- Stage and outcome aggregate values (base flow data)
- Friction overlay: stage dwell time, count concentration, emphasis hints
- Outcomes overlay: terminal outcome summary (count, percent, avg days)
- Aging overlay: status x aging-bucket intensity matrix (5 buckets: 0-2, 3-5, 6-10, 11-20, 21+ days)
- Mix overlay: composition hierarchy (EntityType → ColorGroup → Status)
- Period filter value

**Optional Fields:**
- Overlay annotation text or tooltip metadata
- Warm-to-cool color progression hints for stage flow visualization

**Validation Rules:**
- Chapter selection must map to supported chapter keys only.
- Unsupported chapter key falls back to `flow`.
- Overlay values must be aligned to the same period and role scope as base flow.

## Role-Based Visibility

**Roles that can use chapter overlays:**
- DistributionUser — Read
- DistributionManager — Read
- Underwriter — Read
- RelationshipManager — Read
- ProgramManager — Read
- Admin — Read

**Data Visibility:**
- InternalOnly content: chapter overlays and drilldown metadata
- ExternalVisible content: none

## Non-Functional Expectations

- Performance: chapter switch render p95 < 250ms after data availability.
- Security: chapter-specific overlays cannot broaden data scope beyond base opportunities permissions.
- Reliability: chapter switch failures remain isolated and recoverable without full dashboard reload.

## Dependencies

**Depends On:**
- F0012-S0001 — unified flat infographic canvas shell

**Related Stories:**
- F0012-S0004 — adaptive canvas behavior with rail collapse states
- F0012-S0005 — responsive and accessibility parity

**Absorbs from deprecated F0011:**
- F0011-S0003 scope: modern visual system (warm-to-cool color progression, selective emphasis, reduced border noise)
- F0011-S0004 scope: rebalancing Heatmap/Treemap/Sunburst from equal-weight tabs to supporting overlay roles

## Out of Scope

- New predictive AI insights inside chapter overlays
- User-defined custom chapter creation
- Exporting chapter visuals to PDF/image

## Questions & Assumptions

**Open Questions:**
- [ ] Should chapter selection persist across sessions per user preference?

**Assumptions (to be validated):**
- Existing heatmap/treemap/radial datasets from F0010 can be remapped as overlay data without creating new workflow entities.
- Friction and outcomes data can be derived from existing stage aggregates with minor DTO additions.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: N/A (read-only view)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0012-S0002-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
