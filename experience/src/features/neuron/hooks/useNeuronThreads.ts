import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import { THREADS_PATH, threadPath } from '../lib/constants';
import type { CreateThreadRequest, NeuronThread, ThreadPage } from '../types';

const THREADS_KEY = ['neuron', 'threads'] as const;

/**
 * F0039-S0003 — the caller's own threads, most-recently-updated first.
 *
 * The server scopes every row to the authenticated owner, so this hook never needs to
 * filter by user; there is no client-side authorization here to get wrong. An owner
 * with no threads gets an empty list, which drives the panel's empty state rather than
 * an error.
 */
export function useNeuronThreads(limit?: number) {
  const path = limit ? `${THREADS_PATH}?limit=${limit}` : THREADS_PATH;
  return useQuery({
    queryKey: [...THREADS_KEY, limit ?? null],
    queryFn: () => api.get<ThreadPage>(path),
  });
}

/** Create a thread. Pass `thread_idempotency_key` to make a retry return the original. */
export function useCreateThread() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateThreadRequest) =>
      api.post<NeuronThread>(THREADS_PATH, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: THREADS_KEY });
    },
  });
}

/**
 * Rename a thread — title only. The anchor is immutable server-side, so there is no
 * re-anchoring path to expose here.
 */
export function useRenameThread() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ threadId, title }: { threadId: string; title: string }) =>
      api.patch<NeuronThread>(threadPath(threadId), { title }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: THREADS_KEY });
    },
  });
}

/** Soft-delete a thread; it disappears from the list on the next fetch. */
export function useDeleteThread() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (threadId: string) => api.delete(threadPath(threadId)),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: THREADS_KEY });
    },
  });
}

export { THREADS_KEY };
