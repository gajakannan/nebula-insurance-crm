import { Popover } from '@/components/ui/Popover';
import { cn } from '@/lib/utils';
import type { OpportunityOutcomeDto } from '../types';
import { OpportunityOutcomePopoverContent } from './OpportunityOutcomePopover';

interface OpportunityOutcomesRailProps {
  outcomes: OpportunityOutcomeDto[];
  periodDays: number;
}

function branchStyleClass(branchStyle: string): string {
  switch (branchStyle) {
    case 'solid':
      return 'border-l-2 border-status-success';
    case 'gray_dotted':
      return 'border-l-2 border-dotted border-text-muted';
    case 'red_dashed':
    default:
      return 'border-l-2 border-dashed border-status-error';
  }
}

export function OpportunityOutcomesRail({
  outcomes,
  periodDays,
}: OpportunityOutcomesRailProps) {
  return (
    <aside
      aria-label="Terminal outcomes rail"
      className="rounded-lg border border-border-muted bg-surface-card p-3"
    >
      <h3 className="mb-3 text-sm font-semibold text-text-secondary">
        Terminal Outcomes
      </h3>
      <div className="space-y-2">
        {outcomes.map((outcome) => {
          const percentLabel = outcome.percentOfTotal.toFixed(1);
          const trigger = (
            <button
              type="button"
              className={cn(
                'w-full rounded-md bg-surface-main/55 px-3 py-2 text-left transition hover:bg-surface-main/75 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-nebula-violet/45',
                branchStyleClass(outcome.branchStyle),
              )}
              aria-label={`${outcome.label}: ${outcome.count} exits, ${percentLabel}% of total`}
            >
              <div className="flex items-center justify-between gap-2">
                <span className="text-sm font-medium text-text-primary">
                  {outcome.label}
                </span>
                <span className="text-xs text-text-muted">
                  {percentLabel}%
                </span>
              </div>
              <div className="mt-1 flex items-center justify-between text-xs text-text-muted">
                <span>{outcome.count} exits</span>
                <span>
                  avg {outcome.averageDaysToExit?.toFixed(1) ?? '-'}d
                </span>
              </div>
            </button>
          );

          return (
            <Popover key={outcome.key} trigger={trigger}>
              <OpportunityOutcomePopoverContent
                outcomeKey={outcome.key}
                periodDays={periodDays}
              />
            </Popover>
          );
        })}
      </div>
      <p className="mt-3 text-[11px] text-text-muted">
        Solid: positive conversion. Dashed red: negative outcomes. Dotted gray: passive expiry outcomes.
      </p>
    </aside>
  );
}
