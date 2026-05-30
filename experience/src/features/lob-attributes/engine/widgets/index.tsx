import { cn } from '@/lib/utils'
import type { ReactNode } from 'react'
import type { WidgetProps, WidgetRegistry } from '../types'

/**
 * F0036-S0002 — the ADR-021 MVP widget vocabulary.
 *
 * Ten governed widgets, each a thin theme-aware wrapper over the existing
 * shadcn/Tailwind primitives. Every widget is engine-controlled (value +
 * onChange), exposes a required affordance + an error slot wired for AJV
 * (S0003), and is keyboard-operable with programmatically-associated labels and
 * aria-described errors. Only these ten are registered (governed vocabulary);
 * unknown widgets/options fail closed (S0001 registry).
 */

function fieldId(fieldPath: string): string {
  return `widget-${fieldPath.replace(/[^\w-]/g, '-')}`
}

const controlBase =
  'w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted transition-colors focus:outline-none focus:ring-1'

function controlClass(error?: string): string {
  return cn(controlBase, error ? 'border-status-error focus:ring-status-error' : 'focus:ring-nebula-violet')
}

/** Shared label + required-marker + error-slot shell with aria wiring. */
function FieldShell({
  id,
  label,
  required,
  error,
  children,
}: {
  id: string
  label: string
  required?: boolean
  error?: string
  children: ReactNode
}) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-xs font-medium text-text-secondary">
        {label}
        {required && (
          <span aria-hidden="true" className="ml-0.5 text-status-error">
            *
          </span>
        )}
      </label>
      {children}
      {error && (
        <p id={`${id}-error`} className="text-xs text-status-error" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}

function ariaProps(id: string, error?: string, required?: boolean) {
  return {
    'aria-invalid': error ? true : undefined,
    'aria-describedby': error ? `${id}-error` : undefined,
    'aria-required': required || undefined,
  }
}

export function TextWidget({ fieldPath, label, value, onChange, error, required, disabled }: WidgetProps) {
  const id = fieldId(fieldPath)
  return (
    <FieldShell id={id} label={label} required={required} error={error}>
      <input
        id={id}
        type="text"
        className={controlClass(error)}
        value={(value as string | undefined) ?? ''}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
        {...ariaProps(id, error, required)}
      />
    </FieldShell>
  )
}

export function TextareaWidget({ fieldPath, label, value, onChange, error, required, disabled }: WidgetProps) {
  const id = fieldId(fieldPath)
  return (
    <FieldShell id={id} label={label} required={required} error={error}>
      <textarea
        id={id}
        rows={3}
        className={controlClass(error)}
        value={(value as string | undefined) ?? ''}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
        {...ariaProps(id, error, required)}
      />
    </FieldShell>
  )
}

export function NumberWidget({ fieldPath, label, value, onChange, error, required, disabled }: WidgetProps) {
  const id = fieldId(fieldPath)
  return (
    <FieldShell id={id} label={label} required={required} error={error}>
      <input
        id={id}
        type="number"
        inputMode="numeric"
        className={controlClass(error)}
        value={value == null || value === '' ? '' : String(value)}
        disabled={disabled}
        onChange={(event) => {
          const raw = event.target.value
          onChange(raw === '' ? undefined : Number(raw))
        }}
        {...ariaProps(id, error, required)}
      />
    </FieldShell>
  )
}

/**
 * Stores an integer minor-unit amount (e.g. cents) and displays the major unit
 * (e.g. dollars). Round-trips without precision loss.
 */
export function MoneyMinorWidget({ fieldPath, label, value, onChange, error, required, disabled }: WidgetProps) {
  const id = fieldId(fieldPath)
  const minor = typeof value === 'number' ? value : undefined
  const major = minor == null ? '' : (minor / 100).toFixed(2)
  return (
    <FieldShell id={id} label={label} required={required} error={error}>
      <div className="flex items-center gap-2">
        <span aria-hidden="true" className="text-sm text-text-muted">
          $
        </span>
        <input
          id={id}
          type="number"
          inputMode="decimal"
          step="0.01"
          min="0"
          className={controlClass(error)}
          value={major}
          disabled={disabled}
          onChange={(event) => {
            const raw = event.target.value
            if (raw === '') {
              onChange(undefined)
              return
            }
            onChange(Math.round(Number(raw) * 100))
          }}
          {...ariaProps(id, error, required)}
        />
      </div>
    </FieldShell>
  )
}

export function SelectWidget({
  fieldPath,
  label,
  value,
  onChange,
  error,
  required,
  disabled,
  options = [],
}: WidgetProps) {
  const id = fieldId(fieldPath)
  return (
    <FieldShell id={id} label={label} required={required} error={error}>
      <select
        id={id}
        className={controlClass(error)}
        value={(value as string | undefined) ?? ''}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
        {...ariaProps(id, error, required)}
      >
        <option value="">Select…</option>
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>
    </FieldShell>
  )
}

export function MultiSelectWidget({
  fieldPath,
  label,
  value,
  onChange,
  error,
  required,
  disabled,
  options = [],
}: WidgetProps) {
  const id = fieldId(fieldPath)
  const selected = Array.isArray(value) ? (value as string[]) : []
  return (
    <fieldset className="space-y-1.5" aria-describedby={error ? `${id}-error` : undefined}>
      <legend className="block text-xs font-medium text-text-secondary">
        {label}
        {required && (
          <span aria-hidden="true" className="ml-0.5 text-status-error">
            *
          </span>
        )}
      </legend>
      <div className="space-y-1.5">
        {options.map((opt) => {
          const checked = selected.includes(opt.value)
          return (
            <label key={opt.value} className="flex items-center gap-2 text-sm text-text-primary">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-surface-border text-nebula-violet focus:ring-nebula-violet"
                checked={checked}
                disabled={disabled}
                onChange={(event) => {
                  const next = event.target.checked
                    ? [...selected, opt.value]
                    : selected.filter((entry) => entry !== opt.value)
                  onChange(next)
                }}
              />
              <span>{opt.label}</span>
            </label>
          )
        })}
      </div>
      {error && (
        <p id={`${id}-error`} className="text-xs text-status-error" role="alert">
          {error}
        </p>
      )}
    </fieldset>
  )
}

export function CheckboxWidget({ fieldPath, label, value, onChange, error, disabled }: WidgetProps) {
  const id = fieldId(fieldPath)
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="flex items-center gap-2 text-sm text-text-primary">
        <input
          id={id}
          type="checkbox"
          className="h-4 w-4 rounded border-surface-border text-nebula-violet focus:ring-nebula-violet"
          checked={value === true}
          disabled={disabled}
          onChange={(event) => onChange(event.target.checked)}
          {...ariaProps(id, error)}
        />
        <span>{label}</span>
      </label>
      {error && (
        <p id={`${id}-error`} className="text-xs text-status-error" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}

export function DateWidget({ fieldPath, label, value, onChange, error, required, disabled }: WidgetProps) {
  const id = fieldId(fieldPath)
  return (
    <FieldShell id={id} label={label} required={required} error={error}>
      <input
        id={id}
        type="date"
        className={controlClass(error)}
        value={(value as string | undefined) ?? ''}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
        {...ariaProps(id, error, required)}
      />
    </FieldShell>
  )
}

/** Layout primitive: a titled group. Field composition is the form's job (S0003). */
export function SectionWidget({ fieldPath, label }: WidgetProps) {
  return (
    <h4 id={fieldId(fieldPath)} className="text-xs font-semibold uppercase tracking-wide text-text-secondary">
      {label}
    </h4>
  )
}

/** Non-editable resolved value (read-only summary). Renders no editable control. */
export function ReadonlySummaryWidget({ fieldPath, label, value }: WidgetProps) {
  const id = fieldId(fieldPath)
  const display =
    value == null || value === '' ? '—' : Array.isArray(value) ? (value as string[]).join(', ') : String(value)
  return (
    <div className="space-y-1.5">
      <span id={`${id}-label`} className="block text-xs font-medium text-text-secondary">
        {label}
      </span>
      <p aria-labelledby={`${id}-label`} className="text-sm text-text-primary">
        {display}
      </p>
    </div>
  )
}

/** Registers the full MVP vocabulary into a registry instance. */
export function registerMvpWidgets(registry: WidgetRegistry): WidgetRegistry {
  registry.register('text', { component: TextWidget })
  registry.register('textarea', { component: TextareaWidget })
  registry.register('number', { component: NumberWidget })
  registry.register('money-minor', { component: MoneyMinorWidget })
  registry.register('select', { component: SelectWidget })
  registry.register('multi-select', { component: MultiSelectWidget })
  registry.register('checkbox', { component: CheckboxWidget })
  registry.register('date', { component: DateWidget })
  registry.register('section', { component: SectionWidget })
  registry.register('readonly-summary', { component: ReadonlySummaryWidget })
  return registry
}

export const MVP_WIDGET_NAMES = [
  'text',
  'textarea',
  'number',
  'money-minor',
  'select',
  'multi-select',
  'checkbox',
  'date',
  'section',
  'readonly-summary',
] as const
