import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import { threadHistoryPath } from '../lib/constants';
import type { ThreadHistoryPage } from '../types';

/**
 * F0039-S0003 — server-rehydrated transcript for one thread.
 *
 * The server owns message order (a per-thread `sequence`), so the panel replays exactly
 * what was persisted rather than reconstructing order from local state. That is what
 * makes a reload or a thread switch show the real conversation instead of whatever the
 * browser happened to still hold in memory.
 *
 * Disabled until a thread is selected — there is no history to fetch before then.
 */
export function useNeuronMessages(threadId: string | undefined, limit = 50) {
  return useQuery({
    queryKey: ['neuron', 'thread-messages', threadId ?? null, limit],
    queryFn: () =>
      api.get<ThreadHistoryPage>(`${threadHistoryPath(threadId as string)}?limit=${limit}`),
    enabled: Boolean(threadId),
  });
}
