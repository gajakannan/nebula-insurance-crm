# Nebula Design Tokens - Editorial Palette (F0013)

**Version:** 3.0  
**Last Updated:** 2026-03-16  
**Applies To:** Frontend Developer

## Overview

This document defines the editorial palette used by F0013. The visual direction is muted and data-journalism oriented:

- Dark theme: deep navy frame with coral and steel-blue accents.
- Light theme: warm gray frame with the same accent pair, adjusted for contrast.
- Status colors (success/warning/error) remain unchanged.

## Primary Theme Palette

### Dark Theme

| Token | Value | Notes |
|---|---|---|
| Background | `#1a2332` | App frame and base surface |
| Elevated Surface | `#243044` | Cards and elevated panels |
| Panel | `#2a3650` | Hover/elevated panel state |
| Border | `#2e3a4e` | Surface and chrome borders |
| Text Primary | `#e8e8e8` | Main body text |
| Text Secondary | `#a0a8b4` | Supporting labels |
| Accent Primary | `#e06070` | Coral, primary interactive emphasis |
| Accent Secondary | `#5080a5` | Steel blue, secondary emphasis |
| Focus Ring | `#e06070` | Keyboard focus ring color |

### Light Theme

| Token | Value | Notes |
|---|---|---|
| Background | `#f0eded` | App frame and base surface |
| Elevated Surface | `#ffffff` | Cards and elevated panels |
| Panel | `#f5f2f0` | Hover/elevated panel state |
| Border | `#d8d2ce` | Surface and chrome borders |
| Text Primary | `#3a3a3a` | Main body text |
| Text Secondary | `#7a7a7a` | Supporting labels |
| Accent Primary | `#d4726a` | Coral, primary interactive emphasis |
| Accent Secondary | `#6a9ab8` | Steel blue, secondary emphasis |
| Focus Ring | `#d4726a` | Keyboard focus ring color |

## Data Visualization Palette

Use these semantic tokens for charts, popovers, and timeline mini-visualizations.

### Dark

| Semantic | Value |
|---|---|
| `--data-primary` | `#e06070` |
| `--data-secondary` | `#5080a5` |
| `--data-tertiary` | `#d4a054` |
| `--data-quaternary` | `#6aaa7a` |
| `--data-muted` | `#4a5568` |
| `--data-danger` | `#d05050` |

### Light

| Semantic | Value |
|---|---|
| `--data-primary` | `#d4726a` |
| `--data-secondary` | `#6a9ab8` |
| `--data-tertiary` | `#c08840` |
| `--data-quaternary` | `#5a9068` |
| `--data-muted` | `#a0aab4` |
| `--data-danger` | `#c04848` |

## CSS Token Contract

Source of truth: `experience/src/index.css`.

### Runtime variables

- Theme runtime variables are defined in `:root` (dark default) and overridden in `[data-theme="light"]`.
- Accent aliases:
  - `--accent-primary` (coral)
  - `--accent-secondary` (steel blue)
  - `--accent-tertiary` (warm amber)
  - `--ring` (focus ring)
- Data aliases:
  - `--data-primary`, `--data-secondary`, `--data-tertiary`, `--data-quaternary`, `--data-muted`, `--data-danger`
- Story panel callout border:
  - `--callout-border` — solid border for ghost-bordered story panels. Dark mode: blue (`--accent-secondary`) at 70% opacity. Light mode: salmon (`--accent-primary`) at 70% opacity. Intentionally swapped from accent defaults to create visual contrast with the accent color already used on the spine/anchors.

### Tailwind token bridge (`@theme`)

`@theme` maps runtime variables into utility classes, including:

- Brand aliases used across the app:
  - `--color-nebula-violet: var(--accent-primary)`
  - `--color-nebula-fuchsia: var(--accent-secondary)`
- Data aliases:
  - `--color-data-primary` ... `--color-data-danger`
- Glow/focus aliases:
  - `--color-glow-violet`, `--color-glow-fuchsia`, `--color-ring`

This keeps component usage stable while allowing theme-level palette refreshes.

## Inset Content Container (sidebar-08 pattern)

The main content area uses an inset container inspired by shadcn sidebar-08:

| Property | Value | Notes |
|---|---|---|
| Background | `var(--shell-inset-bg)` | Slightly different from frame background |
| Border | `1px solid var(--sidebar-border)` | Visible container edge |
| Border radius | `0.75rem` (12px) | Rounded corners |
| Shadow | `var(--shell-inset-shadow)` | Subtle inset depth |
| Gap | `0.75rem` padding on `.lg-sidebar-offset` | Space between sidebars and content border |

Both `.content-inset` and `.content-shell-flat` receive the same inset treatment. Left nav and right Neuron rail remain flush to viewport edges.

## Utility Expectations

- `glass-card` hover glow uses editorial coral/steel tones.
- `gradient-accent-top` and `gradient-accent-left` use coral -> steel-blue gradients.
- `skeleton-glow` midpoint uses coral-toned highlight.
- Active emphasis shadows should come from tokenized utilities (for example `shadow-brand-active`) rather than hardcoded legacy violet/fuchsia values.

## Accessibility Guardrails

- Normal text contrast target: `>= 4.5:1`.
- Large text contrast target: `>= 3:1`.
- Focus rings must remain visible in both themes.
- Theme transitions should remain smooth (`200ms`) without flash artifacts.

## Migration Notes

- Legacy neon references (`#8b5cf6`, `#d946ef`) are deprecated for F0013.
- Existing class names like `text-nebula-violet` remain valid but now resolve to editorial coral via token mapping.
