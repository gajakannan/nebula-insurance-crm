import { Select } from '@/components/ui/Select'
import { TextInput } from '@/components/ui/TextInput'
import type {
  DocumentMetadata,
  DocumentMetadataJsonSchema,
  DocumentMetadataPropertySchema,
  DocumentMetadataValue,
} from '../types'

interface DocumentMetadataFieldsProps {
  schema?: DocumentMetadataJsonSchema
  value: DocumentMetadata
  onChange: (value: DocumentMetadata) => void
}

export function DocumentMetadataFields({ schema, value, onChange }: DocumentMetadataFieldsProps) {
  const properties = schema?.properties ?? {}
  const entries = Object.entries(properties)
  if (entries.length === 0) return null

  return (
    <div className="grid gap-4 md:grid-cols-2">
      {entries.map(([name, property]) => (
        <Field
          key={name}
          name={name}
          property={property}
          value={value[name]}
          required={schema?.required?.includes(name) ?? false}
          onChange={(next) => onChange({ ...value, [name]: next })}
        />
      ))}
    </div>
  )
}

function Field({
  name,
  property,
  value,
  required,
  onChange,
}: {
  name: string
  property: DocumentMetadataPropertySchema
  value: DocumentMetadataValue | undefined
  required: boolean
  onChange: (value: DocumentMetadataValue) => void
}) {
  const label = property.title ?? labelize(name)

  if (property.enum?.length) {
    return (
      <Select
        label={label}
        value={typeof value === 'string' ? value : ''}
        required={required}
        placeholder={required ? undefined : 'None'}
        onChange={(event) => onChange(event.target.value || null)}
        options={property.enum.map((item) => ({ value: item, label: item }))}
      />
    )
  }

  if (allows(property, 'boolean')) {
    return (
      <label className="flex min-h-[58px] items-center gap-2 rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-secondary">
        <input
          type="checkbox"
          checked={value === true}
          onChange={(event) => onChange(event.target.checked)}
          className="h-4 w-4 rounded border-surface-border text-nebula-violet focus:ring-nebula-violet"
        />
        <span>
          {label}
          {required && <span className="ml-0.5 text-status-error">*</span>}
        </span>
      </label>
    )
  }

  if (allows(property, 'integer') || allows(property, 'number')) {
    return (
      <TextInput
        label={label}
        type="number"
        required={required}
        value={typeof value === 'number' ? String(value) : ''}
        min={property.minimum}
        max={property.maximum}
        onChange={(event) => {
          const next = event.target.value
          onChange(next === '' ? null : Number(next))
        }}
      />
    )
  }

  return (
    <TextInput
      label={label}
      type={property.format === 'date' ? 'date' : 'text'}
      required={required}
      value={typeof value === 'string' ? value : ''}
      maxLength={property.maxLength}
      onChange={(event) => onChange(event.target.value || null)}
    />
  )
}

function allows(property: DocumentMetadataPropertySchema, type: string) {
  if (!property.type) return type === 'string'
  return Array.isArray(property.type) ? property.type.includes(type) : property.type === type
}

function labelize(value: string) {
  return value
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/[-_]/g, ' ')
    .replace(/\b\w/g, (char) => char.toUpperCase())
}
