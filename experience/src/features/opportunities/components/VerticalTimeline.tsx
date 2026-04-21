import { useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { cn } from '@/lib/utils';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Skeleton } from '@/components/ui/Skeleton';
import type {
  OpportunityAgingDto,
  DashboardOpportunitiesDto,
  OpportunityEntityType,
  OpportunityFlowDto,
  OpportunityOutcomeDto,
} from '../types';
import { useTimelineGeometry } from '../hooks/useTimelineGeometry';
import { StageNodeStoryPanel } from './StageNodeStoryPanel';
import { TerminalOutcomesRail } from './TerminalOutcomesRail';
import { TimelineStageNode } from './TimelineStageNode';
import { TimelineSvgOverlay } from './TimelineSvgOverlay';
import type { OutcomeAnchor, StageAnchor } from './storyTimelineTypes';
import type { StoryChapter } from './storyTypes';

interface VerticalTimelineProps {
  flow: OpportunityFlowDto;
  opportunities?: DashboardOpportunitiesDto;
  outcomes: OpportunityOutcomeDto[];
  chapter: StoryChapter;
  periodDays: number;
  outcomesLoading: boolean;
  outcomesError: boolean;
  onRetryOutcomes: () => void;
  aging?: OpportunityAgingDto;
  outcomeEntityTypes?: OpportunityEntityType[];
}

export function VerticalTimeline({
  flow,
  opportunities,
  outcomes,
  chapter,
  periodDays,
  outcomesLoading,
  outcomesError,
  onRetryOutcomes,
  aging,
  outcomeEntityTypes,
}: VerticalTimelineProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const stageButtonRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const [containerWidth, setContainerWidth] = useState(1160);

  useEffect(() => {
    if (!containerRef.current || typeof ResizeObserver === 'undefined') return;

    const observer = new ResizeObserver((entries) => {
      const entry = entries[0];
      if (entry) setContainerWidth(Math.round(entry.contentRect.width));
    });

    observer.observe(containerRef.current);
    return () => observer.disconnect();
  }, []);

  const { geometry, registerBadge, registerSpineCell, registerOutcome } = useTimelineGeometry(containerRef);

  const stageNodes = flow.nodes
    .filter((node) => !node.isTerminal)
    .sort((left, right) => left.displayOrder - right.displayOrder);

  if (stageNodes.length === 0) {
    return (
      <div className="py-8 text-center text-sm text-text-muted">
        No opportunity flow data for the selected period.
      </div>
    );
  }

  const phoneLayout = containerWidth < 560;
  const compactLayout = containerWidth < 900;
  const allStageCountsZero = stageNodes.every((node) => node.currentCount === 0);
  const allOutcomesZero = outcomes.length > 0 && outcomes.every((o) => o.count === 0);

  const stageAnchors: StageAnchor[] = stageNodes.map((node) => ({
    status: node.status,
    label: node.label,
    avgDwellDays: node.avgDwellDays,
    emphasis: node.emphasis,
  }));

  const outcomeAnchors: OutcomeAnchor[] = outcomes.map((outcome) => ({
    key: outcome.key,
    label: outcome.label,
    branchStyle: outcome.branchStyle,
    count: outcome.count,
    percentOfTotal: outcome.percentOfTotal,
  }));

  function moveStageFocus(targetIndex: number) {
    if (stageNodes.length === 0) return;
    const bounded = ((targetIndex % stageNodes.length) + stageNodes.length) % stageNodes.length;
    stageButtonRefs.current[bounded]?.focus();
  }

  function onStageKeyDown(event: KeyboardEvent<HTMLButtonElement>, stageIndex: number) {
    if (event.key === 'ArrowDown' || event.key === 'ArrowRight') {
      event.preventDefault();
      moveStageFocus(stageIndex + 1);
    }

    if (event.key === 'ArrowUp' || event.key === 'ArrowLeft') {
      event.preventDefault();
      moveStageFocus(stageIndex - 1);
    }

    if (event.key === 'Home') {
      event.preventDefault();
      moveStageFocus(0);
    }

    if (event.key === 'End') {
      event.preventDefault();
      moveStageFocus(stageNodes.length - 1);
    }
  }

  const spineColWidth = phoneLayout ? 0 : compactLayout ? 36 : 48;

  return (
    <div ref={containerRef} className="canvas-chapter-overlay relative overflow-x-hidden">
      {/* SVG overlay — decorative connections drawn from measured DOM positions */}
      {geometry && (
        <TimelineSvgOverlay
          geometry={geometry}
          stageNodes={stageNodes}
          outcomes={outcomes}
          chapter={chapter}
        />
      )}

      {/* CSS Grid layout */}
      <div
        className={cn(
          'relative mx-auto w-full pt-8',
          phoneLayout ? 'flex flex-col items-center gap-8' : 'grid',
        )}
        style={
          phoneLayout
            ? undefined
            : { gridTemplateColumns: `1fr ${spineColWidth}px 1fr`, rowGap: compactLayout ? '2.5rem' : '3.5rem' }
        }
      >
        {stageNodes.map((node, index) => {
          const anchor = stageAnchors[index];
          const isLeft = index % 2 === 0;
          const faded = node.currentCount === 0;
          const emphasisClass = chapter === 'friction'
            ? `flow-emphasis-${node.emphasis ?? 'normal'}`
            : 'flow-emphasis-normal';

          if (phoneLayout) {
            // Phone: single-column stacked layout
            return (
              <div key={anchor.status} className="flex flex-col items-center gap-3">
                {chapter === 'friction' && (
                  <FrictionIndicator anchor={anchor} />
                )}
                <div ref={registerBadge(anchor.status)} className={cn(faded && 'opacity-60', chapter === 'outcomes' && 'opacity-70')}>
                  <TimelineStageNode
                    anchor={anchor}
                    entityType={flow.entityType}
                    node={node}
                    compact={compactLayout}
                    emphasisClass={emphasisClass}
                    buttonRef={(el) => { stageButtonRefs.current[index] = el; }}
                    onKeyDown={(e) => onStageKeyDown(e, index)}
                    popoverDisabled={allStageCountsZero}
                  />
                </div>
                {!allStageCountsZero && (
                  <StageNodeStoryPanel
                    node={node}
                    entityType={flow.entityType}
                    opportunities={opportunities}
                    periodDays={periodDays}
                    chapter={chapter}
                    outcomes={outcomes}
                    agingStatus={aging?.statuses.find((s) => s.status === node.status)}
                  />
                )}
              </div>
            );
          }

          // Desktop / compact: 3-column grid row
          return (
            <div key={anchor.status} className="col-span-3 grid" style={{ gridTemplateColumns: 'subgrid' }}>
              {/* Left cell */}
              <div className={cn('flex items-center gap-3', isLeft ? 'justify-end' : '')}>
                {isLeft && !allStageCountsZero && (
                  <StageNodeStoryPanel
                    node={node}
                    entityType={flow.entityType}
                    opportunities={opportunities}
                    periodDays={periodDays}
                    chapter={chapter}
                    outcomes={outcomes}
                    agingStatus={aging?.statuses.find((s) => s.status === node.status)}
                  />
                )}
                {isLeft && (
                  <div className={cn('flex flex-col items-center gap-1', faded && 'opacity-60', chapter === 'outcomes' && 'opacity-70')}>
                    {chapter === 'friction' && <FrictionIndicator anchor={anchor} />}
                    <div ref={registerBadge(anchor.status)}>
                      <TimelineStageNode
                        anchor={anchor}
                        entityType={flow.entityType}
                        node={node}
                        compact={compactLayout}
                        emphasisClass={emphasisClass}
                        buttonRef={(el) => { stageButtonRefs.current[index] = el; }}
                        onKeyDown={(e) => onStageKeyDown(e, index)}
                        popoverDisabled={allStageCountsZero}
                      />
                    </div>
                  </div>
                )}
              </div>

              {/* Spine cell */}
              <div ref={registerSpineCell(anchor.status)} className="flex items-center justify-center" />

              {/* Right cell */}
              <div className={cn('flex items-center gap-3', !isLeft ? 'justify-start' : '')}>
                {!isLeft && (
                  <div className={cn('flex flex-col items-center gap-1', faded && 'opacity-60', chapter === 'outcomes' && 'opacity-70')}>
                    {chapter === 'friction' && <FrictionIndicator anchor={anchor} />}
                    <div ref={registerBadge(anchor.status)}>
                      <TimelineStageNode
                        anchor={anchor}
                        entityType={flow.entityType}
                        node={node}
                        compact={compactLayout}
                        emphasisClass={emphasisClass}
                        buttonRef={(el) => { stageButtonRefs.current[index] = el; }}
                        onKeyDown={(e) => onStageKeyDown(e, index)}
                        popoverDisabled={allStageCountsZero}
                      />
                    </div>
                  </div>
                )}
                {!isLeft && !allStageCountsZero && (
                  <StageNodeStoryPanel
                    node={node}
                    entityType={flow.entityType}
                    opportunities={opportunities}
                    periodDays={periodDays}
                    chapter={chapter}
                    outcomes={outcomes}
                    agingStatus={aging?.statuses.find((s) => s.status === node.status)}
                  />
                )}
              </div>
            </div>
          );
        })}

        {/* Outcomes section */}
        <div className={phoneLayout ? 'w-full' : 'col-span-3'}>
          {outcomeAnchors.length === 0 ? (
            <p className="py-8 text-center text-xs font-medium text-text-muted">
              No exits in period
            </p>
          ) : (
            <>
              <TerminalOutcomesRail
                anchors={outcomeAnchors}
                periodDays={periodDays}
                chapter={chapter}
                allOutcomesZero={allOutcomesZero}
                entityTypes={outcomeEntityTypes}
                registerOutcome={registerOutcome}
              />
              {chapter === 'outcomes' && (
                <>
                  {outcomesLoading && (
                    <div className="mx-4 mt-3">
                      <Skeleton className="h-14 w-full" />
                    </div>
                  )}
                  {outcomesError && (
                    <div className="mx-4 mt-3">
                      <ErrorFallback message="Unable to load outcomes overlay data" onRetry={onRetryOutcomes} />
                    </div>
                  )}
                </>
              )}
            </>
          )}

          {allOutcomesZero && (
            <p className="py-4 text-center text-xs font-medium text-text-muted">
              No outcomes in period
            </p>
          )}

          {allStageCountsZero && (
            <p className="py-4 text-center text-xs font-medium text-text-muted">
              No activity in period
            </p>
          )}
        </div>
      </div>
    </div>
  );
}

/** Inline friction indicator rendered above a stage badge */
function FrictionIndicator({ anchor }: { anchor: StageAnchor }) {
  const emphasis = anchor.emphasis ?? 'normal';
  return (
    <div className="flex flex-col items-center">
      <div className="rounded-full bg-surface-main/75 px-2 py-0.5 text-[10px] uppercase tracking-wide text-text-muted">
        {emphasis}
      </div>
      <p
        className={cn(
          'mt-1 text-center text-[11px] font-semibold text-text-secondary',
          emphasis !== 'normal' && `flow-emphasis-${emphasis}`,
        )}
      >
        {(anchor.avgDwellDays ?? 0).toFixed(1)}d
      </p>
    </div>
  );
}
