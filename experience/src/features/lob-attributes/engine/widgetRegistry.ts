import type {
  WidgetName,
  WidgetRegistry,
  WidgetRegistryEntry,
} from './types'

/**
 * Raised when the engine is asked to resolve a widget (or option) that is not
 * registered. Per ADR-021, the frontend mirrors the backend's "bundle
 * activation fails on unknown widget" rule: the engine fails closed rather than
 * silently rendering an unverified fallback input.
 */
export class UnknownWidgetError extends Error {
  constructor(public readonly widgetName: string) {
    super(`Unknown widget: "${widgetName}" is not registered in the widget registry`)
    this.name = 'UnknownWidgetError'
  }
}

/**
 * Raised when a select/multi-select option is not permitted by the widget's
 * option schema. Also fails closed (ADR-021 "unknown option").
 */
export class UnknownWidgetOptionError extends Error {
  constructor(
    public readonly widgetName: string,
    public readonly option: string,
  ) {
    super(`Unknown option: "${option}" is not allowed for widget "${widgetName}"`)
    this.name = 'UnknownWidgetOptionError'
  }
}

/**
 * Creates a widget registry — the single source of widget resolution for the
 * engine. There is intentionally no inline fallback: `resolve` throws on an
 * unregistered name so callers cannot accidentally render around an unknown
 * widget.
 */
export function createWidgetRegistry(): WidgetRegistry {
  const entries = new Map<WidgetName, WidgetRegistryEntry>()

  return {
    register(name: WidgetName, entry: WidgetRegistryEntry): void {
      entries.set(name, entry)
    },
    resolve(name: string): WidgetRegistryEntry {
      const entry = entries.get(name as WidgetName)
      if (!entry) {
        throw new UnknownWidgetError(name)
      }
      return entry
    },
    has(name: string): boolean {
      return entries.has(name as WidgetName)
    },
    registeredNames(): WidgetName[] {
      return [...entries.keys()]
    },
  }
}

/**
 * Resolves the option set for a widget, failing closed when the widget rejects
 * an option. Concrete option schemas are supplied by the widgets in S0002.
 */
export function assertOptionAllowed(
  registry: WidgetRegistry,
  widgetName: WidgetName,
  option: string,
): void {
  const entry = registry.resolve(widgetName)
  if (entry.isAllowedOption && !entry.isAllowedOption(option)) {
    throw new UnknownWidgetOptionError(widgetName, option)
  }
}
