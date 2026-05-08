export interface LobAttributeEnvelopeDto {
  productKey: string
  productVersion: string
  schemaVersion: string
  lineOfBusiness: string | null
  attributes: Record<string, unknown>
}

export interface LobValidationIssueDto {
  code: string
  path: string
  message: string
  severity: 'error' | 'warning' | string
}

export interface CyberLobAttributeValues {
  revenueBand: string
  recordsHeld: string
  mfaEnabled: boolean
  mfaMaturity: string
  edrEnabled: boolean
  backupEnabled: boolean
  trainingFrequency: string
  requestedLimit: string
  requestedRetention: string
}

export interface LobSchemaBundleDto {
  id: string
  productKey: string
  productVersion: string
  lineOfBusiness: string
  schemaVersion: string
  status: string
  dataSchema: unknown
  uiSchema: unknown
  rules: unknown
  projectionMap: unknown
  contentHash: string
  activatedAt: string | null
  activatedByUserId: string | null
  rowVersion: string
}
