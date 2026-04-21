import type { OpportunityColorGroup } from '../types';

const bgClasses: Record<OpportunityColorGroup, string> = {
  intake: 'bg-opportunity-intake',
  triage: 'bg-opportunity-triage',
  waiting: 'bg-opportunity-waiting',
  review: 'bg-opportunity-review',
  decision: 'bg-opportunity-decision',
  won: 'bg-opportunity-won',
  lost: 'bg-opportunity-lost',
};

const textClasses: Record<OpportunityColorGroup, string> = {
  intake: 'text-opportunity-intake',
  triage: 'text-opportunity-triage',
  waiting: 'text-opportunity-waiting',
  review: 'text-opportunity-review',
  decision: 'text-opportunity-decision',
  won: 'text-opportunity-won',
  lost: 'text-opportunity-lost',
};

const borderClasses: Record<OpportunityColorGroup, string> = {
  intake: 'border-opportunity-intake',
  triage: 'border-opportunity-triage',
  waiting: 'border-opportunity-waiting',
  review: 'border-opportunity-review',
  decision: 'border-opportunity-decision',
  won: 'border-opportunity-won',
  lost: 'border-opportunity-lost',
};

const baseStrokeColors: Record<OpportunityColorGroup, string> = {
  intake: 'var(--color-opportunity-intake)',
  triage: 'var(--color-opportunity-triage)',
  waiting: 'var(--color-opportunity-waiting)',
  review: 'var(--color-opportunity-review)',
  decision: 'var(--color-opportunity-decision)',
  won: 'var(--color-opportunity-won)',
  lost: 'var(--color-opportunity-lost)',
};

const lightStrokeColors: Record<OpportunityColorGroup, string> = {
  intake: 'var(--color-opportunity-intake-light)',
  triage: 'var(--color-opportunity-triage-light)',
  waiting: 'var(--color-opportunity-waiting-light)',
  review: 'var(--color-opportunity-review-light)',
  decision: 'var(--color-opportunity-decision-light)',
  won: 'var(--color-opportunity-won-light)',
  lost: 'var(--color-opportunity-lost-light)',
};

export function opportunityBg(group: OpportunityColorGroup): string {
  return bgClasses[group];
}

export function opportunityText(group: OpportunityColorGroup): string {
  return textClasses[group];
}

export function opportunityBorder(group: OpportunityColorGroup): string {
  return borderClasses[group];
}

export function opportunityHex(group: OpportunityColorGroup): string {
  return baseStrokeColors[group];
}

export function opportunityHexLight(group: OpportunityColorGroup): string {
  return lightStrokeColors[group];
}
