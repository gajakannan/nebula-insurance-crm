import type {
  RenewalLostReasonCode,
  RenewalStatus,
  RenewalUrgency,
} from '../types';

export const RENEWAL_STATUS_META: Array<{
  value: RenewalStatus;
  label: string;
  tone: 'default' | 'info' | 'warning' | 'success' | 'error' | 'gradient';
}> = [
  { value: 'Identified', label: 'Identified', tone: 'default' },
  { value: 'Outreach', label: 'Outreach', tone: 'warning' },
  { value: 'InReview', label: 'In Review', tone: 'info' },
  { value: 'Quoted', label: 'Quoted', tone: 'gradient' },
  { value: 'Completed', label: 'Completed', tone: 'success' },
  { value: 'Lost', label: 'Lost', tone: 'error' },
];

export const RENEWAL_STATUS_LABELS = RENEWAL_STATUS_META.reduce<Record<string, string>>(
  (accumulator, status) => {
    accumulator[status.value] = status.label;
    return accumulator;
  },
  {},
);

export const RENEWAL_SORT_OPTIONS = [
  { value: 'policyExpirationDate', label: 'Expiration Date' },
  { value: 'accountName', label: 'Account Name' },
  { value: 'currentStatus', label: 'Status' },
  { value: 'assignedToUserId', label: 'Assigned User' },
] as const;

export const RENEWAL_LOST_REASON_OPTIONS: Array<{
  value: RenewalLostReasonCode;
  label: string;
}> = [
  { value: 'NonRenewal', label: 'Non-renewal' },
  { value: 'CompetitiveLoss', label: 'Competitive loss' },
  { value: 'BusinessClosed', label: 'Business closed' },
  { value: 'CoverageNoLongerNeeded', label: 'Coverage no longer needed' },
  { value: 'PricingDeclined', label: 'Pricing declined' },
  { value: 'Other', label: 'Other' },
];

export function getRenewalStatusLabel(status: string): string {
  return RENEWAL_STATUS_LABELS[status] ?? status;
}

export function getRenewalUrgencyLabel(urgency: RenewalUrgency): string | null {
  if (urgency === 'overdue') return 'Overdue';
  if (urgency === 'approaching') return 'Approaching';
  return null;
}

export function getRenewalTransitionLabel(targetState: RenewalStatus): string {
  switch (targetState) {
    case 'Outreach':
      return 'Advance to Outreach';
    case 'InReview':
      return 'Advance to In Review';
    case 'Quoted':
      return 'Advance to Quoted';
    case 'Completed':
      return 'Complete Renewal';
    case 'Lost':
      return 'Mark as Lost';
    default:
      return `Advance to ${getRenewalStatusLabel(targetState)}`;
  }
}

export function getAllowedAssignmentRoles(status: RenewalStatus): string[] {
  return status === 'Identified' || status === 'Outreach'
    ? ['DistributionUser', 'DistributionManager', 'Admin']
    : ['Underwriter', 'Admin'];
}
