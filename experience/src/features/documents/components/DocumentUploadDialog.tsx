import { useState } from 'react'
import { Upload } from 'lucide-react'
import { Modal } from '@/components/ui/Modal'
import { Select } from '@/components/ui/Select'
import { TextInput } from '@/components/ui/TextInput'
import { ApiError } from '@/services/api'
import { useDocumentMetadataSchemas, useUploadDocuments } from '../hooks'
import type { DocumentClassification, DocumentMetadata, DocumentParentRefDto, DocumentUploadResponseDto } from '../types'
import { DocumentMetadataFields } from './DocumentMetadataFields'

const CLASSIFICATION_OPTIONS = [
  { value: 'public', label: 'Public' },
  { value: 'confidential', label: 'Confidential' },
  { value: 'restricted', label: 'Restricted' },
]

const TYPE_OPTIONS = [
  { value: '', label: 'Auto detect' },
  { value: 'acord', label: 'ACORD' },
  { value: 'loss-run', label: 'Loss run' },
  { value: 'financials', label: 'Financials' },
  { value: 'supplemental', label: 'Supplemental' },
]

function schemaOptions(schemaIds: string[]) {
  const seen = new Set<string>()
  const values = [
    ...TYPE_OPTIONS.map((option) => option.value).filter(Boolean),
    ...schemaIds,
  ].filter((value) => {
    if (seen.has(value)) return false
    seen.add(value)
    return true
  })

  return [
    { value: '', label: 'Auto detect' },
    ...values.map((value) => ({
      value,
      label: TYPE_OPTIONS.find((option) => option.value === value)?.label ?? labelize(value),
    })),
  ]
}

function labelize(value: string) {
  return value
    .replace(/-/g, ' ')
    .replace(/\b\w/g, (char) => char.toUpperCase())
}

interface DocumentUploadDialogProps {
  open: boolean
  parent: DocumentParentRefDto
  onClose: () => void
}

export function DocumentUploadDialog({ open, parent, onClose }: DocumentUploadDialogProps) {
  const upload = useUploadDocuments(parent)
  const metadataSchemas = useDocumentMetadataSchemas()
  const [files, setFiles] = useState<File[]>([])
  const [classification, setClassification] = useState<DocumentClassification>('confidential')
  const [type, setType] = useState('')
  const [documentMetadata, setDocumentMetadata] = useState<DocumentMetadata>({})
  const [error, setError] = useState('')
  const [result, setResult] = useState<DocumentUploadResponseDto | null>(null)
  const schema = metadataSchemas.data?.schemas.find((item) => item.id === type)?.schema

  function resetAndClose() {
    setFiles([])
    setType('')
    setDocumentMetadata({})
    setError('')
    setResult(null)
    onClose()
  }

  async function submit() {
    if (files.length === 0) {
      setError('Select at least one file.')
      return
    }

    try {
      setError('')
      const response = await upload.mutateAsync({
        files,
        classification,
        type: type || undefined,
        metadata: type ? documentMetadata : undefined,
      })
      setResult(response)
      if (response.documents.length > 0 && response.rejected.length === 0) {
        resetAndClose()
      }
    } catch (nextError) {
      setError(describeDocumentError(nextError))
    }
  }

  return (
    <Modal open={open} onClose={resetAndClose} title="Upload documents" className="max-w-2xl">
      <div className="space-y-4">
        <div className="grid gap-4 md:grid-cols-2">
          <Select
            label="Classification"
            value={classification}
            onChange={(event) => setClassification(event.target.value as DocumentClassification)}
            options={CLASSIFICATION_OPTIONS}
          />
          <Select
            label="Type"
            value={type}
            onChange={(event) => {
              setType(event.target.value)
              setDocumentMetadata({})
            }}
            options={schemaOptions(metadataSchemas.data?.schemas.map((item) => item.id) ?? [])}
          />
        </div>

        <DocumentMetadataFields
          schema={schema}
          value={documentMetadata}
          onChange={setDocumentMetadata}
        />

        <div className="space-y-1.5">
          <label htmlFor="document-files" className="block text-xs font-medium text-text-secondary">
            Files
          </label>
          <input
            id="document-files"
            type="file"
            multiple
            accept=".pdf,.png,.docx,.xlsx,.csv"
            onChange={(event) => {
              setError('')
              setResult(null)
              setFiles(Array.from(event.target.files ?? []))
            }}
            className="block w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-secondary file:mr-3 file:rounded-md file:border-0 file:bg-nebula-violet/15 file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-nebula-violet"
          />
          <p className="text-xs text-text-muted">
            {files.length > 0 ? `${files.length} selected` : 'PDF, PNG, DOCX, XLSX, and CSV are accepted.'}
          </p>
        </div>

        {result?.rejected.length ? (
          <div className="rounded-lg border border-status-warning/35 bg-status-warning/10 px-3 py-2">
            {result.rejected.map((item) => (
              <p key={`${item.index}-${item.code}`} className="text-sm text-text-secondary">
                {item.logicalName ?? `File ${item.index + 1}`}: {item.detail ?? item.code}
              </p>
            ))}
          </div>
        ) : null}

        {error && <p className="text-sm text-status-error">{error}</p>}

        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={resetAndClose}
            className="rounded-lg border border-surface-border px-3 py-1.5 text-sm text-text-secondary hover:bg-surface-card"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={submit}
            disabled={upload.isPending}
            className="inline-flex items-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-60"
          >
            <Upload size={15} />
            {upload.isPending ? 'Uploading' : 'Upload'}
          </button>
        </div>
      </div>
    </Modal>
  )
}

export function TemplateUploadFields({
  file,
  classification,
  tags,
  onFile,
  onClassification,
  onTags,
}: {
  file: File | null
  classification: DocumentClassification
  tags: string
  onFile: (file: File | null) => void
  onClassification: (classification: DocumentClassification) => void
  onTags: (tags: string) => void
}) {
  return (
    <div className="grid gap-4 md:grid-cols-2">
      <div className="space-y-1.5">
        <label htmlFor="template-file" className="block text-xs font-medium text-text-secondary">
          Template file
        </label>
        <input
          id="template-file"
          type="file"
          accept=".pdf,.png,.docx,.xlsx,.csv"
          onChange={(event) => onFile(event.target.files?.[0] ?? null)}
          className="block w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-secondary file:mr-3 file:rounded-md file:border-0 file:bg-nebula-violet/15 file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-nebula-violet"
        />
        <p className="text-xs text-text-muted">{file ? file.name : 'Select a reusable document template.'}</p>
      </div>
      <Select
        label="Classification"
        value={classification}
        onChange={(event) => onClassification(event.target.value as DocumentClassification)}
        options={CLASSIFICATION_OPTIONS}
      />
      <div className="md:col-span-2">
        <TextInput
          label="Tags"
          value={tags}
          onChange={(event) => onTags(event.target.value)}
          placeholder="renewal, broker, acord"
        />
      </div>
    </div>
  )
}

export function describeDocumentError(error: unknown): string {
  if (error instanceof ApiError) {
    return error.problem?.detail ?? error.problem?.title ?? error.code ?? error.message
  }

  return 'Unable to complete the document request.'
}
