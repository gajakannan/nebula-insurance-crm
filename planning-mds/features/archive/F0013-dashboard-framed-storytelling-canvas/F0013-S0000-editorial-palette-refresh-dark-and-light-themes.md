# F0013-S0000: Editorial Palette Refresh — Dark & Light Themes

**Story ID:** F0013-S0000
**Feature:** F0013 — Dashboard Framed Storytelling Canvas
**Title:** Editorial palette refresh — dark & light themes
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** dashboard user
**I want** the application to use a muted, editorial color palette — deep navy with muted coral and steel blue accents in dark mode, warm gray with the same accent pair in light mode
**So that** the visual tone supports data-journalism-style storytelling rather than neon SaaS glare, and the design tokens are established before any other F0013 story builds on them

## Context & Background

The current palette uses neon violet (#8b5cf6) and fuchsia (#d946ef) as primary and accent colors. These are high-energy SaaS colors that compete with data visualizations and feel loud on the dark theme. The pipeline inspiration images (`pipeline5.png` for dark, `pipeline4.png` for light) demonstrate a calmer, more editorial palette — muted coral and steel blue on deep navy (dark) or warm gray (light). This palette lets the data breathe and the timeline infographic take center stage.

This story is S0000 because it establishes the foundational design tokens that every subsequent story (S0001 glass-card restoration, S0002 timeline, S0003 mini-visualizations, etc.) depends on.

### Current vs. Target Palette

**Dark Theme:**

| Token | Current | Target | Source |
|-------|---------|--------|--------|
| Background | Graphite #0b0f18 | Deep navy #1a2332 | pipeline5.png |
| Surface (elevated) | #121726 | Slate #243044 | pipeline5.png |
| Panel | #171d2e | #2a3650 | pipeline5.png |
| Primary accent | Violet #8b5cf6 | Muted coral #e06070 | pipeline5.png |
| Secondary accent | Fuchsia #d946ef | Steel blue #5080a5 | pipeline5.png |
| Text primary | #f1f3f7 | Off-white #e8e8e8 | pipeline5.png |
| Text secondary | #aeb6c7 | Silver #a0a8b4 | pipeline5.png |
| Border | #262c3b | #2e3a4e | pipeline5.png |

**Light Theme:**

| Token | Current | Target | Source |
|-------|---------|--------|--------|
| Background | Cool white #f4f6fb | Warm gray #f0eded | pipeline4.png |
| Surface (elevated) | #edf0f7 | White #ffffff | pipeline4.png |
| Panel | #e7ecf4 | #f5f2f0 | pipeline4.png |
| Primary accent | Violet #8b5cf6 | Muted coral #d4726a | pipeline4.png |
| Secondary accent | Fuchsia #d946ef | Steel blue #6a9ab8 | pipeline4.png |
| Text primary | #141824 | Charcoal #3a3a3a | pipeline4.png |
| Text secondary | #6b7280 | Warm gray #7a7a7a | pipeline4.png |
| Border | #d6dbe7 | #d8d2ce | pipeline4.png |

## Acceptance Criteria

**Happy Path:**
- **Given** the design tokens file (`planning-mds/screens/design-tokens.md`) exists
- **When** the developer updates `globals.css` with the new palette
- **Then** the dark theme uses the deep navy / muted coral / steel blue palette
- **And** the light theme uses the warm gray / muted coral / steel blue palette
- **And** all existing components (buttons, cards, inputs, nav, rails) render correctly with the new palette
- **And** the glass-card, glow, and gradient utilities use the new accent colors
- **And** WCAG AA contrast ratios are maintained for all text/background combinations

**Dark Theme Verification:**
- **Given** the user switches to dark mode
- **When** the dashboard renders
- **Then** the background is deep navy (~#1a2332), not graphite (#0b0f18)
- **And** primary interactive elements (buttons, links, active states) use muted coral (~#e06070)
- **And** secondary accents (borders, badges, secondary buttons) use steel blue (~#5080a5)
- **And** the overall feel matches the editorial tone of `pipeline5.png`

**Light Theme Verification:**
- **Given** the user switches to light mode
- **When** the dashboard renders
- **Then** the background is warm gray (~#f0eded), not cool white (#f4f6fb)
- **And** primary interactive elements use muted coral (~#d4726a)
- **And** secondary accents use steel blue (~#6a9ab8)
- **And** the overall feel matches the editorial tone of `pipeline4.png`

**Glow & Gradient Update:**
- **Given** a glass-card component with hover glow
- **When** the user hovers
- **Then** the glow uses muted coral tones (not violet/fuchsia)
- **And** gradient borders use coral-to-steel-blue (not violet-to-fuchsia)

**Alternative Flows / Edge Cases:**
- System theme preference → Correct palette applies automatically
- Theme toggle mid-session → Smooth 200ms transition between palettes (no flash)
- Components using hardcoded violet/fuchsia hex values → Must be migrated to use CSS custom properties
- Status colors (success green, warning amber, error red) → Remain unchanged (palette-independent)
- Chart/visualization accent colors → Updated to use coral/steel blue/complementary data palette (see Data Visualization Palette below)

**Checklist:**
- [ ] Dark theme CSS custom properties updated in `globals.css`
- [ ] Light theme CSS custom properties updated in `globals.css`
- [ ] Shared accent colors (`:root`) updated from violet/fuchsia to coral/steel-blue
- [ ] RGB values for gradients updated (`--rgb-coral`, `--rgb-steel-blue`, etc.)
- [ ] `glass-card` shadow color updated for both themes
- [ ] `glow-*-hover` utilities updated to use coral/steel-blue glow
- [ ] `gradient-border-*` utilities updated to coral-to-steel-blue
- [ ] `gradient-text` utility updated
- [ ] Button gradient variants updated
- [ ] Focus ring color updated (`--ring`)
- [ ] Tailwind config `colors` and `backgroundImage` extended theme updated
- [ ] All hardcoded violet/fuchsia hex values in components replaced with CSS custom properties
- [ ] Design tokens documentation (`design-tokens.md`) updated with new palette
- [ ] WCAG AA contrast verified for all text/background pairs in both themes
- [ ] No visual regression in existing pages (dashboard, broker list, login)

## Data Visualization Palette

The timeline mini-visualizations (S0003) and chapter overlays (S0004) need a coordinated data palette that works on both dark and light backgrounds:

| Semantic | Dark Mode | Light Mode | Usage |
|----------|-----------|------------|-------|
| `--data-primary` | Muted coral #e06070 | Muted coral #d4726a | Primary data series, donut primary segment |
| `--data-secondary` | Steel blue #5080a5 | Steel blue #6a9ab8 | Secondary data series, bar chart secondary |
| `--data-tertiary` | Warm amber #d4a054 | Warm amber #c08840 | Third series, SLA "approaching" band |
| `--data-quaternary` | Sage green #6aaa7a | Sage green #5a9068 | Fourth series, "on-time" states |
| `--data-muted` | Slate #4a5568 | Cool gray #a0aab4 | Background segments, "other" category |
| `--data-danger` | Soft red #d05050 | Soft red #c04848 | Overdue, bottleneck states |

These map to the accent palette visible in pipeline5.png and pipeline4.png and provide enough range for multi-segment donuts, bar charts, and gauges without clashing.

## Data Requirements

No backend data. This story is purely frontend design tokens and CSS.

## Role-Based Visibility

All roles see the same theme. Theme preference is user-level (stored in browser/OS), not role-scoped.

## Non-Functional Expectations

- Performance: Theme switch must complete within 200ms (CSS transition, no layout recalculation). No FOUC (flash of unstyled content) on page load.
- Accessibility: All text/background combinations meet WCAG AA (4.5:1 for normal text, 3:1 for large text). Focus indicators remain visible in both themes. Gradient text has a solid fallback for high-contrast mode.
- Browser support: CSS custom properties work in all target browsers. No IE11 requirement.

## Dependencies

**Depends On:** None — this is the foundational story.

**Blocks:**
- F0013-S0001 — Glass-card restoration uses the new glow/accent colors
- F0013-S0002 — Timeline spine and flow ribbon colors
- F0013-S0003 — Mini-visualization accent palette
- F0013-S0004 — Chapter emphasis ring colors
- F0013-S0005 — Responsive/a11y verification uses the final palette

## Out of Scope

- Changing the font family (Sora + JetBrains Mono remain)
- Redesigning component layouts or shapes
- Adding new components — this story only changes colors/tokens on existing components
- Dark/light theme toggle UX (existing ThemeSwitcher component stays as-is)

## UI/UX Notes

- The palette shift is from "neon SaaS" to "editorial data journalism." Think NYT interactive data pages, not Stripe dashboard.
- Muted coral is warm but not aggressive. Steel blue is cool but not cold. Together they feel professional and calm.
- The coral/steel-blue pair is the same hue family in both themes, just shifted for contrast — users switching themes feel continuity, not a jarring palette swap.
- Gradients shift from violet→fuchsia to coral→steel-blue. The angle and blur remain the same.

## Questions & Assumptions

**Assumptions:**
- The existing CSS custom property architecture (HSL values in `globals.css`, consumed via `hsl(var(--token))` in Tailwind) is preserved — only the values change
- No component logic changes — only CSS token values and any hardcoded hex references
- The data visualization palette (6 semantic colors) is sufficient for all chart types in S0003/S0004

## Definition of Done

- [ ] Acceptance criteria met for both dark and light themes
- [ ] Design tokens documentation updated
- [ ] All existing pages render correctly with new palette
- [ ] WCAG AA contrast verified
- [ ] No hardcoded old palette hex values remain in component code
- [ ] Tests pass
- [ ] Story filename matches Story ID prefix
- [ ] Story index regenerated
