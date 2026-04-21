import type {
  OpportunityStatusCountDto,
  OpportunityEntityType,
  OpportunityColorGroup,
} from '../types';
import { opportunityBg, opportunityText } from '../lib/opportunity-colors';
import { cn } from '@/lib/utils';
import { Popover } from '@/components/ui/Popover';
import { OpportunityPopoverContent } from './OpportunityPopover';

interface OpportunityPipelineBoardProps {
  label: string;
  entityType: OpportunityEntityType;
  statuses: OpportunityStatusCountDto[];
}

function formatStatus(status: string): string {
  return status.replace(/([A-Z])/g, ' $1').trim();
}

export function OpportunityPipelineBoard({
  label,
  entityType,
  statuses,
}: OpportunityPipelineBoardProps) {
  const orderedStatuses = statuses;
  const nonZero = orderedStatuses.filter((s) => s.count > 0);

  if (nonZero.length === 0) {
    return (
      <div className="py-4 text-center text-sm text-text-muted">
        No open {label.toLowerCase()} in this period
      </div>
    );
  }

  const maxCount = Math.max(...orderedStatuses.map((s) => s.count));
  const blockedStatus = orderedStatuses.find((s) => s.count === maxCount && maxCount > 0)?.status ?? null;
  const activeStatus = [...orderedStatuses].reverse().find((s) => s.count > 0)?.status ?? null;

  const topBottlenecks = [...nonZero]
    .sort((a, b) => b.count - a.count)
    .slice(0, 2);

  return (
    <section aria-label={`${label} opportunities flow`}>
      <h3 className="mb-3 text-sm font-semibold text-text-secondary">
        {label}
      </h3>

      {/* Desktop/tablet: connected flow */}
      <div className="hidden md:block">
        <div className="overflow-x-auto pb-2">
          <div className="flex min-w-max items-center" role="list">
            {orderedStatuses.map((status, index) => (
              <div key={status.status} className="flex items-center" role="listitem">
                <PipelineStageCard
                  status={status.status}
                  count={status.count}
                  colorGroup={status.colorGroup}
                  entityType={entityType}
                  emphasis={
                    status.status === blockedStatus
                      ? 'blocked'
                      : status.status === activeStatus
                        ? 'active'
                        : 'normal'
                  }
                />
                {index < orderedStatuses.length - 1 && (
                  <div className="mx-1 h-0.5 w-8 rounded bg-border-muted" aria-hidden="true" />
                )}
              </div>
            ))}
          </div>
        </div>
        <div className="mt-2 text-[11px] text-text-muted">
          Milestones: &lt;SLA 2d&gt; - - - &lt;SLA 5d&gt; - - - &lt;SLA 10d&gt;
        </div>
      </div>

      {/* Mobile: simplified stacked stages */}
      <div className="space-y-3 md:hidden">
        <div className="rounded-lg border border-border-muted bg-surface-main/55 p-3">
          <p className="mb-2 text-xs font-medium text-text-secondary">Top Bottlenecks</p>
          <div className="space-y-1 text-xs text-text-muted">
            {topBottlenecks.map((item, index) => (
              <p key={item.status}>
                {index + 1}) {formatStatus(item.status)} ({item.count})
                {item.status === blockedStatus ? ' !' : ''}
              </p>
            ))}
          </div>
        </div>
        <div className="space-y-2">
          {nonZero.map((s) => (
            <PipelineStageCard
              key={s.status}
              status={s.status}
              count={s.count}
              colorGroup={s.colorGroup}
              entityType={entityType}
              emphasis={
                s.status === blockedStatus
                  ? 'blocked'
                  : s.status === activeStatus
                    ? 'active'
                    : 'normal'
              }
            />
          ))}
        </div>
      </div>
    </section>
  );
}

interface PipelineStageCardProps {
  status: string;
  count: number;
  colorGroup: OpportunityColorGroup;
  entityType: OpportunityEntityType;
  emphasis: 'normal' | 'active' | 'blocked';
}

function PipelineStageCard({
  status,
  count,
  colorGroup,
  entityType,
  emphasis,
}: PipelineStageCardProps) {
  const statusLabel = formatStatus(status);
  const isBlocked = emphasis === 'blocked';
  const isActive = emphasis === 'active';

  const trigger = (
    <button
      type="button"
      aria-label={`${statusLabel}: ${count} opportunities`}
      className={cn(
        'flex min-w-[7rem] flex-col items-center gap-1 rounded-lg border border-border-muted bg-surface-card px-3 py-2 text-center transition-colors hover:bg-surface-panel focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-nebula-violet/50',
        isBlocked && 'border-rose-500/60 shadow-alert-blocked',
        isActive && 'border-nebula-violet/60 shadow-brand-active',
      )}
    >
      <span
        className={cn(
          'text-2xl font-bold tabular-nums',
          opportunityText(colorGroup),
        )}
      >
        {count}
      </span>
      <span className="text-xs text-text-muted">
        {statusLabel}
        {isActive ? ' *' : ''}
        {isBlocked ? ' !' : ''}
      </span>
      <span
        className={cn('mt-1 h-1 w-8 rounded-full', opportunityBg(colorGroup))}
        aria-hidden="true"
      />
    </button>
  );

  return (
    <Popover trigger={trigger}>
      <OpportunityPopoverContent entityType={entityType} status={status} />
    </Popover>
  );
}
