import { useState, type KeyboardEvent } from 'react';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Skeleton } from '@/components/ui/Skeleton';
import { KpiCardsRow } from '@/features/kpis';
import { cn } from '@/lib/utils';
import { useDashboardOpportunities } from '../hooks/useDashboardOpportunities';
import { useOpportunityFlow } from '../hooks/useOpportunityFlow';
import { useOpportunityOutcomes } from '../hooks/useOpportunityOutcomes';
import { useOpportunityAging } from '../hooks/useOpportunityAging';
import type { OpportunityEntityType } from '../types';
import { OPPORTUNITY_SCOPE_OPTIONS } from '../lib/opportunity-scope';
import type { StoryChapter } from './storyTypes';
import { MobilePipelineSummary } from './MobilePipelineSummary';
import { OpportunityScopeDropdown } from './OpportunityScopeDropdown';
import { VerticalTimeline } from './VerticalTimeline';

const PERIOD_WINDOWS = [30, 90, 180, 365] as const;

const CHAPTERS: { key: StoryChapter; label: string }[] = [
  { key: 'flow', label: 'Flow' },
  { key: 'friction', label: 'Friction' },
  { key: 'outcomes', label: 'Outcomes' },
];

export function StoryCanvas() {
  const [periodDays, setPeriodDays] = useState<(typeof PERIOD_WINDOWS)[number]>(180);
  const [chapter, setChapter] = useState<StoryChapter>('flow');
  const [selectedEntityTypes, setSelectedEntityTypes] = useState<OpportunityEntityType[]>(
    OPPORTUNITY_SCOPE_OPTIONS.map((option) => option.value),
  );

  const opportunitiesQuery = useDashboardOpportunities(periodDays);
  const submissionSelected = selectedEntityTypes.includes('submission');
  const renewalSelected = selectedEntityTypes.includes('renewal');
  const submissionFlowQuery = useOpportunityFlow('submission', periodDays, { enabled: submissionSelected });
  const renewalFlowQuery = useOpportunityFlow('renewal', periodDays, { enabled: renewalSelected });
  const submissionOutcomesQuery = useOpportunityOutcomes(periodDays, ['submission'], { enabled: submissionSelected });
  const renewalOutcomesQuery = useOpportunityOutcomes(periodDays, ['renewal'], { enabled: renewalSelected });
  const submissionAgingQuery = useOpportunityAging('submission', periodDays, { enabled: submissionSelected });
  const renewalAgingQuery = useOpportunityAging('renewal', periodDays, { enabled: renewalSelected });

  function toggleEntityType(entityType: OpportunityEntityType) {
    setSelectedEntityTypes((current) => {
      if (current.includes(entityType)) {
        if (current.length === 1) {
          return current;
        }

        return OPPORTUNITY_SCOPE_OPTIONS
          .map((option) => option.value)
          .filter((value) => current.includes(value) && value !== entityType);
      }

      const next = new Set([...current, entityType]);
      return OPPORTUNITY_SCOPE_OPTIONS
        .map((option) => option.value)
        .filter((value) => next.has(value));
    });
  }

  const selectedLanes = OPPORTUNITY_SCOPE_OPTIONS
    .filter((option) => selectedEntityTypes.includes(option.value))
    .map((option) => ({
      ...option,
      flowQuery: option.value === 'submission' ? submissionFlowQuery : renewalFlowQuery,
      outcomesQuery: option.value === 'submission' ? submissionOutcomesQuery : renewalOutcomesQuery,
      agingQuery: option.value === 'submission' ? submissionAgingQuery : renewalAgingQuery,
    }));

  function focusChapter(
    chapterIndex: number,
    chapterTabs: NodeListOf<HTMLButtonElement> | undefined,
  ) {
    const nextIndex = ((chapterIndex % CHAPTERS.length) + CHAPTERS.length) % CHAPTERS.length;
    setChapter(CHAPTERS[nextIndex].key);
    chapterTabs?.[nextIndex]?.focus();
  }

  function onChapterKeyDown(
    event: KeyboardEvent<HTMLButtonElement>,
    chapterIndex: number,
  ) {
    const chapterTabs = event.currentTarget.parentElement?.querySelectorAll<HTMLButtonElement>('button[role="tab"]');
    if (event.key === 'ArrowRight') {
      event.preventDefault();
      focusChapter(chapterIndex + 1, chapterTabs);
    }

    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      focusChapter(chapterIndex - 1, chapterTabs);
    }

    if (event.key === 'Home') {
      event.preventDefault();
      focusChapter(0, chapterTabs);
    }

    if (event.key === 'End') {
      event.preventDefault();
      focusChapter(CHAPTERS.length - 1, chapterTabs);
    }
  }

  return (
    <section aria-label="Opportunity story canvas">
      <header className="canvas-section canvas-zone-tight" aria-label="Story controls">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div className="sm:hidden">
            <label htmlFor="story-period-window" className="sr-only">
              Opportunity period window
            </label>
            <select
              id="story-period-window"
              aria-label="Opportunity period window"
              value={periodDays}
              onChange={(event) => setPeriodDays(Number(event.target.value) as (typeof PERIOD_WINDOWS)[number])}
              className="w-full rounded-lg border border-surface-border bg-surface-main/65 px-3 py-2 text-sm font-medium text-text-primary"
            >
              {PERIOD_WINDOWS.map((windowDays) => (
                <option key={windowDays} value={windowDays}>
                  {windowDays} days
                </option>
              ))}
            </select>
          </div>

          <div
            className="hidden flex-wrap items-center gap-1 sm:inline-flex"
            role="tablist"
            aria-label="Opportunity period window"
          >
            {PERIOD_WINDOWS.map((windowDays) => {
              const active = windowDays === periodDays;
              return (
                <button
                  key={windowDays}
                  type="button"
                  role="tab"
                  aria-selected={active}
                  aria-controls="story-flow-canvas"
                  onClick={() => setPeriodDays(windowDays)}
                  className={cn(
                    'rounded-full bg-surface-main/55 px-3 py-1 text-xs font-semibold text-text-secondary transition-colors',
                    active && 'story-pill-active',
                  )}
                >
                  {windowDays}d
                </button>
              );
            })}
          </div>

          <div
            className="hidden w-full max-w-full items-center gap-1 overflow-x-auto pb-1 [scrollbar-width:none] lg:inline-flex lg:w-auto"
            role="tablist"
            aria-label="Story chapters"
          >
            {CHAPTERS.map((chapterOption, chapterIndex) => {
              const active = chapterOption.key === chapter;
              return (
                <button
                  key={chapterOption.key}
                  type="button"
                  role="tab"
                  aria-selected={active}
                  aria-controls="story-flow-canvas"
                  tabIndex={active ? 0 : -1}
                  onClick={() => setChapter(chapterOption.key)}
                  onKeyDown={(event) => onChapterKeyDown(event, chapterIndex)}
                  className={cn(
                    'min-w-fit whitespace-nowrap rounded-full bg-surface-main/55 px-3 py-1 text-xs font-semibold text-text-secondary transition-colors',
                    active && 'story-pill-active',
                  )}
                >
                  {chapterOption.label}
                </button>
              );
            })}
          </div>

          <div className="lg:justify-self-end">
            <OpportunityScopeDropdown
              selectedEntityTypes={selectedEntityTypes}
              onToggle={toggleEntityType}
            />
          </div>
        </div>
      </header>

      <KpiCardsRow periodDays={periodDays} className="canvas-zone-tight" />

      <section id="story-flow-canvas" className="canvas-section canvas-zone-default space-y-8">
        {selectedLanes.map((lane) => (
          <section key={lane.value} aria-label={`${lane.label} opportunity lane`} className="space-y-4">
            <div className="flex flex-col gap-1">
              <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-text-muted">
                {lane.label}
              </p>
              <p className="text-sm text-text-secondary">{lane.description}</p>
            </div>

            {lane.flowQuery.isLoading && <Skeleton className="h-[320px] w-full" />}

            {lane.flowQuery.isError && (
              <ErrorFallback
                message={`Unable to load ${lane.label.toLowerCase()} flow`}
                onRetry={() => lane.flowQuery.refetch()}
              />
            )}

            {lane.flowQuery.data && (
              <>
                <div className="hidden lg:block">
                  <VerticalTimeline
                    flow={lane.flowQuery.data}
                    opportunities={opportunitiesQuery.data}
                    outcomes={lane.outcomesQuery.data?.outcomes ?? []}
                    chapter={chapter}
                    periodDays={periodDays}
                    outcomesLoading={lane.outcomesQuery.isLoading}
                    outcomesError={lane.outcomesQuery.isError}
                    onRetryOutcomes={() => lane.outcomesQuery.refetch()}
                    aging={lane.agingQuery.data}
                    outcomeEntityTypes={[lane.value]}
                  />
                </div>
                <div className="lg:hidden">
                  <MobilePipelineSummary
                    flow={lane.flowQuery.data}
                    outcomes={lane.outcomesQuery.data?.outcomes ?? []}
                  />
                </div>
              </>
            )}
          </section>
        ))}
      </section>
    </section>
  );
}
