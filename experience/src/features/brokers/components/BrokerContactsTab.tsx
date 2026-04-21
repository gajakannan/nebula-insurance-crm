import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { useBrokerContacts } from '../hooks/useBrokerContacts';
import type { BrokerStatus, ContactDto } from '../types';

interface BrokerContactsTabProps {
  brokerId: string;
  brokerStatus: BrokerStatus;
  onAddContact: () => void;
  onEditContact: (contact: ContactDto) => void;
  onDeleteContact: (contact: ContactDto) => void;
}

export function BrokerContactsTab({
  brokerId,
  brokerStatus,
  onAddContact,
  onEditContact,
  onDeleteContact,
}: BrokerContactsTabProps) {
  const { data: page, isLoading, isError, refetch } = useBrokerContacts(brokerId);
  const contacts = page?.data ?? [];

  if (isLoading) {
    return (
      <div className="space-y-3">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-16 w-full" />
        ))}
      </div>
    );
  }

  if (isError) {
    return <ErrorFallback message="Unable to load contacts." onRetry={() => refetch()} />;
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h3 className="text-sm font-medium text-text-secondary">
          {contacts.length > 0 ? `${contacts.length} contact${contacts.length > 1 ? 's' : ''}` : ''}
        </h3>
        <button
          onClick={onAddContact}
          className="rounded-lg bg-nebula-violet px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-nebula-violet/90"
        >
          Add Contact
        </button>
      </div>

      {contacts.length === 0 && (
        <p className="py-8 text-center text-sm text-text-muted">No contacts yet.</p>
      )}

      {contacts.length > 0 && (
        <div className="space-y-2">
          {contacts.map((contact) => (
            <ContactRow
              key={contact.id}
              contact={contact}
              brokerStatus={brokerStatus}
              onEdit={() => onEditContact(contact)}
              onDelete={() => onDeleteContact(contact)}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function ContactRow({
  contact,
  brokerStatus,
  onEdit,
  onDelete,
}: {
  contact: ContactDto;
  brokerStatus: BrokerStatus;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const isInactive = brokerStatus === 'Inactive';

  return (
    <div className="flex items-center justify-between rounded-lg border border-surface-border p-3">
      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium text-text-primary">{contact.fullName}</p>
        <div className="mt-0.5 flex flex-wrap gap-3 text-xs text-text-secondary">
          {contact.role && <span>{contact.role}</span>}
          <span>
            {contact.email ? (
              contact.email
            ) : isInactive ? (
              <span className="text-text-muted italic">Masked</span>
            ) : (
              '—'
            )}
          </span>
          <span>
            {contact.phone ? (
              contact.phone
            ) : isInactive ? (
              <span className="text-text-muted italic">Masked</span>
            ) : (
              '—'
            )}
          </span>
        </div>
      </div>
      <div className="ml-2 flex gap-1">
        <button
          onClick={onEdit}
          className="rounded-md p-1.5 text-text-muted transition-colors hover:bg-surface-card-hover hover:text-text-secondary"
          title="Edit contact"
        >
          <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
          </svg>
        </button>
        <button
          onClick={onDelete}
          className="rounded-md p-1.5 text-text-muted transition-colors hover:bg-status-error/15 hover:text-status-error"
          title="Delete contact"
        >
          <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
          </svg>
        </button>
      </div>
    </div>
  );
}
