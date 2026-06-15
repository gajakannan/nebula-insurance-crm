import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  ProducerOwnershipAssignmentRequest,
  ProducerOwnershipDto,
  ProducerOwnershipLookupResponse,
  ScopeType,
} from '../types';

export function useProducerOwnership(scopeType: ScopeType, scopeId: string | undefined, asOf?: string) {
  return useQuery({
    queryKey: ['producer-ownership', { scopeType, scopeId, asOf }],
    queryFn: () => {
      const params = new URLSearchParams({ scopeType, scopeId: scopeId! });
      if (asOf) params.set('asOf', asOf);
      return api.get<ProducerOwnershipLookupResponse>(`/producer-ownership?${params.toString()}`);
    },
    enabled: Boolean(scopeId),
  });
}

export function useAssignProducerOwnership() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ request, rowVersion }: { request: ProducerOwnershipAssignmentRequest; rowVersion?: string }) =>
      api.post<ProducerOwnershipDto>(
        '/producer-ownership',
        request,
        rowVersion ? { 'If-Match': `"${rowVersion}"` } : undefined,
      ),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['producer-ownership'] });
    },
  });
}
