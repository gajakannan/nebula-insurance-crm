import { useCallback, useMemo, type ReactNode } from 'react'
import {
  CYBER_BUNDLE_IDENTITY,
  cyberValuesToAttributes,
  isCyberLineOfBusiness,
  normalizeCyberEnvelope,
} from '../lib/cyber'
import type { CyberLobAttributeValues, LobAttributeEnvelopeDto } from '../types'
import { useCyberSchemaBundle } from '../hooks/useLobSchemaBundle'
import { SchemaDrivenForm } from '../engine/SchemaDrivenForm'
import { createWidgetRegistry } from '../engine/widgetRegistry'
import { registerMvpWidgets } from '../engine/widgets'
import { usePinnedBundle, type ResolvedBundle } from '../engine/usePinnedBundle'
import { CYBER_UI_CONDITIONAL_MAP } from '../engine/uiConditionalMap'
import type { LobErrorIssue } from '../engine/types'
import type { PreserveConfig } from '../engine/FormPreservation'
import { useCurrentUser } from '@/features/auth/useCurrentUser'

/**
 * F0036-S0005 — `DynamicAttributePanel` reimplemented on the schema-driven
 * engine. The public prop contract is preserved exactly so the five consuming
 * screens (CreateSubmission, CreatePolicy, PolicyDetail, RenewalDetail,
 * SubmissionDetail) are untouched. Internally it bridges the flat
 * `CyberLobAttributeValues` to the engine's nested bundle shape and renders the
 * governed engine — no hardcoded Cyber field list or option constants remain in
 * the rendering path.
 */

// Built once: the MVP widget vocabulary registered into a registry.
const cyberRegistry = registerMvpWidgets(createWidgetRegistry())

// Flat field -> engine value pointer (dotted), for binding host-supplied errors.
const FLAT_TO_POINTER: Record<string, string> = {
  revenueBand: 'revenueBand',
  recordsHeld: 'recordsHeld',
  mfaEnabled: 'controls.mfaEnabled',
  mfaMaturity: 'controls.mfaMaturity',
  edrEnabled: 'controls.edrEnabled',
  backupEnabled: 'controls.backupEnabled',
  trainingFrequency: 'controls.trainingFrequency',
  requestedLimit: 'requestedLimit.amountMinor',
  requestedRetention: 'requestedRetention.amountMinor',
}

interface DynamicAttributePanelProps {
  lineOfBusiness: string | null | undefined
  value: CyberLobAttributeValues
  onChange?: (value: CyberLobAttributeValues) => void
  errors?: Record<string, string>
  readOnly?: boolean
  actions?: ReactNode
}

export function DynamicAttributePanel({
  lineOfBusiness,
  value,
  onChange,
  errors = {},
  readOnly = false,
  actions,
}: DynamicAttributePanelProps) {
  const visible = isCyberLineOfBusiness(lineOfBusiness)
  const bundleQuery = useCyberSchemaBundle(visible)
  const user = useCurrentUser()

  // F0035 preservation (S0006): engage only for an editable form with a known
  // user. form_key is scoped to the route so concurrent forms do not collide.
  const preserve: PreserveConfig | undefined = useMemo(() => {
    if (!onChange || !user) return undefined
    const route = typeof window !== 'undefined' ? window.location.pathname : '/'
    return { userId: user.sub, formKey: `cyber-attributes:${route}`, route }
  }, [onChange, user])

  const resolve = useCallback(
    (): ResolvedBundle | null =>
      bundleQuery.data
        ? { dataSchema: bundleQuery.data.dataSchema, uiSchema: bundleQuery.data.uiSchema }
        : null,
    [bundleQuery.data],
  )

  // Pin-during-edit (S0004): the form binds to this product version for the session.
  const pinned = usePinnedBundle(
    visible ? CYBER_BUNDLE_IDENTITY.productVersionId : undefined,
    'session',
    resolve,
  )

  const nestedValue = useMemo(() => cyberValuesToAttributes(value), [value])

  const lobErrors: LobErrorIssue[] = useMemo(
    () =>
      Object.entries(errors).map(([key, message]) => ({
        code: 'host',
        path: `$.attributes.${FLAT_TO_POINTER[key] ?? key}`,
        message,
      })),
    [errors],
  )

  const handleChange = useCallback(
    (next: Record<string, unknown>) => {
      if (!onChange) return
      const envelope: LobAttributeEnvelopeDto = { ...CYBER_BUNDLE_IDENTITY, attributes: next }
      onChange(normalizeCyberEnvelope(envelope))
    },
    [onChange],
  )

  if (!visible) return null

  const status = bundleQuery.data?.status ?? (bundleQuery.isError ? 'Unavailable' : 'Loading')
  const subtitle = `Bundle ${CYBER_BUNDLE_IDENTITY.productVersion} · ${status}`

  // No hardcoded fallback fields: while the bundle loads / on failure render a
  // controlled region, never a guessed form.
  if (!pinned.bundle) {
    return (
      <section
        className="space-y-4 rounded-lg border border-surface-border bg-surface-card/50 p-4"
        aria-busy={bundleQuery.isLoading}
      >
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div>
            <h3 className="text-sm font-semibold text-text-primary">Cyber attributes</h3>
            <p className="mt-1 text-xs text-text-muted">{subtitle}</p>
          </div>
          {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
        </div>
        <p
          className={bundleQuery.isError ? 'text-sm text-status-error' : 'text-sm text-text-muted'}
          role={bundleQuery.isError ? 'alert' : undefined}
        >
          {bundleQuery.isError ? 'Cyber product definition is unavailable.' : 'Loading Cyber attributes…'}
        </p>
      </section>
    )
  }

  return (
    <SchemaDrivenForm
      bundle={pinned.bundle}
      registry={cyberRegistry}
      value={nestedValue}
      onChange={onChange ? handleChange : undefined}
      lobErrors={lobErrors}
      readOnly={readOnly}
      actions={actions}
      uiConditionalMap={CYBER_UI_CONDITIONAL_MAP}
      preserve={preserve}
      title="Cyber attributes"
      subtitle={subtitle}
    />
  )
}
