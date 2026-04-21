import { useMemo, useState } from 'react';
import type { OpportunityHierarchyNodeDto, OpportunityColorGroup } from '../types';
import { useOpportunityHierarchy } from '../hooks/useOpportunityHierarchy';
import { opportunityHex } from '../lib/opportunity-colors';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';

interface OpportunitySunburstProps {
  periodDays: number;
}

interface ArcData {
  node: OpportunityHierarchyNodeDto;
  startAngle: number;
  endAngle: number;
  innerRadius: number;
  outerRadius: number;
  depth: number;
}

const SIZE = 360;
const CX = SIZE / 2;
const CY = SIZE / 2;
const RING_WIDTH = 48;
const INNER_RADIUS = 56;
const GAP_DEGREES = 1.2;

function buildArcs(
  node: OpportunityHierarchyNodeDto,
  startAngle: number,
  endAngle: number,
  depth: number,
): ArcData[] {
  if (node.count === 0) return [];

  const arcs: ArcData[] = [];
  const innerRadius = INNER_RADIUS + depth * RING_WIDTH;
  const outerRadius = innerRadius + RING_WIDTH - 3;

  if (depth > 0) {
    arcs.push({ node, startAngle, endAngle, innerRadius, outerRadius, depth });
  }

  if (node.children && node.children.length > 0) {
    let currentAngle = startAngle;
    const angleRange = endAngle - startAngle;
    const activeChildren = node.children.filter((c) => c.count > 0);

    for (let i = 0; i < activeChildren.length; i++) {
      const child = activeChildren[i];
      const childAngle = (child.count / node.count) * angleRange;
      const gapOffset = activeChildren.length > 1 ? GAP_DEGREES / 2 : 0;
      arcs.push(
        ...buildArcs(
          child,
          currentAngle + gapOffset,
          currentAngle + childAngle - gapOffset,
          depth + 1,
        ),
      );
      currentAngle += childAngle;
    }
  }

  return arcs;
}

function arcPath(
  cx: number,
  cy: number,
  innerR: number,
  outerR: number,
  startAngle: number,
  endAngle: number,
): string {
  const startRad = (startAngle - 90) * (Math.PI / 180);
  const endRad = (endAngle - 90) * (Math.PI / 180);

  const x1 = cx + outerR * Math.cos(startRad);
  const y1 = cy + outerR * Math.sin(startRad);
  const x2 = cx + outerR * Math.cos(endRad);
  const y2 = cy + outerR * Math.sin(endRad);
  const x3 = cx + innerR * Math.cos(endRad);
  const y3 = cy + innerR * Math.sin(endRad);
  const x4 = cx + innerR * Math.cos(startRad);
  const y4 = cy + innerR * Math.sin(startRad);

  const largeArc = endAngle - startAngle > 180 ? 1 : 0;

  return [
    `M ${x1} ${y1}`,
    `A ${outerR} ${outerR} 0 ${largeArc} 1 ${x2} ${y2}`,
    `L ${x3} ${y3}`,
    `A ${innerR} ${innerR} 0 ${largeArc} 0 ${x4} ${y4}`,
    'Z',
  ].join(' ');
}

function flattenLeaves(
  root: OpportunityHierarchyNodeDto,
): OpportunityHierarchyNodeDto[] {
  if (!root.children || root.children.length === 0) return [root];
  return root.children.flatMap(flattenLeaves);
}

export function OpportunitySunburst({ periodDays }: OpportunitySunburstProps) {
  const { data, isLoading, isError, refetch } =
    useOpportunityHierarchy(periodDays);
  const [hovered, setHovered] = useState<ArcData | null>(null);

  const arcs = useMemo(
    () => (data ? buildArcs(data.root, 0, 360, 0) : []),
    [data],
  );

  const leaves = useMemo(
    () => (data ? flattenLeaves(data.root).filter((l) => l.count > 0) : []),
    [data],
  );

  if (isLoading) {
    return <Skeleton className="mx-auto h-[360px] w-[360px] rounded-full" />;
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
    <section aria-label="Opportunities hierarchy sunburst">
      <h3 className="mb-3 text-sm font-semibold text-text-secondary">
        Hierarchy
      </h3>
      <div className="flex justify-center">
        <svg
          viewBox={`0 0 ${SIZE} ${SIZE}`}
          className="max-w-[360px]"
          role="img"
          aria-label={`Sunburst chart showing ${data.root.count} total opportunities`}
        >
          {arcs.map((arc) => {
            const hex = arc.node.colorGroup
              ? opportunityHex(arc.node.colorGroup as OpportunityColorGroup)
              : '#6b7280';

            const isHovered = hovered?.node.id === arc.node.id;
            const opacity = isHovered ? 1 : arc.depth === 1 ? 0.6 : 0.85;

            return (
              <path
                key={`${arc.node.id}-d${arc.depth}`}
                d={arcPath(
                  CX,
                  CY,
                  arc.innerRadius,
                  arc.outerRadius,
                  arc.startAngle,
                  arc.endAngle,
                )}
                fill={hex}
                fillOpacity={opacity}
                stroke="var(--color-surface-main)"
                strokeWidth={2}
                className="cursor-pointer transition-all"
                onMouseEnter={() => setHovered(arc)}
                onMouseLeave={() => setHovered(null)}
                onFocus={() => setHovered(arc)}
                onBlur={() => setHovered(null)}
                tabIndex={0}
                role="button"
                aria-label={`${arc.node.label}: ${arc.node.count} opportunities`}
              >
                <title>
                  {arc.node.label}: {arc.node.count}
                </title>
              </path>
            );
          })}

          {/* Center label */}
          <text
            x={CX}
            y={CY - 10}
            textAnchor="middle"
            className="fill-text-primary text-2xl font-bold"
          >
            {hovered ? hovered.node.count : data.root.count}
          </text>
          <text
            x={CX}
            y={CY + 10}
            textAnchor="middle"
            className="fill-text-muted text-[11px]"
          >
            {hovered ? hovered.node.label : 'Total'}
          </text>
        </svg>
      </div>

      {/* Legend */}
      <div className="mt-3 flex flex-wrap justify-center gap-x-4 gap-y-1">
        {leaves.map((l) => {
          const hex = l.colorGroup
            ? opportunityHex(l.colorGroup as OpportunityColorGroup)
            : '#6b7280';
          return (
            <span key={l.id} className="flex items-center gap-1.5 text-xs text-text-secondary">
              <span
                className="inline-block h-2.5 w-2.5 rounded-full"
                style={{ backgroundColor: hex }}
              />
              {l.label} ({l.count})
            </span>
          );
        })}
      </div>

      {/* Accessible text summary */}
      <div className="sr-only" role="list" aria-label="Sunburst data summary">
        {arcs.map((a) => (
          <div key={a.node.id} role="listitem">
            {a.node.label}: {a.node.count} opportunities
          </div>
        ))}
      </div>
    </section>
  );
}
