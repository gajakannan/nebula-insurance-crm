type MiniVisualKind =
  | 'waffle'
  | 'gauge'
  | 'progress'
  | 'donut'
  | 'bar'
  | 'dotmap'
  | 'stacked'
  | 'badge'
  | 'entity';

interface Segment {
  label: string;
  count: number;
  color: string;
}

interface StageViewShape {
  id: string;
  kind: MiniVisualKind;
  summary: string;
  count: number;
  segments?: Segment[];
  value?: number;
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

function donutStops(segments: Segment[]) {
  const total = Math.max(1, segments.reduce((sum, segment) => sum + segment.count, 0));
  let offset = 0;

  return segments.map((segment) => {
    const ratio = segment.count / total;
    const length = ratio * 100;
    const start = offset;
    offset += length;
    return {
      ...segment,
      ratio,
      length,
      start,
    };
  });
}

export function MiniVisualization({
  view,
  simplified,
}: {
  view: StageViewShape;
  simplified: boolean;
}) {
  const renderAsBadge = simplified || view.kind === 'badge';

  if (renderAsBadge) {
    return (
      <div className="flex h-[76px] items-center justify-center rounded-xl bg-surface-main/50">
        <div className="text-center">
          <p className="text-3xl font-bold text-text-primary">{view.count}</p>
          <p className="text-[11px] uppercase tracking-wide text-text-muted">Count</p>
        </div>
      </div>
    );
  }

  if (view.kind === 'gauge') {
    const raw = view.value ?? 0;
    const progress = clamp(Number.isFinite(raw) ? raw : 0, 0, 1);
    const circumference = Math.PI * 30;
    const drawLength = progress * circumference;

    return (
      <svg viewBox="0 0 100 70" className="h-[76px] w-full" role="img" aria-label={view.summary}>
        <path d="M 20 58 A 30 30 0 0 1 80 58" fill="none" stroke="color-mix(in srgb, var(--color-data-muted) 35%, transparent)" strokeWidth="8" strokeLinecap="round" />
        <path
          d="M 20 58 A 30 30 0 0 1 80 58"
          fill="none"
          stroke="var(--color-data-primary)"
          strokeWidth="8"
          strokeLinecap="round"
          strokeDasharray={`${drawLength} ${circumference}`}
        />
        <text x="50" y="48" textAnchor="middle" className="fill-text-primary text-[13px] font-semibold">
          {Math.round(progress * 100)}%
        </text>
      </svg>
    );
  }

  if (view.kind === 'progress') {
    const raw = view.value ?? 0;
    const progress = clamp(Number.isFinite(raw) ? raw : 0, 0, 1);
    const circumference = 2 * Math.PI * 28;
    const drawLength = progress * circumference;

    return (
      <svg viewBox="0 0 100 100" className="h-[76px] w-full" role="img" aria-label={view.summary}>
        <circle cx="50" cy="50" r="28" fill="none" stroke="color-mix(in srgb, var(--color-data-muted) 35%, transparent)" strokeWidth="10" />
        <circle
          cx="50"
          cy="50"
          r="28"
          fill="none"
          stroke="var(--color-data-secondary)"
          strokeWidth="10"
          strokeLinecap="round"
          strokeDasharray={`${drawLength} ${circumference}`}
          transform="rotate(-90 50 50)"
        />
        <text x="50" y="55" textAnchor="middle" className="fill-text-primary text-[13px] font-semibold">
          {Math.round(progress * 100)}%
        </text>
      </svg>
    );
  }

  if (view.kind === 'stacked') {
    const segments = view.segments ?? [];
    const total = Math.max(1, segments.reduce((sum, segment) => sum + segment.count, 0));

    return (
      <div className="space-y-2">
        <div className="flex h-3 overflow-hidden rounded-full bg-surface-main/45">
          {segments.map((segment) => (
            <span
              key={`${view.id}-${segment.label}`}
              style={{
                width: `${(segment.count / total) * 100}%`,
                backgroundColor: segment.color,
              }}
            />
          ))}
        </div>
        <div className="flex items-center justify-between text-[10px] text-text-muted">
          {segments.slice(0, 2).map((segment) => (
            <span key={`legend-${view.id}-${segment.label}`}>{segment.label}</span>
          ))}
        </div>
      </div>
    );
  }

  if (view.kind === 'bar') {
    const segments = (view.segments ?? []).slice(0, 4);
    const maxValue = Math.max(1, ...segments.map((segment) => segment.count));

    return (
      <div className="space-y-1.5">
        {segments.map((segment) => (
          <div key={`${view.id}-${segment.label}`} className="grid grid-cols-[1fr_auto] items-center gap-2">
            <div className="h-2 overflow-hidden rounded bg-surface-main/45">
              <span
                className="block h-full rounded"
                style={{
                  width: `${(segment.count / maxValue) * 100}%`,
                  backgroundColor: segment.color,
                }}
              />
            </div>
            <span className="text-[10px] tabular-nums text-text-muted">{segment.count}</span>
          </div>
        ))}
      </div>
    );
  }

  if (view.kind === 'dotmap') {
    const segments = view.segments ?? [];
    const total = Math.max(1, segments.reduce((sum, segment) => sum + segment.count, 0));
    const muted = 'color-mix(in srgb, var(--color-data-muted) 18%, transparent)';
    const regionLabels = ['NW', 'MW', '', 'NE', '', 'W', 'SW', 'SE', 'MA', '', '', '', 'S', '', 'FL'];
    const nonEmptyLabels = regionLabels.filter(Boolean);
    const regionFill = regionLabels.map((label) => {
      if (!label) return 'transparent';
      const regionIndex = nonEmptyLabels.indexOf(label);
      const seg = segments[regionIndex % Math.max(1, segments.length)];
      return seg ? seg.color : muted;
    });
    const regionOpacity = regionLabels.map((label) => {
      if (!label) return 0;
      const regionIndex = nonEmptyLabels.indexOf(label);
      const seg = segments[regionIndex % Math.max(1, segments.length)];
      return seg ? Math.max(0.4, Math.min(1, (seg.count / total) * 3)) : 0.2;
    });

    return (
      <svg viewBox="0 0 100 66" className="h-[76px] w-full" role="img" aria-label={view.summary}>
        {regionLabels.map((label, index) => {
          if (!label) return null;
          const col = index % 5;
          const row = Math.floor(index / 5);
          return (
            <g key={`${view.id}-region-${label}`}>
              <rect
                x={col * 20 + 1}
                y={row * 22 + 1}
                width={18}
                height={20}
                rx={3}
                fill={regionFill[index]}
                fillOpacity={regionOpacity[index]}
              />
              <text
                x={col * 20 + 10}
                y={row * 22 + 13}
                textAnchor="middle"
                dominantBaseline="middle"
                className="fill-text-primary text-[5px] font-medium"
                fillOpacity={0.7}
              >
                {label}
              </text>
            </g>
          );
        })}
      </svg>
    );
  }

  if (view.kind === 'waffle') {
    const segments = view.segments ?? [];
    const total = Math.max(1, segments.reduce((sum, segment) => sum + segment.count, 0));
    const cells = 20;
    const cols = 5;
    const muted = 'color-mix(in srgb, var(--color-data-muted) 25%, transparent)';
    const lobIcons: Record<string, string> = {
      property: 'M8 1L1 5v10h5V10h4v5h5V5L8 1z',
      casualty: 'M8 1L2 4v4c0 4.4 2.6 7.4 6 8 3.4-.6 6-3.6 6-8V4L8 1z',
      marine: 'M8 1a2 2 0 100 4 2 2 0 000-4zM7 5v2H4l4 8 4-8H9V5H7z',
      professional: 'M6 2v2H2a1 1 0 00-1 1v8a1 1 0 001 1h12a1 1 0 001-1V5a1 1 0 00-1-1h-4V2H6zm1 1h2v1H7V3z',
      auto: 'M3 6l1.5-3h7L13 6h1a1 1 0 011 1v4h-2v1h-2v-1H5v1H3v-1H1V7a1 1 0 011-1h1zm1.5 2a1 1 0 100 2 1 1 0 000-2zm7 0a1 1 0 100 2 1 1 0 000-2z',
      other: 'M8 2a6 6 0 100 12A6 6 0 008 2zm0 2a4 4 0 110 8 4 4 0 010-8z',
    };
    const iconKeys = Object.keys(lobIcons);
    type WaffleCell = { color: string; iconPath: string };
    const cellData: WaffleCell[] = [];

    segments.forEach((segment, segIndex) => {
      const count = Math.round((segment.count / total) * cells);
      const icon = iconKeys[segIndex % iconKeys.length];
      for (let index = 0; index < count; index += 1) {
        cellData.push({ color: segment.color, iconPath: lobIcons[icon] });
      }
    });

    while (cellData.length < cells) {
      cellData.push({ color: muted, iconPath: lobIcons.other });
    }

    const cellW = 18;
    const cellH = 16;
    const gapX = 2;
    const gapY = 2;
    const svgW = cols * cellW + (cols - 1) * gapX;
    const rows = Math.ceil(cells / cols);
    const svgH = rows * cellH + (rows - 1) * gapY;

    return (
      <svg
        viewBox={`0 0 ${svgW} ${svgH}`}
        className="h-[76px] w-full"
        role="img"
        aria-label={view.summary}
      >
        {cellData.slice(0, cells).map((cell, index) => {
          const col = index % cols;
          const row = Math.floor(index / cols);
          const x = col * (cellW + gapX);
          const y = row * (cellH + gapY);
          return (
            <g key={`${view.id}-waffle-${index}`}>
              <rect x={x} y={y} width={cellW} height={cellH} rx={3} fill={cell.color} fillOpacity={0.18} />
              <svg x={x + 1} y={y} width={cellW} height={cellH} viewBox="0 0 16 16">
                <path d={cell.iconPath} fill={cell.color} fillOpacity={0.85} />
              </svg>
            </g>
          );
        })}
      </svg>
    );
  }

  const segments = donutStops((view.segments ?? []).slice(0, 5));

  return (
    <svg viewBox="0 0 120 120" className="h-[76px] w-full" role="img" aria-label={view.summary}>
      <circle cx="60" cy="60" r="34" fill="none" stroke="color-mix(in srgb, var(--color-data-muted) 28%, transparent)" strokeWidth="16" />
      {segments.map((segment) => (
        <circle
          key={`${view.id}-arc-${segment.label}`}
          cx="60"
          cy="60"
          r="34"
          fill="none"
          stroke={segment.color}
          strokeWidth="16"
          strokeLinecap="butt"
          strokeDasharray={`${segment.length} 100`}
          strokeDashoffset={-segment.start}
          pathLength={100}
          transform="rotate(-90 60 60)"
        />
      ))}
      <text x="60" y="60" textAnchor="middle" dominantBaseline="middle" className="fill-text-primary text-[16px] font-semibold">
        {view.count}
      </text>
    </svg>
  );
}
