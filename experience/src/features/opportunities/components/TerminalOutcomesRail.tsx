import type { RefCallback } from 'react';
import { Popover } from '@/components/ui/Popover';
import { cn } from '@/lib/utils';
import { OpportunityOutcomePopoverContent } from './OpportunityOutcomePopover';
import type { StoryChapter } from './storyTypes';
import type { OutcomeAnchor } from './storyTimelineTypes';
import type { OpportunityEntityType } from '../types';

interface TerminalOutcomesRailProps {
  anchors: OutcomeAnchor[];
  periodDays: number;
  chapter: StoryChapter;
  allOutcomesZero: boolean;
  entityTypes?: OpportunityEntityType[];
  registerOutcome?: (key: string) => RefCallback<HTMLElement>;
}

function branchStyleLabel(branchStyle: OutcomeAnchor['branchStyle']): string {
  if (branchStyle === 'solid') return 'Positive';
  if (branchStyle === 'gray_dotted') return 'Passive';
  return 'Negative';
}

export function TerminalOutcomesRail({
  anchors,
  periodDays,
  chapter,
  allOutcomesZero,
  entityTypes,
  registerOutcome,
}: TerminalOutcomesRailProps) {
  if (anchors.length === 0) {
    return null;
  }

  return (
    <aside aria-label="Terminal outcome branches" className="flex flex-wrap justify-center gap-4 pt-8">
      {anchors.map((anchor) => (
        <div
          key={anchor.key}
          ref={registerOutcome?.(anchor.key)}
        >
          <Popover
            contentAriaLabel={`${anchor.label} outcome details, ${anchor.count} exits, ${anchor.percentOfTotal.toFixed(1)} percent`}
            trigger={
              <button
                type="button"
                className={cn(
                  'story-focus-ring w-[156px] rounded-xl bg-surface-main/65 px-3 py-2 text-left shadow-sm transition-colors hover:bg-surface-main/80',
                  chapter === 'outcomes' && !allOutcomesZero && 'story-active-ring',
                  chapter === 'outcomes' && allOutcomesZero && 'opacity-65',
                )}
                aria-label={`${anchor.label} outcome, ${anchor.count} exits, ${anchor.percentOfTotal.toFixed(1)} percent of total`}
              >
                <p className="truncate text-xs font-semibold uppercase tracking-wide text-text-muted">
                  {anchor.label}
                </p>
                <div className="mt-1 flex items-center justify-between">
                  <span
                    className={cn(
                      'text-base font-semibold text-text-primary',
                      chapter === 'outcomes' && !allOutcomesZero && 'text-lg',
                    )}
                  >
                    {anchor.count}
                  </span>
                  <span
                    className={cn(
                      'text-xs text-text-muted',
                      chapter === 'outcomes' && !allOutcomesZero && 'text-sm font-semibold text-text-primary',
                    )}
                  >
                    {anchor.percentOfTotal.toFixed(1)}%
                  </span>
                </div>
                <p className="mt-1 text-[11px] text-text-muted">{branchStyleLabel(anchor.branchStyle)}</p>
              </button>
            }
          >
            <OpportunityOutcomePopoverContent outcomeKey={anchor.key} periodDays={periodDays} entityTypes={entityTypes} />
          </Popover>
        </div>
      ))}
    </aside>
  );
}
