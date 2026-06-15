import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  DistributionNodeAncestorsResponse,
  DistributionNodeDto,
  DistributionNodeParentRequest,
  PaginatedResponse,
} from '../types';

export function useDistributionAncestors(nodeId: string | undefined) {
  return useQuery({
    queryKey: ['distribution-node', nodeId, 'ancestors'],
    queryFn: () => api.get<DistributionNodeAncestorsResponse>(`/distribution-nodes/${nodeId}/ancestors`),
    enabled: Boolean(nodeId),
  });
}

export function useDistributionDescendants(
  nodeId: string | undefined,
  depth = 2,
  page = 1,
  pageSize = 20,
) {
  return useQuery({
    queryKey: ['distribution-node', nodeId, 'descendants', { depth, page, pageSize }],
    queryFn: () =>
      api.get<PaginatedResponse<DistributionNodeDto>>(
        `/distribution-nodes/${nodeId}/descendants?depth=${depth}&page=${page}&pageSize=${pageSize}`,
      ),
    enabled: Boolean(nodeId),
  });
}

export function useSetDistributionParent(nodeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ request, rowVersion }: { request: DistributionNodeParentRequest; rowVersion: string }) =>
      api.put<DistributionNodeDto>(`/distribution-nodes/${nodeId}/parent`, request, {
        'If-Match': `"${rowVersion}"`,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['distribution-node', nodeId] });
    },
  });
}
