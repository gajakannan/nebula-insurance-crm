import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { RenewalDto, RenewalLobAttributesUpdateDto } from '../types';

function ifMatch(rowVersion: string) {
  return { 'If-Match': `"${rowVersion}"` };
}

export function useRenewal(renewalId: string) {
  return useQuery({
    queryKey: ['renewals', 'detail', renewalId],
    queryFn: () => api.get<RenewalDto>(`/renewals/${renewalId}`),
    enabled: !!renewalId,
  });
}

export function useUpdateRenewalLobAttributes(renewalId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: RenewalLobAttributesUpdateDto; rowVersion: string }) =>
      api.put<RenewalDto>(`/renewals/${renewalId}/lob-attributes`, dto, ifMatch(rowVersion)),
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
