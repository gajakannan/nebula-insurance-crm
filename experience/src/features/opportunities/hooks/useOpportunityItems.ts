import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { OpportunityEntityType, OpportunityItemsDto } from '../types';

export function useOpportunityItems(
  entityType: OpportunityEntityType,
  status: string,
  enabled: boolean,
) {
  return useQuery({
    queryKey: ['dashboard', 'opportunities', entityType, status, 'items'],
    queryFn: () =>
      api.get<OpportunityItemsDto>(
        `/dashboard/opportunities/${entityType}/${encodeURIComponent(status)}/items`,
      ),
    enabled,
  });
}
