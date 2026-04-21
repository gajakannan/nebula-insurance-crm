import { useId, useRef } from 'react';
import { cn } from '@/lib/utils';

interface TabsProps {
  tabs: string[];
  activeTab: string;
  onTabChange: (tab: string) => void;
  children: React.ReactNode;
}

export function Tabs({ tabs, activeTab, onTabChange, children }: TabsProps) {
  const tabsId = useId();
  const tabRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const activeIndex = Math.max(0, tabs.indexOf(activeTab));

  const focusTabAt = (index: number) => {
    const clamped = Math.max(0, Math.min(tabs.length - 1, index));
    tabRefs.current[clamped]?.focus();
  };

  const tabId = (index: number) => `${tabsId}-tab-${index}`;
  const panelId = `${tabsId}-panel`;

  return (
    <div>
      <div
        role="tablist"
        aria-label="Section tabs"
        className="flex gap-1 overflow-x-auto border-b border-surface-border"
      >
        {tabs.map((tab, index) => (
          <button
            key={tab}
            id={tabId(index)}
            ref={(el) => {
              tabRefs.current[index] = el;
            }}
            type="button"
            role="tab"
            aria-selected={tab === activeTab}
            aria-controls={panelId}
            tabIndex={tab === activeTab ? 0 : -1}
            onClick={() => onTabChange(tab)}
            onKeyDown={(e) => {
              let targetIndex: number | null = null;
              if (e.key === 'ArrowRight') targetIndex = (index + 1) % tabs.length;
              if (e.key === 'ArrowLeft') targetIndex = (index - 1 + tabs.length) % tabs.length;
              if (e.key === 'Home') targetIndex = 0;
              if (e.key === 'End') targetIndex = tabs.length - 1;
              if (targetIndex === null) return;
              e.preventDefault();
              onTabChange(tabs[targetIndex]);
              focusTabAt(targetIndex);
            }}
            className={cn(
              'whitespace-nowrap px-4 py-2.5 text-sm font-medium transition-colors',
              tab === activeTab
                ? 'border-b-2 border-nebula-violet text-text-primary'
                : 'text-text-muted hover:text-text-secondary',
            )}
          >
            {tab}
          </button>
        ))}
      </div>
      <div
        id={panelId}
        role="tabpanel"
        aria-labelledby={tabId(activeIndex)}
        className="pt-4"
      >
        {children}
      </div>
    </div>
  );
}
