import { cn } from '@/lib/utils';

interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label: string;
  error?: string;
  options: { value: string; label: string }[];
  placeholder?: string;
}

export function Select({ label, error, options, placeholder, className, id, ...props }: SelectProps) {
  const selectId = id ?? label.toLowerCase().replace(/\s+/g, '-');

  return (
    <div className="space-y-1.5">
      <label htmlFor={selectId} className="block text-xs font-medium text-text-secondary">
        {label}
        {props.required && <span className="ml-0.5 text-status-error">*</span>}
      </label>
      <select
        id={selectId}
        className={cn(
          'w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary transition-colors',
          'focus:outline-none focus:ring-1',
          error
            ? 'border-status-error focus:ring-status-error'
            : 'focus:ring-nebula-violet',
          className,
        )}
        {...props}
      >
        {placeholder && (
          <option value="" className="text-text-muted">
            {placeholder}
          </option>
        )}
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>
      {error && <p className="text-xs text-status-error">{error}</p>}
    </div>
  );
}
