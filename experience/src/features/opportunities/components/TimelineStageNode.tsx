import type { KeyboardEventHandler, RefCallback } from 'react';
import { Popover } from '@/components/ui/Popover';
import { cn } from '@/lib/utils';
import { opportunityText } from '../lib/opportunity-colors';
import type { OpportunityEntityType, OpportunityFlowNodeDto } from '../types';
import { OpportunityPopoverContent } from './OpportunityPopover';
import type { StageAnchor } from './storyTimelineTypes';

interface TimelineStageNodeProps {
  anchor: StageAnchor;
  entityType: OpportunityEntityType;
  node: OpportunityFlowNodeDto;
  compact?: boolean;
  emphasisClass: string;
  buttonRef: RefCallback<HTMLButtonElement>;
  onKeyDown: KeyboardEventHandler<HTMLButtonElement>;
  popoverDisabled: boolean;
}

export function TimelineStageNode({
  anchor,
  entityType,
  node,
  compact = false,
  emphasisClass,
  buttonRef,
  onKeyDown,
  popoverDisabled,
}: TimelineStageNodeProps) {
  const stageTrigger = (
    <button
      ref={buttonRef}
      type="button"
      onKeyDown={onKeyDown}
      className={cn(
        'story-focus-ring min-h-[70px] shrink-0 rounded-xl bg-surface-main/70 px-3 py-2 text-left shadow-sm transition-colors hover:bg-surface-main/80',
        compact ? 'w-[118px]' : 'w-[132px]',
        emphasisClass,
      )}
      aria-label={`${anchor.label} stage, ${node.currentCount} opportunities`}
    >
      <p className="truncate text-xs font-semibold uppercase tracking-wide text-text-muted">
        {anchor.label}
      </p>
      <p className={cn('mt-1 text-xl font-semibold', opportunityText(node.colorGroup))}>
        {node.currentCount}
      </p>
    </button>
  );

  if (popoverDisabled) {
    return stageTrigger;
  }

  return (
    <Popover
      contentAriaLabel={`${anchor.label} stage details, ${node.currentCount} opportunities in stage`}
      trigger={stageTrigger}
    >
      <OpportunityPopoverContent entityType={entityType} status={anchor.status} />
    </Popover>
  );
}
