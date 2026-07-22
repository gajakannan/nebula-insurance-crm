import { useId } from 'react'
import { ApiError } from '@/services/api'

const controlClass = 'h-11 w-full rounded-lg border border-surface-border bg-surface-card px-3 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-nebula-violet disabled:opacity-50'

export function TextField({
  label,
  value,
  onChange,
  type = 'text',
  required = false,
  min,
  step,
  placeholder,
}: {
  label: string
  value: string | number
  onChange: (value: string) => void
  type?: 'text' | 'number' | 'date' | 'search'
  required?: boolean
  min?: number
  step?: string
  placeholder?: string
}) {
  const id = useId()
  return (
    <label htmlFor={id} className="grid gap-1 text-xs font-medium text-text-muted">
      {label}
      <input
        id={id}
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        required={required}
        min={min}
        step={step}
        placeholder={placeholder}
        className={controlClass}
      />
    </label>
  )
}

export function SelectField({ label, value, options, onChange }: {
  label: string
  value: string
  options: readonly string[]
  onChange: (value: string) => void
}) {
  const id = useId()
  return (
    <label htmlFor={id} className="grid gap-1 text-xs font-medium text-text-muted">
      {label}
      <select id={id} value={value} onChange={(event) => onChange(event.target.value)} className={controlClass}>
        {options.map((option) => <option key={option} value={option}>{option}</option>)}
      </select>
    </label>
  )
}

export function MutationFeedback({
  isPending,
  isSuccess,
  error,
  pending,
  success,
  failure,
}: {
  isPending: boolean
  isSuccess: boolean
  error: unknown
  pending: string
  success: string
  failure: string
}) {
  if (isPending) return <p role="status" className="text-sm text-text-muted">{pending}</p>
  if (isSuccess) return <p role="status" className="text-sm text-status-success">{success}</p>
  if (!error) return null
  const detail = error instanceof ApiError ? error.problem?.detail : error instanceof Error ? error.message : null
  return (
    <p role="alert" className="rounded-lg border border-status-error/35 bg-status-error/15 px-3 py-2 text-sm text-text-primary">
      {detail || failure} Retry after refreshing current records if the change may have become stale.
    </p>
  )
}
