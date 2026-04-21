import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  RenewalDto,
  RenewalTransitionRequestDto,
  WorkflowTransitionRecordDto,
} from '../types';

function ifMatch(rowVersion: string) {
  return { 'If-Match': `"${rowVersion}"` };
}

export function useTransitionRenewal(renewalId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: RenewalTransitionRequestDto; rowVersion: string }) =>
      api.post<WorkflowTransitionRecordDto>(`/renewals/${renewalId}/transitions`, dto, ifMatch(rowVersion)),
    onSuccess: async () => {
      const renewal = await api.get<RenewalDto>(`/renewals/${renewalId}`);
      queryClient.setQueryData(['renewals', 'detail', renewalId], renewal);
      await Promise.all([
        queryClient.refetchQueries({ queryKey: ['renewals', 'timeline', renewalId], type: 'active' }),
        queryClient.invalidateQueries({ queryKey: ['renewals', 'list'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
      ]);
    },
  });
}
