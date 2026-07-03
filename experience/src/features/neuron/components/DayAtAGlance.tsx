import { useCallback, useState } from 'react';
import { cn } from '@/lib/utils';
import { ApiError } from '@/services/api';
import { useCompanionAction } from '../hooks/useCompanionAction';
import { useGlance } from '../hooks/useGlance';
import { useSendMessage } from '../hooks/useSendMessage';
import { ZONE_DISPLAY_ORDER } from '../lib/constants';
import { ActionProvider, type CompanionAction } from '../registry/actionContext';
import type { MessageEnvelope, ZonePayload } from '../types';
import { Composer } from './Composer';
import { MessagePartView } from './MessagePartView';
import { ZoneSlot } from './ZoneSlot';

/**
 * F0038-S0002/S0003/S0007 — the Day-at-a-Glance shell. Fetches the zone-dispatch assembly
 * and lays out one slot per registered head (Renewals prominent/first, stub zones after)
 * plus the assistant message envelope. Registry components dispatch actions (e.g. drill)
 * through the ActionContext; the composer (S0007) sends scope-guarded free-text messages.
 * All returned envelopes are appended to the single thread.
 */
function orderZones(zones: ZonePayload[]): ZonePayload[] {
  const rank = (id: string) => {
    const index = ZONE_DISPLAY_ORDER.indexOf(id);
    return index < 0 ? ZONE_DISPLAY_ORDER.length : index;
  };
  return [...zones].sort((a, b) => rank(a.zone_id) - rank(b.zone_id));
}

export function DayAtAGlance() {
  const { data, isLoading, isError, error, refetch } = useGlance();
  const [appended, setAppended] = useState<MessageEnvelope[]>([]);
  const action = useCompanionAction();
  const send = useSendMessage();

  const dispatch = useCallback(
    (companionAction: CompanionAction) => {
      action.mutate(
        {
          thread_id: data?.thread_id,
          action_type: companionAction.actionType,
          payload: companionAction.payload,
        },
        { onSuccess: (envelope) => setAppended((prev) => [...prev, envelope]) },
      );
    },
    [action, data?.thread_id],
  );

  const handleSend = useCallback(
    (text: string) => {
      if (send.isPending) return;
      // Echo the user's turn immediately, then append Neuron's scope-guarded reply.
      const userTurn: MessageEnvelope = {
        envelope_version: 1,
        thread_id: data?.thread_id ?? '',
        message_id: `local-${Date.now()}`,
        role: 'user',
        parts: [{ part_type: 'text', text }],
      };
      setAppended((prev) => [...prev, userTurn]);
      send.mutate(
        { text, thread_id: data?.thread_id },
        {
          onSuccess: (reply) => setAppended((prev) => [...prev, reply]),
          onError: () =>
            setAppended((prev) => [
              ...prev,
              {
                envelope_version: 1,
                thread_id: data?.thread_id ?? '',
                message_id: `local-err-${Date.now()}`,
                role: 'assistant',
                parts: [
                  { part_type: 'text', text: "Sorry — I couldn't send that. Please try again." },
                ],
              },
            ]),
        },
      );
    },
    [send, data?.thread_id],
  );

  if (isLoading) {
    return (
      <div data-testid="glance-loading" className="p-3 text-sm text-text-muted">
        Loading your day at a glance…
      </div>
    );
  }

  if (isError) {
    const authRequired =
      error instanceof ApiError && (error.status === 401 || error.status === 403);
    return (
      <div data-testid="glance-error" role="alert" className="space-y-2 p-3 text-sm text-text-muted">
        <p>
          {authRequired
            ? 'You need to sign in again to view your day at a glance.'
            : 'We couldn’t load your day at a glance.'}
        </p>
        {!authRequired ? (
          <button
            type="button"
            onClick={() => refetch()}
            className="rounded-md border border-surface-border px-2 py-1 text-xs text-text-secondary hover:bg-surface-highlight"
          >
            Retry
          </button>
        ) : null}
      </div>
    );
  }

  if (!data) {
    return null;
  }

  const zones = orderZones(data.zones);

  return (
    <ActionProvider value={{ dispatch, pending: action.isPending }}>
      <div data-testid="day-at-a-glance" className="flex h-full min-h-0 flex-col">
        <div className="flex-1 space-y-3 overflow-y-auto">
          <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
            {zones.map((zone) => (
              <div
                key={zone.zone_id}
                className={zone.zone_id === 'renewals' ? 'sm:col-span-2 lg:col-span-3' : undefined}
              >
                <ZoneSlot zone={zone} />
              </div>
            ))}
          </div>

          <div
            data-testid="glance-thread"
            className="rounded-lg border border-surface-border bg-surface-card p-3"
          >
            {data.message.parts.map((part, index) => (
              <div key={index} className="mb-1 last:mb-0">
                <MessagePartView part={part} />
              </div>
            ))}

            {appended.map((envelope, envelopeIndex) => (
              <div
                key={envelope.message_id ?? envelopeIndex}
                data-testid="thread-message"
                data-role={envelope.role}
                className={cn('mt-2 space-y-1', envelope.role === 'user' && 'text-right')}
              >
                {envelope.parts.map((part, index) => (
                  <MessagePartView key={index} part={part} />
                ))}
              </div>
            ))}
          </div>
        </div>

        <Composer onSend={handleSend} pending={send.isPending} />
      </div>
    </ActionProvider>
  );
}
