import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ApiError } from '@/services/api';
import { useNeuronMessages } from '../hooks/useNeuronMessages';
import { useCompanionAction } from '../hooks/useCompanionAction';
import { useCreateThread, useNeuronThreads } from '../hooks/useNeuronThreads';
import { useSendMessage } from '../hooks/useSendMessage';
import { ActionProvider, type CompanionAction } from '../registry/actionContext';
import type { MessageEnvelope } from '../types';
import { Composer } from './Composer';
import { MessagePartView } from './MessagePartView';
import { ThreadList } from './ThreadList';

/**
 * F0039-S0003 — the conversation-first companion panel.
 *
 * The transcript is **server-owned**: it is replayed from the thread's persisted
 * history in server-sequence order, not reconstructed from browser state. That is the
 * whole point of the slice — a reload, a thread switch, or a restart shows the real
 * conversation, in the order it actually happened.
 *
 * A locally appended turn exists only to keep typing responsive: the user's own message
 * shows immediately, and is dropped once the refetched server history has replaced it,
 * so the transcript never renders the same turn twice. If the send fails the local turn
 * stays put (their text is not thrown away) alongside a bounded failure notice.
 */
export function NeuronConversation() {
  const [activeThreadId, setActiveThreadId] = useState<string | undefined>();
  const [pendingTurns, setPendingTurns] = useState<MessageEnvelope[]>([]);
  const [sendFailed, setSendFailed] = useState(false);

  const threads = useNeuronThreads();
  const createThread = useCreateThread();
  const history = useNeuronMessages(activeThreadId);
  const send = useSendMessage();
  const action = useCompanionAction();

  const transcriptRef = useRef<HTMLDivElement>(null);
  const autoCreated = useRef(false);

  // Select the most recent thread once the list arrives, so reopening the panel lands
  // the user back in the conversation they were last in.
  useEffect(() => {
    if (activeThreadId || threads.data === undefined) return;
    const [mostRecent] = threads.data.data;
    if (mostRecent) {
      setActiveThreadId(mostRecent.thread_id);
      return;
    }
    // A brand-new user has no threads yet; open one so the composer has a home.
    if (!autoCreated.current && !createThread.isPending) {
      autoCreated.current = true;
      createThread.mutate(
        { anchor_type: 'free_form' },
        { onSuccess: (thread) => setActiveThreadId(thread.thread_id) },
      );
    }
  }, [activeThreadId, threads.data, createThread]);

  const serverMessages = useMemo(() => history.data?.data ?? [], [history.data]);

  const messages = useMemo(
    () => [...serverMessages, ...pendingTurns],
    [serverMessages, pendingTurns],
  );

  useEffect(() => {
    const node = transcriptRef.current;
    if (node) node.scrollTop = node.scrollHeight;
  }, [messages.length]);

  const dispatch = useCallback(
    (companionAction: CompanionAction) => {
      action.mutate(
        {
          thread_id: activeThreadId,
          action_type: companionAction.actionType,
          payload: companionAction.payload,
        },
        { onSuccess: () => void history.refetch() },
      );
    },
    [action, activeThreadId, history],
  );

  const handleSend = useCallback(
    (text: string) => {
      if (send.isPending || !activeThreadId) return;
      const userTurn: MessageEnvelope = {
        envelope_version: 1,
        thread_id: activeThreadId,
        message_id: `local-${Date.now()}`,
        role: 'user',
        parts: [{ part_type: 'text', text }],
      };
      setSendFailed(false);
      setPendingTurns((prev) => [...prev, userTurn]);
      send.mutate(
        { text, thread_id: activeThreadId },
        {
          // Refetch rather than trusting the local copy: the server owns message order.
          // The optimistic turn is dropped only once the refetched history replaces it,
          // so the transcript never shows the same turn twice or briefly loses it.
          onSuccess: async () => {
            await history.refetch();
            void threads.refetch();
            setPendingTurns([]);
          },
          // Keep the user's own turn on screen so their text isn't lost, and show a
          // bounded failure notice — never a raw error.
          onError: () => setSendFailed(true),
        },
      );
    },
    [send, activeThreadId, history, threads],
  );

  const handleSelect = useCallback((threadId: string) => {
    // Switching threads clears local turns — they belong to the thread we left.
    setPendingTurns([]);
    setSendFailed(false);
    setActiveThreadId(threadId);
  }, []);

  const historyFailed = history.isError;
  const authRequired =
    history.error instanceof ApiError &&
    (history.error.status === 401 || history.error.status === 403);

  return (
    <ActionProvider value={{ dispatch, pending: action.isPending }}>
      <div data-testid="neuron-conversation" className="flex h-full min-h-0 gap-3">
        {/* A plain div, not <aside>: the companion panel already sits inside a
            landmark, and a nested complementary landmark is an a11y violation. The
            thread list carries its own `aria-label` on the list itself. */}
        <div className="hidden w-48 shrink-0 flex-col border-r border-border-subtle pr-2 sm:flex">
          <ThreadList activeThreadId={activeThreadId} onSelect={handleSelect} />
        </div>

        <div className="flex min-h-0 flex-1 flex-col">
          <div
            ref={transcriptRef}
            data-testid="neuron-transcript"
            className="flex-1 space-y-3 overflow-y-auto"
          >
            {history.isLoading ? (
              <p className="p-3 text-sm text-text-muted">Loading your conversation…</p>
            ) : historyFailed ? (
              <div role="alert" className="space-y-2 p-3 text-sm text-text-muted">
                <p>
                  {authRequired
                    ? 'You need to sign in again to view this conversation.'
                    : 'We couldn’t load this conversation.'}
                </p>
                {!authRequired ? (
                  <button
                    type="button"
                    onClick={() => history.refetch()}
                    className="rounded-md border border-surface-border px-2 py-1 text-xs text-text-secondary hover:bg-surface-highlight"
                  >
                    Retry
                  </button>
                ) : null}
              </div>
            ) : messages.length === 0 ? (
              <p className="p-3 text-sm text-text-muted">
                Ask about your renewals, tasks, or pipeline to get started.
              </p>
            ) : (
              messages.map((message) => (
                <div
                  key={message.message_id}
                  data-role={message.role}
                  className="rounded-lg border border-surface-border bg-surface-card p-3"
                >
                  {message.parts.map((part, index) => (
                    <div key={index} className="mb-1 last:mb-0">
                      <MessagePartView part={part} />
                    </div>
                  ))}
                </div>
              ))
            )}

            {sendFailed ? (
              <div
                role="alert"
                className="rounded-lg border border-surface-border bg-surface-card p-3 text-sm text-text-muted"
              >
                Sorry — I couldn&apos;t send that. Please try again.
              </div>
            ) : null}
          </div>

          <Composer onSend={handleSend} pending={send.isPending} />
        </div>
      </div>
    </ActionProvider>
  );
}
