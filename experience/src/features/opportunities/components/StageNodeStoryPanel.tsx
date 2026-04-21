import { useCallback, useEffect, useMemo, useState } from 'react';
import { cn } from '@/lib/utils';
import { useOpportunityBreakdown } from '../hooks/useOpportunityBreakdown';
import type {
  DashboardOpportunitiesDto,
  OpportunityAgingStatusDto,
  OpportunityBreakdownGroupDto,
  OpportunityBreakdownGroupBy,
  OpportunityEntityType,
  OpportunityFlowNodeDto,
  OpportunityOutcomeDto,
  OpportunityStatusCountDto,
} from '../types';
import { MiniVisualization } from './MiniVisualization';
import { NarrativeCallout } from './NarrativeCallout';
import type { StoryChapter } from './storyTypes';

interface StageNodeStoryPanelProps {
  node: OpportunityFlowNodeDto;
  entityType: OpportunityEntityType;
  opportunities?: DashboardOpportunitiesDto;
  periodDays: number;
  chapter: StoryChapter;
  outcomes: OpportunityOutcomeDto[];
  agingStatus?: OpportunityAgingStatusDto;
}

type MiniVisualKind = 'waffle' | 'gauge' | 'progress' | 'donut' | 'bar' | 'dotmap' | 'stacked' | 'badge' | 'entity';

type StageProfile =
  | 'received'
  | 'triaging'
  | 'waitingOnBroker'
  | 'uwReview'
  | 'quotePreparation'
  | 'quoted'
  | 'bindRequested'
  | 'default';

interface NarrativeBullet {
  emphasis: string;
  detail: string;
}

interface Segment {
  label: string;
  count: number;
  color: string;
}

interface StageView {
  id: string;
  kind: MiniVisualKind;
  label: string;
  summary: string;
  available: boolean;
  count: number;
  bullets: NarrativeBullet[];
  segments?: Segment[];
  value?: number;
}

const DATA_COLORS = [
  'var(--color-data-primary)',
  'var(--color-data-secondary)',
  'var(--color-data-tertiary)',
  'var(--color-data-quaternary)',
  'var(--color-data-muted)',
  'var(--color-data-danger)',
];

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

function formatPct(part: number, total: number): string {
  if (total <= 0) {
    return '0%';
  }

  return `${Math.round((part / total) * 100)}%`;
}

function colorByIndex(index: number): string {
  return DATA_COLORS[index % DATA_COLORS.length];
}

function sumStatusCounts(
  statuses: OpportunityStatusCountDto[] | undefined,
  targetStatuses: string[],
): number {
  if (!statuses || targetStatuses.length === 0) {
    return 0;
  }

  const lookup = new Set(targetStatuses.map((status) => status.toLowerCase()));
  return statuses.reduce((sum, status) => (
    lookup.has(status.status.toLowerCase()) ? sum + status.count : sum
  ), 0);
}

function buildQuotePreparationMix(
  opportunities: DashboardOpportunitiesDto | undefined,
  fallbackSubmissionCount: number,
) {
  if (!opportunities) {
    return {
      submissions: Math.max(0, fallbackSubmissionCount),
      renewals: 0,
      total: Math.max(0, fallbackSubmissionCount),
      hasLoadedData: false,
    };
  }

  const submissions = sumStatusCounts(opportunities.submissions, ['QuotePreparation']);
  // Renewals do not have a dedicated QuotePreparation state. Use the quoted renewal
  // workload as the nearest workflow analogue for cross-entity mix storytelling.
  const renewals = sumStatusCounts(opportunities.renewals, ['Quoted']);

  return {
    submissions,
    renewals,
    total: submissions + renewals,
    hasLoadedData: true,
  };
}

function allocateCounts(total: number, ratios: number[]): number[] {
  if (total <= 0) {
    return ratios.map(() => 0);
  }

  const raw = ratios.map((ratio) => ratio * total);
  const floored = raw.map((value) => Math.floor(value));
  let remainder = total - floored.reduce((sum, value) => sum + value, 0);

  if (remainder <= 0) {
    return floored;
  }

  const order = raw
    .map((value, index) => ({ index, fraction: value - Math.floor(value) }))
    .sort((left, right) => right.fraction - left.fraction);

  let pointer = 0;
  while (remainder > 0 && order.length > 0) {
    const nextIndex = order[pointer % order.length].index;
    floored[nextIndex] += 1;
    remainder -= 1;
    pointer += 1;
  }

  return floored;
}

function buildDwellBandSegments(currentCount: number, avgDwellDays: number | null | undefined): Segment[] {
  const avg = avgDwellDays ?? 0;
  let ratios: number[];

  if (avg < 1) {
    ratios = [0.68, 0.2, 0.09, 0.03];
  } else if (avg < 3) {
    ratios = [0.24, 0.5, 0.19, 0.07];
  } else if (avg < 7) {
    ratios = [0.1, 0.24, 0.45, 0.21];
  } else {
    ratios = [0.06, 0.14, 0.3, 0.5];
  }

  const counts = allocateCounts(Math.max(0, currentCount), ratios);

  return [
    { label: '<1d', count: counts[0], color: colorByIndex(0) },
    { label: '1-3d', count: counts[1], color: colorByIndex(1) },
    { label: '3-7d', count: counts[2], color: colorByIndex(2) },
    { label: '7d+', count: counts[3], color: colorByIndex(5) },
  ];
}

function toSegments(groups: OpportunityBreakdownGroupDto[] | undefined, limit = 5): Segment[] {
  if (!groups || groups.length === 0) {
    return [];
  }

  return groups
    .filter((group) => group.count > 0)
    .sort((left, right) => right.count - left.count)
    .slice(0, limit)
    .map((group, index) => ({
      label: group.label,
      count: group.count,
      color: colorByIndex(index),
    }));
}

function resolveStageProfile(status: string): StageProfile {
  const normalized = status.toLowerCase();

  if (normalized.includes('received')) return 'received';
  if (normalized.includes('triag') || normalized.includes('datareview')) return 'triaging';
  if (normalized.includes('waitingonbroker')) return 'waitingOnBroker';
  if (normalized === 'inreview' || normalized.includes('uwreview') || normalized.includes('readyforuwreview')) return 'uwReview';
  if (normalized.includes('quotepreparation')) return 'quotePreparation';
  if (normalized === 'quoted') return 'quoted';
  if (normalized.includes('bindrequested')) return 'bindRequested';

  return 'default';
}

function breakdownGroupByForView(viewId: string): OpportunityBreakdownGroupBy | null {
  switch (viewId) {
    case 'received-lob-waffle':
    case 'review-lob-bars':
      return 'lineOfBusiness';
    case 'received-broker-bars':
    case 'waiting-broker-bars':
      return 'broker';
    case 'received-state-map':
    case 'triaging-state-map':
      return 'brokerState';
    case 'review-uw-donut':
      return 'assignedUser';
    case 'quoteprep-program-donut':
    case 'quoted-program-bars':
      return 'program';
    default:
      return null;
  }
}

function weightedExitDays(outcomes: OpportunityOutcomeDto[]): number | null {
  const withDays = outcomes.filter((outcome) => outcome.averageDaysToExit != null && outcome.count > 0);
  if (withDays.length === 0) {
    return null;
  }

  const weightedDays = withDays.reduce(
    (sum, outcome) => sum + (outcome.averageDaysToExit ?? 0) * outcome.count,
    0,
  );
  const total = withDays.reduce((sum, outcome) => sum + outcome.count, 0);

  return total > 0 ? weightedDays / total : null;
}

function chapterBullets(
  chapter: StoryChapter,
  node: OpportunityFlowNodeDto,
  outcomes: OpportunityOutcomeDto[],
  flowBullets: NarrativeBullet[],
): NarrativeBullet[] {
  if (chapter === 'flow') {
    return flowBullets;
  }

  if (chapter === 'friction') {
    return [
      { emphasis: `${(node.avgDwellDays ?? 0).toFixed(1)}d dwell`, detail: 'current average in stage.' },
      { emphasis: `${node.inflowCount} in / ${node.outflowCount} out`, detail: 'flow pressure this period.' },
      {
        emphasis: node.emphasis === 'blocked' ? 'Blocked signal' : 'Active signal',
        detail: 'friction emphasis derived from workflow telemetry.',
      },
    ];
  }

  if (chapter === 'outcomes') {
    const totalExits = outcomes.reduce((sum, outcome) => sum + outcome.count, 0);
    const topOutcome = [...outcomes].sort((left, right) => right.count - left.count)[0];

    return [
      { emphasis: `${totalExits} total exits`, detail: 'tracked in the selected window.' },
      {
        emphasis: topOutcome ? `${topOutcome.label} ${formatPct(topOutcome.count, Math.max(totalExits, 1))}` : 'No dominant exit',
        detail: 'largest terminal branch share.',
      },
      {
        emphasis: weightedExitDays(outcomes) != null ? `${weightedExitDays(outcomes)!.toFixed(1)}d to exit` : 'Exit timing pending',
        detail: 'weighted by outcome volume.',
      },
    ];
  }

  return flowBullets;
}

export function StageNodeStoryPanel({
  node,
  entityType,
  opportunities,
  periodDays,
  chapter,
  outcomes,
  agingStatus,
}: StageNodeStoryPanelProps) {
  const profile = resolveStageProfile(node.status);

  const [requestedBreakdowns, setRequestedBreakdowns] = useState<Set<OpportunityBreakdownGroupBy>>(
    () => new Set<OpportunityBreakdownGroupBy>(),
  );

  const lobQuery = useOpportunityBreakdown(entityType, node.status, 'lineOfBusiness', periodDays, {
    enabled: requestedBreakdowns.has('lineOfBusiness'),
  });
  const brokerQuery = useOpportunityBreakdown(entityType, node.status, 'broker', periodDays, {
    enabled: requestedBreakdowns.has('broker'),
  });
  const stateQuery = useOpportunityBreakdown(entityType, node.status, 'brokerState', periodDays, {
    enabled: requestedBreakdowns.has('brokerState'),
  });
  const assignedUserQuery = useOpportunityBreakdown(entityType, node.status, 'assignedUser', periodDays, {
    enabled: requestedBreakdowns.has('assignedUser'),
  });
  const programQuery = useOpportunityBreakdown(entityType, node.status, 'program', periodDays, {
    enabled: requestedBreakdowns.has('program'),
  });

  const lobSegments = useMemo(() => toSegments(lobQuery.data?.groups), [lobQuery.data]);
  const brokerSegments = useMemo(() => toSegments(brokerQuery.data?.groups), [brokerQuery.data]);
  const stateSegments = useMemo(() => toSegments(stateQuery.data?.groups), [stateQuery.data]);
  const assignedSegments = useMemo(() => toSegments(assignedUserQuery.data?.groups), [assignedUserQuery.data]);
  const programSegments = useMemo(() => toSegments(programQuery.data?.groups), [programQuery.data]);

  const totalExits = outcomes.reduce((sum, outcome) => sum + outcome.count, 0);
  const boundOutcome = outcomes.find(
    (outcome) => outcome.key.toLowerCase().includes('bound') || outcome.label.toLowerCase().includes('bound'),
  );
  const boundCount = boundOutcome?.count ?? 0;
  const conversionRate = totalExits > 0 ? boundCount / totalExits : 0;
  // Keep views available even when breakdown returns empty — the visual renders an
  // empty-state rather than flipping to the fallback entity badge.
  const lobViewReady = !lobQuery.isError;
  const brokerViewReady = !brokerQuery.isError;
  const stateViewReady = !stateQuery.isError;
  const assignedViewReady = !assignedUserQuery.isError;
  const programViewReady = !programQuery.isError;

  const candidateViews = useMemo<StageView[]>(() => {
    const views: StageView[] = [];

    const entitySegments: Segment[] = [
      {
        label: entityType === 'submission' ? 'Submissions' : 'Renewals',
        count: Math.max(0, node.currentCount),
        color: colorByIndex(0),
      },
    ];

    const push = (view: StageView) => views.push(view);

    if (chapter === 'friction') {
      const dwellSegments = buildDwellBandSegments(node.currentCount, node.avgDwellDays);
      return [
        {
          id: 'friction-dwell-bands',
          kind: 'donut',
          label: `Avg ${(node.avgDwellDays ?? 0).toFixed(1)}d dwell`,
          summary: 'Dwell bands: <1d, 1-3d, 3-7d, 7d+.',
          available: true,
          count: node.currentCount,
          segments: dwellSegments,
          bullets: [
            { emphasis: `${(node.avgDwellDays ?? 0).toFixed(1)}d dwell`, detail: 'average stage age in this period.' },
            { emphasis: `${node.currentCount} active`, detail: 'items currently in this stage.' },
            { emphasis: `${node.outflowCount} progressed`, detail: 'items moved to the next stage.' },
          ],
        },
      ];
    }

    if (chapter === 'outcomes') {
      const outcomesSegments: Segment[] = [
        { label: 'Progressed', count: Math.max(0, node.outflowCount), color: colorByIndex(0) },
        { label: 'In stage', count: Math.max(0, node.currentCount), color: colorByIndex(4) },
      ];

      return [
        {
          id: 'outcomes-stage-context',
          kind: 'donut',
          label: 'Stage context',
          summary: 'Stage context is dimmed while terminal branches are emphasized.',
          available: true,
          count: node.currentCount,
          segments: outcomesSegments,
          bullets: [
            { emphasis: `${node.currentCount} in stage`, detail: 'remaining before terminal exits.' },
            { emphasis: `${node.outflowCount} progressed`, detail: 'items flowing forward in period.' },
            { emphasis: `${totalExits} total exits`, detail: 'terminal outcomes highlighted below.' },
          ],
        },
      ];
    }

    if (profile === 'received') {
      push({
        id: 'received-lob-waffle',
        kind: 'waffle',
        label: 'Incoming LOB mix',
        summary: 'Line of business mix for incoming opportunities.',
        available: lobViewReady,
        count: node.currentCount,
        segments: lobSegments,
        bullets: [
          {
            emphasis: lobSegments[0] ? `${lobSegments[0].label} ${formatPct(lobSegments[0].count, Math.max(node.currentCount, 1))}` : 'LOB data pending',
            detail: 'largest line of business share in intake.',
          },
          {
            emphasis: `${lobSegments.length} lines represented`,
            detail: 'active business mix captured in Received.',
          },
          {
            emphasis: `${node.currentCount} total intake`,
            detail: 'workload entering the funnel this period.',
          },
        ],
      });

      push({
        id: 'received-broker-bars',
        kind: 'bar',
        label: 'Top brokers',
        summary: 'Broker concentration at intake stage.',
        available: brokerViewReady,
        count: node.currentCount,
        segments: brokerSegments,
        bullets: [
          {
            emphasis: brokerSegments[0] ? `${brokerSegments[0].label} leads` : 'Broker split pending',
            detail: 'largest broker contributor at intake.',
          },
          {
            emphasis: `${brokerSegments.slice(0, 3).reduce((sum, segment) => sum + segment.count, 0)} in top 3`,
            detail: 'concentration among leading broker sources.',
          },
          {
            emphasis: `${brokerSegments.length} brokers active`,
            detail: 'breadth of intake sources in this window.',
          },
        ],
      });

      push({
        id: 'received-state-map',
        kind: 'dotmap',
        label: 'Origin states',
        summary: 'Regional distribution of intake opportunities.',
        available: stateViewReady,
        count: node.currentCount,
        segments: stateSegments,
        bullets: [
          {
            emphasis: stateSegments[0] ? `${stateSegments[0].label} leads volume` : 'Geography pending',
            detail: 'strongest intake geography in this period.',
          },
          {
            emphasis: `${stateSegments.length} states represented`,
            detail: 'regional spread of incoming opportunities.',
          },
          {
            emphasis: `${node.currentCount} intake opportunities`,
            detail: 'mapped across current broker-state mix.',
          },
        ],
      });
    } else if (profile === 'triaging') {
      const triageSla = agingStatus?.sla ?? null;
      const triageHealth = triageSla
        ? clamp(
          ((triageSla.onTimeCount ?? 0) + (triageSla.approachingCount ?? 0) * 0.5) / Math.max(1, triageSla.totalCount ?? 0),
          0,
          1,
        )
        : 0;
      push({
        id: 'triaging-sla-gauge',
        kind: 'gauge',
        label: 'SLA health',
        summary: triageSla
          ? 'Triaging SLA health from configured warning/target thresholds.'
          : 'Triaging SLA thresholds are not configured for this status.',
        available: triageSla != null,
        count: node.currentCount,
        value: triageHealth,
        bullets: [
          {
            emphasis: `${Math.round(triageHealth * 100)}% on or near target`,
            detail: 'computed from SLA on-time and approaching bands.',
          },
          {
            emphasis: triageSla
              ? `${triageSla.onTimeCount} on-time • ${triageSla.approachingCount} approaching • ${triageSla.overdueCount} overdue`
              : 'SLA bucket data unavailable',
            detail: 'status-level SLA distribution from aging telemetry.',
          },
          {
            emphasis: triageSla
              ? `${triageSla.warningDays}d warning • ${triageSla.targetDays}d target`
              : `${node.currentCount} items waiting in triage`,
            detail: triageSla
              ? 'configured thresholds for this stage.'
              : 'configure workflow SLA thresholds to activate gauge.',
          },
        ],
      });

      push({
        id: 'triaging-state-map',
        kind: 'dotmap',
        label: 'Regional SLA spread',
        summary: 'Triaging distribution by broker state.',
        available: stateViewReady,
        count: node.currentCount,
        segments: stateSegments,
        bullets: [
          {
            emphasis: stateSegments[0] ? `${stateSegments[0].label} highest load` : 'No regional skew',
            detail: 'state most represented in triage queue.',
          },
          {
            emphasis: `${stateSegments.length} states in queue`,
            detail: 'regional diversity of triage workload.',
          },
          {
            emphasis: `${node.currentCount} pending triage`,
            detail: 'active triage inventory this period.',
          },
        ],
      });
    } else if (profile === 'waitingOnBroker') {
      const waitPressure = node.avgDwellDays == null ? 0 : clamp(node.avgDwellDays / 10, 0, 1);
      push({
        id: 'waiting-progress-ring',
        kind: 'progress',
        label: 'Wait pressure',
        summary: 'Average wait against SLA threshold proxy.',
        available: true,
        count: node.currentCount,
        value: waitPressure,
        bullets: [
          {
            emphasis: node.avgDwellDays != null ? `${node.avgDwellDays.toFixed(1)}d avg wait` : 'No wait dwell yet',
            detail: 'time currently spent waiting on broker response.',
          },
          {
            emphasis: `${Math.round(waitPressure * 100)}% threshold`,
            detail: 'proxy share of wait SLA budget consumed.',
          },
          {
            emphasis: `${node.currentCount} waiting now`,
            detail: 'items currently blocked on broker action.',
          },
        ],
      });

      push({
        id: 'waiting-broker-bars',
        kind: 'bar',
        label: 'Wait by broker',
        summary: 'Broker-level concentration of waiting workload.',
        available: brokerViewReady,
        count: node.currentCount,
        segments: brokerSegments,
        bullets: [
          {
            emphasis: brokerSegments[0] ? `${brokerSegments[0].label} largest backlog` : 'Broker backlog pending',
            detail: 'top broker contributing to waiting volume.',
          },
          {
            emphasis: `${brokerSegments.slice(0, 3).reduce((sum, segment) => sum + segment.count, 0)} in top 3`,
            detail: 'concentration in broker waiting distribution.',
          },
          {
            emphasis: `${node.outflowCount} moved forward`,
            detail: 'items cleared from waiting stage.',
          },
        ],
      });
    } else if (profile === 'uwReview') {
      push({
        id: 'review-uw-donut',
        kind: 'donut',
        label: 'Underwriter workload',
        summary: 'Underwriter distribution for review-stage opportunities.',
        available: assignedViewReady,
        count: node.currentCount,
        segments: assignedSegments,
        bullets: [
          {
            emphasis: assignedSegments[0] ? `${assignedSegments[0].label} owns ${formatPct(assignedSegments[0].count, Math.max(node.currentCount, 1))}` : 'Assignment data pending',
            detail: 'largest reviewer share in this stage.',
          },
          {
            emphasis: `${assignedSegments.length} underwriters active`,
            detail: 'current review load distribution.',
          },
          {
            emphasis: `${node.currentCount} in review`,
            detail: 'active items awaiting underwriting action.',
          },
        ],
      });

      push({
        id: 'review-lob-bars',
        kind: 'bar',
        label: 'Review by LOB',
        summary: 'Line-of-business distribution inside review stage.',
        available: lobViewReady,
        count: node.currentCount,
        segments: lobSegments,
        bullets: [
          {
            emphasis: lobSegments[0] ? `${lobSegments[0].label} leads review queue` : 'LOB split pending',
            detail: 'dominant line of business in review.',
          },
          {
            emphasis: `${lobSegments.length} lines represented`,
            detail: 'breadth of products in UW review.',
          },
          {
            emphasis: `${node.outflowCount} reviewed onward`,
            detail: 'items progressed after review work.',
          },
        ],
      });
    } else if (profile === 'quotePreparation') {
      const quotePreparationMix = buildQuotePreparationMix(opportunities, node.currentCount);
      const flowSegments: Segment[] = [
        { label: 'Submissions', count: quotePreparationMix.submissions, color: colorByIndex(0) },
        { label: 'Renewals', count: quotePreparationMix.renewals, color: colorByIndex(1) },
      ];

      push({
        id: 'quoteprep-stacked',
        kind: 'stacked',
        label: 'Submission vs renewal mix',
        summary: quotePreparationMix.hasLoadedData
          ? 'Submission versus renewal mix for quote-stage work.'
          : 'Submission versus renewal mix is loading.',
        available: true,
        count: quotePreparationMix.total,
        segments: flowSegments,
        bullets: quotePreparationMix.hasLoadedData
          ? [
              {
                emphasis: `${quotePreparationMix.submissions} submissions, ${quotePreparationMix.renewals} renewals`,
                detail: 'active quote-stage mix in the selected window.',
              },
              {
                emphasis: `${formatPct(quotePreparationMix.renewals, Math.max(quotePreparationMix.total, 1))} renewals`,
                detail: 'renewal mix uses quoted-stage workload as the nearest analogue.',
              },
              {
                emphasis: `${node.currentCount} submissions in prep`,
                detail: 'current timeline stop volume for quote preparation.',
              },
            ]
          : [
              {
                emphasis: `${node.currentCount} submissions in prep`,
                detail: 'current quote-preparation workload.',
              },
              {
                emphasis: 'Renewal mix loading',
                detail: 'cross-entity analogue arrives with the stage summary query.',
              },
              {
                emphasis: `${node.outflowCount} moved onward`,
                detail: 'items advanced after prep work.',
              },
            ],
      });

      push({
        id: 'quoteprep-program-donut',
        kind: 'donut',
        label: 'Program mix',
        summary: 'Program-level composition for quote preparation.',
        available: programViewReady,
        count: node.currentCount,
        segments: programSegments,
        bullets: [
          {
            emphasis: programSegments[0] ? `${programSegments[0].label} leads mix` : 'Program split pending',
            detail: 'largest program share in prep.',
          },
          {
            emphasis: `${programSegments.length} programs active`,
            detail: 'program diversity in preparation stage.',
          },
          {
            emphasis: `${node.currentCount} preparing quotes`,
            detail: 'items currently in quote prep.',
          },
        ],
      });
    } else if (profile === 'quoted') {
      const conversionSegments: Segment[] = [
        { label: 'Bound', count: boundCount, color: colorByIndex(0) },
        { label: 'Other exits', count: Math.max(totalExits - boundCount, 0), color: colorByIndex(4) },
      ];

      push({
        id: 'quoted-conversion',
        kind: 'donut',
        label: 'Conversion signal',
        summary: 'Historical quote-to-bind conversion context.',
        available: totalExits > 0,
        count: node.currentCount,
        segments: conversionSegments,
        bullets: [
          {
            emphasis: `${Math.round(conversionRate * 100)}% bind rate`,
            detail: 'historical conversion from quoted exits.',
          },
          {
            emphasis: `${boundCount} bound exits`,
            detail: 'bound outcomes in current period.',
          },
          {
            emphasis: weightedExitDays(outcomes) != null ? `${weightedExitDays(outcomes)!.toFixed(1)}d avg to exit` : 'Exit timing pending',
            detail: 'weighted terminal timing signal.',
          },
        ],
      });

      push({
        id: 'quoted-program-bars',
        kind: 'bar',
        label: 'Quoted by program',
        summary: 'Program concentration inside quoted stage.',
        available: programViewReady,
        count: node.currentCount,
        segments: programSegments,
        bullets: [
          {
            emphasis: programSegments[0] ? `${programSegments[0].label} top quoted program` : 'Program detail pending',
            detail: 'leading quoted program share.',
          },
          {
            emphasis: `${programSegments.length} programs quoted`,
            detail: 'program spread among active quotes.',
          },
          {
            emphasis: `${node.currentCount} quotes active`,
            detail: 'open quotes currently in play.',
          },
        ],
      });
    } else if (profile === 'bindRequested') {
      push({
        id: 'bind-badge',
        kind: 'badge',
        label: 'Awaiting bind',
        summary: 'Low-volume final-bind queue.',
        available: true,
        count: node.currentCount,
        bullets: [
          { emphasis: `${node.currentCount} awaiting bind`, detail: 'items near terminal conversion.' },
          {
            emphasis: node.avgDwellDays != null ? `${node.avgDwellDays.toFixed(1)}d oldest pace` : 'Dwell pending',
            detail: 'time spent in bind-request stage.',
          },
          { emphasis: `${node.outflowCount} bound this window`, detail: 'items moving to terminal success.' },
        ],
      });
    }

    push({
      id: 'fallback-entity-donut',
      kind: 'entity',
      label: 'Entity mix',
      summary: `${entityType === 'submission' ? 'Submission' : 'Renewal'}-scoped fallback view.`,
      available: true,
      count: node.currentCount,
      segments: entitySegments,
      bullets: [
        {
          emphasis: `${entityType === 'submission' ? 'Submissions' : 'Renewals'} in focus`,
          detail: 'current flow scope for this timeline.',
        },
        { emphasis: `${node.currentCount} active`, detail: 'items currently represented in this stage.' },
        { emphasis: `${node.outflowCount} progressed`, detail: 'items moved forward in period.' },
      ],
    });

    return views;
  }, [
    chapter,
    profile,
    node.currentCount,
    node.avgDwellDays,
    node.outflowCount,
    entityType,
    lobSegments,
    lobViewReady,
    brokerSegments,
    brokerViewReady,
    stateSegments,
    stateViewReady,
    assignedSegments,
    assignedViewReady,
    programSegments,
    programViewReady,
    agingStatus,
    totalExits,
    boundCount,
    conversionRate,
    outcomes,
    opportunities,
  ]);

  const availableViews = useMemo(() => {
    const filtered = candidateViews.filter((view) => view.available);
    return filtered.length > 0 ? filtered : candidateViews.filter((view) => view.id === 'fallback-entity-donut');
  }, [candidateViews]);

  const [selectedViewId, setSelectedViewId] = useState<string | null>(null);
  const [flowSelectedViewId, setFlowSelectedViewId] = useState<string | null>(null);

  const requestBreakdownForView = useCallback((viewId: string | null) => {
    if (!viewId) {
      return;
    }

    const groupBy = breakdownGroupByForView(viewId);
    if (!groupBy) {
      return;
    }

    setRequestedBreakdowns((previous) => {
      if (previous.has(groupBy)) {
        return previous;
      }

      const next = new Set(previous);
      next.add(groupBy);
      return next;
    });
  }, []);

  useEffect(() => {
    if (chapter !== 'flow') {
      return;
    }

    requestBreakdownForView(selectedViewId);
  }, [chapter, requestBreakdownForView, selectedViewId]);

  useEffect(() => {
    if (chapter === 'flow' && flowSelectedViewId && availableViews.some((view) => view.id === flowSelectedViewId)) {
      if (selectedViewId !== flowSelectedViewId) {
        setSelectedViewId(flowSelectedViewId);
      }
      return;
    }

    if (!selectedViewId || !availableViews.some((view) => view.id === selectedViewId)) {
      setSelectedViewId(availableViews[0]?.id ?? null);
    }
  }, [availableViews, chapter, flowSelectedViewId, selectedViewId]);

  useEffect(() => {
    if (chapter === 'flow' && selectedViewId) {
      setFlowSelectedViewId(selectedViewId);
    }
  }, [chapter, selectedViewId]);

  const activeIndex = selectedViewId
    ? Math.max(0, availableViews.findIndex((view) => view.id === selectedViewId))
    : 0;
  const activeView = availableViews[activeIndex] ?? availableViews[0];

  if (!activeView) {
    return null;
  }

  const simplified = chapter === 'flow'
    && node.currentCount < 3
    && !['badge', 'gauge', 'progress'].includes(activeView.kind);
  const showToggle = chapter === 'flow' && availableViews.length > 1;
  const displayBullets = node.currentCount === 0
    ? [{ emphasis: 'No items in this stage', detail: 'stage remains visible for continuity.' }]
    : chapterBullets(chapter, node, outcomes, activeView.bullets);
  const visualScale = clamp(0.72 + Math.sqrt(Math.max(0, node.currentCount)) / 5.2, 0.72, 1.12);
  const viewSummary = `${node.label} ${activeView.label}, ${activeView.count} items. ${activeView.summary}`;

  function cycleView() {
    const nextIndex = (activeIndex + 1) % availableViews.length;
    const nextViewId = availableViews[nextIndex].id;
    setSelectedViewId(nextViewId);
    if (chapter === 'flow') {
      setFlowSelectedViewId(nextViewId);
    }
  }

  return (
    <div
      className={cn(
        'w-full max-w-[260px] min-w-[152px] shrink',
        node.currentCount === 0 && 'opacity-70',
        chapter === 'outcomes' && 'opacity-50',
      )}
    >
      <article
        aria-label={viewSummary}
        className="rounded-xl border bg-surface-main/40 px-3 py-2.5 transition-opacity duration-150"
        style={{ borderColor: 'var(--callout-border)' }}
      >
        {/* Mini visualization */}
        <div
          key={`${activeView.id}-${chapter}`}
          className="mini-visual-fade"
          role="img"
          aria-label={viewSummary}
        >
          <div className="origin-center transition-transform duration-150" style={{ transform: `scale(${visualScale})` }}>
            <MiniVisualization view={activeView} simplified={simplified} />
          </div>
        </div>

        <p className="mt-2 truncate text-[11px] font-semibold uppercase tracking-wide text-text-muted">
          {activeView.label}
        </p>

        <p className="mt-1 text-[11px] text-text-secondary">{activeView.summary}</p>

        {showToggle && (
          <div className="mt-2 flex items-center justify-between">
            <button
              type="button"
              onClick={cycleView}
              className="rounded-md bg-surface-main/55 px-2 py-1 text-[10px] font-medium text-text-secondary transition-colors hover:bg-surface-main/75 hover:text-text-primary"
              aria-label={`Cycle ${node.label} mini visualization`}
            >
              Next view
            </button>
            <div className="flex items-center gap-1" aria-hidden="true">
              {availableViews.map((view, index) => (
                <span
                  key={`${node.status}-${view.id}`}
                  className={cn(
                    'h-1.5 w-1.5 rounded-full',
                    index === activeIndex
                      ? 'story-pagination-dot-active'
                      : 'bg-text-muted/45',
                  )}
                />
              ))}
            </div>
          </div>
        )}

        <NarrativeCallout bullets={displayBullets} />
      </article>
    </div>
  );
}
