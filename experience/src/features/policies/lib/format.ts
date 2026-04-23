import type { PolicyStatus } from '../types';

export const POLICY_STATUS_META: Record<PolicyStatus, {
  label: string;
  tone: 'default' | 'info' | 'warning' | 'success' | 'error';
}> = {
  Pending: { label: 'Pending', tone: 'warning' },
  Issued: { label: 'Issued', tone: 'success' },
  Cancelled: { label: 'Cancelled', tone: 'error' },
  Expired: { label: 'Expired', tone: 'default' },
};

export const CANCELLATION_REASON_OPTIONS = [
  { value: 'NonPayment', label: 'Non-payment' },
  { value: 'InsuredRequest', label: 'Insured request' },
  { value: 'UnderwritingDecision', label: 'Underwriting decision' },
  { value: 'MaterialMisrepresentation', label: 'Material misrepresentation' },
  { value: 'CoverageNoLongerNeeded', label: 'Coverage no longer needed' },
  { value: 'CarrierWithdrawal', label: 'Carrier withdrawal' },
  { value: 'Other', label: 'Other' },
];

export function formatPolicyDate(value: string | null | undefined): string {
  if (!value) return '-';
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(value));
}

export function formatPolicyDateTime(value: string | null | undefined): string {
  if (!value) return '-';
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(value));
}

export function formatPolicyCurrency(value: number | null | undefined, currency = 'USD'): string {
  if (value === null || typeof value === 'undefined') return '-';
  if (currency === 'MIXED') {
    return `${Math.round(value).toLocaleString()} mixed`;
  }

  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value);
}

export function normalizeOptionalText(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length ? trimmed : null;
}
