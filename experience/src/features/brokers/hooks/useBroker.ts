import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { BrokerDto } from '../types';

export function useBroker(brokerId: string) {
  return useQuery({
    queryKey: ['brokers', brokerId],
    queryFn: () => api.get<BrokerDto>(`/brokers/${brokerId}`),
    enabled: !!brokerId,
  });
}
