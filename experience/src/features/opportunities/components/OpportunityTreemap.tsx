import { useMemo, useState } from 'react';
import type { OpportunityHierarchyNodeDto, OpportunityColorGroup } from '../types';
import { useOpportunityHierarchy } from '../hooks/useOpportunityHierarchy';
import { opportunityHex } from '../lib/opportunity-colors';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { OpportunityPopoverContent } from './OpportunityPopover';

interface OpportunityTreemapProps {
  periodDays: number;
}

interface TreemapRect {
  node: OpportunityHierarchyNodeDto;
  x: number;
  y: number;
  w: number;
  h: number;
}

/**
 * Squarified treemap layout (Bruls, Huizing, van Wijk).
 * Groups items into rows, optimizing aspect ratio at each step.
 */
function squarify(
  nodes: OpportunityHierarchyNodeDto[],
  x: number,
  y: number,
  w: number,
  h: number,
  totalArea: number,
): TreemapRect[] {
  if (nodes.length === 0 || totalArea === 0) return [];

  const sorted = [...nodes].sort((a, b) => b.count - a.count);
  const area = w * h;
  const rects: TreemapRect[] = [];

  let remaining = [...sorted];
  let cx = x;
  let cy = y;
  let cw = w;
  let ch = h;

  while (remaining.length > 0) {
    const shortSide = Math.min(cw, ch);
    const remainingTotal = remaining.reduce((s, n) => s + n.count, 0);

    // Find best row: add items until aspect ratio worsens
    const row: OpportunityHierarchyNodeDto[] = [remaining[0]];
    let rowSum = remaining[0].count;

    for (let i = 1; i < remaining.length; i++) {
      const candidate = remaining[i];
      const newSum = rowSum + candidate.count;
      if (worstAspect(row, rowSum, shortSide, remainingTotal, area) >=
          worstAspect([...row, candidate], newSum, shortSide, remainingTotal, area)) {
        row.push(candidate);
        rowSum = newSum;
      } else {
        break;
      }
    }

    // Lay out the row
    const rowFraction = rowSum / remainingTotal;
    const isHorizontal = cw >= ch;
    const rowSpan = isHorizontal ? cw * rowFraction : ch * rowFraction;

    let pos = 0;
    for (const node of row) {
      const nodeFraction = node.count / rowSum;
      if (isHorizontal) {
        const rh = ch * nodeFraction;
        rects.push({ node, x: cx, y: cy + pos, w: rowSpan, h: rh });
        pos += rh;
      } else {
        const rw = cw * nodeFraction;
        rects.push({ node, x: cx + pos, y: cy, w: rw, h: rowSpan });
        pos += rw;
      }
    }

    // Shrink remaining area
    if (isHorizontal) {
      cx += rowSpan;
      cw -= rowSpan;
    } else {
      cy += rowSpan;
      ch -= rowSpan;
    }

    remaining = remaining.slice(row.length);
  }

  return rects;
}

function worstAspect(
  row: OpportunityHierarchyNodeDto[],
  rowSum: number,
  shortSide: number,
  totalRemaining: number,
  totalArea: number,
): number {
  if (rowSum === 0 || totalRemaining === 0) return Infinity;
  const rowArea = (rowSum / totalRemaining) * totalArea;
  const rowLength = rowArea / shortSide;
  let worst = 0;
  for (const node of row) {
    const nodeArea = (node.count / totalRemaining) * totalArea;
    const nodeSize = nodeArea / rowLength;
    const aspect = Math.max(rowLength / nodeSize, nodeSize / rowLength);
    if (aspect > worst) worst = aspect;
  }
  return worst;
}

function flattenLeaves(
  root: OpportunityHierarchyNodeDto,
): OpportunityHierarchyNodeDto[] {
  if (!root.children || root.children.length === 0) return [root];
  return root.children.flatMap(flattenLeaves);
}

function extractEntityAndStatus(
  nodeId: string,
): { entityType: 'submission' | 'renewal'; status: string } | null {
  const parts = nodeId.split(':');
  if (parts.length < 3) return null;
  const entityType = parts[0] as 'submission' | 'renewal';
  const status = parts.slice(2).join(':');
  return { entityType, status };
}

const TREEMAP_WIDTH = 600;
const TREEMAP_HEIGHT = 340;
const PAD = 2;

export function OpportunityTreemap({ periodDays }: OpportunityTreemapProps) {
  const { data, isLoading, isError, refetch } =
    useOpportunityHierarchy(periodDays);
  const [selected, setSelected] = useState<string | null>(null);

  const leaves = useMemo(
    () => (data ? flattenLeaves(data.root).filter((l) => l.count > 0) : []),
    [data],
  );

  const rects = useMemo(
    () =>
      data && data.root.count > 0
        ? squarify(leaves, 0, 0, TREEMAP_WIDTH, TREEMAP_HEIGHT, data.root.count)
        : [],
    [data, leaves],
  );

  const selectedParsed = selected ? extractEntityAndStatus(selected) : null;
  const selectedNode = selected
    ? leaves.find((l) => l.id === selected)
    : null;

  if (isLoading) {
    return <Skeleton className="h-[340px] w-full" />;
  }

  if (isError || !data) {
    return (
      <ErrorFallback
        message="Unable to load hierarchy data"
        onRetry={() => refetch()}
      />
    );
  }

  if (data.root.count === 0) {
    return (
      <div className="py-4 text-center text-sm text-text-muted">
        No open opportunities in this period
      </div>
    );
  }

  return (
    <section aria-label="Opportunities composition treemap">
      <h3 className="mb-3 text-sm font-semibold text-text-secondary">
        Composition
      </h3>
      <svg
        viewBox={`0 0 ${TREEMAP_WIDTH} ${TREEMAP_HEIGHT}`}
        className="w-full"
        role="img"
        aria-label={`Treemap showing ${data.root.count} total opportunities across statuses`}
      >
        {rects.map((r) => {
          const hex = r.node.colorGroup
            ? opportunityHex(r.node.colorGroup as OpportunityColorGroup)
            : '#6b7280';
          const isSelected = selected === r.node.id;
          const rw = Math.max(r.w - PAD * 2, 0);
          const rh = Math.max(r.h - PAD * 2, 0);
          const showLabel = rw > 60 && rh > 36;

          return (
            <g key={r.node.id}>
              <rect
                x={r.x + PAD}
                y={r.y + PAD}
                width={rw}
                height={rh}
                fill={hex}
                fillOpacity={isSelected ? 1 : 0.8}
                stroke={isSelected ? 'white' : 'rgba(0,0,0,0.2)'}
                strokeWidth={isSelected ? 2 : 1}
                rx={6}
                className="cursor-pointer transition-all hover:brightness-110"
                onClick={() =>
                  setSelected(isSelected ? null : r.node.id)
                }
                tabIndex={0}
                role="button"
                aria-label={`${r.node.label}: ${r.node.count} opportunities (${data.root.count > 0 ? Math.round((r.node.count / data.root.count) * 100) : 0}%)`}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    setSelected(isSelected ? null : r.node.id);
                  } else if (e.key === 'Escape') {
                    setSelected(null);
                  }
                }}
              >
                <title>
                  {r.node.label}: {r.node.count} (
                  {data.root.count > 0
                    ? Math.round((r.node.count / data.root.count) * 100)
                    : 0}
                  %)
                </title>
              </rect>
              {showLabel && (
                <>
                  <text
                    x={r.x + PAD + rw / 2}
                    y={r.y + PAD + rh / 2 - 6}
                    textAnchor="middle"
                    className="pointer-events-none fill-white text-[11px] font-semibold"
                    style={{ textShadow: '0 1px 2px rgba(0,0,0,0.5)' }}
                  >
                    {r.node.label}
                  </text>
                  <text
                    x={r.x + PAD + rw / 2}
                    y={r.y + PAD + rh / 2 + 10}
                    textAnchor="middle"
                    className="pointer-events-none fill-white/90 text-[11px]"
                    style={{ textShadow: '0 1px 2px rgba(0,0,0,0.5)' }}
                  >
                    {r.node.count}
                  </text>
                </>
              )}
            </g>
          );
        })}
      </svg>

      {/* Legend */}
      <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1">
        {leaves.map((l) => {
          const hex = l.colorGroup
            ? opportunityHex(l.colorGroup as OpportunityColorGroup)
            : '#6b7280';
          return (
            <button
              key={l.id}
              type="button"
              className="flex items-center gap-1.5 text-xs text-text-secondary hover:text-text-primary"
              onClick={() => setSelected(selected === l.id ? null : l.id)}
            >
              <span
                className="inline-block h-2.5 w-2.5 rounded-sm"
                style={{ backgroundColor: hex }}
              />
              {l.label} ({l.count})
            </button>
          );
        })}
      </div>

      {/* Drilldown panel for selected tile */}
      {selectedParsed && selectedNode && (
        <div className="mt-3 rounded-lg border border-border-muted bg-surface-card p-3">
          <div className="mb-2 flex items-center justify-between">
            <span className="text-sm font-medium text-text-primary">
              {selectedNode.label} — {selectedNode.count} items
            </span>
            <button
              type="button"
              className="text-xs text-text-muted hover:text-text-secondary"
              onClick={() => setSelected(null)}
              aria-label="Close drilldown"
            >
              Close
            </button>
          </div>
          <OpportunityPopoverContent
            entityType={selectedParsed.entityType}
            status={selectedParsed.status}
          />
        </div>
      )}

      {/* Accessible text summary */}
      <div className="sr-only" role="list" aria-label="Treemap data summary">
        {leaves.map((l) => (
          <div key={l.id} role="listitem">
            {l.label}: {l.count} opportunities
          </div>
        ))}
      </div>
    </section>
  );
}
