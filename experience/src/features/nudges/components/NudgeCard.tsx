import type { NudgeCardDto } from '../types';
import { Badge } from '@/components/ui/Badge';
import { canNavigateTo, getEntityPath } from '@/lib/navigation';
import { cn } from '@/lib/utils';
import { Link } from 'react-router-dom';

interface NudgeCardProps {
  nudge: NudgeCardDto;
  onDismiss: () => void;
}

const urgencyVariant: Record<string, 'error' | 'warning' | 'info'> = {
  OverdueTask: 'error',
  StaleSubmission: 'warning',
  UpcomingRenewal: 'info',
};

export function NudgeCard({ nudge, onDismiss }: NudgeCardProps) {
  const variant = urgencyVariant[nudge.nudgeType] ?? 'warning';
  const path = getEntityPath(nudge.linkedEntityType, nudge.linkedEntityId);
  const showCta = canNavigateTo(nudge.linkedEntityType);

  return (
    <div
      role="alert"
      className={cn(
        'glass-card operational-panel relative rounded-xl p-4',
        variant === 'error' && 'fx-shadow-alert-error',
        variant === 'warning' && 'fx-shadow-alert-warning',
        variant === 'info' && 'fx-shadow-alert-info',
      )}
    >
      <button
        onClick={onDismiss}
        className="absolute right-3 top-3 text-text-muted transition-colors hover:text-text-secondary"
        aria-label="Dismiss"
      >
        &times;
      </button>

      <div className="mb-2 flex items-center gap-2">
        <Badge variant={variant}>
          {nudge.nudgeType === 'OverdueTask'
            ? 'Overdue'
            : nudge.nudgeType === 'StaleSubmission'
              ? 'Stale'
              : 'Upcoming'}
        </Badge>
      </div>

      <h3 className="text-sm font-semibold text-text-primary">{nudge.title}</h3>
      <p className="mt-1 text-xs text-text-secondary">{nudge.description}</p>

      {showCta && path && (
        <Link
          to={path}
          className="fx-shadow-cta-brand-hover mt-3 inline-block rounded-lg bg-gradient-to-r from-nebula-violet/20 to-nebula-fuchsia/20 px-3 py-1.5 text-xs font-medium text-nebula-violet transition-all hover:from-nebula-violet/30 hover:to-nebula-fuchsia/30"
        >
          {nudge.ctaLabel}
        </Link>
      )}
    </div>
  );
}
