import { useMemo, useState } from 'react';
import type { BrokerTrends, TrendPoint } from '../types';
import { formatCurrency } from '../utils';

interface TrendsChartProps {
  trends: BrokerTrends;
}

interface TooltipState {
  point: TrendPoint;
  x: number;
  y: number;
}

const WIDTH = 760;
const HEIGHT = 280;
const MARGIN = { top: 24, right: 16, bottom: 70, left: 44 };

export default function TrendsChart({ trends }: TrendsChartProps) {
  const [tooltip, setTooltip] = useState<TooltipState | null>(null);
  const maxValue = Math.max(
    1,
    ...trends.points.flatMap((point) => [point.submissions, point.bound]),
  );
  const plotWidth = WIDTH - MARGIN.left - MARGIN.right;
  const plotHeight = HEIGHT - MARGIN.top - MARGIN.bottom;
  const rotateLabels = trends.points.length > 6;
  const yTicks = useMemo(() => {
    return Array.from({ length: 5 }, (_, index) => Math.ceil((maxValue / 4) * index));
  }, [maxValue]);
  const groupWidth = plotWidth / Math.max(1, trends.points.length);
  const barWidth = Math.max(5, Math.min(18, groupWidth * 0.24));

  const yForValue = (value: number) => (
    MARGIN.top + plotHeight - (value / maxValue) * plotHeight
  );

  return (
    <section className="glass-card rounded-xl p-5">
      <div className="mb-4 flex items-center justify-between gap-4">
        <h2 className="text-xs uppercase tracking-widest text-text-muted">
          Submission & Bind Trends
        </h2>
        <span
          className="rounded-full px-3 py-1 text-xs font-medium"
          style={{
            background: 'color-mix(in srgb, var(--accent-secondary) 14%, transparent)',
            color: 'var(--accent-secondary)',
            border: '1px solid color-mix(in srgb, var(--accent-secondary) 28%, transparent)',
          }}
        >
          {trends.granularity === 'week' ? 'Weekly' : 'Monthly'}
        </span>
      </div>
      <div className="relative">
        <svg
          viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
          role="img"
          aria-label="Submission and bind trend bar chart"
          className="h-auto w-full overflow-visible"
          onMouseLeave={() => setTooltip(null)}
        >
          {yTicks.map((tick, index) => {
            const y = yForValue(tick);
            return (
              <g key={`${tick}-${index}`}>
                <line
                  x1={MARGIN.left}
                  x2={WIDTH - MARGIN.right}
                  y1={y}
                  y2={y}
                  stroke="var(--surface-border)"
                  strokeDasharray="4 6"
                />
                <text
                  x={MARGIN.left - 10}
                  y={y + 4}
                  textAnchor="end"
                  fontSize="11"
                  fill="var(--text-muted)"
                >
                  {tick}
                </text>
              </g>
            );
          })}

          {trends.points.map((point, index) => {
            const centerX = MARGIN.left + groupWidth * index + groupWidth / 2;
            const submissionsHeight = plotHeight - (yForValue(point.submissions) - MARGIN.top);
            const boundHeight = plotHeight - (yForValue(point.bound) - MARGIN.top);
            const labelY = MARGIN.top + plotHeight + 24;
            return (
              <g key={point.period_label}>
                <rect
                  x={centerX - barWidth - 2}
                  y={yForValue(point.submissions)}
                  width={barWidth}
                  height={submissionsHeight}
                  rx="3"
                  fill="var(--data-primary)"
                  onMouseMove={(event) => setTooltip({
                    point,
                    x: event.clientX,
                    y: event.clientY,
                  })}
                />
                <rect
                  x={centerX + 2}
                  y={yForValue(point.bound)}
                  width={barWidth}
                  height={boundHeight}
                  rx="3"
                  fill="var(--data-secondary)"
                  onMouseMove={(event) => setTooltip({
                    point,
                    x: event.clientX,
                    y: event.clientY,
                  })}
                />
                <text
                  x={centerX}
                  y={labelY}
                  textAnchor={rotateLabels ? 'end' : 'middle'}
                  transform={rotateLabels ? `rotate(-40 ${centerX} ${labelY})` : undefined}
                  fontSize="11"
                  fill="var(--text-muted)"
                >
                  {point.period_label}
                </text>
              </g>
            );
          })}
        </svg>

        {tooltip && (
          <div
            className="pointer-events-none fixed z-50 rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-xs text-text-secondary shadow-lg"
            style={{
              left: tooltip.x + 12,
              top: tooltip.y + 12,
            }}
          >
            <p className="font-semibold text-text-primary">{tooltip.point.period_label}</p>
            <p>Submissions: {tooltip.point.submissions}</p>
            <p>Bound: {tooltip.point.bound}</p>
            <p>Renewals: {tooltip.point.renewals_completed}</p>
            <p>Premium: {formatCurrency(tooltip.point.premium)}</p>
          </div>
        )}
      </div>
      <div className="mt-3 flex flex-wrap items-center gap-4 text-xs text-text-secondary">
        <span className="inline-flex items-center gap-2">
          <span
            className="h-2.5 w-2.5 rounded-full"
            style={{ background: 'var(--data-primary)' }}
          />
          Submissions
        </span>
        <span className="inline-flex items-center gap-2">
          <span
            className="h-2.5 w-2.5 rounded-full"
            style={{ background: 'var(--data-secondary)' }}
          />
          Bound
        </span>
      </div>
    </section>
  );
}

