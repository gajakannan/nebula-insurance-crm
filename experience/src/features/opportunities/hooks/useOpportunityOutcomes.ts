import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { OpportunityEntityType, OpportunityOutcomesDto } from '../types';

function serializeEntityTypes(entityTypes?: OpportunityEntityType[]) {
  return entityTypes && entityTypes.length > 0
    ? entityTypes.slice().sort().join(',')
    : 'all';
}

export function useOpportunityOutcomes(
  periodDays = 180,
  entityTypes?: OpportunityEntityType[],
  options?: { enabled?: boolean },
) {
  const normalizedEntityTypes = entityTypes && entityTypes.length > 0
    ? entityTypes.slice().sort()
    : undefined;
  const entityTypesQuery = normalizedEntityTypes
    ? `&entityTypes=${encodeURIComponent(normalizedEntityTypes.join(','))}`
    : '';

  return useQuery({
    queryKey: ['dashboard', 'opportunities', 'outcomes', serializeEntityTypes(normalizedEntityTypes), periodDays],
    queryFn: () =>
      api.get<OpportunityOutcomesDto>(
        `/dashboard/opportunities/outcomes?periodDays=${periodDays}${entityTypesQuery}`,
      ),
    enabled: options?.enabled ?? true,
  });
}
