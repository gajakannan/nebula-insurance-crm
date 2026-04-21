import { cn } from '@/lib/utils';

interface TextInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
}

export function TextInput({ label, error, className, id, ...props }: TextInputProps) {
  const inputId = id ?? label.toLowerCase().replace(/\s+/g, '-');

  return (
    <div className="space-y-1.5">
      <label htmlFor={inputId} className="block text-xs font-medium text-text-secondary">
        {label}
        {props.required && <span className="ml-0.5 text-status-error">*</span>}
      </label>
      <input
        id={inputId}
        className={cn(
          'w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted transition-colors',
          'focus:outline-none focus:ring-1',
          error
            ? 'border-status-error focus:ring-status-error'
            : 'focus:ring-nebula-violet',
          className,
        )}
        {...props}
      />
      {error && <p className="text-xs text-status-error">{error}</p>}
    </div>
  );
}
