import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/Badge';
import { useCurrentUser } from '@/features/auth';
import { useSubmissions } from '../hooks/useSubmissions';

const NUDGE_CARD_ROLES = new Set(['DistributionUser', 'DistributionManager', 'Admin']);

export function StaleSubmissionNudgeCard() {
  const currentUser = useCurrentUser();
  const canViewNudge = currentUser?.roles.some((role) => NUDGE_CARD_ROLES.has(role)) ?? false;
  const { data, isError } = useSubmissions({
    stale: true,
    page: 1,
    pageSize: 1,
    enabled: canViewNudge,
  });

  if (!canViewNudge || isError || !data || data.totalCount === 0) {
    return null;
  }

  return (
    <div className="canvas-section canvas-zone-tight" aria-label="Submission stale nudge">
      <div className="grid grid-cols-1 gap-3">
        <div className="glass-card operational-panel rounded-xl p-4 fx-shadow-alert-warning">
          <div className="mb-2 flex items-center gap-2">
            <Badge variant="warning">Stale</Badge>
            <span className="text-xs font-medium uppercase tracking-[0.18em] text-text-muted">
              Intake attention
            </span>
          </div>
          <h3 className="text-sm font-semibold text-text-primary">
            {data.totalCount} stale submission{data.totalCount === 1 ? '' : 's'} need follow-up
          </h3>
          <p className="mt-1 text-xs text-text-secondary">
            Open the submission pipeline filtered to stale work and clear the oldest bottlenecks first.
          </p>
          <Link
            to="/submissions?stale=true"
            className="fx-shadow-cta-brand-hover mt-3 inline-block rounded-lg bg-gradient-to-r from-nebula-violet/20 to-nebula-fuchsia/20 px-3 py-1.5 text-xs font-medium text-nebula-violet transition-all hover:from-nebula-violet/30 hover:to-nebula-fuchsia/30"
          >
            Review stale submissions
          </Link>
        </div>
      </div>
    </div>
  );
}
