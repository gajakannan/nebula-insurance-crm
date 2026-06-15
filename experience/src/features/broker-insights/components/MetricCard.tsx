type MetricColor = 'success' | 'warning' | 'danger' | 'primary' | 'default';

interface MetricCardProps {
  label: string;
  value: string;
  subLabel?: string;
  color?: MetricColor;
}

const COLOR_BY_METRIC: Record<MetricColor, string> = {
  success: 'var(--color-status-success)',
  warning: 'var(--color-status-warning)',
  danger: 'var(--color-status-error)',
  primary: 'var(--accent-primary)',
  default: 'var(--text-primary)',
};

export default function MetricCard({
  label,
  value,
  subLabel,
  color = 'default',
}: MetricCardProps) {
  return (
    <div className="glass-card gradient-accent-top rounded-xl p-5">
      <p
        className="text-xs uppercase tracking-widest"
        style={{ color: 'var(--text-muted)' }}
      >
        {label}
      </p>
      <p
        className="mt-2 text-2xl font-bold"
        style={{ color: COLOR_BY_METRIC[color] }}
      >
        {value}
      </p>
      {subLabel && (
        <p
          className="mt-1 text-xs"
          style={{ color: 'var(--text-secondary)' }}
        >
          {subLabel}
        </p>
      )}
    </div>
  );
}

