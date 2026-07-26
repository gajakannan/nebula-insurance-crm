import { useEffect, useRef, useState } from 'react';
import { Check, MessageSquarePlus, Pencil, Trash2, X } from 'lucide-react';
import { cn } from '@/lib/utils';
import {
  useCreateThread,
  useDeleteThread,
  useNeuronThreads,
  useRenameThread,
} from '../hooks/useNeuronThreads';
import type { NeuronThread } from '../types';

interface ThreadListProps {
  activeThreadId?: string;
  onSelect: (threadId: string) => void;
}

/**
 * F0039-S0003 — the conversation list.
 *
 * Threads are server-owned and owner-scoped, so this component only ever renders what
 * the API returned for the signed-in user. Rename is inline (a title is a small edit,
 * not a dialog's worth of ceremony) and delete asks for confirmation because it is
 * destructive from the user's point of view even though the server soft-deletes.
 */
export function ThreadList({ activeThreadId, onSelect }: ThreadListProps) {
  const { data, isLoading, isError } = useNeuronThreads();
  const createThread = useCreateThread();
  const renameThread = useRenameThread();
  const deleteThread = useDeleteThread();

  const [editingId, setEditingId] = useState<string | null>(null);
  const [draftTitle, setDraftTitle] = useState('');
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const editInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (editingId) editInputRef.current?.focus();
  }, [editingId]);

  const threads = data?.data ?? [];

  const startRename = (thread: NeuronThread) => {
    setEditingId(thread.thread_id);
    setDraftTitle(thread.title);
  };

  const commitRename = (threadId: string) => {
    const title = draftTitle.trim();
    // An empty title is rejected server-side; don't send a request we know will fail.
    if (!title) {
      setEditingId(null);
      return;
    }
    renameThread.mutate({ threadId, title }, { onSettled: () => setEditingId(null) });
  };

  const handleCreate = () => {
    createThread.mutate(
      { anchor_type: 'free_form' },
      { onSuccess: (thread) => onSelect(thread.thread_id) },
    );
  };

  return (
    <div className="flex min-h-0 flex-col gap-2" data-testid="neuron-thread-list">
      <button
        type="button"
        onClick={handleCreate}
        disabled={createThread.isPending}
        className="inline-flex items-center gap-2 rounded-md border border-border-subtle px-2 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-highlight hover:text-text-primary disabled:opacity-60"
      >
        <MessageSquarePlus size={14} aria-hidden="true" />
        New conversation
      </button>

      {isLoading ? (
        <p className="px-2 py-1 text-xs text-text-muted">Loading conversations…</p>
      ) : isError ? (
        <p className="px-2 py-1 text-xs text-text-muted" role="status">
          Your conversations are unavailable right now.
        </p>
      ) : threads.length === 0 ? (
        <p className="px-2 py-1 text-xs text-text-muted">
          No conversations yet — start one above.
        </p>
      ) : (
        <ul className="flex min-h-0 flex-col gap-0.5 overflow-y-auto" aria-label="Conversations">
          {threads.map((thread) => {
            const isActive = thread.thread_id === activeThreadId;
            const isEditing = editingId === thread.thread_id;
            const isConfirming = confirmingId === thread.thread_id;

            return (
              <li key={thread.thread_id}>
                {isEditing ? (
                  <div className="flex items-center gap-1 px-1 py-0.5">
                    <input
                      ref={editInputRef}
                      value={draftTitle}
                      maxLength={120}
                      aria-label={`Rename ${thread.title}`}
                      onChange={(event) => setDraftTitle(event.target.value)}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter') commitRename(thread.thread_id);
                        if (event.key === 'Escape') setEditingId(null);
                      }}
                      className="min-w-0 flex-1 rounded border border-border-subtle bg-surface-base px-1.5 py-1 text-xs text-text-primary"
                    />
                    <button
                      type="button"
                      aria-label="Save title"
                      onClick={() => commitRename(thread.thread_id)}
                      className="rounded p-1 text-text-muted hover:text-text-primary"
                    >
                      <Check size={13} />
                    </button>
                    <button
                      type="button"
                      aria-label="Cancel rename"
                      onClick={() => setEditingId(null)}
                      className="rounded p-1 text-text-muted hover:text-text-primary"
                    >
                      <X size={13} />
                    </button>
                  </div>
                ) : (
                  <div
                    className={cn(
                      'group flex items-center gap-1 rounded-md px-2 py-1.5 transition-colors',
                      isActive ? 'bg-surface-highlight' : 'hover:bg-surface-highlight/60',
                    )}
                  >
                    <button
                      type="button"
                      onClick={() => onSelect(thread.thread_id)}
                      aria-current={isActive ? 'true' : undefined}
                      className="min-w-0 flex-1 truncate text-left text-xs text-text-secondary group-hover:text-text-primary"
                    >
                      {thread.title}
                    </button>

                    {isConfirming ? (
                      <>
                        <button
                          type="button"
                          aria-label={`Confirm delete ${thread.title}`}
                          onClick={() =>
                            deleteThread.mutate(thread.thread_id, {
                              onSettled: () => setConfirmingId(null),
                            })
                          }
                          className="rounded p-1 text-status-danger hover:bg-surface-base"
                        >
                          <Check size={13} />
                        </button>
                        <button
                          type="button"
                          aria-label="Cancel delete"
                          onClick={() => setConfirmingId(null)}
                          className="rounded p-1 text-text-muted hover:text-text-primary"
                        >
                          <X size={13} />
                        </button>
                      </>
                    ) : (
                      <>
                        <button
                          type="button"
                          aria-label={`Rename ${thread.title}`}
                          onClick={() => startRename(thread)}
                          className="rounded p-1 text-text-muted opacity-0 transition-opacity hover:text-text-primary focus:opacity-100 group-hover:opacity-100"
                        >
                          <Pencil size={13} />
                        </button>
                        <button
                          type="button"
                          aria-label={`Delete ${thread.title}`}
                          onClick={() => setConfirmingId(thread.thread_id)}
                          className="rounded p-1 text-text-muted opacity-0 transition-opacity hover:text-status-danger focus:opacity-100 group-hover:opacity-100"
                        >
                          <Trash2 size={13} />
                        </button>
                      </>
                    )}
                  </div>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
