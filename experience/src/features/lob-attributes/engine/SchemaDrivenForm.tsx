import { useEffect, useMemo, useState } from 'react'
import { Controller, useForm, useWatch, type FieldValues, type Path } from 'react-hook-form'
import type { FormSnapshotRecord } from '@/features/session-continuity'
import type { EngineFormProps } from './types'
import { deriveSections, UnmappableFieldError, type DerivedField } from './deriveWidgets'
import { createDataSchemaValidator } from './ajvValidator'
import { evaluateConditional } from './uiConditionalMap'
import { FormPreservation } from './FormPreservation'

/**
 * F0036-S0003 — schema-driven Cyber form.
 *
 * Renders fields/sections/order/labels entirely from the pinned bundle
 * (`data-schema.json` + `ui-schema.json`) through the widget registry — no
 * Cyber-specific field list in component code. React Hook Form holds field
 * state; client AJV validates the data-schema layer and blocks the form's own
 * submit while invalid. Cross-field rules are backend-authoritative and bound to
 * fields from `lobErrors[]` by pointer.
 */

function pointerToDotted(pointer: string): string {
  return pointer.replace(/^\$\.attributes\.?/, '')
}

function errorMessageFor(
  field: DerivedField,
  clientMap: Map<string, string>,
  backendMap: Map<string, string>,
): string | undefined {
  return (
    clientMap.get(field.valuePath) ??
    clientMap.get(field.fieldPath) ??
    backendMap.get(field.valuePath) ??
    backendMap.get(field.fieldPath)
  )
}

export function SchemaDrivenForm({
  bundle,
  registry,
  value,
  onChange,
  lobErrors = [],
  readOnly = false,
  actions,
  uiConditionalMap,
  preserve,
  onSubmit,
  submitLabel = 'Save',
  title = 'Product attributes',
  subtitle,
}: EngineFormProps) {
  // RHF owns field state (so dirty tracking survives for F0035 snapshotting,
  // S0006). External `value` changes (entity swap, restore) are synced via the
  // effect below; the user's own edits round-trip back as a `value` equal to the
  // form's values, so no reset fires and dirty state is preserved.
  const form = useForm<FieldValues>({ defaultValues: value as FieldValues })
  const { control } = form
  const [restored, setRestored] = useState(false)

  useEffect(() => {
    if (JSON.stringify(form.getValues()) !== JSON.stringify(value)) {
      form.reset(value as FieldValues)
    }
  }, [value, form])

  // F0035 restore (S0006): rehydrate snapshot values back through the host's
  // onChange — never auto-saved; the user re-saves explicitly.
  function handleRestore(record: FormSnapshotRecord<FieldValues>) {
    onChange?.(record.form_values)
    setRestored(true)
  }

  // Derive the field model + compile the validator once per bundle. A bundle the
  // engine cannot map fails closed (controlled error), never a guessed render.
  const derived = useMemo(() => {
    try {
      return { sections: deriveSections(bundle.dataSchema, bundle.uiSchema), error: null as string | null }
    } catch (err) {
      if (err instanceof UnmappableFieldError) {
        return { sections: [], error: err.message }
      }
      throw err
    }
  }, [bundle.dataSchema, bundle.uiSchema])

  const validator = useMemo(() => createDataSchemaValidator(bundle.dataSchema), [bundle.dataSchema])

  const watched = (useWatch({ control }) ?? value) as Record<string, unknown>
  const clientErrors = useMemo(() => validator.validate(watched), [validator, watched])

  const clientMap = useMemo(() => {
    const map = new Map<string, string>()
    for (const e of clientErrors) map.set(pointerToDotted(e.pointer), e.message)
    return map
  }, [clientErrors])

  const backendMap = useMemo(() => {
    const map = new Map<string, string>()
    for (const e of lobErrors) map.set(pointerToDotted(e.path), e.message)
    return map
  }, [lobErrors])

  const isValid = clientErrors.length === 0
  const Section = registry.resolve('section').component

  if (derived.error) {
    return (
      <section className="rounded-lg border border-status-error bg-surface-card/50 p-4" role="alert">
        <p className="text-sm text-status-error">Unable to render attributes: {derived.error}</p>
      </section>
    )
  }

  function propagate() {
    onChange?.(form.getValues())
  }

  return (
    <section className="space-y-4 rounded-lg border border-surface-border bg-surface-card/50 p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h3 className="text-sm font-semibold text-text-primary">{title}</h3>
          <p className="mt-1 text-xs text-text-muted">
            {subtitle ?? `Bundle ${bundle.productVersionId} · ${bundle.stage}`}
          </p>
        </div>
        {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
      </div>

      {preserve && <FormPreservation form={form} preserve={preserve} onRestore={handleRestore} />}
      {restored && (
        <p
          role="status"
          className="rounded-md border border-surface-border bg-surface-card px-3 py-2 text-xs text-text-secondary"
        >
          We saved your edits while you signed in. Click Save when ready.
        </p>
      )}

      {derived.sections.map((section) => (
        <fieldset key={section.id} className="space-y-3">
          <Section fieldPath={`section.${section.id}`} label={section.title} value={undefined} onChange={() => {}} />
          <div className="grid gap-4 md:grid-cols-2">
            {section.fields.map((field) => {
              const Widget = registry.resolve(field.widget).component
              // ADR-021 §4: presentational gating from the declarative conditional map,
              // applied generically (no ad-hoc per-field logic).
              const cond = evaluateConditional(uiConditionalMap, field.fieldPath, watched)
              const disabled = readOnly || (cond.gated && !cond.enabled)
              const required = cond.gated ? cond.enabled : field.required
              return (
                <Controller
                  key={field.fieldPath}
                  name={field.valuePath as Path<FieldValues>}
                  control={control}
                  render={({ field: rhf }) => (
                    <Widget
                      fieldPath={field.fieldPath}
                      label={field.label}
                      value={rhf.value}
                      onChange={(next) => {
                        rhf.onChange(next)
                        propagate()
                      }}
                      error={errorMessageFor(field, clientMap, backendMap)}
                      required={required}
                      disabled={disabled}
                      options={field.options}
                    />
                  )}
                />
              )
            })}
          </div>
        </fieldset>
      ))}

      {!readOnly && onSubmit && (
        <div className="flex justify-end">
          <button
            type="button"
            disabled={!isValid}
            aria-disabled={!isValid}
            onClick={() => {
              if (isValid) onSubmit?.(form.getValues())
            }}
            className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-text-primary disabled:cursor-not-allowed disabled:opacity-50"
          >
            {submitLabel}
          </button>
        </div>
      )}
    </section>
  )
}
