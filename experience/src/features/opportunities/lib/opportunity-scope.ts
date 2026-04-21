import type { OpportunityEntityType } from '../types';

export const OPPORTUNITY_SCOPE_OPTIONS: Array<{
  value: OpportunityEntityType;
  label: string;
  description: string;
}> = [
  {
    value: 'submission',
    label: 'New Business',
    description: 'Submission flow, friction, and outcomes.',
  },
  {
    value: 'renewal',
    label: 'Renewals',
    description: 'Renewal flow, friction, and outcomes.',
  },
];

export function formatOpportunityScopeSummary(selectedEntityTypes: OpportunityEntityType[]) {
  if (selectedEntityTypes.length === OPPORTUNITY_SCOPE_OPTIONS.length) {
    return 'All';
  }

  if (selectedEntityTypes.length === 1) {
    return OPPORTUNITY_SCOPE_OPTIONS.find((option) => option.value === selectedEntityTypes[0])?.label ?? 'Custom';
  }

  return `${selectedEntityTypes.length} selected`;
}
