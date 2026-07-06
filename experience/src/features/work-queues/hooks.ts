import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  AssignmentRule,
  AssignmentRuleUpsertRequest,
  CoverageWindow,
  CoverageWindowUpsertRequest,
  ListResponse,
  QueueReassignmentRequest,
  QueueRebalanceRequest,
  QueueWorkItem,
  RoutingEvent,
  WorkQueue,
  WorkQueueUpsertRequest,
  WorkType,
} from './types';

const keys = {
  queues: (workType?: string, status?: string) => ['work-queues', workType ?? '', status ?? ''],
  rules: (queueId?: string) => ['assignment-rules', queueId ?? ''],
  coverage: (queueId?: string) => ['coverage-windows', queueId ?? ''],
  items: (queueId?: string, status?: string) => ['queue-work-items', queueId ?? '', status ?? ''],
  events: (sourceType?: string) => ['routing-events', sourceType ?? ''],
};

const API_PREFIX = '/api';

function query(params: Record<string, string | undefined>) {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value) search.set(key, value);
  });
  return search.toString();
}

export function useWorkQueues(filters: { workType?: WorkType | ''; status?: string }) {
  return useQuery({
    queryKey: keys.queues(filters.workType, filters.status),
    queryFn: () => api.get<ListResponse<WorkQueue>>(`${API_PREFIX}/work-queues?${query(filters)}`),
  });
}

export function useAssignmentRules(queueId?: string) {
  return useQuery({
    queryKey: keys.rules(queueId),
    queryFn: () => api.get<ListResponse<AssignmentRule>>(`${API_PREFIX}/assignment-rules?${query({ queueId })}`),
  });
}

export function useCoverageWindows(queueId?: string) {
  return useQuery({
    queryKey: keys.coverage(queueId),
    queryFn: () => api.get<ListResponse<CoverageWindow>>(`${API_PREFIX}/coverage-windows?${query({ queueId })}`),
  });
}

export function useQueueItems(queueId?: string, status?: string) {
  return useQuery({
    queryKey: keys.items(queueId, status),
    enabled: Boolean(queueId),
    queryFn: () => api.get<ListResponse<QueueWorkItem>>(`${API_PREFIX}/work-queues/${queueId}/items?${query({ status })}`),
  });
}

export function useRoutingEvents(sourceType?: string) {
  return useQuery({
    queryKey: keys.events(sourceType),
    queryFn: () => api.get<ListResponse<RoutingEvent>>(`${API_PREFIX}/routing-events?${query({ sourceType })}`),
  });
}

export function useWorkQueueMutations() {
  const queryClient = useQueryClient();
  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['work-queues'] }),
      queryClient.invalidateQueries({ queryKey: ['assignment-rules'] }),
      queryClient.invalidateQueries({ queryKey: ['coverage-windows'] }),
      queryClient.invalidateQueries({ queryKey: ['queue-work-items'] }),
      queryClient.invalidateQueries({ queryKey: ['routing-events'] }),
    ]);
  };

  return {
    createQueue: useMutation({
      mutationFn: (body: WorkQueueUpsertRequest) => api.post<WorkQueue>(`${API_PREFIX}/work-queues`, body),
      onSuccess: invalidate,
    }),
    updateQueue: useMutation({
      mutationFn: ({ queue, body }: { queue: WorkQueue; body: WorkQueueUpsertRequest }) =>
        api.put<WorkQueue>(`${API_PREFIX}/work-queues/${queue.id}`, body, { 'If-Match': String(queue.rowVersion) }),
      onSuccess: invalidate,
    }),
    createRule: useMutation({
      mutationFn: (body: AssignmentRuleUpsertRequest) => api.post<AssignmentRule>(`${API_PREFIX}/assignment-rules`, body),
      onSuccess: invalidate,
    }),
    createCoverage: useMutation({
      mutationFn: (body: CoverageWindowUpsertRequest) => api.post<CoverageWindow>(`${API_PREFIX}/coverage-windows`, body),
      onSuccess: invalidate,
    }),
    reassign: useMutation({
      mutationFn: ({ item, body }: { item: QueueWorkItem; body: QueueReassignmentRequest }) =>
        api.put<QueueWorkItem>(`${API_PREFIX}/queue-work-items/${item.id}/assignment`, body, { 'If-Match': String(item.rowVersion) }),
      onSuccess: invalidate,
    }),
    rebalance: useMutation({
      mutationFn: ({ queueId, body }: { queueId: string; body: QueueRebalanceRequest }) =>
        api.post<QueueWorkItem[]>(`${API_PREFIX}/work-queues/${queueId}/rebalance`, body),
      onSuccess: invalidate,
    }),
  };
}

export type { WorkQueue, QueueWorkItem };
