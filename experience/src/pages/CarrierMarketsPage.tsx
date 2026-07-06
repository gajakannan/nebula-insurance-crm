import { useEffect, useMemo, useState } from 'react'
import { Link2, Plus, Save, Trash2 } from 'lucide-react'
import { DashboardLayout } from '@/components/layout/DashboardLayout'
import { Card, CardHeader, CardTitle } from '@/components/ui/Card'
import { ErrorFallback } from '@/components/ui/ErrorFallback'
import { Skeleton } from '@/components/ui/Skeleton'
import { useDebounce } from '@/hooks/useDebounce'
import { ApiError } from '@/services/api'
import {
  useAddCarrierActivityLink,
  useAddCarrierAppointment,
  useAddCarrierAppetiteNote,
  useAddCarrierContact,
  useCarrierMarket,
  useCarrierMarkets,
  useCreateCarrierMarket,
  useDeleteCarrierContact,
  useDeleteCarrierMarket,
  useUpdateCarrierMarket,
} from '@/features/carrier-markets'
import type {
  AppointmentStatus,
  AppetiteLevel,
  CarrierAppointmentUpsertDto,
  CarrierAppetiteNoteUpsertDto,
  CarrierMarketActivityLinkCreateDto,
  CarrierMarketContactUpsertDto,
  CarrierMarketCreateDto,
  CarrierMarketDetailDto,
  CarrierMarketDto,
  CarrierMarketStatus,
  CarrierMarketType,
  RelatedEntityType,
  RelationshipKind,
} from '@/features/carrier-markets'

const STATUS_OPTIONS = ['All', 'Active', 'Inactive', 'Prospect'] as const
const MARKET_TYPE_OPTIONS = ['All', 'Admitted', 'NonAdmitted', 'MGA', 'Wholesaler', 'Other'] as const
const APPETITE_OPTIONS: AppetiteLevel[] = ['Preferred', 'Open', 'Selective', 'Restricted', 'Closed']
const APPOINTMENT_OPTIONS: AppointmentStatus[] = ['NotAppointed', 'InProgress', 'Appointed', 'Suspended', 'Terminated']
const RELATIONSHIP_OPTIONS: RelationshipKind[] = ['Marketed', 'Quoted', 'Bound', 'Declined', 'AppointedContext', 'GeneralReference']

const emptyMarket: CarrierMarketCreateDto = {
  code: '',
  name: '',
  naicCode: null,
  amBestRating: null,
  status: 'Prospect',
  marketType: 'Other',
  relationshipOwnerUserId: null,
  websiteUrl: null,
  generalEmail: null,
  mainPhone: null,
  notes: null,
}

const emptyContact: CarrierMarketContactUpsertDto = {
  fullName: '',
  title: null,
  email: null,
  phone: null,
  roles: ['Underwriter'],
  isPrimary: false,
  notes: null,
}

const emptyAppetite: CarrierAppetiteNoteUpsertDto = {
  lineOfBusiness: null,
  region: null,
  appetiteLevel: 'Open',
  summary: '',
  detail: null,
  effectiveFrom: null,
  effectiveTo: null,
  source: null,
}

const emptyAppointment: CarrierAppointmentUpsertDto = {
  appointmentStatus: 'NotAppointed',
  states: [],
  lineOfBusiness: null,
  appointmentNumber: null,
  effectiveDate: null,
  expirationDate: null,
  notes: null,
}

const emptyLink: CarrierMarketActivityLinkCreateDto = {
  relatedEntityType: 'Submission',
  relatedEntityId: '',
  relationshipKind: 'GeneralReference',
  note: null,
}

export default function CarrierMarketsPage() {
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<string>('All')
  const [marketType, setMarketType] = useState<string>('All')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [newMarket, setNewMarket] = useState(emptyMarket)
  const debouncedSearch = useDebounce(search)

  const marketsQuery = useCarrierMarkets({ search: debouncedSearch, status, marketType })
  const selectedQuery = useCarrierMarket(selectedId)
  const createMarket = useCreateCarrierMarket()

  useEffect(() => {
    const first = marketsQuery.data?.data[0]
    if (!selectedId && first) setSelectedId(first.id)
  }, [marketsQuery.data, selectedId])

  const selected = selectedQuery.data ?? null

  return (
    <DashboardLayout title="Carrier Markets">
      <div className="grid gap-5 xl:grid-cols-[minmax(360px,440px)_1fr]">
        <Card>
          <CardHeader>
            <CardTitle>Market Directory</CardTitle>
          </CardHeader>
          <div className="space-y-4">
            <div className="grid gap-2 sm:grid-cols-3">
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search markets..."
                className="sm:col-span-3 rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              />
              <select value={status} onChange={(event) => setStatus(event.target.value)} className="rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary">
                {STATUS_OPTIONS.map((option) => <option key={option} value={option}>{option}</option>)}
              </select>
              <select value={marketType} onChange={(event) => setMarketType(event.target.value)} className="rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary">
                {MARKET_TYPE_OPTIONS.map((option) => <option key={option} value={option}>{option}</option>)}
              </select>
              <button
                type="button"
                onClick={() => createMarket.mutate(cleanMarketCreate(newMarket), {
                  onSuccess: (created) => {
                    setSelectedId(created.id)
                    setNewMarket(emptyMarket)
                  },
                })}
                disabled={!newMarket.code || !newMarket.name || createMarket.isPending}
                className="inline-flex items-center justify-center gap-2 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white disabled:opacity-50"
              >
                <Plus size={16} /> Add
              </button>
            </div>

            <div className="grid gap-2 border-t border-surface-border pt-4">
              <TextInput label="Code" value={newMarket.code} onChange={(value) => setNewMarket((current) => ({ ...current, code: value }))} />
              <TextInput label="Name" value={newMarket.name} onChange={(value) => setNewMarket((current) => ({ ...current, name: value }))} />
            </div>

            {marketsQuery.isLoading && <ListSkeleton />}
            {marketsQuery.isError && (
              marketsQuery.error instanceof ApiError && marketsQuery.error.status === 403
                ? <p className="py-8 text-center text-sm text-text-muted">You don't have permission to view carrier markets.</p>
                : <ErrorFallback message="Unable to load carrier markets." onRetry={() => marketsQuery.refetch()} />
            )}
            {marketsQuery.data && (
              <div className="max-h-[58vh] overflow-auto rounded-lg border border-surface-border">
                {marketsQuery.data.data.map((market) => (
                  <button
                    key={market.id}
                    type="button"
                    onClick={() => setSelectedId(market.id)}
                    className={`block w-full border-b border-surface-border px-3 py-3 text-left last:border-b-0 ${market.id === selectedId ? 'bg-surface-highlight' : 'hover:bg-surface-card-hover'}`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="font-medium text-text-primary">{market.name}</p>
                        <p className="mt-0.5 font-mono text-xs text-text-muted">{market.code}</p>
                      </div>
                      <StatusPill status={market.status} />
                    </div>
                    <p className="mt-2 text-xs text-text-secondary">{market.marketType}{market.naicCode ? ` · NAIC ${market.naicCode}` : ''}</p>
                  </button>
                ))}
              </div>
            )}
          </div>
        </Card>

        {selectedId && selectedQuery.isLoading && <SelectedSkeleton />}
        {selectedId && selectedQuery.isError && <ErrorFallback message="Unable to load carrier market detail." onRetry={() => selectedQuery.refetch()} />}
        {selected && <CarrierMarketWorkspace market={selected} />}
        {!selectedId && !marketsQuery.isLoading && (
          <Card>
            <div className="py-12 text-center text-sm text-text-muted">No carrier market selected.</div>
          </Card>
        )}
      </div>
    </DashboardLayout>
  )
}

function CarrierMarketWorkspace({ market }: { market: CarrierMarketDetailDto }) {
  const updateMarket = useUpdateCarrierMarket()
  const deleteMarket = useDeleteCarrierMarket()
  const addContact = useAddCarrierContact()
  const deleteContact = useDeleteCarrierContact()
  const addAppetite = useAddCarrierAppetiteNote()
  const addAppointment = useAddCarrierAppointment()
  const addLink = useAddCarrierActivityLink()

  const [profile, setProfile] = useState(market)
  const [contact, setContact] = useState(emptyContact)
  const [appetite, setAppetite] = useState(emptyAppetite)
  const [appointment, setAppointment] = useState(emptyAppointment)
  const [link, setLink] = useState(emptyLink)

  useEffect(() => setProfile(market), [market])

  const saveDisabled = !profile.name || updateMarket.isPending
  const mutationError = useMemo(() => [
    updateMarket.error,
    deleteMarket.error,
    addContact.error,
    deleteContact.error,
    addAppetite.error,
    addAppointment.error,
    addLink.error,
  ].find(Boolean), [addAppointment.error, addAppetite.error, addContact.error, addLink.error, deleteContact.error, deleteMarket.error, updateMarket.error])

  return (
    <div className="space-y-5">
      <Card>
        <CardHeader>
          <CardTitle>{market.name}</CardTitle>
        </CardHeader>
        <div className="grid gap-3 lg:grid-cols-4">
          <TextInput label="Name" value={profile.name} onChange={(value) => setProfile((current) => ({ ...current, name: value }))} />
          <TextInput label="NAIC" value={profile.naicCode ?? ''} onChange={(value) => setProfile((current) => ({ ...current, naicCode: nullable(value) }))} />
          <TextInput label="AM Best" value={profile.amBestRating ?? ''} onChange={(value) => setProfile((current) => ({ ...current, amBestRating: nullable(value) }))} />
          <SelectInput label="Status" value={profile.status} options={STATUS_OPTIONS.filter((item) => item !== 'All')} onChange={(value) => setProfile((current) => ({ ...current, status: value as CarrierMarketStatus }))} />
          <SelectInput label="Market" value={profile.marketType} options={MARKET_TYPE_OPTIONS.filter((item) => item !== 'All')} onChange={(value) => setProfile((current) => ({ ...current, marketType: value as CarrierMarketType }))} />
          <TextInput label="Email" value={profile.generalEmail ?? ''} onChange={(value) => setProfile((current) => ({ ...current, generalEmail: nullable(value) }))} />
          <TextInput label="Phone" value={profile.mainPhone ?? ''} onChange={(value) => setProfile((current) => ({ ...current, mainPhone: nullable(value) }))} />
          <TextInput label="Website" value={profile.websiteUrl ?? ''} onChange={(value) => setProfile((current) => ({ ...current, websiteUrl: nullable(value) }))} />
          <label className="lg:col-span-4 text-xs font-medium text-text-muted">
            Notes
            <textarea
              value={profile.notes ?? ''}
              onChange={(event) => setProfile((current) => ({ ...current, notes: nullable(event.target.value) }))}
              rows={3}
              className="mt-1 w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary"
            />
          </label>
        </div>
        {mutationError && <MutationError error={mutationError} />}
        <div className="mt-4 flex flex-wrap gap-2">
          <button
            type="button"
            disabled={saveDisabled}
            onClick={() => updateMarket.mutate({ id: market.id, dto: cleanMarketUpdate(profile), rowVersion: market.rowVersion })}
            className="inline-flex items-center gap-2 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white disabled:opacity-50"
          >
            <Save size={16} /> Save
          </button>
          <button
            type="button"
            onClick={() => deleteMarket.mutate(market.id)}
            className="inline-flex items-center gap-2 rounded-lg border border-red-500/40 px-3 py-2 text-sm font-medium text-red-400"
          >
            <Trash2 size={16} /> Deactivate
          </button>
        </div>
      </Card>

      <div className="grid gap-5 2xl:grid-cols-2">
        <CollectionSection title="Contacts">
          <div className="grid gap-2 md:grid-cols-2">
            <TextInput label="Name" value={contact.fullName} onChange={(value) => setContact((current) => ({ ...current, fullName: value }))} />
            <TextInput label="Title" value={contact.title ?? ''} onChange={(value) => setContact((current) => ({ ...current, title: nullable(value) }))} />
            <TextInput label="Email" value={contact.email ?? ''} onChange={(value) => setContact((current) => ({ ...current, email: nullable(value) }))} />
            <TextInput label="Phone" value={contact.phone ?? ''} onChange={(value) => setContact((current) => ({ ...current, phone: nullable(value) }))} />
            <TextInput label="Roles" value={contact.roles.join(', ')} onChange={(value) => setContact((current) => ({ ...current, roles: csv(value) }))} />
            <label className="flex items-end gap-2 pb-2 text-sm text-text-secondary">
              <input type="checkbox" checked={contact.isPrimary} onChange={(event) => setContact((current) => ({ ...current, isPrimary: event.target.checked }))} />
              Primary
            </label>
          </div>
          <AddButton disabled={!contact.fullName || addContact.isPending} onClick={() => addContact.mutate({ marketId: market.id, dto: contact }, { onSuccess: () => setContact(emptyContact) })} />
          <div className="mt-4 space-y-2">
            {market.contacts.map((item) => (
              <div key={item.id} className="flex items-start justify-between gap-3 rounded-lg border border-surface-border p-3">
                <div>
                  <p className="text-sm font-medium text-text-primary">{item.fullName}{item.isPrimary ? ' · Primary' : ''}</p>
                  <p className="text-xs text-text-muted">{item.roles.join(', ')}{item.email ? ` · ${item.email}` : ''}</p>
                </div>
                <IconButton label="Delete contact" onClick={() => deleteContact.mutate({ marketId: market.id, contactId: item.id })} />
              </div>
            ))}
          </div>
        </CollectionSection>

        <CollectionSection title="Appetite">
          <div className="grid gap-2 md:grid-cols-2">
            <TextInput label="Summary" value={appetite.summary} onChange={(value) => setAppetite((current) => ({ ...current, summary: value }))} />
            <SelectInput label="Level" value={appetite.appetiteLevel} options={APPETITE_OPTIONS} onChange={(value) => setAppetite((current) => ({ ...current, appetiteLevel: value as AppetiteLevel }))} />
            <TextInput label="LOB" value={appetite.lineOfBusiness ?? ''} onChange={(value) => setAppetite((current) => ({ ...current, lineOfBusiness: nullable(value) }))} />
            <TextInput label="Region" value={appetite.region ?? ''} onChange={(value) => setAppetite((current) => ({ ...current, region: nullable(value) }))} />
          </div>
          <AddButton disabled={!appetite.summary || addAppetite.isPending} onClick={() => addAppetite.mutate({ marketId: market.id, dto: appetite }, { onSuccess: () => setAppetite(emptyAppetite) })} />
          <ItemList items={market.appetiteNotes.map((item) => ({ id: item.id, title: item.summary, meta: `${item.appetiteLevel}${item.lineOfBusiness ? ` · ${item.lineOfBusiness}` : ''}${item.region ? ` · ${item.region}` : ''}` }))} />
        </CollectionSection>

        <CollectionSection title="Appointments">
          <div className="grid gap-2 md:grid-cols-2">
            <SelectInput label="Status" value={appointment.appointmentStatus} options={APPOINTMENT_OPTIONS} onChange={(value) => setAppointment((current) => ({ ...current, appointmentStatus: value as AppointmentStatus }))} />
            <TextInput label="States" value={appointment.states.join(', ')} onChange={(value) => setAppointment((current) => ({ ...current, states: csv(value).map((state) => state.toUpperCase()) }))} />
            <TextInput label="LOB" value={appointment.lineOfBusiness ?? ''} onChange={(value) => setAppointment((current) => ({ ...current, lineOfBusiness: nullable(value) }))} />
            <TextInput label="Number" value={appointment.appointmentNumber ?? ''} onChange={(value) => setAppointment((current) => ({ ...current, appointmentNumber: nullable(value) }))} />
          </div>
          <AddButton disabled={addAppointment.isPending} onClick={() => addAppointment.mutate({ marketId: market.id, dto: appointment }, { onSuccess: () => setAppointment(emptyAppointment) })} />
          <ItemList items={market.appointments.map((item) => ({ id: item.id, title: item.appointmentStatus, meta: `${item.states.join(', ') || 'No states'}${item.lineOfBusiness ? ` · ${item.lineOfBusiness}` : ''}` }))} />
        </CollectionSection>

        <CollectionSection title="Activity Links">
          <div className="grid gap-2 md:grid-cols-2">
            <SelectInput label="Type" value={link.relatedEntityType} options={['Submission', 'Policy']} onChange={(value) => setLink((current) => ({ ...current, relatedEntityType: value as RelatedEntityType }))} />
            <SelectInput label="Kind" value={link.relationshipKind} options={RELATIONSHIP_OPTIONS} onChange={(value) => setLink((current) => ({ ...current, relationshipKind: value as RelationshipKind }))} />
            <TextInput label="Related ID" value={link.relatedEntityId} onChange={(value) => setLink((current) => ({ ...current, relatedEntityId: value }))} />
            <TextInput label="Note" value={link.note ?? ''} onChange={(value) => setLink((current) => ({ ...current, note: nullable(value) }))} />
          </div>
          <button
            type="button"
            disabled={!link.relatedEntityId || addLink.isPending}
            onClick={() => addLink.mutate({ marketId: market.id, dto: link }, { onSuccess: () => setLink(emptyLink) })}
            className="mt-3 inline-flex items-center gap-2 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white disabled:opacity-50"
          >
            <Link2 size={16} /> Link
          </button>
          <ItemList items={market.activityLinks.map((item) => ({ id: item.id, title: `${item.relatedEntityType} · ${item.relationshipKind}`, meta: item.note ?? item.relatedEntityId }))} />
        </CollectionSection>
      </div>
    </div>
  )
}

function CollectionSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
      </CardHeader>
      {children}
    </Card>
  )
}

function TextInput({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return (
    <label className="text-xs font-medium text-text-muted">
      {label}
      <input
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="mt-1 w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary"
      />
    </label>
  )
}

function SelectInput({ label, value, options, onChange }: { label: string; value: string; options: readonly string[]; onChange: (value: string) => void }) {
  return (
    <label className="text-xs font-medium text-text-muted">
      {label}
      <select value={value} onChange={(event) => onChange(event.target.value)} className="mt-1 w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary">
        {options.map((option) => <option key={option} value={option}>{option}</option>)}
      </select>
    </label>
  )
}

function AddButton({ disabled, onClick }: { disabled: boolean; onClick: () => void }) {
  return (
    <button type="button" disabled={disabled} onClick={onClick} className="mt-3 inline-flex items-center gap-2 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white disabled:opacity-50">
      <Plus size={16} /> Add
    </button>
  )
}

function IconButton({ label, onClick }: { label: string; onClick: () => void }) {
  return (
    <button type="button" aria-label={label} title={label} onClick={onClick} className="rounded-md p-1.5 text-text-muted hover:bg-red-500/10 hover:text-red-400">
      <Trash2 size={16} />
    </button>
  )
}

function ItemList({ items }: { items: Array<{ id: string; title: string; meta: string }> }) {
  return (
    <div className="mt-4 space-y-2">
      {items.map((item) => (
        <div key={item.id} className="rounded-lg border border-surface-border p-3">
          <p className="text-sm font-medium text-text-primary">{item.title}</p>
          <p className="mt-1 text-xs text-text-muted">{item.meta}</p>
        </div>
      ))}
    </div>
  )
}

function StatusPill({ status }: { status: string }) {
  return (
    <span className="rounded-full border border-surface-border px-2 py-0.5 text-xs text-text-secondary">
      {status}
    </span>
  )
}

function MutationError({ error }: { error: unknown }) {
  if (!(error instanceof ApiError)) return null
  return <p className="mt-3 text-sm text-red-400">{error.problem?.detail ?? error.message}</p>
}

function ListSkeleton() {
  return <div className="space-y-2">{Array.from({ length: 6 }).map((_, index) => <Skeleton key={index} className="h-16 w-full" />)}</div>
}

function SelectedSkeleton() {
  return <div className="space-y-5"><Skeleton className="h-72 w-full" /><Skeleton className="h-80 w-full" /></div>
}

function nullable(value: string): string | null {
  const trimmed = value.trim()
  return trimmed ? trimmed : null
}

function csv(value: string): string[] {
  return value.split(',').map((item) => item.trim()).filter(Boolean)
}

function cleanMarketCreate(value: CarrierMarketCreateDto): CarrierMarketCreateDto {
  return { ...value, code: value.code.trim(), name: value.name.trim() }
}

function cleanMarketUpdate(value: CarrierMarketDto) {
  return {
    name: value.name.trim(),
    naicCode: value.naicCode,
    amBestRating: value.amBestRating,
    status: value.status,
    marketType: value.marketType,
    relationshipOwnerUserId: value.relationshipOwnerUserId,
    websiteUrl: value.websiteUrl,
    generalEmail: value.generalEmail,
    mainPhone: value.mainPhone,
    notes: value.notes,
  }
}
