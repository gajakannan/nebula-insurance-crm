import { ApiError } from '@/services/api';

export function normalizeOptionalText(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
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

export function describeRenewalApiError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'Unable to save renewal changes. Please try again.';
  }

  switch (error.code) {
    case 'duplicate_renewal':
      return 'An active renewal already exists for this policy.';
    case 'invalid_assignee':
      return error.problem?.detail ?? 'The selected assignee is invalid.';
    case 'inactive_assignee':
      return error.problem?.detail ?? 'The selected assignee is inactive.';
    case 'precondition_failed':
      return error.problem?.detail ?? 'This renewal changed. Refresh and retry.';
    case 'missing_transition_prerequisite':
      return error.problem?.detail ?? 'Required transition fields are missing.';
    case 'invalid_transition':
      return error.problem?.detail ?? 'That transition is not allowed from the current state.';
    case 'assignment_not_allowed_in_terminal_state':
      return error.problem?.detail ?? 'Completed and lost renewals cannot be reassigned.';
    case 'not_found':
      return error.problem?.detail ?? 'The requested renewal or policy was not found.';
    default:
      return error.problem?.detail ?? 'Unable to save renewal changes. Please try again.';
  }
}
