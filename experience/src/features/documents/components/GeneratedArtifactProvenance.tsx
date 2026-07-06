import type { DocumentSidecarDto } from '../types'

export function GeneratedArtifactProvenance({ sidecar }: { sidecar: DocumentSidecarDto }) {
  if (sidecar.type !== 'generated-document') return null

  const artifactFamily = stringValue(sidecar.metadata.artifactFamily)
  const templateDocumentId = stringValue(sidecar.metadata.templateDocumentId)
  const sourceSnapshotHash = stringValue(sidecar.metadata.sourceSnapshotHash)

  return (
    <div className="rounded-lg border border-surface-border bg-surface-card/40 px-4 py-3">
      <div className="grid gap-3 text-sm sm:grid-cols-3">
        <Field label="Artifact" value={artifactFamily?.toUpperCase() ?? 'Generated'} />
        <Field label="Template" value={templateDocumentId ?? sidecar.provenance?.source ?? 'Unknown'} />
        <Field label="Source Snapshot" value={sourceSnapshotHash?.slice(0, 22) ?? 'Unknown'} />
      </div>
    </div>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-text-muted">{label}</p>
      <p className="mt-1 break-words font-medium text-text-primary">{value}</p>
    </div>
  )
}

function stringValue(value: unknown): string | null {
  return typeof value === 'string' && value.length > 0 ? value : null
}
