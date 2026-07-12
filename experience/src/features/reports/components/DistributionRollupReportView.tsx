import { Link } from 'react-router-dom';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { useDistributionRollupReport } from '../hooks';
import type { DistributionRollupParams, DistributionRollupRow } from '../types';
import { StatTile } from './ReportShared';

// WHY: Production/Activity rollups are backed by broker-insight projections that only carry their own metric
// family — the other columns are structurally not computed (0-by-construction). Per the PRD, missing metrics
// must read as explicitly unavailable ("—") rather than a fabricated 0. Workflow rollups compute every
// column from the operational-report projection, so all columns are available there.
interface MetricAvailability {
  production: boolean;
  workflowOpen: boolean;
  workflowOverdue: boolean;
  activity: boolean;
}

function metricAvailability(metricFamily: string): MetricAvailability {
  const isWorkflow = metricFamily === 'Workflow';
  return {
    production: isWorkflow || metricFamily === 'Production',
    workflowOpen: isWorkflow,
    workflowOverdue: isWorkflow,
    activity: isWorkflow || metricFamily === 'Activity',
  };
}

const UNAVAILABLE = '—';

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
  const avail = metricAvailability(data.metricFamily);

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-5">
        <StatTile label="Records" value={data.totals.recordCount} />
        <StatTile label="Production" value={avail.production ? data.totals.productionCount : null} />
        <StatTile label="Open workflow" value={avail.workflowOpen ? data.totals.workflowOpen : null} />
        <StatTile label="Overdue" value={avail.workflowOverdue ? data.totals.workflowOverdue : null} />
        <StatTile label="Activity" value={avail.activity ? data.totals.activityCount : null} />
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
              {data.rows.map((row) => <RollupRow key={`${row.groupType}:${row.groupKey}`} row={row} avail={avail} />)}
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

function RollupRow({ row, avail }: { row: DistributionRollupRow; avail: MetricAvailability }) {
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
      <td className="px-3 py-2 text-right text-text-secondary">{avail.production ? row.metrics.productionCount : UNAVAILABLE}</td>
      <td className="px-3 py-2 text-right text-text-secondary">{avail.workflowOpen ? row.metrics.workflowOpen : UNAVAILABLE}</td>
      <td className="px-3 py-2 text-right text-text-secondary">{avail.workflowOverdue ? row.metrics.workflowOverdue : UNAVAILABLE}</td>
      <td className="px-3 py-2 text-right text-text-secondary">{avail.activity ? row.metrics.activityCount : UNAVAILABLE}</td>
    </tr>
  );
}
