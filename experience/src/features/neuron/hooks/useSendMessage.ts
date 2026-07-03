import { useMutation } from '@tanstack/react-query';
import { api } from '@/services/api';
import { MESSAGES_PATH } from '../lib/constants';
import type { MessageEnvelope } from '../types';

export interface SendMessageRequest {
  text: string;
  thread_id?: string;
}

/**
 * F0038-S0007 — send a free-text message to the companion. The scope guard runs
 * server-side before any handler: in-scope CRM intents route to a specialist head and
 * return content; out-of-scope / prompt-injection messages return a polite CRM redirect
 * (never a general-assistant answer). The shared `api` client forwards the user token;
 * the engine (via Neuron) authorizes any resulting read.
 */
export function useSendMessage() {
  return useMutation({
    mutationFn: (request: SendMessageRequest) =>
      api.post<MessageEnvelope>(MESSAGES_PATH, request),
  });
}
