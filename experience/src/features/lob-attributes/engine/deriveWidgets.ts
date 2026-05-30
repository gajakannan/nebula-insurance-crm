import type { WidgetName, WidgetOption } from './types'
import { deriveOptions } from './options'

/**
 * F0036-S0003 — derive the ordered field model from the bundle.
 *
 * Widgets are derived from `data-schema.json` (type/enum/format); `ui-schema.json`
 * supplies section order, field order, and labels only. No Cyber-specific field
 * list lives in component code (ADR-021). A field/shape the engine cannot map
 * fails closed (controlled error) rather than rendering a guessed control.
 */

export class UnmappableFieldError extends Error {
  constructor(public readonly fieldPath: string, reason: string) {
    super(`Cannot derive a widget for field "${fieldPath}": ${reason}`)
    this.name = 'UnmappableFieldError'
  }
}

export interface DerivedField {
  /** ui-schema field key (e.g. `controls.mfaEnabled`, `requestedLimit`). */
  fieldPath: string
  /** RHF value path the widget binds to (money objects bind to `<field>.amountMinor`). */
  valuePath: string
  widget: WidgetName
  label: string
  required: boolean
  options?: WidgetOption[]
}

export interface DerivedSection {
  id: string
  title: string
  fields: DerivedField[]
}

interface JsonSchemaNode {
  type?: string | string[]
  enum?: unknown[]
  format?: string
  properties?: Record<string, JsonSchemaNode>
  required?: string[]
  items?: JsonSchemaNode
}

interface UiSchemaSection {
  id: string
  title: string
  fields: string[]
}

function hasType(node: JsonSchemaNode, wanted: string): boolean {
  return Array.isArray(node.type) ? node.type.includes(wanted) : node.type === wanted
}

/** Walk `dataSchema.properties` along a dotted path; returns the node + its parent (for `required`). */
function resolveNode(
  root: JsonSchemaNode,
  dottedPath: string,
): { node: JsonSchemaNode; parent: JsonSchemaNode; key: string } {
  const parts = dottedPath.split('.')
  let parent = root
  let node: JsonSchemaNode | undefined = root
  for (let i = 0; i < parts.length; i += 1) {
    const key = parts[i]
    const props: Record<string, JsonSchemaNode> | undefined = node?.properties
    if (!props || !props[key]) {
      throw new UnmappableFieldError(dottedPath, `no data-schema property at "${parts.slice(0, i + 1).join('.')}"`)
    }
    parent = node as JsonSchemaNode
    node = props[key]
  }
  return { node: node as JsonSchemaNode, parent, key: parts[parts.length - 1] }
}

function isMoneyObject(node: JsonSchemaNode): boolean {
  return hasType(node, 'object') && !!node.properties?.amountMinor
}

function enumStrings(node: JsonSchemaNode): string[] {
  return (node.enum ?? []).filter((v): v is string => typeof v === 'string')
}

function deriveWidget(fieldPath: string, node: JsonSchemaNode): WidgetName {
  if (hasType(node, 'boolean')) return 'checkbox'
  if (Array.isArray(node.enum) && node.enum.length > 0 && (hasType(node, 'string') || node.type === undefined))
    return 'select'
  if (hasType(node, 'string') && node.format === 'date') return 'date'
  if (hasType(node, 'string')) return 'text'
  if (hasType(node, 'integer') || hasType(node, 'number')) return 'number'
  if (isMoneyObject(node)) return 'money-minor'
  if (hasType(node, 'array')) return 'multi-select'
  throw new UnmappableFieldError(fieldPath, `unsupported schema node type ${JSON.stringify(node.type)}`)
}

export function deriveSections(dataSchema: unknown, uiSchema: unknown): DerivedSection[] {
  const root = (dataSchema ?? {}) as JsonSchemaNode
  const ui = (uiSchema ?? {}) as { sections?: UiSchemaSection[]; fieldLabels?: Record<string, string> }
  const labels = ui.fieldLabels ?? {}
  if (!Array.isArray(ui.sections)) {
    throw new UnmappableFieldError('<root>', 'ui-schema has no sections array')
  }

  return ui.sections.map((section) => ({
    id: section.id,
    title: section.title,
    fields: section.fields.map((fieldPath) => {
      const { node, parent, key } = resolveNode(root, fieldPath)
      const widget = deriveWidget(fieldPath, node)
      const required = Array.isArray(parent.required) && parent.required.includes(key)
      const label = labels[fieldPath] ?? key
      const field: DerivedField = {
        fieldPath,
        valuePath: widget === 'money-minor' ? `${fieldPath}.amountMinor` : fieldPath,
        widget,
        label,
        required,
      }
      if (widget === 'select' || widget === 'multi-select') {
        const enumNode = widget === 'multi-select' ? (node.items ?? node) : node
        field.options = deriveOptions(enumStrings(enumNode))
      }
      return field
    }),
  }))
}
