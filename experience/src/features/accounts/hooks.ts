import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { TimelineEventDto } from '@/contracts/timeline';
import { ApiError, api } from '@/services/api';
import type {
  AccountContactDto,
  AccountContactRequestDto,
  AccountCreateRequestDto,
  AccountDto,
  AccountLifecycleRequestDto,
  AccountListItemDto,
  AccountListQuery,
  AccountMergeRequestDto,
  AccountPolicyListItemDto,
  AccountRelationshipRequestDto,
  AccountSummaryDto,
  AccountUpdateRequestDto,
  PaginatedResponse,
} from './types';

export function useAccountList({
  query,
  status,
  territoryCode,
  region,
  brokerOfRecordId,
  primaryLineOfBusiness,
  includeSummary = true,
  includeRemoved = false,
  sort = 'displayName',
  sortDir = 'asc',
  page = 1,
  pageSize = 25,
}: AccountListQuery = {}) {
  const params = new URLSearchParams();
  if (query) params.set('q', query);
  if (status) params.set('status', status);
  if (territoryCode) params.set('territoryCode', territoryCode);
  if (region) params.set('region', region);
  if (brokerOfRecordId) params.set('brokerOfRecordId', brokerOfRecordId);
  if (primaryLineOfBusiness) params.set('primaryLineOfBusiness', primaryLineOfBusiness);
  if (includeSummary) params.set('include', 'summary');
  if (includeRemoved) params.set('includeRemoved', 'true');
  params.set('sort', sort);
  params.set('sortDir', sortDir);
  params.set('page', String(page));
  params.set('pageSize', String(pageSize));

  return useQuery({
    queryKey: ['accounts', 'list', { query, status, territoryCode, region, brokerOfRecordId, primaryLineOfBusiness, includeSummary, includeRemoved, sort, sortDir, page, pageSize }],
    queryFn: () => api.get<PaginatedResponse<AccountListItemDto>>(`/accounts?${params.toString()}`),
  });
}

export function useAccount(accountId: string) {
  return useQuery({
    queryKey: ['accounts', 'detail', accountId],
    queryFn: () => api.get<AccountDto>(`/accounts/${accountId}`),
    enabled: !!accountId,
  });
}

export function useCreateAccount() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: AccountCreateRequestDto) => api.post<AccountDto>('/accounts', dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      queryClient.invalidateQueries({ queryKey: ['referenceData', 'accounts'] });
    },
  });
}

export function useUpdateAccount(accountId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: AccountUpdateRequestDto; rowVersion: string }) =>
      api.put<AccountDto>(`/accounts/${accountId}`, dto, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: () => {
      invalidateAccountQueries(queryClient, accountId);
    },
  });
}

export function useTransitionAccount(accountId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: AccountLifecycleRequestDto; rowVersion: string }) =>
      api.post<AccountDto>(`/accounts/${accountId}/lifecycle`, dto, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: () => {
      invalidateAccountQueries(queryClient, accountId);
      queryClient.invalidateQueries({ queryKey: ['submissions'] });
      queryClient.invalidateQueries({ queryKey: ['renewals'] });
    },
  });
}

export function useMergeAccount(accountId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: AccountMergeRequestDto; rowVersion: string }) =>
      api.post<AccountDto>(`/accounts/${accountId}/merge`, dto, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: () => {
      invalidateAccountQueries(queryClient, accountId);
      queryClient.invalidateQueries({ queryKey: ['submissions'] });
      queryClient.invalidateQueries({ queryKey: ['renewals'] });
    },
  });
}

export function useChangeAccountRelationship(accountId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: AccountRelationshipRequestDto; rowVersion: string }) =>
      api.post<AccountDto>(`/accounts/${accountId}/relationships`, dto, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: () => {
      invalidateAccountQueries(queryClient, accountId);
    },
  });
}

export function useAccountSummary(accountId: string, enabled = true) {
  return useQuery({
    queryKey: ['accounts', 'summary', accountId],
    queryFn: () => api.get<AccountSummaryDto>(`/accounts/${accountId}/summary`),
    enabled: !!accountId && enabled,
  });
}

export function useAccountContacts(accountId: string, pageSize = 50) {
  return useQuery({
    queryKey: ['accounts', accountId, 'contacts', pageSize],
    queryFn: () => api.get<PaginatedResponse<AccountContactDto>>(`/accounts/${accountId}/contacts?page=1&pageSize=${pageSize}`),
    enabled: !!accountId,
  });
}

export function useCreateAccountContact(accountId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: AccountContactRequestDto) =>
      api.post<AccountContactDto>(`/accounts/${accountId}/contacts`, dto),
    onSuccess: () => {
      invalidateAccountQueries(queryClient, accountId);
    },
  });
}

export function useUpdateAccountContact(accountId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ contactId, dto, rowVersion }: { contactId: string; dto: AccountContactRequestDto; rowVersion: string }) =>
      api.put<AccountContactDto>(`/accounts/${accountId}/contacts/${contactId}`, dto, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: () => {
      invalidateAccountQueries(queryClient, accountId);
    },
  });
}

export function useDeleteAccountContact(accountId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ contactId, rowVersion }: { contactId: string; rowVersion: string }) =>
      api.delete(`/accounts/${accountId}/contacts/${contactId}`, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: () => {
      invalidateAccountQueries(queryClient, accountId);
    },
  });
}

export function useAccountPolicies(accountId: string, pageSize = 10) {
  return useQuery({
    queryKey: ['accounts', accountId, 'policies', pageSize],
    queryFn: () => api.get<PaginatedResponse<AccountPolicyListItemDto>>(`/accounts/${accountId}/policies?page=1&pageSize=${pageSize}`),
    enabled: !!accountId,
  });
}

export function useAccountTimeline(accountId: string, pageSize = 20) {
  return useInfiniteQuery({
    queryKey: ['accounts', accountId, 'timeline', pageSize],
    queryFn: ({ pageParam }) =>
      api.get<PaginatedResponse<TimelineEventDto>>(`/accounts/${accountId}/timeline?page=${pageParam}&pageSize=${pageSize}`),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => (
      lastPage.page < lastPage.totalPages ? lastPage.page + 1 : undefined
    ),
    enabled: !!accountId,
  });
}

export function describeAccountApiError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.problem?.errors) {
      const firstMessage = Object.values(error.problem.errors)[0]?.[0];
      if (firstMessage) return firstMessage;
    }

    return error.problem?.detail ?? error.problem?.title ?? error.message;
  }

  return 'Unable to complete the account request.';
}

export function extractProblemFieldErrors(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError) || !error.problem?.errors) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(error.problem.errors).map(([field, messages]) => [field, messages[0] ?? 'Invalid value.']),
  );
}

function invalidateAccountQueries(queryClient: ReturnType<typeof useQueryClient>, accountId: string) {
  queryClient.invalidateQueries({ queryKey: ['accounts'] });
  queryClient.invalidateQueries({ queryKey: ['accounts', 'detail', accountId] });
  queryClient.invalidateQueries({ queryKey: ['accounts', 'summary', accountId] });
  queryClient.invalidateQueries({ queryKey: ['accounts', accountId, 'contacts'] });
  queryClient.invalidateQueries({ queryKey: ['accounts', accountId, 'policies'] });
  queryClient.invalidateQueries({ queryKey: ['accounts', accountId, 'timeline'] });
}
