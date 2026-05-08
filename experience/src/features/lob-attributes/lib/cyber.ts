import type { CyberLobAttributeValues, LobAttributeEnvelopeDto } from '../types'

export const CYBER_BUNDLE_IDENTITY = {
  productVersionId: '48f5f86a-7396-50bf-92dd-a3a36fe63c20',
  productKey: 'cyber',
  productVersion: '1.0.0',
  schemaVersion: '1.0.0',
  lineOfBusiness: 'Cyber',
} as const

export const CYBER_REVENUE_BANDS = [
  { value: '0-10M', label: '$0-10M' },
  { value: '10-50M', label: '$10-50M' },
  { value: '50-250M', label: '$50-250M' },
  { value: '250M+', label: '$250M+' },
]

export const CYBER_MFA_MATURITY_OPTIONS = [
  { value: 'Implemented', label: 'Implemented' },
  { value: 'Partial', label: 'Partial' },
  { value: 'Planned', label: 'Planned' },
]

export const CYBER_TRAINING_FREQUENCY_OPTIONS = [
  { value: 'Annual', label: 'Annual' },
  { value: 'SemiAnnual', label: 'Semi-annual' },
  { value: 'Quarterly', label: 'Quarterly' },
]

export function emptyCyberLobAttributes(): CyberLobAttributeValues {
  return {
    revenueBand: '',
    recordsHeld: '',
    mfaEnabled: false,
    mfaMaturity: '',
    edrEnabled: false,
    backupEnabled: false,
    trainingFrequency: '',
    requestedLimit: '',
    requestedRetention: '',
  }
}

export function isCyberLineOfBusiness(lineOfBusiness: string | null | undefined): boolean {
  return lineOfBusiness === CYBER_BUNDLE_IDENTITY.lineOfBusiness
}

export function normalizeCyberEnvelope(envelope: LobAttributeEnvelopeDto | null | undefined): CyberLobAttributeValues {
  if (!envelope || envelope.productKey !== CYBER_BUNDLE_IDENTITY.productKey) {
    return emptyCyberLobAttributes()
  }

  const attributes = envelope.attributes
  const controls = asRecord(attributes.controls)
  const requestedLimit = asRecord(attributes.requestedLimit)
  const requestedRetention = asRecord(attributes.requestedRetention)
  return {
    revenueBand: typeof attributes.revenueBand === 'string' ? attributes.revenueBand : '',
    recordsHeld: toText(attributes.recordsHeld),
    mfaEnabled: controls?.mfaEnabled === true,
    mfaMaturity: typeof controls?.mfaMaturity === 'string' ? controls.mfaMaturity : '',
    edrEnabled: controls?.edrEnabled === true,
    backupEnabled: controls?.backupEnabled === true,
    trainingFrequency: typeof controls?.trainingFrequency === 'string' ? controls.trainingFrequency : '',
    requestedLimit: minorToMajorText(requestedLimit?.amountMinor),
    requestedRetention: minorToMajorText(requestedRetention?.amountMinor),
  }
}

export function buildCyberEnvelope(values: CyberLobAttributeValues): LobAttributeEnvelopeDto {
  return {
    ...CYBER_BUNDLE_IDENTITY,
    attributes: {
      revenueBand: values.revenueBand,
      recordsHeld: Number(values.recordsHeld),
      controls: {
        mfaEnabled: values.mfaEnabled,
        mfaMaturity: values.mfaEnabled ? values.mfaMaturity : null,
        edrEnabled: values.edrEnabled,
        backupEnabled: values.backupEnabled,
        trainingFrequency: values.trainingFrequency,
      },
      requestedLimit: {
        amountMinor: majorToMinor(values.requestedLimit),
        currency: 'USD',
      },
      requestedRetention: {
        amountMinor: majorToMinor(values.requestedRetention),
        currency: 'USD',
      },
    },
  }
}

export function validateCyberLobAttributes(values: CyberLobAttributeValues): Record<string, string> {
  const errors: Record<string, string> = {}

  if (!values.revenueBand) errors.revenueBand = 'Revenue band is required.'
  if (!values.recordsHeld) {
    errors.recordsHeld = 'Records held is required.'
  } else if (!isNonNegativeNumber(values.recordsHeld)) {
    errors.recordsHeld = 'Records held must be zero or greater.'
  }

  if (Number(values.recordsHeld) >= 1_000_000 && !values.mfaEnabled) {
    errors.mfaEnabled = 'MFA is required for high record counts.'
  }

  if (values.mfaEnabled && !values.mfaMaturity) {
    errors.mfaMaturity = 'MFA maturity is required when MFA is enabled.'
  }

  if (!values.trainingFrequency) {
    errors.trainingFrequency = 'Training frequency is required.'
  }

  if (!values.requestedLimit) {
    errors.requestedLimit = 'Requested limit is required.'
  } else if (!isPositiveNumber(values.requestedLimit)) {
    errors.requestedLimit = 'Requested limit must be greater than zero.'
  }

  if (!values.requestedRetention) {
    errors.requestedRetention = 'Requested retention is required.'
  } else if (!isNonNegativeNumber(values.requestedRetention)) {
    errors.requestedRetention = 'Requested retention must be zero or greater.'
  } else if (Number(values.requestedLimit) > 0 && Number(values.requestedRetention) < Number(values.requestedLimit) * 0.01) {
    errors.requestedRetention = 'Requested retention must be at least 1% of requested limit.'
  }

  return errors
}

function toText(value: unknown): string {
  return typeof value === 'number' || typeof value === 'string' ? String(value) : ''
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object' && !Array.isArray(value)
    ? value as Record<string, unknown>
    : null
}

function majorToMinor(value: string): number {
  return Math.round(Number(value || 0) * 100)
}

function minorToMajorText(value: unknown): string {
  return typeof value === 'number' ? String(value / 100) : ''
}

function isPositiveNumber(value: string): boolean {
  const parsed = Number(value)
  return Number.isFinite(parsed) && parsed > 0
}

function isNonNegativeNumber(value: string): boolean {
  const parsed = Number(value)
  return Number.isFinite(parsed) && parsed >= 0
}
