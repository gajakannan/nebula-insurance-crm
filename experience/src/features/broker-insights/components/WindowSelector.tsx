interface WindowSelectorProps {
  value: number;
  onChange: (days: number) => void;
  options?: number[];
  formatLabel?: (value: number) => string;
  ariaLabel?: string;
}

export default function WindowSelector({
  value,
  onChange,
  options = [90, 180, 365],
  formatLabel = (days) => `${days}d`,
  ariaLabel = 'Select analytics window',
}: WindowSelectorProps) {
  return (
    <div className="inline-flex" role="group" aria-label={ariaLabel}>
      {options.map((option, index) => {
        const active = option === value;
        return (
          <button
            key={option}
            type="button"
            onClick={() => onChange(option)}
            aria-pressed={active}
            className={[
              'px-3 py-1.5 text-xs font-semibold transition-colors',
              index === 0 ? 'rounded-l-lg' : '-ml-px',
              index === options.length - 1 ? 'rounded-r-lg' : '',
            ].join(' ')}
            style={{
              background: active
                ? 'color-mix(in srgb, var(--accent-primary) 18%, transparent)'
                : 'transparent',
              color: active ? 'var(--accent-primary)' : 'var(--text-muted)',
              border: active
                ? '1px solid color-mix(in srgb, var(--accent-primary) 35%, transparent)'
                : '1px solid var(--surface-border)',
            }}
          >
            {formatLabel(option)}
          </button>
        );
      })}
    </div>
  );
}

