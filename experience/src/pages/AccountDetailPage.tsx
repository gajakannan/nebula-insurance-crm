import { useEffect, useMemo, useState } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Modal } from '@/components/ui/Modal';
import { Select } from '@/components/ui/Select';
import { Skeleton } from '@/components/ui/Skeleton';
import { Tabs } from '@/components/ui/Tabs';
import { TextInput } from '@/components/ui/TextInput';
import { AccountStatusBadge } from '@/features/accounts/components/AccountReference';
import {
  describeAccountApiError,
  extractProblemFieldErrors,
  useAccount,
  useAccountContacts,
  useAccountList,
  useAccountPolicies,
  useAccountSummary,
  useAccountTimeline,
  useChangeAccountRelationship,
  useCreateAccountContact,
  useDeleteAccountContact,
  useMergeAccount,
  useTransitionAccount,
  useUpdateAccount,
  useUpdateAccountContact,
  type AccountContactDto,
  type AccountContactRequestDto,
  type AccountDto,
  type AccountStatus,
  type AccountUpdateRequestDto,
} from '@/features/accounts';
import { useBrokers } from '@/features/brokers';
import { RenewalStatusBadge, useRenewals } from '@/features/renewals';
import { SubmissionStatusBadge, useSubmissions } from '@/features/submissions';
import { AssigneePicker, type UserSummaryDto } from '@/features/tasks';
import { ActivityFeedItem } from '@/features/timeline/components/ActivityFeedItem';
import { ApiError } from '@/services/api';
import { US_STATES } from '@/lib/us-states';

const TABS = ['Overview', 'Contacts', 'Activity'];
const DELETE_REASON_OPTIONS = [
  { value: 'Duplicate', label: 'Duplicate' },
  { value: 'NoLongerInsured', label: 'No Longer Insured' },
  { value: 'Other', label: 'Other' },
];

type RelationshipType = 'BrokerOfRecord' | 'PrimaryProducer' | 'Territory';

interface ForwardedState {
  forwardedFrom?: string;
  sourceName?: string;
}

export default function AccountDetailPage() {
  const { accountId = '' } = useParams<{ accountId: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const forwardedState = (location.state as ForwardedState | null) ?? null;

  const accountQuery = useAccount(accountId);
  const summaryQuery = useAccountSummary(accountId, !!accountQuery.data);
  const contactsQuery = useAccountContacts(accountId);
  const policiesQuery = useAccountPolicies(accountId, 5);
  const submissionsQuery = useSubmissions({ accountId, pageSize: 5, enabled: !!accountQuery.data });
  const renewalsQuery = useRenewals({ accountId, includeTerminal: true, pageSize: 5, enabled: !!accountQuery.data });
  const timelineQuery = useAccountTimeline(accountId);
  const updateAccount = useUpdateAccount(accountId);
  const transitionAccount = useTransitionAccount(accountId);
  const mergeAccount = useMergeAccount(accountId);
  const changeRelationship = useChangeAccountRelationship(accountId);
  const createContact = useCreateAccountContact(accountId);
  const updateContact = useUpdateAccountContact(accountId);
  const deleteContact = useDeleteAccountContact(accountId);
  const brokersQuery = useBrokers({ status: 'Active', page: 1, pageSize: 100 });
  const survivorCandidatesQuery = useAccountList({ status: 'Active', includeSummary: false, page: 1, pageSize: 100 });

  const [activeTab, setActiveTab] = useState('Overview');
  const [editOpen, setEditOpen] = useState(false);
  const [editForm, setEditForm] = useState<AccountUpdateRequestDto | null>(null);
  const [editErrors, setEditErrors] = useState<Record<string, string>>({});
  const [editServerError, setEditServerError] = useState('');

  const [contactOpen, setContactOpen] = useState(false);
  const [editingContact, setEditingContact] = useState<AccountContactDto | null>(null);
  const [contactForm, setContactForm] = useState<AccountContactRequestDto>({
    fullName: '',
    role: '',
    email: '',
    phone: '',
    isPrimary: false,
  });
  const [contactErrors, setContactErrors] = useState<Record<string, string>>({});
  const [contactServerError, setContactServerError] = useState('');
  const [deletingContact, setDeletingContact] = useState<AccountContactDto | null>(null);

  const [lifecycleTarget, setLifecycleTarget] = useState<AccountStatus | null>(null);
  const [lifecycleReasonCode, setLifecycleReasonCode] = useState('');
  const [lifecycleReasonDetail, setLifecycleReasonDetail] = useState('');
  const [lifecycleError, setLifecycleError] = useState('');

  const [mergeOpen, setMergeOpen] = useState(false);
  const [mergeSurvivorId, setMergeSurvivorId] = useState('');
  const [mergeNotes, setMergeNotes] = useState('');
  const [mergeError, setMergeError] = useState('');

  const [relationshipType, setRelationshipType] = useState<RelationshipType | null>(null);
  const [relationshipBrokerId, setRelationshipBrokerId] = useState('');
  const [relationshipProducer, setRelationshipProducer] = useState<UserSummaryDto | null>(null);
  const [relationshipTerritory, setRelationshipTerritory] = useState('');
  const [relationshipNotes, setRelationshipNotes] = useState('');
  const [relationshipError, setRelationshipError] = useState('');

  useEffect(() => {
    const account = accountQuery.data;
    if (!account) return;
    if (account.status !== 'Merged' || !account.survivorAccountId || forwardedState?.forwardedFrom) {
      return;
    }

    navigate(`/accounts/${account.survivorAccountId}`, {
      replace: true,
      state: {
        forwardedFrom: account.id,
        sourceName: account.displayName,
      } satisfies ForwardedState,
    });
  }, [accountQuery.data, forwardedState?.forwardedFrom, navigate]);

  const timelineEvents = timelineQuery.data?.pages.flatMap((page) => page.data) ?? [];
  const account = accountQuery.data;
  const summary = summaryQuery.data;
  const brokers = brokersQuery.data?.data ?? [];
  const survivorOptions = (survivorCandidatesQuery.data?.data ?? []).filter((candidate) => candidate.id !== accountId);
  const canMutate = account && (account.status === 'Active' || account.status === 'Inactive');

  const mergedWarning = forwardedState?.forwardedFrom && account?.status === 'Merged'
    ? 'Redirect stopped after one hop because the survivor is also merged. Review the chain manually.'
    : null;

  const profileRows = useMemo(() => {
    if (!account) return [];

    return [
      ['Legal name', account.legalName ?? '—'],
      ['Tax ID', account.taxId ?? '—'],
      ['Industry', account.industry ?? '—'],
      ['Primary line', account.primaryLineOfBusiness ?? '—'],
      ['Region', account.region ?? '—'],
      ['Address', formatAddress(account)],
      ['Updated', formatDateTime(account.updatedAt)],
    ] as const;
  }, [account]);

  if (accountQuery.isLoading) {
    return (
      <DashboardLayout title="Account">
        <div className="space-y-4">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-80 w-full" />
        </div>
      </DashboardLayout>
    );
  }

  if (accountQuery.error) {
    const apiError = accountQuery.error instanceof ApiError ? accountQuery.error : null;

    if (apiError?.status === 410) {
      const problem = (apiError.problem ?? {}) as {
        stableDisplayName?: string;
        removedAt?: string;
        reasonCode?: string;
      };

      return (
        <DashboardLayout title="Account">
          <div className="space-y-6">
            <Link
              to="/accounts"
              className="inline-flex items-center gap-1 text-xs text-text-muted hover:text-text-secondary"
            >
              <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
              </svg>
              Accounts
            </Link>

            <Card className="max-w-3xl">
              <div className="space-y-4">
                <div className="flex items-center gap-2">
                  <AccountStatusBadge status="Deleted" />
                  <span className="text-sm text-text-muted">Read-only tombstone</span>
                </div>
                <div>
                  <h2 className="text-2xl font-semibold text-text-primary">
                    {problem.stableDisplayName ?? 'Deleted account'}
                  </h2>
                  <p className="mt-2 text-sm text-text-secondary">
                    This account was deleted and can no longer be edited or used for new work.
                  </p>
                </div>
                <div className="grid gap-4 md:grid-cols-2">
                  <DetailField label="Removed At" value={problem.removedAt ? formatDateTime(problem.removedAt) : 'Unavailable'} />
                  <DetailField label="Reason Code" value={problem.reasonCode ?? 'Unavailable'} />
                </div>
              </div>
            </Card>
          </div>
        </DashboardLayout>
      );
    }

    if (apiError?.status === 404) {
      return (
        <DashboardLayout title="Account">
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <p className="text-sm text-text-secondary">Account not found.</p>
            <Link to="/accounts" className="mt-3 text-sm text-nebula-violet hover:underline">
              Back to account list
            </Link>
          </div>
        </DashboardLayout>
      );
    }

    return (
      <DashboardLayout title="Account">
        <ErrorFallback message="Unable to load account." onRetry={() => accountQuery.refetch()} />
      </DashboardLayout>
    );
  }

  if (!account) return null;
  const currentAccount = account;

  function openEditModal() {
    setEditForm({
      displayName: currentAccount.displayName,
      legalName: currentAccount.legalName,
      taxId: currentAccount.taxId,
      industry: currentAccount.industry,
      primaryLineOfBusiness: currentAccount.primaryLineOfBusiness,
      region: currentAccount.region,
      address1: currentAccount.address1,
      address2: currentAccount.address2,
      city: currentAccount.city,
      state: currentAccount.state,
      postalCode: currentAccount.postalCode,
      country: currentAccount.country,
    });
    setEditErrors({});
    setEditServerError('');
    setEditOpen(true);
  }

  async function saveEdit() {
    if (!editForm) return;

    try {
      await updateAccount.mutateAsync({
        dto: normalizeAccountUpdate(editForm),
        rowVersion: currentAccount.rowVersion,
      });
      setEditOpen(false);
    } catch (error) {
      setEditErrors(extractProblemFieldErrors(error));
      setEditServerError(describeAccountApiError(error));
    }
  }

  function openContactModal(contact?: AccountContactDto) {
    setEditingContact(contact ?? null);
    setContactForm({
      fullName: contact?.fullName ?? '',
      role: contact?.role ?? '',
      email: contact?.email ?? '',
      phone: contact?.phone ?? '',
      isPrimary: contact?.isPrimary ?? false,
    });
    setContactErrors({});
    setContactServerError('');
    setContactOpen(true);
  }

  async function saveContact() {
    if (!contactForm.fullName.trim()) {
      setContactErrors({ fullName: 'Full name is required.' });
      return;
    }

    try {
      if (editingContact) {
        await updateContact.mutateAsync({
          contactId: editingContact.id,
          dto: normalizeContact(contactForm),
          rowVersion: editingContact.rowVersion,
        });
      } else {
        await createContact.mutateAsync(normalizeContact(contactForm));
      }
      setContactOpen(false);
      setEditingContact(null);
    } catch (error) {
      setContactErrors(extractProblemFieldErrors(error));
      setContactServerError(describeAccountApiError(error));
    }
  }

  async function confirmDeleteContact() {
    if (!deletingContact) return;

    try {
      await deleteContact.mutateAsync({
        contactId: deletingContact.id,
        rowVersion: deletingContact.rowVersion,
      });
      setDeletingContact(null);
    } catch (error) {
      setContactServerError(describeAccountApiError(error));
      setDeletingContact(null);
    }
  }

  async function saveLifecycle() {
    if (!lifecycleTarget) return;

    try {
      await transitionAccount.mutateAsync({
        dto: {
          toState: lifecycleTarget,
          reasonCode: lifecycleTarget === 'Deleted' ? normalizeOptional(lifecycleReasonCode) : null,
          reasonDetail: lifecycleTarget === 'Deleted' ? normalizeOptional(lifecycleReasonDetail) : null,
        },
        rowVersion: currentAccount.rowVersion,
      });
      setLifecycleTarget(null);
      setLifecycleReasonCode('');
      setLifecycleReasonDetail('');
      setLifecycleError('');
    } catch (error) {
      setLifecycleError(describeAccountApiError(error));
    }
  }

  async function saveMerge() {
    if (!mergeSurvivorId) {
      setMergeError('Select a survivor account before merging.');
      return;
    }

    try {
      await mergeAccount.mutateAsync({
        dto: {
          survivorAccountId: mergeSurvivorId,
          notes: normalizeOptional(mergeNotes),
        },
        rowVersion: currentAccount.rowVersion,
      });
      setMergeOpen(false);
      setMergeError('');
    } catch (error) {
      setMergeError(describeAccountApiError(error));
    }
  }

  function openRelationshipModal(type: RelationshipType) {
    setRelationshipType(type);
    setRelationshipBrokerId(currentAccount.brokerOfRecordId ?? '');
    setRelationshipProducer(currentAccount.primaryProducerUserId && currentAccount.primaryProducerDisplayName
      ? {
          userId: currentAccount.primaryProducerUserId,
          displayName: currentAccount.primaryProducerDisplayName,
          email: '',
          roles: [],
          isActive: true,
        }
      : null);
    setRelationshipTerritory(currentAccount.territoryCode ?? '');
    setRelationshipNotes('');
    setRelationshipError('');
  }

  async function saveRelationship() {
    if (!relationshipType) return;

    const newValue = relationshipType === 'BrokerOfRecord'
      ? relationshipBrokerId
      : relationshipType === 'PrimaryProducer'
        ? relationshipProducer?.userId ?? ''
        : relationshipTerritory.trim();

    if (!newValue) {
      setRelationshipError('Provide a replacement value before saving.');
      return;
    }

    try {
      await changeRelationship.mutateAsync({
        dto: {
          relationshipType,
          newValue,
          notes: normalizeOptional(relationshipNotes),
        },
        rowVersion: currentAccount.rowVersion,
      });
      setRelationshipType(null);
      setRelationshipError('');
    } catch (error) {
      setRelationshipError(describeAccountApiError(error));
    }
  }

  return (
    <DashboardLayout title="Account">
      <div className="space-y-6">
        <Link
          to="/accounts"
          className="inline-flex items-center gap-1 text-xs text-text-muted hover:text-text-secondary"
        >
          <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
          </svg>
          Accounts
        </Link>

        {forwardedState?.forwardedFrom && (
          <Card className="border border-status-warning/35 bg-status-warning/10">
            <p className="text-sm text-text-secondary">
              Redirected from merged account <span className="font-medium text-text-primary">{forwardedState.sourceName ?? 'unknown account'}</span>.
            </p>
            {mergedWarning && (
              <p className="mt-2 text-xs text-text-muted">{mergedWarning}</p>
            )}
          </Card>
        )}

        <Card>
          <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
            <div className="space-y-3">
              <div className="flex flex-wrap items-center gap-2">
                <AccountStatusBadge status={account.status} />
                {account.primaryLineOfBusiness && (
                  <span className="rounded-full border border-surface-border bg-surface-card px-2 py-0.5 text-[11px] font-medium text-text-muted">
                    {account.primaryLineOfBusiness}
                  </span>
                )}
              </div>
              <div>
                <h2 className="text-2xl font-semibold text-text-primary">{account.displayName}</h2>
                <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-text-secondary">
                  {account.region && <span>{account.region}</span>}
                  {account.territoryCode && (
                    <>
                      <span aria-hidden="true">·</span>
                      <span>{account.territoryCode}</span>
                    </>
                  )}
                  {account.brokerOfRecordName && (
                    <>
                      <span aria-hidden="true">·</span>
                      <span>{account.brokerOfRecordName}</span>
                    </>
                  )}
                </div>
              </div>
            </div>

            {canMutate && (
              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  onClick={openEditModal}
                  className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
                >
                  Edit Profile
                </button>
                <button
                  type="button"
                  onClick={() => openRelationshipModal('BrokerOfRecord')}
                  className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
                >
                  Change BOR
                </button>
                <button
                  type="button"
                  onClick={() => openRelationshipModal('PrimaryProducer')}
                  className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
                >
                  Change Producer
                </button>
                <button
                  type="button"
                  onClick={() => openRelationshipModal('Territory')}
                  className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
                >
                  Change Territory
                </button>
              </div>
            )}
          </div>
        </Card>

        <Card>
          <Tabs tabs={TABS} activeTab={activeTab} onTabChange={setActiveTab}>
            {activeTab === 'Overview' && (
              <div className="space-y-6">
                <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
                  <SummaryMetric label="Active Policies" value={String(summary?.activePolicyCount ?? 0)} />
                  <SummaryMetric label="Open Submissions" value={String(summary?.openSubmissionCount ?? 0)} />
                  <SummaryMetric label="Renewals Due" value={String(summary?.renewalDueCount ?? 0)} />
                  <SummaryMetric label="Last Activity" value={summary?.lastActivityAt ? formatDate(summary.lastActivityAt) : 'No activity'} />
                </div>

                <div className="grid gap-4 xl:grid-cols-[1.3fr_1fr]">
                  <Card className="border border-surface-border bg-surface-card/35">
                    <CardHeader>
                      <CardTitle>Profile</CardTitle>
                    </CardHeader>
                    <div className="grid gap-3 md:grid-cols-2">
                      {profileRows.map(([label, value]) => (
                        <DetailField key={label} label={label} value={value} />
                      ))}
                      <DetailField label="Primary Producer" value={account.primaryProducerDisplayName ?? 'Unassigned'} />
                      <DetailField label="Broker of Record" value={account.brokerOfRecordName ?? 'Unassigned'} />
                      <DetailField label="Territory" value={account.territoryCode ?? '—'} />
                      <DetailField label="Stable Display Name" value={account.stableDisplayName} />
                    </div>
                  </Card>

                  <Card className="border border-surface-border bg-surface-card/35">
                    <CardHeader>
                      <CardTitle>Lifecycle</CardTitle>
                    </CardHeader>
                    <div className="space-y-3">
                      <DetailField label="Current Status" value={account.status} />
                      {account.removedAt && (
                        <DetailField label="Removed At" value={formatDateTime(account.removedAt)} />
                      )}
                      <div className="flex flex-wrap gap-2">
                        {account.status === 'Active' && (
                          <button
                            type="button"
                            onClick={() => setLifecycleTarget('Inactive')}
                            className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
                          >
                            Deactivate
                          </button>
                        )}
                        {account.status === 'Inactive' && (
                          <button
                            type="button"
                            onClick={() => setLifecycleTarget('Active')}
                            className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
                          >
                            Reactivate
                          </button>
                        )}
                        {canMutate && (
                          <>
                            <button
                              type="button"
                              onClick={() => setLifecycleTarget('Deleted')}
                              className="rounded-lg border border-status-error/35 bg-status-error/10 px-3 py-1.5 text-sm text-text-secondary transition-colors hover:bg-status-error/20 hover:text-text-primary"
                            >
                              Delete
                            </button>
                            <button
                              type="button"
                              onClick={() => setMergeOpen(true)}
                              className="rounded-lg border border-status-warning/35 bg-status-warning/10 px-3 py-1.5 text-sm text-text-secondary transition-colors hover:bg-status-warning/20 hover:text-text-primary"
                            >
                              Merge
                            </button>
                          </>
                        )}
                      </div>
                    </div>
                  </Card>
                </div>

                <div className="grid gap-4 xl:grid-cols-3">
                  <RailCard title="Submissions">
                    {submissionsQuery.data?.data.length ? (
                      submissionsQuery.data.data.map((submission) => (
                        <Link key={submission.id} to={`/submissions/${submission.id}`} className="flex items-center justify-between rounded-lg border border-surface-border px-3 py-2 hover:bg-surface-card">
                          <span className="text-sm text-text-primary">{submission.accountDisplayName}</span>
                          <SubmissionStatusBadge status={submission.currentStatus} />
                        </Link>
                      ))
                    ) : (
                      <EmptyRail message="No submissions linked." />
                    )}
                  </RailCard>

                  <RailCard title="Renewals">
                    {renewalsQuery.data?.data.length ? (
                      renewalsQuery.data.data.map((renewal) => (
                        <Link
                          key={renewal.id}
                          to={`/renewals/${renewal.id}`}
                          className="flex items-center justify-between rounded-lg border border-surface-border px-3 py-2 hover:bg-surface-card"
                        >
                          <span className="text-sm text-text-primary">{renewal.policyNumber}</span>
                          <RenewalStatusBadge status={renewal.currentStatus} />
                        </Link>
                      ))
                    ) : (
                      <EmptyRail message="No renewals linked." />
                    )}
                  </RailCard>

                  <RailCard title="Policies">
                    {policiesQuery.data?.data.length ? (
                      policiesQuery.data.data.map((policy) => (
                        <div key={policy.id} className="rounded-lg border border-surface-border px-3 py-2">
                          <p className="text-sm font-medium text-text-primary">{policy.policyNumber}</p>
                          <p className="mt-1 text-xs text-text-muted">
                            {policy.carrier ?? 'Carrier unavailable'} • {formatDate(policy.expirationDate)}
                          </p>
                        </div>
                      ))
                    ) : (
                      <EmptyRail message="No policies linked." />
                    )}
                  </RailCard>
                </div>
              </div>
            )}

            {activeTab === 'Contacts' && (
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-sm font-semibold text-text-primary">Account contacts</h3>
                    <p className="mt-1 text-xs text-text-muted">
                      Maintain account-scoped contacts with a single primary.
                    </p>
                  </div>
                  {canMutate && (
                    <button
                      type="button"
                      onClick={() => openContactModal()}
                      className="rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90"
                    >
                      Add Contact
                    </button>
                  )}
                </div>

                {contactsQuery.isLoading && <AccountSectionSkeleton />}
                {!contactsQuery.isLoading && !contactsQuery.data?.data.length && (
                  <EmptyRail message="No contacts recorded yet." />
                )}
                {!!contactsQuery.data?.data.length && (
                  <div className="space-y-3">
                    {contactsQuery.data.data.map((contact) => (
                      <div key={contact.id} className="flex flex-col gap-3 rounded-lg border border-surface-border p-4 md:flex-row md:items-center md:justify-between">
                        <div>
                          <div className="flex items-center gap-2">
                            <p className="font-medium text-text-primary">{contact.fullName}</p>
                            {contact.isPrimary && (
                              <span className="rounded-full border border-nebula-violet/30 bg-nebula-violet/10 px-2 py-0.5 text-[11px] font-medium text-nebula-violet">
                                Primary
                              </span>
                            )}
                          </div>
                          <p className="mt-1 text-sm text-text-secondary">
                            {[contact.role, contact.email, contact.phone].filter(Boolean).join(' • ') || 'No additional details'}
                          </p>
                        </div>
                        {canMutate && (
                          <div className="flex gap-2">
                            <button
                              type="button"
                              onClick={() => openContactModal(contact)}
                              className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
                            >
                              Edit
                            </button>
                            <button
                              type="button"
                              onClick={() => setDeletingContact(contact)}
                              className="rounded-lg border border-status-error/35 bg-status-error/10 px-3 py-1.5 text-sm text-text-secondary transition-colors hover:bg-status-error/20 hover:text-text-primary"
                            >
                              Remove
                            </button>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {activeTab === 'Activity' && (
              <div className="space-y-4">
                {timelineQuery.isLoading && <AccountSectionSkeleton />}
                {timelineQuery.isError && (
                  <div className="rounded-lg border border-status-error/35 bg-status-error/10 px-3 py-3 text-sm text-text-secondary">
                    Unable to load account activity.
                  </div>
                )}
                {!timelineQuery.isLoading && !timelineQuery.isError && timelineEvents.length === 0 && (
                  <EmptyRail message="No activity recorded yet." />
                )}
                {!timelineQuery.isLoading && !timelineQuery.isError && timelineEvents.length > 0 && (
                  <div className="space-y-4">
                    <div className="space-y-1">
                      {timelineEvents.map((event, index) => (
                        <ActivityFeedItem key={event.id} event={event} isLast={index === timelineEvents.length - 1} />
                      ))}
                    </div>

                    {timelineQuery.hasNextPage && (
                      <button
                        type="button"
                        onClick={() => timelineQuery.fetchNextPage()}
                        disabled={timelineQuery.isFetchingNextPage}
                        className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
                      >
                        {timelineQuery.isFetchingNextPage ? 'Loading…' : 'Load more'}
                      </button>
                    )}
                  </div>
                )}
              </div>
            )}
          </Tabs>
        </Card>
      </div>

      <Modal open={editOpen} onClose={() => setEditOpen(false)} title="Edit Account Profile">
        {editForm && (
          <div className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <TextInput label="Display Name" value={editForm.displayName ?? ''} onChange={(event) => setEditForm((current) => current ? { ...current, displayName: event.target.value } : current)} error={editErrors.displayName} />
              <TextInput label="Legal Name" value={editForm.legalName ?? ''} onChange={(event) => setEditForm((current) => current ? { ...current, legalName: event.target.value } : current)} error={editErrors.legalName} />
              <TextInput label="Tax ID" value={editForm.taxId ?? ''} onChange={(event) => setEditForm((current) => current ? { ...current, taxId: event.target.value } : current)} error={editErrors.taxId} />
              <TextInput label="Industry" value={editForm.industry ?? ''} onChange={(event) => setEditForm((current) => current ? { ...current, industry: event.target.value } : current)} error={editErrors.industry} />
              <TextInput label="Primary Line of Business" value={editForm.primaryLineOfBusiness ?? ''} onChange={(event) => setEditForm((current) => current ? { ...current, primaryLineOfBusiness: event.target.value } : current)} error={editErrors.primaryLineOfBusiness} />
              <TextInput label="Region" value={editForm.region ?? ''} onChange={(event) => setEditForm((current) => current ? { ...current, region: event.target.value } : current)} error={editErrors.region} />
              <TextInput label="Address 1" value={editForm.address1 ?? ''} onChange={(event) => setEditForm((current) => current ? { ...current, address1: event.target.value } : current)} error={editErrors.address1} />
              <TextInput label="Address 2" value={editForm.address2 ?? ''} onChange={(event) => setEditForm((current) => current ? { ...current, address2: event.target.value } : current)} error={editErrors.address2} />
              <TextInput label="City" value={editForm.city ?? ''} onChange={(event) => setEditForm((current) => current ? { ...current, city: event.target.value } : current)} error={editErrors.city} />
              <Select label="State" value={editForm.state ?? ''} onChange={(event) => setEditForm((current) => current ? { ...current, state: event.target.value } : current)} options={US_STATES} placeholder="Select state" error={editErrors.state} />
              <TextInput label="Postal Code" value={editForm.postalCode ?? ''} onChange={(event) => setEditForm((current) => current ? { ...current, postalCode: event.target.value } : current)} error={editErrors.postalCode} />
              <TextInput label="Country" value={editForm.country ?? ''} onChange={(event) => setEditForm((current) => current ? { ...current, country: event.target.value } : current)} error={editErrors.country} />
            </div>

            {editServerError && <p className="text-sm text-status-error">{editServerError}</p>}

            <div className="flex justify-end gap-2">
              <button type="button" onClick={() => setEditOpen(false)} className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary">
                Cancel
              </button>
              <button type="button" onClick={() => void saveEdit()} disabled={updateAccount.isPending} className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-60">
                {updateAccount.isPending ? 'Saving…' : 'Save Changes'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      <Modal
        open={!!relationshipType}
        onClose={() => setRelationshipType(null)}
        title={relationshipType === 'BrokerOfRecord' ? 'Change Broker of Record' : relationshipType === 'PrimaryProducer' ? 'Change Primary Producer' : 'Change Territory'}
      >
        <div className="space-y-4">
          {relationshipType === 'BrokerOfRecord' && (
            <Select
              label="Broker of Record"
              value={relationshipBrokerId}
              onChange={(event) => setRelationshipBrokerId(event.target.value)}
              options={brokers.map((broker) => ({ value: broker.id, label: broker.legalName }))}
              placeholder="Select broker"
            />
          )}

          {relationshipType === 'PrimaryProducer' && (
            <AssigneePicker label="Primary Producer" selectedUser={relationshipProducer} onSelect={setRelationshipProducer} />
          )}

          {relationshipType === 'Territory' && (
            <TextInput label="Territory Code" value={relationshipTerritory} onChange={(event) => setRelationshipTerritory(event.target.value)} />
          )}

          <TextInput label="Notes" value={relationshipNotes} onChange={(event) => setRelationshipNotes(event.target.value)} />

          {relationshipError && <p className="text-sm text-status-error">{relationshipError}</p>}

          <div className="flex justify-end gap-2">
            <button type="button" onClick={() => setRelationshipType(null)} className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary">
              Cancel
            </button>
            <button type="button" onClick={() => void saveRelationship()} disabled={changeRelationship.isPending} className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-60">
              {changeRelationship.isPending ? 'Saving…' : 'Save'}
            </button>
          </div>
        </div>
      </Modal>

      <Modal
        open={!!lifecycleTarget}
        onClose={() => setLifecycleTarget(null)}
        title={lifecycleTarget === 'Deleted' ? 'Delete Account' : lifecycleTarget === 'Inactive' ? 'Deactivate Account' : 'Reactivate Account'}
      >
        <div className="space-y-4">
          {lifecycleTarget === 'Deleted' && (
            <>
              <Select
                label="Reason Code"
                value={lifecycleReasonCode}
                onChange={(event) => setLifecycleReasonCode(event.target.value)}
                options={DELETE_REASON_OPTIONS}
                placeholder="Select reason"
              />
              <TextInput
                label="Reason Detail"
                value={lifecycleReasonDetail}
                onChange={(event) => setLifecycleReasonDetail(event.target.value)}
              />
            </>
          )}

          {lifecycleError && <p className="text-sm text-status-error">{lifecycleError}</p>}

          <div className="flex justify-end gap-2">
            <button type="button" onClick={() => setLifecycleTarget(null)} className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary">
              Cancel
            </button>
            <button type="button" onClick={() => void saveLifecycle()} disabled={transitionAccount.isPending} className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-60">
              {transitionAccount.isPending ? 'Saving…' : 'Confirm'}
            </button>
          </div>
        </div>
      </Modal>

      <Modal open={mergeOpen} onClose={() => setMergeOpen(false)} title="Merge Account">
        <div className="space-y-4">
          <Select
            label="Survivor Account"
            value={mergeSurvivorId}
            onChange={(event) => setMergeSurvivorId(event.target.value)}
            options={survivorOptions.map((candidate) => ({ value: candidate.id, label: candidate.displayName }))}
            placeholder="Select survivor account"
          />
          <TextInput
            label="Notes"
            value={mergeNotes}
            onChange={(event) => setMergeNotes(event.target.value)}
          />

          {mergeError && <p className="text-sm text-status-error">{mergeError}</p>}

          <div className="flex justify-end gap-2">
            <button type="button" onClick={() => setMergeOpen(false)} className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary">
              Cancel
            </button>
            <button type="button" onClick={() => void saveMerge()} disabled={mergeAccount.isPending} className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-60">
              {mergeAccount.isPending ? 'Merging…' : 'Merge Account'}
            </button>
          </div>
        </div>
      </Modal>

      <Modal open={contactOpen} onClose={() => setContactOpen(false)} title={editingContact ? 'Edit Contact' : 'Add Contact'}>
        <div className="space-y-4">
          <TextInput label="Full Name" value={contactForm.fullName} onChange={(event) => setContactForm((current) => ({ ...current, fullName: event.target.value }))} error={contactErrors.fullName} />
          <TextInput label="Role" value={contactForm.role ?? ''} onChange={(event) => setContactForm((current) => ({ ...current, role: event.target.value }))} error={contactErrors.role} />
          <TextInput label="Email" value={contactForm.email ?? ''} onChange={(event) => setContactForm((current) => ({ ...current, email: event.target.value }))} error={contactErrors.email} />
          <TextInput label="Phone" value={contactForm.phone ?? ''} onChange={(event) => setContactForm((current) => ({ ...current, phone: event.target.value }))} error={contactErrors.phone} />
          <label className="inline-flex items-center gap-2 text-sm text-text-secondary">
            <input
              type="checkbox"
              checked={contactForm.isPrimary}
              onChange={(event) => setContactForm((current) => ({ ...current, isPrimary: event.target.checked }))}
            />
            Mark as primary contact
          </label>

          {contactServerError && <p className="text-sm text-status-error">{contactServerError}</p>}

          <div className="flex justify-end gap-2">
            <button type="button" onClick={() => setContactOpen(false)} className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary">
              Cancel
            </button>
            <button type="button" onClick={() => void saveContact()} disabled={createContact.isPending || updateContact.isPending} className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-60">
              {createContact.isPending || updateContact.isPending ? 'Saving…' : editingContact ? 'Save Contact' : 'Create Contact'}
            </button>
          </div>
        </div>
      </Modal>

      <Modal open={!!deletingContact} onClose={() => setDeletingContact(null)} title="Remove Contact">
        <div className="space-y-4">
          <p className="text-sm text-text-secondary">
            Remove {deletingContact?.fullName} from this account?
          </p>
          <div className="flex justify-end gap-2">
            <button type="button" onClick={() => setDeletingContact(null)} className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary">
              Cancel
            </button>
            <button type="button" onClick={() => void confirmDeleteContact()} disabled={deleteContact.isPending} className="rounded-lg bg-status-error/80 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-status-error disabled:opacity-60">
              {deleteContact.isPending ? 'Removing…' : 'Remove Contact'}
            </button>
          </div>
        </div>
      </Modal>
    </DashboardLayout>
  );
}

function SummaryMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-surface-border bg-surface-card/40 p-4">
      <p className="text-[11px] font-medium uppercase tracking-[0.16em] text-text-muted">{label}</p>
      <p className="mt-2 text-xl font-semibold text-text-primary">{value}</p>
    </div>
  );
}

function DetailField({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[11px] font-medium uppercase tracking-[0.16em] text-text-muted">{label}</p>
      <p className="mt-1 text-sm text-text-primary">{value}</p>
    </div>
  );
}

function RailCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <Card className="border border-surface-border bg-surface-card/35">
      <CardHeader>
        <CardTitle>{title}</CardTitle>
      </CardHeader>
      <div className="space-y-2">{children}</div>
    </Card>
  );
}

function EmptyRail({ message }: { message: string }) {
  return (
    <div className="rounded-lg border border-dashed border-surface-border bg-surface-card/40 px-3 py-4 text-sm text-text-muted">
      {message}
    </div>
  );
}

function AccountSectionSkeleton() {
  return (
    <div className="space-y-3">
      {Array.from({ length: 3 }).map((_, index) => (
        <Skeleton key={index} className="h-16 w-full" />
      ))}
    </div>
  );
}

function normalizeOptional(value: string | null | undefined) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

function normalizeAccountUpdate(dto: AccountUpdateRequestDto): AccountUpdateRequestDto {
  return {
    displayName: normalizeOptional(dto.displayName),
    legalName: normalizeOptional(dto.legalName),
    taxId: normalizeOptional(dto.taxId),
    industry: normalizeOptional(dto.industry),
    primaryLineOfBusiness: normalizeOptional(dto.primaryLineOfBusiness),
    territoryCode: normalizeOptional(dto.territoryCode),
    region: normalizeOptional(dto.region),
    address1: normalizeOptional(dto.address1),
    address2: normalizeOptional(dto.address2),
    city: normalizeOptional(dto.city),
    state: normalizeOptional(dto.state),
    postalCode: normalizeOptional(dto.postalCode),
    country: normalizeOptional(dto.country),
  };
}

function normalizeContact(dto: AccountContactRequestDto): AccountContactRequestDto {
  return {
    fullName: dto.fullName.trim(),
    role: normalizeOptional(dto.role),
    email: normalizeOptional(dto.email),
    phone: normalizeOptional(dto.phone),
    isPrimary: dto.isPrimary,
  };
}

function formatAddress(account: AccountDto) {
  const parts = [account.address1, account.address2, account.city, account.state, account.postalCode, account.country]
    .filter(Boolean);
  return parts.length ? parts.join(', ') : '—';
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}
