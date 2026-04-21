import type { SubmissionStatus } from '../types';

export const SUBMISSION_STATUS_META: Array<{
  value: SubmissionStatus;
  label: string;
  tone: 'default' | 'info' | 'warning' | 'success' | 'error' | 'gradient';
}> = [
  { value: 'Received', label: 'Received', tone: 'default' },
  { value: 'Triaging', label: 'Triaging', tone: 'info' },
  { value: 'WaitingOnBroker', label: 'Waiting on Broker', tone: 'warning' },
  { value: 'ReadyForUWReview', label: 'Ready for UW Review', tone: 'info' },
  { value: 'InReview', label: 'In Review', tone: 'info' },
  { value: 'Quoted', label: 'Quoted', tone: 'gradient' },
  { value: 'BindRequested', label: 'Bind Requested', tone: 'warning' },
  { value: 'Bound', label: 'Bound', tone: 'success' },
  { value: 'Declined', label: 'Declined', tone: 'error' },
  { value: 'Withdrawn', label: 'Withdrawn', tone: 'default' },
];

export const SUBMISSION_STATUS_LABELS = SUBMISSION_STATUS_META.reduce<Record<string, string>>(
  (accumulator, status) => {
    accumulator[status.value] = status.label;
    return accumulator;
  },
  {},
);

export const LINE_OF_BUSINESS_OPTIONS = [
  { value: 'Property', label: 'Property' },
  { value: 'GeneralLiability', label: 'General Liability' },
  { value: 'CommercialAuto', label: 'Commercial Auto' },
  { value: 'WorkersCompensation', label: "Workers' Compensation" },
  { value: 'ProfessionalLiability', label: 'Professional Liability / E&O' },
  { value: 'Marine', label: 'Marine / Inland Marine' },
  { value: 'Umbrella', label: 'Umbrella / Excess' },
  { value: 'Surety', label: 'Surety / Bond' },
  { value: 'Cyber', label: 'Cyber Liability' },
  { value: 'DirectorsOfficers', label: 'Directors & Officers' },
];

export const SUBMISSION_SORT_OPTIONS = [
  { value: 'createdAt', label: 'Created Date' },
  { value: 'effectiveDate', label: 'Effective Date' },
  { value: 'accountName', label: 'Account Name' },
  { value: 'currentStatus', label: 'Status' },
];

export function getSubmissionStatusLabel(status: string): string {
  return SUBMISSION_STATUS_LABELS[status] ?? status;
}

export function getLineOfBusinessLabel(code: string | null): string {
  if (!code) return 'Unclassified';
  return LINE_OF_BUSINESS_OPTIONS.find((option) => option.value === code)?.label ?? code;
}
