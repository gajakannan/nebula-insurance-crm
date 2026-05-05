export type DocumentParentType = 'account' | 'submission' | 'policy' | 'renewal'

export type DocumentClassification = 'public' | 'confidential' | 'restricted'

export type DocumentStatus = 'quarantined' | 'available' | 'failed_promote'

export interface DocumentParentRefDto {
  type: DocumentParentType
  id: string
}

export interface DocumentLatestUploadDto {
  atUtc: string
  byUserId: string
}

export interface DocumentListItemDto {
  documentId: string
  logicalName: string
  type: string
  classification: DocumentClassification
  latestVersion: number
  status: DocumentStatus
  latestUpload: DocumentLatestUploadDto
  parent: DocumentParentRefDto
}

export interface PaginationDto {
  page: number
  pageSize: number
  total: number
}

export interface PaginatedDocumentListDto {
  documents: DocumentListItemDto[]
  pagination: PaginationDto
}

export interface DocumentVersionDto {
  n: number
  fileName: string
  sizeBytes: number
  sha256: string
  status: DocumentStatus
  uploadedAt: string
  uploadedByUserId: string
  supersedes: number | null
}

export interface DocumentEventDto {
  kind: string
  at: string
  byUserId: string
  version?: number | null
  fromVersion?: number | null
  toVersion?: number | null
  from?: string | null
  to?: string | null
  error?: string | null
  reason?: string | null
}

export type DocumentMetadataValue = string | number | boolean | null
export type DocumentMetadata = Record<string, DocumentMetadataValue>

export interface DocumentMetadataSchemaRefDto {
  id: string
  version: number
  schemaHash: string
}

export interface DocumentMetadataPropertySchema {
  type?: string | string[]
  title?: string
  enum?: string[]
  format?: string
  minimum?: number
  maximum?: number
  maxLength?: number
}

export interface DocumentMetadataJsonSchema {
  title?: string
  type?: string
  required?: string[]
  additionalProperties?: boolean
  properties?: Record<string, DocumentMetadataPropertySchema>
}

export interface DocumentMetadataSchemaDto {
  id: string
  version: number
  status: string
  schemaHash: string
  schema: DocumentMetadataJsonSchema
}

export interface DocumentMetadataSchemaRegistryDto {
  schemas: DocumentMetadataSchemaDto[]
}

export interface DocumentSidecarDto {
  documentId: string
  logicalName: string
  parent: DocumentParentRefDto
  classification: DocumentClassification
  type: string
  tags: string[]
  metadataSchema: DocumentMetadataSchemaRefDto
  metadata: DocumentMetadata
  uploaderId: string
  auditTimestamps: {
    createdAtUtc: string
    updatedAtUtc: string
  }
  provenance: {
    source: string
    materializedAt: string | null
    byUserId: string | null
  } | null
  versions: DocumentVersionDto[]
  useCount: number | null
  lastUsedAt: string | null
  events: DocumentEventDto[]
}

export interface DocumentDetailDto {
  sidecar: DocumentSidecarDto
  previewUrls: Array<string | null>
}

export interface DocumentUploadAcceptedItemDto {
  documentId: string
  logicalName: string
  status: DocumentStatus
}

export interface DocumentUploadRejectedItemDto {
  index: number
  logicalName: string | null
  code: string
  detail: string | null
}

export interface DocumentUploadResponseDto {
  documents: DocumentUploadAcceptedItemDto[]
  rejected: DocumentUploadRejectedItemDto[]
}

export interface DocumentMetadataUpdateRequestDto {
  classification?: DocumentClassification
  type?: string
  tags?: string[]
  metadata?: DocumentMetadata
}

export interface DocumentCompletenessSignalDto {
  parent: DocumentParentRefDto
  totals: {
    available: number
    quarantined: number
    failedPromote: number
  }
  byType: Array<{ type: string; count: number }>
  byClassification: Array<{ classification: DocumentClassification; count: number }>
}

export interface DocumentTemplateDto {
  templateId: string
  logicalName: string
  type: string
  classification: DocumentClassification
  tags: string[]
  useCount: number
  lastUsedAt: string | null
  uploadedAtUtc: string
  uploadedByUserId: string
}

export interface PaginatedDocumentTemplateListDto {
  templates: DocumentTemplateDto[]
  pagination: PaginationDto
}
