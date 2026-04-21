import { Skeleton } from '@/components/ui/Skeleton';
import { OpportunityMiniCard } from './OpportunityMiniCard';
import { useOpportunityOutcomeItems } from '../hooks/useOpportunityOutcomeItems';
import type { OpportunityEntityType } from '../types';

interface OpportunityOutcomePopoverProps {
  outcomeKey: string;
  periodDays: number;
  entityTypes?: OpportunityEntityType[];
}

export function OpportunityOutcomePopoverContent({
  outcomeKey,
  periodDays,
  entityTypes,
}: OpportunityOutcomePopoverProps) {
  const { data, isLoading, isError } = useOpportunityOutcomeItems(
    outcomeKey,
    periodDays,
    true,
    entityTypes,
  );

  if (isLoading) {
    return (
      <div className="space-y-2">
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-12 w-full" />
      </div>
    );
  }

  if (isError || !data) {
    return <p className="text-xs text-text-muted">Unable to load outcome items</p>;
  }

  if (data.items.length === 0) {
    return <p className="text-xs text-text-muted">No items for this outcome in the selected period</p>;
  }

  return (
    <div className="space-y-2">
      {data.items.map((item) => (
        <OpportunityMiniCard key={item.entityId} item={item} />
      ))}
    </div>
  );
}
