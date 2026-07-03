import { useState, type FormEvent, type KeyboardEvent } from 'react';
import { SendHorizontal } from 'lucide-react';

/**
 * F0038-S0007 — the companion composer. Sends a free-text message to Neuron, which
 * scope-guards it before any handler runs. Enter sends; Shift+Enter inserts a newline.
 * Purely presentational: the parent owns the thread + send mutation.
 */
export function Composer({
  onSend,
  pending,
}: {
  onSend: (text: string) => void;
  pending: boolean;
}) {
  const [text, setText] = useState('');

  const submit = () => {
    const clean = text.trim();
    if (!clean || pending) return;
    onSend(clean);
    setText('');
  };

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    submit();
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      submit();
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex items-end gap-2 pt-2">
      <textarea
        rows={2}
        value={text}
        onChange={(event) => setText(event.target.value)}
        onKeyDown={handleKeyDown}
        aria-label="Message the companion"
        placeholder="Ask about your renewals…"
        className="w-full resize-none rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted focus:border-nebula-violet focus:outline-none"
      />
      <button
        type="submit"
        disabled={pending || text.trim().length === 0}
        aria-label="Send message"
        className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-nebula-violet/20 text-nebula-violet transition-colors hover:bg-nebula-violet/30 disabled:cursor-not-allowed disabled:opacity-50"
      >
        <SendHorizontal size={16} />
      </button>
    </form>
  );
}
