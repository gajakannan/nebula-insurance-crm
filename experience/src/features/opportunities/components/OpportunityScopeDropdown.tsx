import { Popover } from '@/components/ui/Popover';
import { cn } from '@/lib/utils';
import type { OpportunityEntityType } from '../types';
import { OPPORTUNITY_SCOPE_OPTIONS, formatOpportunityScopeSummary } from '../lib/opportunity-scope';

interface OpportunityScopeDropdownProps {
  selectedEntityTypes: OpportunityEntityType[];
  onToggle: (entityType: OpportunityEntityType) => void;
}

export function OpportunityScopeDropdown({
  selectedEntityTypes,
  onToggle,
}: OpportunityScopeDropdownProps) {
  const summary = formatOpportunityScopeSummary(selectedEntityTypes);

  return (
    <Popover
      contentAriaLabel="Opportunity scope selector"
      className="w-[min(20rem,calc(100vw-2rem))] p-0"
      trigger={
        <button
          type="button"
          className="rounded-full bg-surface-main/55 px-3 py-1 text-xs font-semibold text-text-secondary transition-colors hover:bg-surface-main/75 hover:text-text-primary"
          aria-label={`Opportunity scope: ${summary}`}
        >
          Scope: {summary}
        </button>
      }
    >
      <div className="space-y-3 p-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-wide text-text-muted">Opportunity Scope</p>
          <p className="mt-1 text-xs text-text-secondary">
            Choose which story lanes appear in the infographic canvas.
          </p>
        </div>

        <fieldset className="space-y-2" aria-label="Opportunity scope selections">
          {OPPORTUNITY_SCOPE_OPTIONS.map((option) => {
            const checked = selectedEntityTypes.includes(option.value);
            const disableUncheck = checked && selectedEntityTypes.length === 1;

            return (
              <label
                key={option.value}
                className={cn(
                  'flex cursor-pointer items-start gap-3 rounded-lg border border-surface-border bg-surface-main/40 px-3 py-2 transition-colors',
                  checked && 'border-nebula-violet/45 bg-surface-main/70',
                  disableUncheck && 'cursor-default opacity-80',
                )}
              >
                <input
                  type="checkbox"
                  checked={checked}
                  disabled={disableUncheck}
                  onChange={() => onToggle(option.value)}
                  className="mt-0.5 h-4 w-4 rounded border-surface-border accent-[var(--color-nebula-violet)]"
                />
                <span className="min-w-0">
                  <span className="block text-sm font-medium text-text-primary">{option.label}</span>
                  <span className="block text-xs text-text-muted">{option.description}</span>
                </span>
              </label>
            );
          })}
        </fieldset>
      </div>
    </Popover>
  );
}
