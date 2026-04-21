import type { OpportunityViewMode } from '../types';

interface OpportunityViewSwitcherProps {
  activeView: OpportunityViewMode;
  onViewChange: (view: OpportunityViewMode) => void;
}

const VIEW_OPTIONS: { key: OpportunityViewMode; label: string }[] = [
  { key: 'pipeline', label: 'Pipeline' },
  { key: 'heatmap', label: 'Heatmap' },
  { key: 'treemap', label: 'Treemap' },
  { key: 'sunburst', label: 'Sunburst' },
];

export function OpportunityViewSwitcher({
  activeView,
  onViewChange,
}: OpportunityViewSwitcherProps) {
  return (
    <div
      className="inline-flex items-center gap-1 rounded-lg border border-border-muted bg-surface-panel p-1"
      role="tablist"
      aria-label="Opportunity view mode"
    >
      {VIEW_OPTIONS.map(({ key, label }) => {
        const active = key === activeView;
        return (
          <button
            key={key}
            type="button"
            role="tab"
            aria-selected={active}
            onClick={() => onViewChange(key)}
            className={
              active
                ? 'rounded-md border border-nebula-violet/45 bg-nebula-violet/15 px-2.5 py-1 text-xs font-semibold text-nebula-violet shadow-sm'
                : 'rounded-md px-2.5 py-1 text-xs text-text-muted hover:text-text-secondary'
            }
          >
            {label}
          </button>
        );
      })}
    </div>
  );
}
