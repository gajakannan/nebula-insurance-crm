import { Modal } from '@/components/ui/Modal';

interface ConfirmDialogProps {
  open: boolean;
  onClose: () => void;
  title: string;
  message: string;
  confirmLabel?: string;
  onConfirm: () => void;
  isPending?: boolean;
  destructive?: boolean;
  error?: string;
}

export function ConfirmDialog({
  open,
  onClose,
  title,
  message,
  confirmLabel = 'Confirm',
  onConfirm,
  isPending = false,
  destructive = false,
  error,
}: ConfirmDialogProps) {
  return (
    <Modal open={open} onClose={onClose} title={title}>
      <p className="text-sm text-text-secondary">{message}</p>
      {error && <p className="mt-3 text-sm text-status-error">{error}</p>}
      <div className="mt-5 flex justify-end gap-3">
        <button
          onClick={onClose}
          disabled={isPending}
          className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
        >
          Cancel
        </button>
        <button
          onClick={onConfirm}
          disabled={isPending}
          className={`rounded-lg px-4 py-2 text-sm font-medium text-white transition-colors disabled:opacity-50 ${
            destructive
              ? 'bg-status-error hover:bg-status-error/90'
              : 'bg-nebula-violet hover:bg-nebula-violet/90'
          }`}
        >
          {isPending ? 'Processing...' : confirmLabel}
        </button>
      </div>
    </Modal>
  );
}
