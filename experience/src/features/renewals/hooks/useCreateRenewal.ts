import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { RenewalCreateDto, RenewalDto } from '../types';

export function useCreateRenewal() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: RenewalCreateDto) => api.post<RenewalDto>('/renewals', dto),
    onSuccess: async (renewal) => {
      queryClient.setQueryData(['renewals', 'detail', renewal.id], renewal);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['renewals', 'list'] }),
        queryClient.invalidateQueries({ queryKey: ['renewals', 'timeline'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
      ]);
    },
  });
}
