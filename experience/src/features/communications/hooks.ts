import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  CommunicationEventCorrectionRequestDto,
  CommunicationEventCreateRequestDto,
  CommunicationEventDto,
  CommunicationEventFollowUpRequestDto,
  CommunicationHistoryResponseDto,
  CommunicationLinkEntityType,
} from './types';
import type { TaskDto } from '@/features/tasks';

export const communicationKeys = {
  history: (entityType: CommunicationLinkEntityType, entityId: string) =>
    ['communications', entityType, entityId] as const,
};

export function useCommunicationHistory(
  entityType: CommunicationLinkEntityType,
  entityId: string,
  enabled = true,
) {
  return useQuery({
    queryKey: communicationKeys.history(entityType, entityId),
    queryFn: () => api.get<CommunicationHistoryResponseDto>(
      `/communications?entityType=${entityType}&entityId=${entityId}&page=1&pageSize=20`,
    ),
    enabled: enabled && !!entityId,
  });
}

export function useCreateCommunication(entityType: CommunicationLinkEntityType, entityId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (body: CommunicationEventCreateRequestDto) =>
      api.post<CommunicationEventDto>('/communications', body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: communicationKeys.history(entityType, entityId) });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['my', 'tasks'] });
    },
  });
}

export function useCreateCommunicationFollowUp(entityType: CommunicationLinkEntityType, entityId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ communicationId, body }: { communicationId: string; body: CommunicationEventFollowUpRequestDto }) =>
      api.post<TaskDto>(`/communications/${communicationId}/follow-up-task`, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: communicationKeys.history(entityType, entityId) });
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['my', 'tasks'] });
    },
  });
}

export function useCorrectCommunication(entityType: CommunicationLinkEntityType, entityId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ communicationId, body }: { communicationId: string; body: CommunicationEventCorrectionRequestDto }) =>
      api.post<CommunicationEventDto>(`/communications/${communicationId}/corrections`, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: communicationKeys.history(entityType, entityId) });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
    },
  });
}
