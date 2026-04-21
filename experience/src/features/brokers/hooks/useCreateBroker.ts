import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { BrokerCreateDto, BrokerDto } from '../types';

export function useCreateBroker() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: BrokerCreateDto) =>
      api.post<BrokerDto>('/brokers', dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['brokers'] });
    },
  });
}
