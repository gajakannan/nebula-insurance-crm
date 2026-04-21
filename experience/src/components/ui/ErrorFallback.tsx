interface ErrorFallbackProps {
  message: string;
  onRetry?: () => void;
}

export function ErrorFallback({ message, onRetry }: ErrorFallbackProps) {
  return (
    <div className="flex flex-col items-center justify-center py-8 text-center">
      <p className="text-sm text-text-secondary">{message}</p>
      {onRetry && (
        <button
          onClick={onRetry}
          className="mt-3 rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
        >
          Try again
        </button>
      )}
    </div>
  );
}
