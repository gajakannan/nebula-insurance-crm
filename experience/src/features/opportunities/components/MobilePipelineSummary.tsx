import { cn } from '@/lib/utils';
import { opportunityBg, opportunityText } from '../lib/opportunity-colors';
import type { OpportunityFlowDto, OpportunityOutcomeDto } from '../types';

interface MobilePipelineSummaryProps {
  flow: OpportunityFlowDto;
  outcomes: OpportunityOutcomeDto[];
}

function branchStyleLabel(branchStyle: OpportunityOutcomeDto['branchStyle']): string {
  if (branchStyle === 'solid') return 'Positive';
  if (branchStyle === 'gray_dotted') return 'Passive';
  return 'Negative';
}

export function MobilePipelineSummary({ flow, outcomes }: MobilePipelineSummaryProps) {
  const stages = flow.nodes
    .filter((node) => !node.isTerminal)
    .sort((a, b) => a.displayOrder - b.displayOrder);

  const maxCount = Math.max(1, ...stages.map((s) => s.currentCount));

  return (
    <div className="space-y-2">
      <div className="glass-card rounded-xl px-3 py-2">
        <p className="mb-2 text-[11px] font-semibold uppercase tracking-wider text-text-muted">
          Pipeline Stages
        </p>
        <div className="space-y-1.5">
          {stages.map((stage) => {
            const barWidth = Math.max(4, (stage.currentCount / maxCount) * 100);
            return (
              <div key={stage.status} className="flex items-center gap-3">
                <span className="w-[72px] shrink-0 truncate text-xs font-medium text-text-secondary">
                  {stage.label}
                </span>
                <div className="relative h-5 flex-1 overflow-hidden rounded-md bg-surface-highlight">
                  <div
                    className={cn('absolute inset-y-0 left-0 rounded-md opacity-70', opportunityBg(stage.colorGroup))}
                    style={{ width: `${barWidth}%` }}
                  />
                </div>
                <span className={cn('w-8 text-right text-sm font-semibold tabular-nums', opportunityText(stage.colorGroup))}>
                  {stage.currentCount}
                </span>
              </div>
            );
          })}
        </div>
      </div>

      {outcomes.length > 0 && (
        <div className="glass-card rounded-xl px-3 py-2">
          <p className="mb-2 text-[11px] font-semibold uppercase tracking-wider text-text-muted">
            Outcomes
          </p>
          <div className="flex flex-wrap gap-3">
            {outcomes.map((outcome) => (
              <div key={outcome.key} className="flex items-baseline gap-1.5">
                <span className="text-sm font-semibold text-text-primary">{outcome.count}</span>
                <span className="text-xs text-text-secondary">{outcome.label}</span>
                <span className="text-[10px] text-text-muted">
                  ({branchStyleLabel(outcome.branchStyle)})
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
