import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { BrokerDto, BrokerUpdateDto } from '../types';

interface UpdateBrokerParams {
  brokerId: string;
  dto: BrokerUpdateDto;
  rowVersion: number;
}

export function useUpdateBroker() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ brokerId, dto, rowVersion }: UpdateBrokerParams) =>
      api.put<BrokerDto>(`/brokers/${brokerId}`, dto, {
        'If-Match': `"${rowVersion}"`,
      }),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['brokers', variables.brokerId] });
      queryClient.invalidateQueries({ queryKey: ['brokers'] });
    },
  });
}
