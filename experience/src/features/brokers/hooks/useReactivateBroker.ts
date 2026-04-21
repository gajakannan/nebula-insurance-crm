import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { BrokerDto } from '../types';

export function useReactivateBroker() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (brokerId: string) =>
      api.post<BrokerDto>(`/brokers/${brokerId}/reactivate`, {}),
    onSuccess: (_data, brokerId) => {
      queryClient.invalidateQueries({ queryKey: ['brokers', brokerId] });
      queryClient.invalidateQueries({ queryKey: ['brokers'] });
    },
  });
}
