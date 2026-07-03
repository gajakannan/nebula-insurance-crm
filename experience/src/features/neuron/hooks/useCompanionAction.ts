import { useMutation } from '@tanstack/react-query';
import { api } from '@/services/api';
import { ACTIONS_PATH } from '../lib/constants';
import type { MessageEnvelope } from '../types';

export interface CompanionActionRequest {
  thread_id?: string;
  action_id?: string;
  action_type: string;
  payload?: Record<string, unknown>;
}

/**
 * F0038-S0003 — POST an allow-listed component action to Neuron. The shared `api`
 * client forwards the user token; Neuron re-authorizes via the engine and returns a
 * message envelope (e.g. the drill context) to render in the thread.
 */
export function useCompanionAction() {
  return useMutation({
    mutationFn: (request: CompanionActionRequest) =>
      api.post<MessageEnvelope>(ACTIONS_PATH, request),
  });
}
