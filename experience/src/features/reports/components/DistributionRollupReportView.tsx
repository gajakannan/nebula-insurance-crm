import { Link } from 'react-router-dom';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { useDistributionRollupReport } from '../hooks';
import type { DistributionRollupParams, DistributionRollupRow } from '../types';
import { StatTile } from './ReportShared';

export function DistributionRollupReportView({ params }: { params: DistributionRollupParams }) {
  const { data, isLoading, isError, refetch } = useDistributionRollupReport(params);

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-5" aria-busy="true">
        {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}
      </div>
    );
  }

  if (isError || !data) {
    return <ErrorFallback message="Could not load distribution rollups." onRetry={() => refetch()} />;
  }

  const empty = data.rows.length === 0;

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-5">
        <StatTile label="Records" value={data.totals.recordCount} />
        <StatTile label="Production" value={data.totals.productionCount} />
        <StatTile label="Open workflow" value={data.totals.workflowOpen} />
        <StatTile label="Overdue" value={data.totals.workflowOverdue} />
        <StatTile label="Activity" value={data.totals.activityCount} />
      </div>

      {empty ? (
        <div className="rounded-lg border border-surface-border bg-surface-card p-4">
          <p className="text-sm font-medium text-text-primary">No visible rollup rows</p>
          <p className="mt-1 text-xs text-text-muted">
            The selected hierarchy, territory, producer, or as-of date has no records available to your access scope.
          </p>
        </div>
      ) : (
        <div className="overflow-hidden rounded-lg border border-surface-border">
          <table className="min-w-full divide-y divide-surface-border text-sm">
            <thead className="bg-surface-card">
              <tr>
                <th scope="col" className="px-3 py-2 text-left font-semibold text-text-primary">Group</th>
                <th scope="col" className="px-3 py-2 text-right font-semibold text-text-primary">Records</th>
                <th scope="col" className="px-3 py-2 text-right font-semibold text-text-primary">Production</th>
                <th scope="col" className="px-3 py-2 text-right font-semibold text-text-primary">Open</th>
                <th scope="col" className="px-3 py-2 text-right font-semibold text-text-primary">Overdue</th>
                <th scope="col" className="px-3 py-2 text-right font-semibold text-text-primary">Activity</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-surface-border bg-surface-panel">
              {data.rows.map((row) => <RollupRow key={`${row.groupType}:${row.groupKey}`} row={row} />)}
            </tbody>
          </table>
        </div>
      )}

      <p className="text-xs text-text-muted">
        Grouped by {data.groupBy} · {data.metricFamily} metrics · as of {data.asOf} · hidden records are excluded from totals.
      </p>
    </div>
  );
}

function RollupRow({ row }: { row: DistributionRollupRow }) {
  const label = row.drilldownUrl ? (
    <Link to={row.drilldownUrl} className="text-text-primary underline-offset-2 hover:underline">
      {row.groupLabel}
    </Link>
  ) : (
    <span className="text-text-primary">{row.groupLabel}</span>
  );

  return (
    <tr>
      <td className="px-3 py-2">{label}</td>
      <td className="px-3 py-2 text-right text-text-secondary">{row.metrics.recordCount}</td>
      <td className="px-3 py-2 text-right text-text-secondary">{row.metrics.productionCount}</td>
      <td className="px-3 py-2 text-right text-text-secondary">{row.metrics.workflowOpen}</td>
      <td className="px-3 py-2 text-right text-text-secondary">{row.metrics.workflowOverdue}</td>
      <td className="px-3 py-2 text-right text-text-secondary">{row.metrics.activityCount}</td>
    </tr>
  );
}
