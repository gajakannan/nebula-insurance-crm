import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { OpportunityEntityType, OpportunityFlowDto } from '../types';

export function useOpportunityFlow(
  entityType: OpportunityEntityType,
  periodDays = 180,
  options?: { enabled?: boolean },
) {
  return useQuery({
    queryKey: ['dashboard', 'opportunities', entityType, 'flow', periodDays],
    queryFn: () =>
      api.get<OpportunityFlowDto>(
        `/dashboard/opportunities/flow?entityType=${entityType}&periodDays=${periodDays}`,
      ),
    enabled: options?.enabled ?? true,
  });
}
