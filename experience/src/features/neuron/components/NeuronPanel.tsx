import { Bot, Expand, Minimize } from 'lucide-react';
import { NeuronConversation } from './NeuronConversation';

interface NeuronPanelProps {
  collapsed: boolean;
  fullscreen: boolean;
  onToggleFullscreen: () => void;
}

/**
 * F0039-S0003 — the companion panel is now **conversation-first**: the body is a durable,
 * server-rehydrated conversation with a thread list, not a single ephemeral glance thread.
 * The Daily Brief still appears — it is persisted as an assistant envelope and replays
 * inside the transcript like any other turn (F0038's zone components render through the
 * same registry). The panel chrome (collapse/fullscreen) is unchanged so the app layout
 * keeps working.
 */
export function NeuronPanel({ collapsed, fullscreen, onToggleFullscreen }: NeuronPanelProps) {
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
          {fullscreen ? 'Neuron · Conversations' : 'Neuron'}
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
        <div className="flex min-h-0 flex-1 flex-col px-3 pb-3 pt-2">
          <NeuronConversation />
        </div>
      )}
    </>
  );
}
