import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  GlobalSearchParams,
  GlobalSearchResponse,
  PaginatedResponse,
  SavedView,
  SavedViewListParams,
  SavedViewUpsertRequest,
} from './types';

function buildSearchQuery(params: GlobalSearchParams): string {
  const sp = new URLSearchParams();
  sp.set('q', params.q);
  if (params.objectTypes?.length) sp.set('objectTypes', params.objectTypes.join(','));
  if (params.status) sp.set('status', params.status);
  if (params.ownerUserId) sp.set('ownerUserId', params.ownerUserId);
  if (params.region) sp.set('region', params.region);
  if (params.lineOfBusiness) sp.set('lineOfBusiness', params.lineOfBusiness);
  sp.set('sort', params.sort ?? 'relevance');
  sp.set('page', String(params.page ?? 1));
  sp.set('pageSize', String(params.pageSize ?? 20));
  return sp.toString();
}

export function useGlobalSearch(params: GlobalSearchParams) {
  const trimmed = params.q.trim();
  const enabled = trimmed.length >= 2;
  return useQuery({
    queryKey: ['search', 'results', { ...params, q: trimmed }],
    queryFn: () => api.get<GlobalSearchResponse>(`/search-results?${buildSearchQuery({ ...params, q: trimmed })}`),
    enabled,
    placeholderData: (prev) => prev,
  });
}

export function useSavedViews(params: SavedViewListParams = {}) {
  const sp = new URLSearchParams();
  if (params.viewType) sp.set('viewType', params.viewType);
  if (params.visibility) sp.set('visibility', params.visibility);
  if (params.includeArchived) sp.set('includeArchived', 'true');
  sp.set('page', String(params.page ?? 1));
  sp.set('pageSize', String(params.pageSize ?? 50));
  return useQuery({
    queryKey: ['saved-views', 'list', params],
    queryFn: () => api.get<PaginatedResponse<SavedView>>(`/saved-views?${sp.toString()}`),
  });
}

export function useCreateSavedView() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: SavedViewUpsertRequest) => api.post<SavedView>('/saved-views', dto),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['saved-views'] }),
  });
}

export function useUpdateSavedView(savedViewId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: SavedViewUpsertRequest; rowVersion: string }) =>
      api.patch<SavedView>(`/saved-views/${savedViewId}`, dto, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['saved-views'] }),
  });
}

export function useArchiveSavedView(savedViewId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ rowVersion }: { rowVersion: string }) =>
      api.delete(`/saved-views/${savedViewId}`, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['saved-views'] }),
  });
}

export function useSetDefaultSavedView(savedViewId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ rowVersion }: { rowVersion: string }) =>
      api.put<SavedView>(`/saved-views/${savedViewId}/default`, {}, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['saved-views'] }),
  });
}
