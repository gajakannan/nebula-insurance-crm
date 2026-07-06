import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  ServiceCaseClaimReferenceUpdateRequestDto,
  ServiceCaseCommunicationLinkRequestDto,
  ServiceCaseCreateRequestDto,
  ServiceCaseDto,
  ServiceCaseFollowUpTaskRequestDto,
  ServiceCaseFollowUpTaskResponse,
  ServiceCaseListQuery,
  ServiceCaseListResponseDto,
  ServiceCaseTransitionRequestDto,
  ServiceCaseUpdateRequestDto,
} from './types';

export const serviceCaseKeys = {
  list: (query: ServiceCaseListQuery) => ['service-cases', query] as const,
  detail: (serviceCaseId: string) => ['service-case', serviceCaseId] as const,
};

function buildQuery(query: ServiceCaseListQuery): string {
  const params = new URLSearchParams();
  Object.entries(query).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      params.set(key, String(value));
    }
  });
  return params.toString();
}

export function useServiceCases(query: ServiceCaseListQuery) {
  return useQuery({
    queryKey: serviceCaseKeys.list(query),
    queryFn: () => api.get<ServiceCaseListResponseDto>(`/service-cases?${buildQuery(query)}`),
  });
}

export function useServiceCase(serviceCaseId: string) {
  return useQuery({
    queryKey: serviceCaseKeys.detail(serviceCaseId),
    queryFn: () => api.get<ServiceCaseDto>(`/service-cases/${serviceCaseId}`),
    enabled: !!serviceCaseId,
  });
}

export function useCreateServiceCase(invalidateQuery?: ServiceCaseListQuery) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: ServiceCaseCreateRequestDto) => api.post<ServiceCaseDto>('/service-cases', body),
    onSuccess: (serviceCase) => {
      queryClient.invalidateQueries({ queryKey: ['service-cases'] });
      if (invalidateQuery) queryClient.invalidateQueries({ queryKey: serviceCaseKeys.list(invalidateQuery) });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.setQueryData(serviceCaseKeys.detail(serviceCase.id), serviceCase);
    },
  });
}

export function useUpdateServiceCase(serviceCaseId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: ServiceCaseUpdateRequestDto) =>
      api.patch<ServiceCaseDto>(`/service-cases/${serviceCaseId}`, body),
    onSuccess: (serviceCase) => {
      queryClient.invalidateQueries({ queryKey: ['service-cases'] });
      queryClient.invalidateQueries({ queryKey: serviceCaseKeys.detail(serviceCase.id) });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
    },
  });
}

export function useTransitionServiceCase(serviceCaseId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: ServiceCaseTransitionRequestDto) =>
      api.post<ServiceCaseDto>(`/service-cases/${serviceCaseId}/transition`, body),
    onSuccess: (serviceCase) => {
      queryClient.invalidateQueries({ queryKey: ['service-cases'] });
      queryClient.invalidateQueries({ queryKey: serviceCaseKeys.detail(serviceCase.id) });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
    },
  });
}

export function useUpdateServiceCaseClaimReference(serviceCaseId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: ServiceCaseClaimReferenceUpdateRequestDto) =>
      api.patch<ServiceCaseDto>(`/service-cases/${serviceCaseId}/claim-reference`, body),
    onSuccess: (serviceCase) => {
      queryClient.invalidateQueries({ queryKey: serviceCaseKeys.detail(serviceCase.id) });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
    },
  });
}

export function useCreateServiceCaseFollowUpTask(serviceCaseId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: ServiceCaseFollowUpTaskRequestDto) =>
      api.post<ServiceCaseFollowUpTaskResponse>(`/service-cases/${serviceCaseId}/follow-up-task`, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: serviceCaseKeys.detail(serviceCaseId) });
      queryClient.invalidateQueries({ queryKey: ['service-cases'] });
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['my', 'tasks'] });
    },
  });
}

export function useLinkServiceCaseCommunication(serviceCaseId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: ServiceCaseCommunicationLinkRequestDto) =>
      api.post<ServiceCaseDto>(`/service-cases/${serviceCaseId}/communication-links`, body),
    onSuccess: (serviceCase) => {
      queryClient.invalidateQueries({ queryKey: serviceCaseKeys.detail(serviceCase.id) });
      queryClient.invalidateQueries({ queryKey: ['service-cases'] });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
    },
  });
}
