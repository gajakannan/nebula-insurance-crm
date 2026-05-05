import { useState } from 'react'
import { FileText, Upload } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Card } from '@/components/ui/Card'
import { Skeleton } from '@/components/ui/Skeleton'
import { cn } from '@/lib/utils'
import { useDocumentCompleteness, useDocuments } from '../hooks'
import type { DocumentListItemDto, DocumentParentRefDto } from '../types'
import { DocumentUploadDialog } from './DocumentUploadDialog'

interface ParentDocumentsPanelProps {
  parent: DocumentParentRefDto
  variant?: 'card' | 'plain'
  className?: string
}

export function ParentDocumentsPanel({ parent, variant = 'card', className }: ParentDocumentsPanelProps) {
  const [uploadOpen, setUploadOpen] = useState(false)
  const documentsQuery = useDocuments({ parent, pageSize: 8 })
  const completenessQuery = useDocumentCompleteness(parent)
  const documents = documentsQuery.data?.documents ?? []
  const content = (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <FileText size={17} className="text-nebula-violet" />
            <h2 className="text-sm font-semibold text-text-primary">Documents</h2>
          </div>
          <div className="mt-2 flex flex-wrap gap-2">
            <Metric label="Available" value={completenessQuery.data?.totals.available ?? 0} />
            <Metric label="Quarantine" value={completenessQuery.data?.totals.quarantined ?? 0} />
            <Metric label="Failed" value={completenessQuery.data?.totals.failedPromote ?? 0} />
          </div>
        </div>
        <button
          type="button"
          onClick={() => setUploadOpen(true)}
          className="inline-flex items-center justify-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white hover:bg-nebula-violet/90"
        >
          <Upload size={15} />
          Upload
        </button>
      </div>

      {documentsQuery.isLoading ? (
        <div className="space-y-2">
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
        </div>
      ) : documentsQuery.error ? (
        <p className="rounded-lg border border-status-error/30 bg-status-error/10 px-3 py-2 text-sm text-status-error">
          Unable to load documents.
        </p>
      ) : documents.length === 0 ? (
        <div className="rounded-lg border border-dashed border-surface-border px-4 py-6 text-center">
          <p className="text-sm text-text-secondary">No documents attached.</p>
        </div>
      ) : (
        <div className="divide-y divide-surface-border overflow-hidden rounded-lg border border-surface-border">
          {documents.map((document) => (
            <DocumentRow key={document.documentId} document={document} />
          ))}
        </div>
      )}

      <DocumentUploadDialog parent={parent} open={uploadOpen} onClose={() => setUploadOpen(false)} />
    </div>
  )

  if (variant === 'plain') {
    return <div className={className}>{content}</div>
  }

  return <Card className={className}>{content}</Card>
}

function DocumentRow({ document }: { document: DocumentListItemDto }) {
  return (
    <Link
      to={`/documents/${document.documentId}`}
      className="grid gap-2 bg-surface-card/40 px-4 py-3 transition-colors hover:bg-surface-card sm:grid-cols-[1fr_auto]"
    >
      <div className="min-w-0">
        <p className="truncate text-sm font-medium text-text-primary">{document.logicalName}</p>
        <p className="mt-1 text-xs text-text-muted">
          {document.type} / v{document.latestVersion} / {formatDate(document.latestUpload.atUtc)}
        </p>
      </div>
      <div className="flex flex-wrap gap-2 sm:justify-end">
        <StatusPill status={document.status} />
        <span className="rounded-full border border-surface-border px-2 py-0.5 text-xs text-text-secondary">
          {document.classification}
        </span>
      </div>
    </Link>
  )
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <span className="rounded-full border border-surface-border bg-surface-card px-2 py-0.5 text-xs text-text-secondary">
      {label}: <span className="font-medium text-text-primary">{value}</span>
    </span>
  )
}

function StatusPill({ status }: { status: string }) {
  return (
    <span
      className={cn(
        'rounded-full px-2 py-0.5 text-xs font-medium',
        status === 'available' && 'bg-status-success/15 text-status-success',
        status === 'quarantined' && 'bg-status-warning/15 text-status-warning',
        status === 'failed_promote' && 'bg-status-error/15 text-status-error',
      )}
    >
      {status.replace('_', ' ')}
    </span>
  )
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(value))
}
