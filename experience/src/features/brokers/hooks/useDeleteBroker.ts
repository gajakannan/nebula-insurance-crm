import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';

export function useDeleteBroker() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (brokerId: string) => api.delete(`/brokers/${brokerId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['brokers'] });
    },
  });
}
