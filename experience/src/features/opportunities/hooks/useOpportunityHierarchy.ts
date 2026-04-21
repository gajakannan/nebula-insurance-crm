import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { OpportunityHierarchyDto } from '../types';

export function useOpportunityHierarchy(
  periodDays = 180,
  options?: { enabled?: boolean },
) {
  return useQuery({
    queryKey: ['dashboard', 'opportunities', 'hierarchy', periodDays],
    queryFn: () =>
      api.get<OpportunityHierarchyDto>(
        `/dashboard/opportunities/hierarchy?periodDays=${periodDays}`,
      ),
    enabled: options?.enabled ?? true,
  });
}
