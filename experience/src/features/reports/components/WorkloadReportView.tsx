import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { useWorkloadReport } from '../hooks';
import type { OperationalReportParams } from '../types';
import { CountList, DrilldownList, StatTile } from './ReportShared';

export function WorkloadReportView({ params }: { params: OperationalReportParams }) {
  const { data, isLoading, isError, refetch } = useWorkloadReport(params);

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4" aria-busy="true">
        {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}
      </div>
    );
  }
  if (isError || !data) {
    return <ErrorFallback message="Could not load the workload report." onRetry={() => refetch()} />;
  }

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatTile label="Total open" value={data.totalOpen} />
        <StatTile label="Due today" value={data.dueToday} />
        <StatTile label="Overdue" value={data.overdue} />
        <StatTile label="Unassigned" value={data.unassigned} />
      </div>
      <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
        <CountList title="By owner" items={data.byOwner} />
        <CountList title="By status" items={data.byStatus} />
        <CountList title="By workflow" items={data.byWorkflowType} />
      </div>
      <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
        <DrilldownList title="Due today" rows={data.dueTodayDrilldown} />
        <DrilldownList title="Overdue" rows={data.overdueDrilldown} />
      </div>
      <p className="text-xs text-text-muted">As of {data.asOf} · counts reflect records you can access.</p>
    </div>
  );
}
