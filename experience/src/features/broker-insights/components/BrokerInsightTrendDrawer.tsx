import { Link } from 'react-router-dom';
import type { BrokerInsightTrend } from '../types';

export function BrokerInsightTrendDrawer({ trend }: { trend: BrokerInsightTrend | undefined }) {
  if (!trend) {
    return <p className="text-sm text-text-muted">Select a broker metric to inspect trend detail.</p>;
  }

  return (
    <div className="space-y-4">
      <div className="grid gap-2 sm:grid-cols-4">
        {trend.points.map((point) => (
          <div key={`${point.bucketStart}:${point.bucketEnd}`} className="rounded-md border border-surface-border bg-surface-card p-3">
            <p className="text-xs text-text-muted">{point.bucketStart}</p>
            <p className="text-lg font-semibold text-text-primary">{point.value ?? 'N/A'}</p>
            <p className="text-xs text-text-muted">Denom {point.denominator} - {point.status}</p>
          </div>
        ))}
      </div>
      <div>
        <h3 className="text-sm font-semibold text-text-primary">Source records</h3>
        {trend.sourceRows.length === 0 ? (
          <p className="mt-2 text-sm text-text-muted">No authorized source rows are available for this metric.</p>
        ) : (
          <ul className="mt-2 divide-y divide-surface-border rounded-md border border-surface-border">
            {trend.sourceRows.map((row) => (
              <li key={`${row.objectType}:${row.objectId}`} className="bg-surface-card p-3">
                <Link className="text-sm font-medium text-accent-primary" to={row.targetUrl}>
                  {row.title}
                </Link>
                <p className="text-xs text-text-muted">{row.subtitle ?? row.status}</p>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
