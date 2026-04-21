import { ApiError } from '@/services/api';

export interface SubmissionFormValues {
  accountId?: string;
  brokerId?: string;
  effectiveDate?: string;
  premiumEstimate?: string;
}

export function validateSubmissionCreate(form: SubmissionFormValues) {
  const errors: Record<string, string> = {};

  if (!form.accountId) {
    errors.accountId = 'Account is required.';
  }

  if (!form.brokerId) {
    errors.brokerId = 'Broker is required.';
  }

  if (!form.effectiveDate) {
    errors.effectiveDate = 'Effective date is required.';
  }

  if (form.premiumEstimate) {
    const parsed = Number(form.premiumEstimate);
    if (Number.isNaN(parsed) || parsed < 0) {
      errors.premiumEstimate = 'Premium estimate must be zero or greater.';
    }
  }

  return errors;
}

export function normalizeOptionalText(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}

export function normalizeOptionalNumber(value: string): number | null {
  if (!value.trim()) return null;

  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

export function extractProblemFieldErrors(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError) || !error.problem?.errors) {
    return {};
  }

  return Object.entries(error.problem.errors).reduce<Record<string, string>>((accumulator, [field, messages]) => {
    accumulator[field] = messages[0] ?? 'Invalid value.';
    return accumulator;
  }, {});
}

export function describeSubmissionApiError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'Unable to save submission. Please try again.';
  }

  switch (error.code) {
    case 'region_mismatch':
      return 'The selected account and broker are not region-aligned.';
    case 'invalid_account':
      return 'The selected account is no longer available.';
    case 'invalid_broker':
      return 'The selected broker is invalid or inactive.';
    case 'invalid_program':
      return 'The selected program is invalid.';
    case 'invalid_lob':
      return 'The selected line of business is invalid.';
    case 'invalid_assignee':
      return error.problem?.detail ?? 'The selected assignee is invalid.';
    case 'missing_transition_prerequisite':
      return error.problem?.detail ?? 'Submission is not ready for that transition.';
    case 'precondition_failed':
      return error.problem?.detail ?? 'This submission changed. Refresh and retry.';
    default:
      return error.problem?.detail ?? 'Unable to save submission. Please try again.';
  }
}
