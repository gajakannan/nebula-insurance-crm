import { useEffect, useRef } from 'react';
import { Bot, Expand, Mic, Minimize, Paperclip, Send } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useNeuronChat } from '../hooks/useNeuronChat';

interface NeuronPanelProps {
  collapsed: boolean;
  fullscreen: boolean;
  onToggleFullscreen: () => void;
}

export function NeuronPanel({ collapsed, fullscreen, onToggleFullscreen }: NeuronPanelProps) {
  const { messages, draft, setDraft, sendMessage } = useNeuronChat();
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (collapsed || !scrollRef.current) return;
    scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
  }, [messages, collapsed]);

  return (
    <>
      <div className="flex h-14 items-center gap-2 px-3">
        <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-nebula-violet/20 text-nebula-violet">
          <Bot size={16} />
        </span>
        <span
          className="overflow-hidden whitespace-nowrap text-sm font-semibold text-text-primary transition-all duration-200"
          style={{ width: collapsed ? 0 : 'auto', opacity: collapsed ? 0 : 1 }}
        >
          {fullscreen ? 'Nebula - Neuron' : 'Neuron'}
        </span>
        <button
          type="button"
          onClick={onToggleFullscreen}
          aria-label={fullscreen ? 'Restore chat panel size' : 'Expand chat panel to fullscreen'}
          className="ml-auto inline-flex h-8 w-8 items-center justify-center rounded-md text-text-muted transition-colors hover:bg-surface-highlight hover:text-text-secondary"
        >
          {fullscreen ? <Minimize size={16} /> : <Expand size={16} />}
        </button>
      </div>

      {collapsed ? (
        <div className="flex-1" />
      ) : (
        <>
          <div ref={scrollRef} className="flex-1 space-y-3 overflow-y-auto px-3 py-2">
            {messages.map((message) => (
              <div key={message.id} className={cn('flex', message.role === 'user' ? 'justify-end' : 'justify-start')}>
                <div
                  className={cn(
                    'max-w-[90%] rounded-lg px-3 py-2 text-sm leading-relaxed',
                    message.role === 'user'
                      ? 'border border-nebula-violet/35 bg-nebula-violet/20 text-text-primary'
                      : 'border border-surface-border bg-surface-card text-text-secondary',
                  )}
                >
                  {message.content}
                </div>
              </div>
            ))}
          </div>

          <div className="px-3 pb-3 pt-2">
            <div className="relative">
              <textarea
                value={draft}
                onChange={(event) => setDraft(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' && !event.shiftKey) {
                    event.preventDefault();
                    sendMessage();
                  }
                }}
                rows={4}
                placeholder="Ask anything..."
                className="w-full resize-none rounded-lg border border-surface-border bg-surface-card px-3 pb-10 pt-2 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              />
              <div className="absolute inset-x-2 bottom-2 flex items-center justify-between">
                <div className="flex items-center gap-1">
                  <button
                    type="button"
                    aria-label="Attach file (coming soon)"
                    title="Attachments coming soon"
                    className="inline-flex h-7 w-7 items-center justify-center rounded-md text-text-muted transition-colors hover:bg-surface-highlight hover:text-text-secondary"
                  >
                    <Paperclip size={15} />
                  </button>
                  <button
                    type="button"
                    aria-label="Talk to chat (coming soon)"
                    title="Voice input coming soon"
                    className="inline-flex h-7 w-7 items-center justify-center rounded-md text-text-muted transition-colors hover:bg-surface-highlight hover:text-text-secondary"
                  >
                    <Mic size={15} />
                  </button>
                </div>
                <button
                  type="button"
                  onClick={sendMessage}
                  disabled={!draft.trim()}
                  aria-label="Send message"
                  className="inline-flex h-7 w-7 items-center justify-center rounded-md bg-nebula-fuchsia/10 text-nebula-fuchsia transition-colors hover:bg-nebula-fuchsia/20 disabled:cursor-not-allowed disabled:opacity-65"
                >
                  <Send size={15} />
                </button>
              </div>
            </div>
          </div>
        </>
      )}
    </>
  );
}
