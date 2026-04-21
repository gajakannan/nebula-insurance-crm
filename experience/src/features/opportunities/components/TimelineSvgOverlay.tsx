import type { OpportunityFlowNodeDto, OpportunityOutcomeDto } from '../types';
import type { TimelineGeometry } from '../hooks/useTimelineGeometry';
import type { StoryChapter } from './storyTypes';

interface TimelineSvgOverlayProps {
  geometry: TimelineGeometry;
  stageNodes: OpportunityFlowNodeDto[];
  outcomes: OpportunityOutcomeDto[];
  chapter: StoryChapter;
}

function branchStroke(
  branchStyle: OpportunityOutcomeDto['branchStyle'],
  muted = false,
) {
  if (muted) {
    if (branchStyle === 'gray_dotted') {
      return {
        stroke: 'color-mix(in srgb, var(--text-muted) 58%, transparent)',
        strokeDasharray: '1 7' as string | undefined,
      };
    }

    if (branchStyle === 'red_dashed') {
      return {
        stroke: 'color-mix(in srgb, var(--text-muted) 58%, transparent)',
        strokeDasharray: '8 6' as string | undefined,
      };
    }

    return {
      stroke: 'color-mix(in srgb, var(--text-muted) 58%, transparent)',
      strokeDasharray: undefined as string | undefined,
    };
  }

  if (branchStyle === 'solid') {
    return { stroke: 'var(--color-status-success)', strokeDasharray: undefined as string | undefined };
  }

  if (branchStyle === 'gray_dotted') {
    return { stroke: 'var(--text-muted)', strokeDasharray: '1 7' };
  }

  return { stroke: 'var(--color-status-error)', strokeDasharray: '8 6' };
}

export function TimelineSvgOverlay({
  geometry,
  stageNodes,
  outcomes,
  chapter,
}: TimelineSvgOverlayProps) {
  const { spineX, stageCenters, spineDots, outcomeCenters, spineTop, spineBottom, containerWidth, containerHeight } = geometry;

  // Collect spine dot Y positions in display order for segments
  const orderedSpineDots = stageNodes
    .map((node) => ({
      status: node.status,
      point: spineDots.get(node.status),
      outflowCount: node.outflowCount,
    }))
    .filter((entry): entry is typeof entry & { point: NonNullable<typeof entry.point> } => entry.point != null);

  const maxSegmentFlow = Math.max(1, ...orderedSpineDots.map((d) => d.outflowCount));
  const allOutcomesZero = outcomes.length > 0 && outcomes.every((o) => o.count === 0);

  return (
    <svg
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 h-full w-full"
      style={{ width: containerWidth, height: containerHeight }}
      viewBox={`0 0 ${containerWidth} ${containerHeight}`}
    >
      {/* Main spine line */}
      {orderedSpineDots.length > 0 && (
        <line
          x1={spineX}
          y1={spineTop}
          x2={spineX}
          y2={outcomeCenters.size > 0 ? spineBottom + 60 : spineBottom}
          stroke="color-mix(in srgb, var(--text-muted) 30%, transparent)"
          strokeWidth={3.5}
          strokeLinecap="round"
        />
      )}

      {/* Spine segments with varying thickness */}
      {orderedSpineDots.slice(0, -1).map((dot, index) => {
        const nextDot = orderedSpineDots[index + 1];
        return (
          <path
            key={`spine-seg-${dot.status}-${nextDot.status}`}
            d={`M ${spineX} ${dot.point.y} L ${spineX} ${nextDot.point.y}`}
            fill="none"
            stroke="color-mix(in srgb, var(--accent-secondary) 28%, transparent)"
            strokeOpacity={chapter === 'outcomes' ? 0.14 : 0.4}
            strokeWidth={3 + (dot.outflowCount / maxSegmentFlow) * 7}
            strokeLinecap="round"
          />
        );
      })}

      {/* Spine dots and horizontal connectors */}
      {orderedSpineDots.map((dot) => {
        const badgeCenter = stageCenters.get(dot.status);
        return (
          <g key={`spine-anchor-${dot.status}`}>
            {/* Horizontal connector from spine to badge */}
            {badgeCenter && (
              <line
                x1={spineX}
                y1={dot.point.y}
                x2={badgeCenter.x}
                y2={dot.point.y}
                stroke="color-mix(in srgb, var(--text-muted) 25%, transparent)"
                strokeWidth={2.5}
                strokeLinecap="round"
              />
            )}
            {/* Outer ring */}
            <circle
              cx={spineX}
              cy={dot.point.y}
              r={8}
              fill="none"
              stroke="color-mix(in srgb, var(--accent-primary) 18%, transparent)"
              strokeWidth={2}
            />
            {/* Inner dot */}
            <circle
              cx={spineX}
              cy={dot.point.y}
              r={4}
              fill="var(--accent-primary)"
              fillOpacity={0.75}
            />
          </g>
        );
      })}

      {/* Outcome branch curves */}
      {outcomes.map((outcome) => {
        const target = outcomeCenters.get(outcome.key);
        if (!target) return null;

        const branchStartY = spineBottom + 30;
        const midY = branchStartY + (target.y - branchStartY) * 0.45;
        const stroke = branchStroke(outcome.branchStyle, allOutcomesZero);

        return (
          <path
            key={`branch-${outcome.key}`}
            d={`M ${spineX} ${branchStartY} C ${spineX} ${midY}, ${target.x} ${midY}, ${target.x} ${target.y}`}
            fill="none"
            stroke={stroke.stroke}
            strokeDasharray={stroke.strokeDasharray}
            strokeOpacity={chapter === 'outcomes' ? (allOutcomesZero ? 0.56 : 1) : 0.74}
            strokeWidth={chapter === 'outcomes' ? 4 : 3.5}
            strokeLinecap="round"
            style={chapter === 'outcomes' && !allOutcomesZero
              ? { filter: `drop-shadow(0 0 8px ${stroke.stroke})` }
              : undefined}
          />
        );
      })}
    </svg>
  );
}
