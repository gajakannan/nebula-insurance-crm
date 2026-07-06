import type { BrokerInsightMetricCard, BrokerInsightMetricKey } from '../types';

interface BrokerInsightCardsProps {
  metrics: BrokerInsightMetricCard[];
  selectedMetric: BrokerInsightMetricKey;
  onSelectMetric: (metric: BrokerInsightMetricKey) => void;
}

export function BrokerInsightCards({ metrics, selectedMetric, onSelectMetric }: BrokerInsightCardsProps) {
  return (
    <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
      {metrics.map((metric) => (
        <button
          key={metric.metricKey}
          type="button"
          onClick={() => onSelectMetric(metric.metricKey)}
          className={`min-h-32 rounded-lg border p-4 text-left transition ${
            selectedMetric === metric.metricKey
              ? 'border-accent-primary bg-surface-card-hover'
              : 'border-surface-border bg-surface-card hover:bg-surface-card-hover'
          }`}
        >
          <div className="flex items-start justify-between gap-3">
            <p className="text-sm font-medium text-text-primary">{metric.label}</p>
            <span className="rounded-full border border-surface-border px-2 py-0.5 text-[11px] text-text-muted">
              {metric.status}
            </span>
          </div>
          <p className="mt-3 text-2xl font-semibold text-text-primary">{formatMetric(metric)}</p>
          <div className="mt-3 grid grid-cols-2 gap-2 text-xs text-text-muted">
            <span>Denom {metric.denominator}</span>
            <span>Sources {metric.sourceRecordCount}</span>
            <span>Compare {formatNullable(metric.comparisonValue)}</span>
            <span>{new Date(metric.lastRefreshedAt).toLocaleDateString()}</span>
          </div>
        </button>
      ))}
    </div>
  );
}

function formatMetric(metric: BrokerInsightMetricCard): string {
  if (metric.value === null) return 'N/A';
  if (metric.unit === 'percentage') return `${metric.value.toFixed(1)}%`;
  if (metric.unit === 'currency') return `$${metric.value.toLocaleString()}`;
  return metric.value.toLocaleString();
}

function formatNullable(value: number | null): string {
  return value === null ? 'N/A' : value.toLocaleString();
}
