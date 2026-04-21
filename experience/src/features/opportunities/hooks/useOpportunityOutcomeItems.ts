import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { OpportunityEntityType, OpportunityItemsDto } from '../types';

function serializeEntityTypes(entityTypes?: OpportunityEntityType[]) {
  return entityTypes && entityTypes.length > 0
    ? entityTypes.slice().sort().join(',')
    : 'all';
}

export function useOpportunityOutcomeItems(
  outcomeKey: string,
  periodDays: number,
  enabled: boolean,
  entityTypes?: OpportunityEntityType[],
) {
  const normalizedEntityTypes = entityTypes && entityTypes.length > 0
    ? entityTypes.slice().sort()
    : undefined;
  const entityTypesQuery = normalizedEntityTypes
    ? `&entityTypes=${encodeURIComponent(normalizedEntityTypes.join(','))}`
    : '';

  return useQuery({
    queryKey: ['dashboard', 'opportunities', 'outcomes', outcomeKey, 'items', serializeEntityTypes(normalizedEntityTypes), periodDays],
    queryFn: () =>
      api.get<OpportunityItemsDto>(
        `/dashboard/opportunities/outcomes/${encodeURIComponent(outcomeKey)}/items?periodDays=${periodDays}${entityTypesQuery}`,
      ),
    enabled,
  });
}
