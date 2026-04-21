import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { RenewalAssignmentRequestDto, RenewalDto } from '../types';

function ifMatch(rowVersion: string) {
  return { 'If-Match': `"${rowVersion}"` };
}

export function useAssignRenewal(renewalId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: RenewalAssignmentRequestDto; rowVersion: string }) =>
      api.put<RenewalDto>(`/renewals/${renewalId}/assignment`, dto, ifMatch(rowVersion)),
    onSuccess: async (renewal) => {
      queryClient.setQueryData(['renewals', 'detail', renewalId], renewal);
      await Promise.all([
        queryClient.refetchQueries({ queryKey: ['renewals', 'timeline', renewalId], type: 'active' }),
        queryClient.invalidateQueries({ queryKey: ['renewals', 'list'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
      ]);
    },
  });
}
