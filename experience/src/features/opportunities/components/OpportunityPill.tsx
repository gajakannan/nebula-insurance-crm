import type { OpportunityColorGroup, OpportunityEntityType } from '../types';
import { opportunityBg } from '../lib/opportunity-colors';
import { cn } from '@/lib/utils';
import { Popover } from '@/components/ui/Popover';
import { OpportunityPopoverContent } from './OpportunityPopover';

interface OpportunityPillProps {
  status: string;
  count: number;
  colorGroup: OpportunityColorGroup;
  entityType: OpportunityEntityType;
}

function formatStatus(status: string): string {
  return status.replace(/([A-Z])/g, ' $1').trim();
}

export function OpportunityPill({ status, count, colorGroup, entityType }: OpportunityPillProps) {
  const statusLabel = formatStatus(status);
  const trigger = (
    <button
      type="button"
      aria-label={`${statusLabel}: ${count} opportunities`}
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium text-white/90 transition-opacity hover:opacity-80',
        opportunityBg(colorGroup),
      )}
    >
      {statusLabel}
      <span className="rounded-full bg-black/20 px-1.5 py-0.5 text-xs font-bold">
        {count}
      </span>
    </button>
  );

  return (
    <Popover trigger={trigger}>
      <OpportunityPopoverContent entityType={entityType} status={status} />
    </Popover>
  );
}
