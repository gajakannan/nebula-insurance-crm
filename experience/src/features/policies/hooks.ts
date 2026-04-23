import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ApiError, api } from '@/services/api';
import type {
  PaginatedResponse,
  PolicyAccountSummaryDto,
  PolicyCancelRequestDto,
  PolicyCoverageLineDto,
  PolicyCreateRequestDto,
  PolicyDto,
  PolicyEndorsementDto,
  PolicyEndorsementRequestDto,
  PolicyImportRequestDto,
  PolicyImportResultDto,
  PolicyIssueRequestDto,
  PolicyListItemDto,
  PolicyReinstateRequestDto,
  PolicyStatus,
  PolicySummaryDto,
  PolicyTimelineResponse,
  PolicyUpdateRequestDto,
  PolicyVersionDto,
} from './types';

interface UsePoliciesOptions {
  query?: string;
  status?: PolicyStatus | '';
  lineOfBusiness?: string;
  carrierId?: string;
  brokerOfRecordId?: string;
  expiringBefore?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
  enabled?: boolean;
}

export function usePolicies({
  query,
  status,
  lineOfBusiness,
  carrierId,
  brokerOfRecordId,
  expiringBefore,
  sort = 'expirationDate:asc',
  page = 1,
  pageSize = 25,
  enabled = true,
}: UsePoliciesOptions = {}) {
  const params = new URLSearchParams();
  if (query) params.set('q', query);
  if (status) params.set('status', status);
  if (lineOfBusiness) params.set('lineOfBusiness', lineOfBusiness);
  if (carrierId) params.set('carrierId', carrierId);
  if (brokerOfRecordId) params.set('brokerOfRecordId', brokerOfRecordId);
  if (expiringBefore) params.set('expiringBefore', expiringBefore);
  params.set('sort', sort);
  params.set('page', String(page));
  params.set('pageSize', String(pageSize));

  return useQuery({
    queryKey: ['policies', 'list', { query, status, lineOfBusiness, carrierId, brokerOfRecordId, expiringBefore, sort, page, pageSize }],
    queryFn: () => api.get<PaginatedResponse<PolicyListItemDto>>(`/policies?${params.toString()}`),
    enabled,
  });
}

export function usePolicy(policyId: string) {
  return useQuery({
    queryKey: ['policies', 'detail', policyId],
    queryFn: () => api.get<PolicyDto>(`/policies/${policyId}`),
    enabled: !!policyId,
  });
}

export function usePolicySummary(policyId: string) {
  return useQuery({
    queryKey: ['policies', 'summary', policyId],
    queryFn: () => api.get<PolicySummaryDto>(`/policies/${policyId}/summary`),
    enabled: !!policyId,
  });
}

export function usePolicyVersions(policyId: string, pageSize = 25) {
  return useQuery({
    queryKey: ['policies', policyId, 'versions', pageSize],
    queryFn: () => api.get<PaginatedResponse<PolicyVersionDto>>(`/policies/${policyId}/versions?page=1&pageSize=${pageSize}`),
    enabled: !!policyId,
  });
}

export function usePolicyEndorsements(policyId: string, pageSize = 25) {
  return useQuery({
    queryKey: ['policies', policyId, 'endorsements', pageSize],
    queryFn: () => api.get<PaginatedResponse<PolicyEndorsementDto>>(`/policies/${policyId}/endorsements?page=1&pageSize=${pageSize}`),
    enabled: !!policyId,
  });
}

export function usePolicyCoverages(policyId: string) {
  return useQuery({
    queryKey: ['policies', policyId, 'coverages'],
    queryFn: () => api.get<PolicyCoverageLineDto[]>(`/policies/${policyId}/coverages`),
    enabled: !!policyId,
  });
}

export function usePolicyTimeline(policyId: string, pageSize = 20) {
  return useQuery({
    queryKey: ['policies', policyId, 'timeline', pageSize],
    queryFn: () => api.get<PolicyTimelineResponse>(`/policies/${policyId}/timeline?page=1&pageSize=${pageSize}`),
    enabled: !!policyId,
  });
}

export function useAccountPolicySummary(accountId: string, enabled = true) {
  return useQuery({
    queryKey: ['accounts', accountId, 'policies', 'summary'],
    queryFn: () => api.get<PolicyAccountSummaryDto>(`/accounts/${accountId}/policies/summary`),
    enabled: !!accountId && enabled,
  });
}

export function useCreatePolicy() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: PolicyCreateRequestDto) => api.post<PolicyDto>('/policies', dto),
    onSuccess: (policy) => invalidatePolicyQueries(queryClient, policy.id, policy.accountId),
  });
}

export function useImportPolicies() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: PolicyImportRequestDto) => api.post<PolicyImportResultDto>('/policies/import', dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['policies'] });
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
    },
  });
}

export function useUpdatePolicy(policyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: PolicyUpdateRequestDto; rowVersion: string }) =>
      api.put<PolicyDto>(`/policies/${policyId}`, dto, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: (policy) => invalidatePolicyQueries(queryClient, policy.id, policy.accountId),
  });
}

export function useIssuePolicy(policyId: string) {
  return usePolicyActionMutation(policyId, 'issue');
}

export function useCancelPolicy(policyId: string) {
  return usePolicyActionMutation<PolicyCancelRequestDto>(policyId, 'cancel');
}

export function useReinstatePolicy(policyId: string) {
  return usePolicyActionMutation<PolicyReinstateRequestDto>(policyId, 'reinstate');
}

export function useEndorsePolicy(policyId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: PolicyEndorsementRequestDto; rowVersion: string }) =>
      api.post<PolicyEndorsementDto>(`/policies/${policyId}/endorse`, dto, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['policies'] });
      queryClient.invalidateQueries({ queryKey: ['policies', 'detail', policyId] });
      queryClient.invalidateQueries({ queryKey: ['policies', 'summary', policyId] });
      queryClient.invalidateQueries({ queryKey: ['policies', policyId] });
    },
  });
}

export function describePolicyApiError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.problem?.errors) {
      const firstMessage = Object.values(error.problem.errors)[0]?.[0];
      if (firstMessage) return firstMessage;
    }

    return error.problem?.detail ?? error.problem?.title ?? error.message;
  }

  return 'Unable to complete the policy request.';
}

export function extractPolicyFieldErrors(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError) || !error.problem?.errors) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(error.problem.errors).map(([field, messages]) => [field, messages[0] ?? 'Invalid value.']),
  );
}

function usePolicyActionMutation<TDto = PolicyIssueRequestDto | null>(policyId: string, action: 'issue' | 'cancel' | 'reinstate') {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: TDto; rowVersion: string }) =>
      api.post<PolicyDto>(`/policies/${policyId}/${action}`, dto ?? {}, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: (policy) => invalidatePolicyQueries(queryClient, policy.id, policy.accountId),
  });
}

function invalidatePolicyQueries(queryClient: ReturnType<typeof useQueryClient>, policyId: string, accountId?: string) {
  queryClient.invalidateQueries({ queryKey: ['policies'] });
  queryClient.invalidateQueries({ queryKey: ['policies', 'detail', policyId] });
  queryClient.invalidateQueries({ queryKey: ['policies', 'summary', policyId] });
  queryClient.invalidateQueries({ queryKey: ['policies', policyId] });
  if (accountId) {
    queryClient.invalidateQueries({ queryKey: ['accounts'] });
    queryClient.invalidateQueries({ queryKey: ['accounts', accountId, 'policies'] });
  }
}
