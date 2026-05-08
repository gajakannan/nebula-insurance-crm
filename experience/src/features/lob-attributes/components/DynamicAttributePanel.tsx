import { Select } from '@/components/ui/Select'
import { TextInput } from '@/components/ui/TextInput'
import {
  CYBER_MFA_MATURITY_OPTIONS,
  CYBER_REVENUE_BANDS,
  CYBER_TRAINING_FREQUENCY_OPTIONS,
  isCyberLineOfBusiness,
} from '../lib/cyber'
import type { CyberLobAttributeValues } from '../types'
import { useCyberSchemaBundle } from '../hooks/useLobSchemaBundle'
import type { ReactNode } from 'react'

interface DynamicAttributePanelProps {
  lineOfBusiness: string | null | undefined
  value: CyberLobAttributeValues
  onChange?: (value: CyberLobAttributeValues) => void
  errors?: Record<string, string>
  readOnly?: boolean
  actions?: ReactNode
}

export function DynamicAttributePanel({
  lineOfBusiness,
  value,
  onChange,
  errors = {},
  readOnly = false,
  actions,
}: DynamicAttributePanelProps) {
  const visible = isCyberLineOfBusiness(lineOfBusiness)
  const bundleQuery = useCyberSchemaBundle(visible)

  if (!visible) return null

  function updateField(field: keyof CyberLobAttributeValues, nextValue: string | boolean) {
    if (!onChange) return
    onChange({ ...value, [field]: nextValue })
  }

  const schemaStatus = bundleQuery.data?.status ?? (bundleQuery.isError ? 'Unavailable' : 'Loading')

  return (
    <section className="space-y-4 rounded-lg border border-surface-border bg-surface-card/50 p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h3 className="text-sm font-semibold text-text-primary">Cyber attributes</h3>
          <p className="mt-1 text-xs text-text-muted">
            Bundle 1.0.0 · {schemaStatus}
          </p>
        </div>
        {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Select
          label="Revenue band"
          required={!readOnly}
          disabled={readOnly}
          value={value.revenueBand}
          onChange={(event) => updateField('revenueBand', event.target.value)}
          error={errors.revenueBand}
          placeholder="Select band"
          options={CYBER_REVENUE_BANDS}
        />

        <TextInput
          label="Records held"
          required={!readOnly}
          disabled={readOnly}
          type="number"
          min="0"
          step="1"
          value={value.recordsHeld}
          onChange={(event) => updateField('recordsHeld', event.target.value)}
          error={errors.recordsHeld}
        />

        <TextInput
          label="Requested limit"
          required={!readOnly}
          disabled={readOnly}
          type="number"
          min="0"
          step="1000"
          value={value.requestedLimit}
          onChange={(event) => updateField('requestedLimit', event.target.value)}
          error={errors.requestedLimit}
        />

        <TextInput
          label="Requested retention"
          required={!readOnly}
          disabled={readOnly}
          type="number"
          min="0"
          step="1000"
          value={value.requestedRetention}
          onChange={(event) => updateField('requestedRetention', event.target.value)}
          error={errors.requestedRetention}
        />
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <BooleanField
          label="MFA enabled"
          checked={value.mfaEnabled}
          disabled={readOnly}
          error={errors.mfaEnabled}
          onChange={(checked) => updateField('mfaEnabled', checked)}
        />
        <BooleanField
          label="EDR enabled"
          checked={value.edrEnabled}
          disabled={readOnly}
          onChange={(checked) => updateField('edrEnabled', checked)}
        />
        <BooleanField
          label="Offline backups"
          checked={value.backupEnabled}
          disabled={readOnly}
          onChange={(checked) => updateField('backupEnabled', checked)}
        />
        <Select
          label="Training frequency"
          required={!readOnly}
          disabled={readOnly}
          value={value.trainingFrequency}
          onChange={(event) => updateField('trainingFrequency', event.target.value)}
          error={errors.trainingFrequency}
          placeholder="Select frequency"
          options={CYBER_TRAINING_FREQUENCY_OPTIONS}
        />
        <Select
          label="MFA maturity"
          required={!readOnly && value.mfaEnabled}
          disabled={readOnly || !value.mfaEnabled}
          value={value.mfaMaturity}
          onChange={(event) => updateField('mfaMaturity', event.target.value)}
          error={errors.mfaMaturity}
          placeholder="Select maturity"
          options={CYBER_MFA_MATURITY_OPTIONS}
        />
      </div>
    </section>
  )
}

function BooleanField({
  label,
  checked,
  disabled,
  error,
  onChange,
}: {
  label: string
  checked: boolean
  disabled?: boolean
  error?: string
  onChange: (checked: boolean) => void
}) {
  return (
    <label className="space-y-1.5">
      <span className="block text-xs font-medium text-text-secondary">{label}</span>
      <span className="flex min-h-10 items-center gap-2 rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary">
        <input
          type="checkbox"
          checked={checked}
          disabled={disabled}
          onChange={(event) => onChange(event.target.checked)}
          className="h-4 w-4 rounded border-surface-border text-nebula-violet focus:ring-nebula-violet"
        />
        <span>{checked ? 'Yes' : 'No'}</span>
      </span>
      {error && <span className="block text-xs text-status-error">{error}</span>}
    </label>
  )
}
