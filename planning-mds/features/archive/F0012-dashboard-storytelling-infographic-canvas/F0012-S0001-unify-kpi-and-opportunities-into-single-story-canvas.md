# F0012-S0001: Unify Nudge Bar, KPI Band, and Connected Opportunity Flow into One Flat Infographic Canvas

**Story ID:** F0012-S0001
**Feature:** F0012 — Dashboard Storytelling Infographic Canvas (Flat Canvas + Collapsible Rails)
**Title:** Unify nudge bar, KPI band, and connected opportunity flow into one flat infographic canvas
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution User or Underwriter
**I want** the dashboard nudge bar, KPI metrics, and opportunities flow presented as one continuous flat infographic canvas
**So that** I can scan from alerts through operational health to workflow progression without encountering panel borders, card wrappers, or visual separators

## Context & Background

Current dashboard assembles nudge bar, KPI cards, and opportunities views as separate bordered panel components. This story establishes one continuous flat narrative surface where all elements flow together — differentiated by spacing and typography, not borders. Nudge bar sits at the top of the canvas, flowing seamlessly into story controls. KPI band is embedded inline. Opportunities render as a connected left-to-right flow with terminal outcome branches (absorbing F0011's connected flow and terminal outcomes scope).

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated internal user opens Dashboard
- **When** the dashboard finishes loading
- **Then** nudge/action items, KPI metrics, story controls, and connected opportunity flow all render within one continuous flat canvas
- **And** no panel borders, card wrappers, elevated surfaces, or divider lines separate these content zones
- **And** nudge bar flows directly into story controls below it with no visual separator
- **And** opportunities render as connected left-to-right stage flow with ribbon-style connections (not disconnected tiles)
- **And** terminal outcome branches display with semantic path styles (solid = positive, dashed = negative, dotted = passive/time-based)
- **And** period changes update KPI, stage flow, and terminal outcome values in sync

**Interaction + Permission:**
- **Given** a user has opportunities read permissions
- **When** they interact with a stage or outcome node in the canvas
- **Then** role-scoped data is shown in drilldowns
- **And** unauthorized users do not receive out-of-scope aggregates or records
- **And** nudge items are scoped to the user's role and assignments

**Alternative Flows / Edge Cases:**
- No nudge items → nudge zone collapses gracefully; story controls and KPI band shift up without layout gap.
- No opportunities data → canvas shows explicit empty narrative state while nudges, KPIs, and controls remain active.
- Partial load failure for KPI or opportunities source → inline non-blocking error for failed section; successful sections remain usable.
- Unknown stage label from backend → fallback label displays; remaining nodes continue rendering.
- Read-only guard → this story does not mutate workflow entities; audit/timeline mutation events are not required.

**Checklist:**
- [ ] Flat infographic canvas shell implemented (no panel borders or card wrappers)
- [ ] Nudge bar integrated as top canvas section with no separator to story controls
- [ ] KPI band embedded as inline canvas section (not separate card components)
- [ ] Connected left-to-right opportunity flow implemented (not disconnected tiles)
- [ ] Terminal outcome branches with semantic path styles (solid/dashed/dotted)
- [ ] KPI + opportunities + terminal outcomes period-synchronized
- [ ] Stage/outcome drilldown remains available
- [ ] Empty/error/loading states defined for all canvas sections

## Data Requirements

**Required Fields:**
- Nudge/action items: title, overdue duration, linked entity, dismiss action
- KPI aggregate values (`activeBrokers`, `openSubmissions`, `renewalRate`, `avgTurnaroundDays`)
- Opportunities stage sequence metadata (deterministic order, connection strengths for ribbon thickness)
- Stage node values: label, count, status indicator
- Terminal outcome summary: `outcome_type`, `count`, `percent_of_exits`, `avg_days_to_exit`
- Terminal outcome branch style: `positive` (solid), `negative` (dashed), `passive` (dotted)
- Selected `periodDays`

**Optional Fields:**
- Stage emphasis hints (`normal`, `active`, `blocked`, `bottleneck`)
- Drilldown target identifiers for terminal outcome nodes

**Validation Rules:**
- Numeric values must be non-negative.
- Period changes apply to all canvas data layers (KPI, stages, outcomes) consistently.
- Scope filtering is applied before aggregate values are returned.
- Nudge items are filtered by user role/assignment scope.
- Stage sequence must be deterministic and ordered.

## Role-Based Visibility

**Roles that can view the infographic canvas:**
- DistributionUser — Read
- DistributionManager — Read
- Underwriter — Read
- RelationshipManager — Read
- ProgramManager — Read
- Admin — Read

**Data Visibility:**
- InternalOnly content: nudge items, KPI aggregates, opportunities flow, terminal outcome aggregates, and drilldown cards
- ExternalVisible content: none (BrokerUser excluded)

## Non-Functional Expectations

- Performance: first meaningful canvas render (nudges + KPI + flow visible) p95 < 600ms after dashboard shell render.
- Security: ABAC scope handling remains aligned with existing dashboard policies for all canvas sections.
- Reliability: one failed data source should not blank the full canvas; each section degrades independently.

## Dependencies

**Depends On:**
- F0010 — Dashboard Opportunities Refactor baseline (Pipeline Board, aging/hierarchy endpoints) — Done

**Related Stories:**
- F0012-S0002 — chapter overlays and narrative controls
- F0012-S0003 — downstream activity/tasks section positioning

**Absorbs from deprecated F0011:**
- F0011-S0001 scope: connected left-to-right flow replacing disconnected pipeline tiles
- F0011-S0002 scope: terminal outcomes rail with branch path semantics and drilldowns

## Out of Scope

- Replacing dashboard-level top navigation
- Editing KPI definitions or business formulas
- Workflow status taxonomy changes
- Nudge bar business logic changes (only layout integration)

## Questions & Assumptions

**Resolved:**
- KPI labels remain static in the Flow chapter but may become chapter-contextual in future iterations. MVP: static labels across all chapters.

**Assumptions (to be validated):**
- Existing KPI and opportunities endpoints can be consumed in one combined frontend render model without schema migration.
- Nudge bar data is available from existing My Tasks endpoint with overdue/attention filtering.
- Connected flow and terminal outcome data can be derived from existing stage aggregates with minor DTO additions (sequence metadata, outcome summary fields).

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] No panel borders, card wrappers, or divider lines in rendered output
- [ ] Audit/timeline logged: N/A (read-only view)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0012-S0001-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
