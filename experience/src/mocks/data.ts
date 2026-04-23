import type { TimelineEventDto } from '@/contracts/timeline'
import type { DashboardKpisDto } from '@/features/kpis'
import type { NudgesResponseDto } from '@/features/nudges'
export { accountReferenceFixture, programReferenceFixture, searchUsers, listSubmissions, getSubmission, getSubmissionTimeline, createSubmission, updateSubmission, assignSubmission, transitionSubmission } from './submissions'
export { brokerListFixture, buildBrokerListResponse } from './brokers'
export {
  assignRenewal,
  createRenewal,
  getRenewal,
  getRenewalTimeline,
  listRenewals,
  transitionRenewal,
} from './renewals'
export {
  cancelPolicy,
  createPolicy,
  endorsePolicy,
  getPolicy,
  getPolicyAccountSummary,
  getPolicySummary,
  importPolicies,
  issuePolicy,
  listAccountPolicies,
  listPolicies,
  listPolicyCoverages,
  listPolicyEndorsements,
  listPolicyTimeline,
  listPolicyVersions,
  reinstatePolicy,
} from './policies'
import type {
  DashboardOpportunitiesDto,
  OpportunityAgingDto,
  OpportunityBreakdownDto,
  OpportunityBreakdownGroupBy,
  OpportunityEntityType,
  OpportunityFlowDto,
  OpportunityItemsDto,
  OpportunityOutcomesDto,
} from '@/features/opportunities/types'
import type { MyTasksResponseDto } from '@/features/tasks'

export const API_ORIGIN = 'http://localhost'

export const dashboardKpisFixture: DashboardKpisDto = {
  activeBrokers: 128,
  openSubmissions: 42,
  renewalRate: 0.83,
  avgTurnaroundDays: 4.6,
}

export const dashboardNudgesFixture: NudgesResponseDto = {
  nudges: [
    {
      nudgeType: 'UpcomingRenewal',
      title: 'Renewal meeting this afternoon',
      description: 'Blue Horizon Risk Partners needs a final strategy sync.',
      linkedEntityType: 'Broker',
      linkedEntityId: 'broker-1',
      linkedEntityName: 'Blue Horizon Risk Partners',
      urgencyValue: 72,
      ctaLabel: 'Open Broker',
    },
    {
      nudgeType: 'StaleSubmission',
      title: 'Submission idle for 9 days',
      description: 'Compass Markets 138 has no recent underwriting movement.',
      linkedEntityType: 'Submission',
      linkedEntityId: 'submission-138',
      linkedEntityName: 'Compass Markets 138',
      urgencyValue: 61,
      ctaLabel: 'Open Submission',
    },
  ],
}

export const dashboardOpportunitiesFixture: DashboardOpportunitiesDto = {
  submissions: [
    { status: 'Received', count: 10, colorGroup: 'intake' },
    { status: 'Triaging', count: 7, colorGroup: 'triage' },
    { status: 'InReview', count: 4, colorGroup: 'review' },
    { status: 'QuotePreparation', count: 4, colorGroup: 'decision' },
    { status: 'Quoted', count: 3, colorGroup: 'decision' },
  ],
  renewals: [
    { status: 'Identified', count: 6, colorGroup: 'intake' },
    { status: 'Outreach', count: 4, colorGroup: 'waiting' },
    { status: 'InReview', count: 2, colorGroup: 'review' },
    { status: 'Quoted', count: 3, colorGroup: 'decision' },
  ],
}

export const submissionFlowFixture: OpportunityFlowDto = {
  entityType: 'submission',
  periodDays: 180,
  windowStartUtc: '2026-01-01T00:00:00Z',
  windowEndUtc: '2026-03-01T00:00:00Z',
  nodes: [
    {
      status: 'Received',
      label: 'Received',
      isTerminal: false,
      displayOrder: 1,
      colorGroup: 'intake',
      currentCount: 10,
      inflowCount: 0,
      outflowCount: 7,
      avgDwellDays: 2.1,
      emphasis: 'normal',
    },
    {
      status: 'Triaging',
      label: 'Triaging',
      isTerminal: false,
      displayOrder: 2,
      colorGroup: 'triage',
      currentCount: 7,
      inflowCount: 8,
      outflowCount: 5,
      avgDwellDays: 5.4,
      emphasis: 'bottleneck',
    },
    {
      status: 'InReview',
      label: 'In Review',
      isTerminal: false,
      displayOrder: 3,
      colorGroup: 'review',
      currentCount: 4,
      inflowCount: 5,
      outflowCount: 4,
      avgDwellDays: 8,
      emphasis: 'blocked',
    },
    {
      status: 'QuotePreparation',
      label: 'Quote Preparation',
      isTerminal: false,
      displayOrder: 4,
      colorGroup: 'decision',
      currentCount: 4,
      inflowCount: 4,
      outflowCount: 3,
      avgDwellDays: 3.2,
      emphasis: 'active',
    },
    {
      status: 'Quoted',
      label: 'Quoted',
      isTerminal: false,
      displayOrder: 5,
      colorGroup: 'decision',
      currentCount: 3,
      inflowCount: 3,
      outflowCount: 2,
      avgDwellDays: 4.4,
      emphasis: 'normal',
    },
    {
      status: 'Bound',
      label: 'Bound',
      isTerminal: true,
      displayOrder: 6,
      colorGroup: 'decision',
      currentCount: 15,
      inflowCount: 5,
      outflowCount: 0,
      avgDwellDays: null,
      emphasis: null,
    },
  ],
  links: [
    { sourceStatus: 'Received', targetStatus: 'Triaging', count: 12 },
    { sourceStatus: 'Triaging', targetStatus: 'InReview', count: 8 },
    { sourceStatus: 'InReview', targetStatus: 'QuotePreparation', count: 4 },
    { sourceStatus: 'QuotePreparation', targetStatus: 'Quoted', count: 3 },
  ],
}

export const opportunityOutcomesFixture: OpportunityOutcomesDto = {
  periodDays: 180,
  totalExits: 20,
  outcomes: [
    {
      key: 'bound',
      label: 'Bound',
      branchStyle: 'solid',
      count: 12,
      percentOfTotal: 60,
      averageDaysToExit: 7.2,
    },
    {
      key: 'declined',
      label: 'Declined',
      branchStyle: 'red_dashed',
      count: 8,
      percentOfTotal: 40,
      averageDaysToExit: 5.8,
    },
  ],
}

export const renewalFlowFixture: OpportunityFlowDto = {
  entityType: 'renewal',
  periodDays: 180,
  windowStartUtc: '2026-01-01T00:00:00Z',
  windowEndUtc: '2026-03-01T00:00:00Z',
  nodes: [
    {
      status: 'Identified',
      label: 'Identified',
      isTerminal: false,
      displayOrder: 1,
      colorGroup: 'intake',
      currentCount: 6,
      inflowCount: 0,
      outflowCount: 4,
      avgDwellDays: 3.8,
      emphasis: 'active',
    },
    {
      status: 'Outreach',
      label: 'Outreach',
      isTerminal: false,
      displayOrder: 2,
      colorGroup: 'waiting',
      currentCount: 4,
      inflowCount: 4,
      outflowCount: 3,
      avgDwellDays: 4.6,
      emphasis: 'normal',
    },
    {
      status: 'InReview',
      label: 'In Review',
      isTerminal: false,
      displayOrder: 3,
      colorGroup: 'review',
      currentCount: 2,
      inflowCount: 3,
      outflowCount: 2,
      avgDwellDays: 6.1,
      emphasis: 'blocked',
    },
    {
      status: 'Quoted',
      label: 'Quoted',
      isTerminal: false,
      displayOrder: 4,
      colorGroup: 'decision',
      currentCount: 3,
      inflowCount: 2,
      outflowCount: 2,
      avgDwellDays: 2.9,
      emphasis: 'normal',
    },
    {
      status: 'Completed',
      label: 'Completed',
      isTerminal: true,
      displayOrder: 5,
      colorGroup: 'won',
      currentCount: 5,
      inflowCount: 2,
      outflowCount: 0,
      avgDwellDays: null,
      emphasis: null,
    },
    {
      status: 'Lost',
      label: 'Lost',
      isTerminal: true,
      displayOrder: 6,
      colorGroup: 'lost',
      currentCount: 2,
      inflowCount: 1,
      outflowCount: 0,
      avgDwellDays: null,
      emphasis: null,
    },
  ],
  links: [
    { sourceStatus: 'Identified', targetStatus: 'Outreach', count: 4 },
    { sourceStatus: 'Outreach', targetStatus: 'InReview', count: 3 },
    { sourceStatus: 'InReview', targetStatus: 'Quoted', count: 2 },
    { sourceStatus: 'Quoted', targetStatus: 'Completed', count: 2 },
  ],
}

export const renewalOutcomesFixture: OpportunityOutcomesDto = {
  periodDays: 180,
  totalExits: 7,
  outcomes: [
    {
      key: 'bound',
      label: 'Bound',
      branchStyle: 'solid',
      count: 5,
      percentOfTotal: 71.4,
      averageDaysToExit: 11.1,
    },
    {
      key: 'no_quote',
      label: 'No Quote',
      branchStyle: 'gray_dotted',
      count: 0,
      percentOfTotal: 0,
      averageDaysToExit: null,
    },
    {
      key: 'declined',
      label: 'Declined',
      branchStyle: 'red_dashed',
      count: 0,
      percentOfTotal: 0,
      averageDaysToExit: null,
    },
    {
      key: 'expired',
      label: 'Expired',
      branchStyle: 'gray_dotted',
      count: 0,
      percentOfTotal: 0,
      averageDaysToExit: null,
    },
    {
      key: 'lost_competitor',
      label: 'Lost to competitor',
      branchStyle: 'red_dashed',
      count: 2,
      percentOfTotal: 28.6,
      averageDaysToExit: 8.4,
    },
  ],
}

export const opportunityAgingFixture: OpportunityAgingDto = {
  entityType: 'submission',
  periodDays: 180,
  statuses: [
    {
      status: 'Triaging',
      label: 'Triaging',
      colorGroup: 'triage',
      displayOrder: 2,
      sla: {
        warningDays: 2,
        targetDays: 5,
        totalCount: 7,
        onTimeCount: 3,
        approachingCount: 2,
        overdueCount: 2,
      },
      buckets: [],
      total: 7,
    },
  ],
}

export const renewalAgingFixture: OpportunityAgingDto = {
  entityType: 'renewal',
  periodDays: 180,
  statuses: [
    {
      status: 'Identified',
      label: 'Identified',
      colorGroup: 'intake',
      displayOrder: 1,
      sla: {
        warningDays: 7,
        targetDays: 30,
        totalCount: 6,
        onTimeCount: 4,
        approachingCount: 1,
        overdueCount: 1,
      },
      buckets: [],
      total: 6,
    },
  ],
}

export const taskFixture: MyTasksResponseDto = {
  tasks: [
    {
      id: 'task-1',
      title: 'Follow up on broker submission package',
      status: 'InProgress',
      dueDate: '2026-03-22T00:00:00Z',
      linkedEntityType: 'Broker',
      linkedEntityId: 'broker-1',
      linkedEntityName: 'Blue Horizon Risk Partners',
      isOverdue: false,
    },
  ],
  totalCount: 1,
}

export const timelineFixture: TimelineEventDto[] = [
  {
    id: 'timeline-1',
    entityType: 'Broker',
    entityId: 'broker-1',
    eventType: 'BrokerUpdated',
    eventDescription: 'Broker appetite notes were refreshed for the west region team.',
    entityName: 'Blue Horizon Risk Partners',
    actorDisplayName: 'Priya Patel',
    occurredAt: '2026-03-20T15:00:00Z',
  },
]

export function buildOpportunityBreakdownFixture(
  entityType: OpportunityEntityType,
  status: string,
  groupBy: OpportunityBreakdownGroupBy,
  periodDays: number,
): OpportunityBreakdownDto {
  const groups = (() => {
    switch (groupBy) {
      case 'lineOfBusiness':
        return [
          { key: 'property', label: 'Property', count: 6 },
          { key: 'casualty', label: 'Casualty', count: 4 },
        ]
      case 'broker':
        return [
          { key: 'broker-1', label: 'Blue Horizon Risk Partners', count: 5 },
          { key: 'broker-2', label: 'Summit Specialty Group', count: 3 },
          { key: 'broker-3', label: 'Atlas Wholesale Brokerage', count: 2 },
        ]
      case 'brokerState':
        return [
          { key: 'CA', label: 'CA', count: 4 },
          { key: 'TX', label: 'TX', count: 3 },
          { key: 'NY', label: 'NY', count: 3 },
        ]
      case 'assignedUser':
        return [
          { key: 'uw-1', label: 'Nadia Brooks', count: 3 },
          { key: 'uw-2', label: 'Alex Kim', count: 1 },
        ]
      case 'program':
        return [
          { key: 'prog-1', label: 'Construction Preferred', count: 2 },
          { key: 'prog-2', label: 'Mid-Market Casualty', count: 2 },
        ]
    }
  })()

  return {
    entityType,
    status,
    groupBy,
    periodDays,
    groups,
    total: groups.reduce((sum, group) => sum + group.count, 0),
  }
}

export function buildOpportunityItemsFixture(): OpportunityItemsDto {
  return {
    items: [
      {
        entityId: 'submission-1',
        entityName: 'Blue Horizon Manufacturing',
        amount: 185000,
        daysInStatus: 4,
        assignedUserInitials: 'NB',
        assignedUserDisplayName: 'Nadia Brooks',
      },
      {
        entityId: 'submission-2',
        entityName: 'Summit Contractors Group',
        amount: 92000,
        daysInStatus: 2,
        assignedUserInitials: 'AK',
        assignedUserDisplayName: 'Alex Kim',
      },
    ],
    totalCount: 2,
  }
}
