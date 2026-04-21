import { Link } from 'react-router-dom';
import { Card } from '@/components/ui/Card';
import { Skeleton } from '@/components/ui/Skeleton';
import { useRenewals } from '../hooks/useRenewals';

export function RenewalPipelineNudgeCard() {
  const overdueQuery = useRenewals({ urgency: 'overdue', pageSize: 1 });
  const approachingQuery = useRenewals({ urgency: 'approaching', pageSize: 1 });

  if (overdueQuery.isError || approachingQuery.isError) {
    return null;
  }

  if (overdueQuery.isLoading || approachingQuery.isLoading) {
    return (
      <div className="canvas-section canvas-zone-tight">
        <Skeleton className="h-36 w-full" />
      </div>
    );
  }

  const overdueCount = overdueQuery.data?.totalCount ?? 0;
  const approachingCount = approachingQuery.data?.totalCount ?? 0;

  if (overdueCount === 0 && approachingCount === 0) {
    return null;
  }

  return (
    <div className="canvas-section canvas-zone-tight">
      <Card className="overflow-hidden">
        <div className="flex flex-col gap-4 rounded-xl bg-gradient-to-r from-status-warning/12 via-surface-card to-status-error/12 p-5 md:flex-row md:items-center md:justify-between">
          <div className="space-y-2">
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-text-muted">
              Renewal Pulse
            </p>
            <div>
              <h2 className="text-lg font-semibold text-text-primary">
                Renewal pipeline needs attention
              </h2>
              <p className="mt-1 text-sm text-text-secondary">
                Overdue and approaching renewals are calculated from the live F0007 timing windows.
              </p>
            </div>
          </div>

          <div className="grid min-w-[16rem] grid-cols-2 gap-3">
            <div className="rounded-xl border border-status-error/30 bg-status-error/12 p-4">
              <p className="text-xs font-medium uppercase tracking-[0.16em] text-text-muted">Overdue</p>
              <p className="mt-2 text-3xl font-semibold text-text-primary">{overdueCount}</p>
              <p className="mt-1 text-xs text-text-secondary">Renewals still in Identified past target outreach.</p>
            </div>
            <div className="rounded-xl border border-status-warning/30 bg-status-warning/12 p-4">
              <p className="text-xs font-medium uppercase tracking-[0.16em] text-text-muted">Approaching</p>
              <p className="mt-2 text-3xl font-semibold text-text-primary">{approachingCount}</p>
              <p className="mt-1 text-xs text-text-secondary">Renewals entering their outreach warning window.</p>
            </div>
          </div>

          <div className="flex flex-col gap-2 md:items-end">
            <Link
              to="/renewals?urgency=overdue"
              className="fx-shadow-cta-brand-hover inline-flex items-center justify-center rounded-lg bg-gradient-to-r from-nebula-violet/20 to-nebula-fuchsia/20 px-4 py-2 text-sm font-medium text-nebula-violet transition-all hover:from-nebula-violet/30 hover:to-nebula-fuchsia/30"
            >
              Open overdue renewals
            </Link>
            <Link
              to="/renewals?urgency=approaching"
              className="text-xs font-medium text-text-secondary transition-colors hover:text-nebula-violet"
            >
              Review approaching window
            </Link>
          </div>
        </div>
      </Card>
    </div>
  );
}
