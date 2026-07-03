import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import { GLANCE_PATH } from '../lib/constants';
import type { GlanceResponse } from '../types';

/**
 * F0038-S0002 — fetch the Day-at-a-Glance assembly from the Neuron service.
 * The shared `api` client forwards the user's bearer token; the engine (via Neuron)
 * enforces authorization. A `thread_id` resumes the single glance thread.
 */
export function useGlance(threadId?: string) {
  const path = threadId
    ? `${GLANCE_PATH}?thread_id=${encodeURIComponent(threadId)}`
    : GLANCE_PATH;

  return useQuery({
    queryKey: ['neuron', 'glance', threadId ?? null],
    queryFn: () => api.get<GlanceResponse>(path),
  });
}
