import type { OpportunityEntityType } from '../types';
import { useOpportunityItems } from '../hooks/useOpportunityItems';
import { Skeleton } from '@/components/ui/Skeleton';
import { OpportunityMiniCard } from './OpportunityMiniCard';

interface OpportunityPopoverProps {
  entityType: OpportunityEntityType;
  status: string;
}

export function OpportunityPopoverContent({ entityType, status }: OpportunityPopoverProps) {
  const { data, isLoading, isError } = useOpportunityItems(entityType, status, true);

  if (isLoading) {
    return (
      <div className="space-y-2">
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-12 w-full" />
      </div>
    );
  }

  if (isError || !data) {
    return <p className="text-xs text-text-muted">Unable to load items</p>;
  }

  if (data.items.length === 0) {
    return <p className="text-xs text-text-muted">No items</p>;
  }

  return (
    <div className="space-y-2">
      {data.items.map((item) => (
        <OpportunityMiniCard key={item.entityId} item={item} />
      ))}
    </div>
  );
}
