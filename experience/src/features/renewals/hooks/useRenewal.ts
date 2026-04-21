import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { RenewalDto } from '../types';

export function useRenewal(renewalId: string) {
  return useQuery({
    queryKey: ['renewals', 'detail', renewalId],
    queryFn: () => api.get<RenewalDto>(`/renewals/${renewalId}`),
    enabled: !!renewalId,
  });
}
