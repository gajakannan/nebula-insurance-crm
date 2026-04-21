import { cn } from '@/lib/utils';
import { NeuronPanel } from '@/features/neuron';

interface RightChatPanelProps {
  collapsed: boolean;
  fullscreen: boolean;
  onToggleFullscreen: () => void;
}

export function RightChatPanel({ collapsed, fullscreen, onToggleFullscreen }: RightChatPanelProps) {
  const effectiveCollapsed = collapsed && !fullscreen;

  return (
    <div className={cn(fullscreen ? 'block' : 'hidden lg:block')}>
      <aside
        className={cn(
          'chat-panel h-full',
          fullscreen ? 'left-0 top-0 w-auto z-[60]' : effectiveCollapsed ? 'w-16' : 'w-[22rem]',
        )}
      >
        <NeuronPanel
          collapsed={effectiveCollapsed}
          fullscreen={fullscreen}
          onToggleFullscreen={onToggleFullscreen}
        />
      </aside>
    </div>
  );
}
