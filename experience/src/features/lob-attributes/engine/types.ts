import type { ComponentType, ReactNode } from 'react'

/**
 * F0036 dynamic form engine — contract types (ADR-021).
 *
 * S0001 ships the contract surface only: the widget-registry shape, the engine
 * entry-component props, and the pinned-bundle reference. Concrete widgets
 * (S0002), schema-driven rendering + AJV (S0003), pin-during-edit (S0004), and
 * the F0035 preservation adapter (S0006) build on these types.
 */

/** The ADR-021 MVP widget vocabulary. Each name must resolve through the registry. */
export type WidgetName =
  | 'text'
  | 'textarea'
  | 'number'
  | 'money-minor'
  | 'select'
  | 'multi-select'
  | 'checkbox'
  | 'date'
  | 'section'
  | 'readonly-summary'

/** A select/multi-select option, derived from a data-schema `enum` (S0002/S0003). */
export interface WidgetOption {
  value: string
  label: string
}

/**
 * Props every widget receives. Widgets are controlled by the engine; in S0003+
 * the engine wires these to React Hook Form field state.
 */
export interface WidgetProps<TValue = unknown> {
  /** Dotted path into the form value object (e.g. `controls.mfaMaturity`). */
  fieldPath: string
  label: string
  value: TValue
  onChange: (next: TValue) => void
  error?: string
  required?: boolean
  disabled?: boolean
  /** Enumerated options for select/multi-select widgets. */
  options?: readonly WidgetOption[]
}

export type WidgetComponent = ComponentType<WidgetProps>

/**
 * A registry entry: the component that renders the widget plus an optional
 * option-schema validator so unknown options can fail closed (S0002).
 */
export interface WidgetRegistryEntry {
  component: WidgetComponent
  /** Returns true when `option` is permitted for this widget; defaults to allow-all. */
  isAllowedOption?: (option: string) => boolean
}

/**
 * The widget registry: the single source of widget resolution. There is no
 * inline widget fallback — unknown names fail closed (ADR-021 governance).
 */
export interface WidgetRegistry {
  register(name: WidgetName, entry: WidgetRegistryEntry): void
  /** Throws `UnknownWidgetError` when `name` is not registered. */
  resolve(name: string): WidgetRegistryEntry
  has(name: string): boolean
  registeredNames(): WidgetName[]
}

/**
 * The bundle the engine renders, pinned to a `(productVersionId, stage)` tuple
 * at open (S0004). `dataSchema` drives widget derivation; `uiSchema` supplies
 * section grouping + labels only.
 */
export interface PinnedBundle {
  productVersionId: string
  stage: string
  /** The bundle `data-schema.json` (JSON Schema). */
  dataSchema: unknown
  /** The bundle `ui-schema.json` (sections + fieldLabels). */
  uiSchema: unknown
}

/** A backend validation issue (LobValidationProblemDetails.lobErrors[] shape). */
export interface LobErrorIssue {
  code: string
  /** Backend pointer, e.g. `$.attributes.controls.mfaEnabled`. */
  path: string
  message: string
  severity?: string
}

/** Engine entry-component props. The future `DynamicAttributePanel` internals (S0005). */
export interface EngineFormProps<TValues = Record<string, unknown>> {
  bundle: PinnedBundle
  registry: WidgetRegistry
  value: TValues
  onChange?: (next: TValues) => void
  errors?: Record<string, string>
  /** Backend-authoritative validation issues (cross-field rules) bound to fields by pointer. */
  lobErrors?: LobErrorIssue[]
  readOnly?: boolean
  actions?: ReactNode
  /** Declarative presentational gating (enable/disable + required marker) — ADR-021 §4. */
  uiConditionalMap?: import('./uiConditionalMap').UiConditionalMap
  /** F0035 preservation config; when set the form registers + restores on mount (S0006). */
  preserve?: import('./FormPreservation').PreserveConfig
  /** Called with current values when the user submits a data-schema-valid form. Omit to let the host own save. */
  onSubmit?: (values: TValues) => void
  submitLabel?: string
  /** Panel heading + sub-heading (defaults: "Product attributes" / bundle id · stage). */
  title?: string
  subtitle?: string
}
