import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  MemberType,
  PaginatedResponse,
  TerritoryAssignmentDto,
  TerritoryAssignmentLookupResponse,
  TerritoryCreateRequest,
  TerritoryDto,
  TerritoryMemberAssignmentRequest,
} from '../types';

export function useCreateTerritory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: TerritoryCreateRequest) => api.post<TerritoryDto>('/territories', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['territories'] });
    },
  });
}

export function useTerritoryMembers(territoryId: string | undefined, asOf?: string, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ['territory-members', territoryId, { asOf, page, pageSize }],
    queryFn: () => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (asOf) params.set('asOf', asOf);
      return api.get<PaginatedResponse<TerritoryAssignmentDto>>(
        `/territories/${territoryId}/members?${params.toString()}`,
      );
    },
    enabled: Boolean(territoryId),
  });
}

export function useAssignTerritoryMember() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      territoryId,
      request,
      rowVersion,
    }: {
      territoryId: string;
      request: TerritoryMemberAssignmentRequest;
      rowVersion?: string;
    }) =>
      api.post<TerritoryAssignmentDto>(
        `/territories/${territoryId}/members`,
        request,
        rowVersion ? { 'If-Match': `"${rowVersion}"` } : undefined,
      ),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['territory-members', variables.territoryId] });
      queryClient.invalidateQueries({ queryKey: ['territory-assignment'] });
    },
  });
}

export function useTerritoryAssignmentForMember(
  memberType: MemberType,
  memberId: string | undefined,
  asOf?: string,
) {
  return useQuery({
    queryKey: ['territory-assignment', { memberType, memberId, asOf }],
    queryFn: () => {
      const params = new URLSearchParams({ memberType, memberId: memberId! });
      if (asOf) params.set('asOf', asOf);
      return api.get<TerritoryAssignmentLookupResponse>(`/territory-assignments?${params.toString()}`);
    },
    enabled: Boolean(memberId),
  });
}
