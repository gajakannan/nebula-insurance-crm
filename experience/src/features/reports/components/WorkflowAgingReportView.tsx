import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { useWorkflowAgingReport } from '../hooks';
import type { OperationalReportParams } from '../types';
import { CountList, DrilldownList, StatTile } from './ReportShared';

export function WorkflowAgingReportView({ params }: { params: OperationalReportParams }) {
  const { data, isLoading, isError, refetch } = useWorkflowAgingReport(params);

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4" aria-busy="true">
        {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}
      </div>
    );
  }
  if (isError || !data) {
    return <ErrorFallback message="Could not load the workflow aging report." onRetry={() => refetch()} />;
  }

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatTile label="Total open" value={data.totalOpen} />
        {data.byAgeBand.map((b) => <StatTile key={b.ageBand} label={b.ageBand} value={b.count} />)}
      </div>
      <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
        <CountList title="By workflow" items={data.byWorkflowType} />
        <CountList title="By status" items={data.byStatus} />
      </div>
      <DrilldownList title="Backlog (oldest first)" rows={data.backlogDrilldown} />
      <p className="text-xs text-text-muted">As of {data.asOf} · counts reflect records you can access.</p>
    </div>
  );
}
