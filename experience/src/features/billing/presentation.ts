export function money(value: number, currency = 'USD') {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(value)
}

export function dateTime(value: string) {
  return new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

export const primaryButtonClass = 'inline-flex min-h-11 items-center justify-center gap-2 rounded-lg bg-nebula-violet px-4 text-sm font-medium text-white disabled:cursor-not-allowed disabled:opacity-50'
export const secondaryButtonClass = 'inline-flex min-h-11 items-center justify-center gap-2 rounded-lg border border-surface-border bg-surface-card px-4 text-sm font-medium text-text-secondary hover:text-text-primary disabled:cursor-not-allowed disabled:opacity-50'
