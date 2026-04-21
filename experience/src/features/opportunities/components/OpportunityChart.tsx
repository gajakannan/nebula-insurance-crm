import { useEffect, useRef, useState } from 'react';
import type {
  OpportunityEntityType,
  OpportunityFlowLinkDto,
  OpportunityFlowNodeDto,
  OpportunityStatusCountDto,
} from '../types';
import { opportunityHex, opportunityHexLight } from '../lib/opportunity-colors';
import { Popover } from '@/components/ui/Popover';
import { useOpportunityFlow } from '../hooks/useOpportunityFlow';
import { OpportunityPopoverContent } from './OpportunityPopover';

interface OpportunityChartProps {
  label: string;
  entityType: OpportunityEntityType;
  statuses: OpportunityStatusCountDto[];
  periodDays: number;
}

interface LayoutNode extends OpportunityFlowNodeDto {
  x: number;
  y: number;
  width: number;
  height: number;
  flowMagnitude: number;
}

interface LayoutLink extends OpportunityFlowLinkDto {
  path: string;
  strokeWidth: number;
  color: string;
  opacity: number;
}

const NODE_BAR_WIDTH = 14;
const COLUMN_PADDING_X = 12;
const CHART_HEIGHT = 300;
const CHART_PADDING_Y = 14;
const NODE_MIN_HEIGHT = 20;
const NODE_MAX_HEIGHT = 88;
const NODE_INSET_Y = 4;
const SIDE_LABEL_GUTTER_X = 120;
const MIN_COLUMN_GAP_X = 84;

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

function formatStatus(status: string): string {
  return status.replace(/([A-Z])/g, ' $1').trim();
}

function buildLayout(
  viewportWidth: number,
  nodes: OpportunityFlowNodeDto[],
  links: OpportunityFlowLinkDto[],
  fallbackColors: Map<string, OpportunityStatusCountDto['colorGroup']>,
): { canvasWidth: number; layoutNodes: LayoutNode[]; layoutLinks: LayoutLink[] } | null {
  if (viewportWidth <= 0 || nodes.length === 0) return null;

  const visibleNodes = nodes
    .filter((node) => node.currentCount > 0 || node.inflowCount > 0 || node.outflowCount > 0)
    .map((node) => ({
      ...node,
      colorGroup: fallbackColors.get(node.status) ?? node.colorGroup,
    }))
    .sort((a, b) => a.displayOrder - b.displayOrder || a.label.localeCompare(b.label));

  if (visibleNodes.length === 0) return null;

  const nonTerminalOrders = Array.from(
    new Set(visibleNodes.filter((node) => !node.isTerminal).map((node) => node.displayOrder)),
  ).sort((a, b) => a - b);
  const orderColumnMap = new Map<number, number>(
    nonTerminalOrders.map((order, index) => [order, index]),
  );
  const terminalColumnIndex = nonTerminalOrders.length;

  const columns = new Map<number, OpportunityFlowNodeDto[]>();
  for (const node of visibleNodes) {
    const column = node.isTerminal
      ? terminalColumnIndex
      : (orderColumnMap.get(node.displayOrder) ?? terminalColumnIndex);
    const existing = columns.get(column);
    if (existing) {
      existing.push(node);
    } else {
      columns.set(column, [node]);
    }
  }

  const columnIndexes = Array.from(columns.keys()).sort((a, b) => a - b);
  const columnCount = Math.max(columnIndexes.length, 1);
  const minPlotWidth =
    COLUMN_PADDING_X * 2 + NODE_BAR_WIDTH + Math.max(0, columnCount - 1) * MIN_COLUMN_GAP_X;
  const minCanvasWidth = minPlotWidth + SIDE_LABEL_GUTTER_X * 2;
  const canvasWidth = Math.max(viewportWidth, minCanvasWidth);
  const plotWidth = canvasWidth - SIDE_LABEL_GUTTER_X * 2;
  const horizontalSpan = plotWidth - COLUMN_PADDING_X * 2 - NODE_BAR_WIDTH;

  const flowMagnitudes = visibleNodes.map((node) =>
    Math.max(node.currentCount, node.inflowCount, node.outflowCount, 1),
  );
  const maxMagnitude = Math.max(...flowMagnitudes, 1);

  const layoutNodes: LayoutNode[] = [];

  for (const columnIndex of columnIndexes) {
    const columnNodes = (columns.get(columnIndex) ?? []).sort(
      (a, b) => a.displayOrder - b.displayOrder || a.label.localeCompare(b.label),
    );

    const provisionalHeights = columnNodes.map((node) =>
      clamp(
        (Math.max(node.currentCount, node.inflowCount, node.outflowCount, 1) / maxMagnitude) * NODE_MAX_HEIGHT,
        NODE_MIN_HEIGHT,
        NODE_MAX_HEIGHT,
      ),
    );

    const innerHeight = CHART_HEIGHT - CHART_PADDING_Y * 2;
    const totalNodeHeight = provisionalHeights.reduce((sum, height) => sum + height, 0);
    const gap = columnNodes.length > 1
      ? clamp((innerHeight - totalNodeHeight) / (columnNodes.length - 1), 8, 18)
      : 0;
    const occupiedHeight = totalNodeHeight + gap * Math.max(0, columnNodes.length - 1);
    let cursorY = CHART_PADDING_Y + Math.max(0, (innerHeight - occupiedHeight) / 2);

    const x = columnCount === 1
      ? SIDE_LABEL_GUTTER_X + (plotWidth - NODE_BAR_WIDTH) / 2
      : SIDE_LABEL_GUTTER_X + COLUMN_PADDING_X + columnIndex * (horizontalSpan / (columnCount - 1));

    for (let i = 0; i < columnNodes.length; i++) {
      const node = columnNodes[i];
      const height = provisionalHeights[i];
      const flowMagnitude = Math.max(node.currentCount, node.inflowCount, node.outflowCount, 1);

      layoutNodes.push({
        ...node,
        x,
        y: cursorY,
        width: NODE_BAR_WIDTH,
        height,
        flowMagnitude,
      });

      cursorY += height + gap;
    }
  }

  const layoutByStatus = new Map(layoutNodes.map((node) => [node.status, node] as const));
  const visibleLinks = links
    .filter((link) => link.count > 0)
    .filter((link) => layoutByStatus.has(link.sourceStatus) && layoutByStatus.has(link.targetStatus));

  const outgoingTotals = new Map<string, number>();
  const incomingTotals = new Map<string, number>();
  for (const link of visibleLinks) {
    outgoingTotals.set(link.sourceStatus, (outgoingTotals.get(link.sourceStatus) ?? 0) + link.count);
    incomingTotals.set(link.targetStatus, (incomingTotals.get(link.targetStatus) ?? 0) + link.count);
  }

  const outgoingCursor = new Map<string, number>();
  const incomingCursor = new Map<string, number>();

  const sortedLinks = [...visibleLinks].sort((a, b) => {
    const sourceA = layoutByStatus.get(a.sourceStatus)!;
    const sourceB = layoutByStatus.get(b.sourceStatus)!;
    if (sourceA.x !== sourceB.x) return sourceA.x - sourceB.x;

    const targetA = layoutByStatus.get(a.targetStatus)!;
    const targetB = layoutByStatus.get(b.targetStatus)!;
    if (targetA.y !== targetB.y) return targetA.y - targetB.y;

    return b.count - a.count;
  });

  const layoutLinks: LayoutLink[] = [];

  for (const link of sortedLinks) {
    const source = layoutByStatus.get(link.sourceStatus)!;
    const target = layoutByStatus.get(link.targetStatus)!;

    const sourceAvailable = Math.max(source.height - NODE_INSET_Y * 2, 1);
    const targetAvailable = Math.max(target.height - NODE_INSET_Y * 2, 1);
    const sourceTotal = Math.max(outgoingTotals.get(source.status) ?? 0, 1);
    const targetTotal = Math.max(incomingTotals.get(target.status) ?? 0, 1);

    const sourceSpan = (link.count / sourceTotal) * sourceAvailable;
    const targetSpan = (link.count / targetTotal) * targetAvailable;

    const sourceOffset = outgoingCursor.get(source.status) ?? 0;
    const targetOffset = incomingCursor.get(target.status) ?? 0;

    outgoingCursor.set(source.status, sourceOffset + sourceSpan);
    incomingCursor.set(target.status, targetOffset + targetSpan);

    const sx = source.x + source.width;
    const tx = target.x;
    const sy = source.y + NODE_INSET_Y + sourceOffset + sourceSpan / 2;
    const ty = target.y + NODE_INSET_Y + targetOffset + targetSpan / 2;

    const dx = tx - sx;
    const cp1x = sx + dx * 0.42;
    const cp2x = tx - dx * 0.42;
    const path = `M ${sx} ${sy} C ${cp1x} ${sy}, ${cp2x} ${ty}, ${tx} ${ty}`;

    layoutLinks.push({
      ...link,
      path,
      strokeWidth: clamp(Math.min(sourceSpan, targetSpan), 2, 22),
      color: opportunityHex(source.colorGroup),
      opacity: source.isTerminal || target.isTerminal ? 0.34 : 0.24,
    });
  }

  return { canvasWidth, layoutNodes, layoutLinks };
}

export function OpportunityChart({ label, entityType, statuses, periodDays }: OpportunityChartProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [containerWidth, setContainerWidth] = useState(() =>
    typeof window === 'undefined' ? 0 : Math.max(window.innerWidth, 320),
  );
  const { data: flow, isLoading: flowLoading, isError: flowError } = useOpportunityFlow(
    entityType,
    periodDays,
  );

  const totalOpen = statuses.reduce((sum, s) => sum + s.count, 0);

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const updateWidth = () => setContainerWidth(el.offsetWidth);
    updateWidth();

    const observer = new ResizeObserver(updateWidth);
    observer.observe(el);

    return () => observer.disconnect();
  }, []);

  const fallbackColors = new Map(statuses.map((status) => [status.status, status.colorGroup] as const));
  const layout = flow
    ? buildLayout(containerWidth, flow.nodes, flow.links, fallbackColors)
    : null;

  const noData = totalOpen === 0 && !flowLoading && (flow?.links.length ?? 0) === 0;

  if (noData) {
    return (
      <div>
        <div className="mb-2 flex items-center justify-between">
          <h3 className="text-xs font-medium uppercase tracking-wider text-text-muted">{label}</h3>
        </div>
        <p className="text-xs text-text-muted">No opportunities or transitions in the selected window.</p>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-3 flex items-center justify-between gap-3">
        <h3 className="text-xs font-medium uppercase tracking-wider text-text-muted">{label}</h3>
        <span className="text-xs text-text-muted">{totalOpen} open</span>
      </div>

      <div ref={containerRef} className="relative overflow-x-auto pb-2">
        <div
          className="relative"
          style={{
            height: CHART_HEIGHT,
            width: layout ? layout.canvasWidth : '100%',
            minWidth: '100%',
          }}
        >
          {layout && (
            <svg
              className="absolute inset-0 pointer-events-none"
              width={layout.canvasWidth}
              height={CHART_HEIGHT}
              viewBox={`0 0 ${layout.canvasWidth} ${CHART_HEIGHT}`}
              aria-hidden="true"
            >
              {layout.layoutLinks.map((link) => (
                <path
                  key={`${link.sourceStatus}->${link.targetStatus}`}
                  d={link.path}
                  fill="none"
                  stroke={link.color}
                  strokeOpacity={link.opacity}
                  strokeWidth={link.strokeWidth}
                  strokeLinecap="round"
                />
              ))}
            </svg>
          )}

          {layout?.layoutNodes.map((node) => {
            const hex = opportunityHex(node.colorGroup);
            const hexLight = opportunityHexLight(node.colorGroup);
            const displayedCount = node.currentCount > 0 ? node.currentCount : Math.max(node.inflowCount, node.outflowCount);
            const countLabel = node.currentCount > 0 ? 'current' : 'flow';
            const showSideLabel = node.isTerminal || node.displayOrder <= 2;
            const sideLabelClass = node.isTerminal
              ? 'left-[calc(100%+6px)] text-left'
              : 'right-[calc(100%+6px)] text-right';

            const nodeElement = (
              <button
                type="button"
                className="absolute cursor-pointer rounded-sm focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-nebula-violet"
                title={`${node.label} (${displayedCount} ${countLabel})`}
                aria-label={`${node.label}: ${displayedCount} ${countLabel}. Inflow ${node.inflowCount}, outflow ${node.outflowCount}. Click for details.`}
                style={{
                  left: node.x,
                  top: node.y,
                  width: node.width,
                  height: node.height,
                }}
              >
                <div
                  className="absolute inset-0 rounded-sm"
                  style={{
                    background: `linear-gradient(180deg, ${hexLight}, ${hex})`,
                    boxShadow: `0 0 8px ${hex}25`,
                  }}
                />
                <span className="absolute -top-4 left-1/2 -translate-x-1/2 rounded bg-surface-panel/95 px-1 py-0.5 text-xs font-semibold leading-none text-text-secondary shadow-sm">
                  {displayedCount}
                </span>
                {showSideLabel && (
                  <span
                    className={`absolute top-1/2 hidden max-w-28 -translate-y-1/2 text-xs leading-tight text-text-muted sm:block ${sideLabelClass}`}
                  >
                    {node.label}
                  </span>
                )}
              </button>
            );

            if (node.currentCount <= 0) {
              return <div key={node.status}>{nodeElement}</div>;
            }

            return (
              <Popover key={node.status} trigger={nodeElement}>
                <OpportunityPopoverContent entityType={entityType} status={node.status} />
              </Popover>
            );
          })}

          {!layout && flowLoading && (
            <div className="absolute inset-0 flex items-center justify-center">
              <p className="text-xs text-text-muted">Loading opportunity flow...</p>
            </div>
          )}

          {!layout && !flowLoading && (
            <div className="absolute inset-0 flex items-center justify-center rounded-lg border border-border-muted bg-surface-panel/50">
              <p className="text-xs text-text-muted">
                {flowError ? 'Unable to load flow data.' : 'No transition flow data available.'}
              </p>
            </div>
          )}
        </div>
      </div>

      <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1">
        {statuses.map((s) => (
          <div key={s.status} className="flex items-center gap-1.5">
            <span
              className="h-2 w-2 rounded-full"
              style={{ background: opportunityHex(s.colorGroup) }}
            />
            <span className="text-xs text-text-muted">{formatStatus(s.status)}</span>
            <span className="text-xs font-medium text-text-secondary">{s.count}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
