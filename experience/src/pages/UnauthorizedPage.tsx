import { useSearchParams } from 'react-router-dom';

const REASON_MESSAGES: Record<string, string> = {
  broker_inactive:
    'Your broker account is currently inactive. Please contact your administrator.',
};

const DEFAULT_MESSAGE = 'You do not have permission to access this page.';

export function UnauthorizedPage() {
  const [searchParams] = useSearchParams();
  const reason = searchParams.get('reason') ?? '';
  const message = REASON_MESSAGES[reason] ?? DEFAULT_MESSAGE;

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-4 p-8 text-center">
      <h1 className="text-2xl font-semibold text-text-primary">Access Denied</h1>
      <p className="max-w-md text-text-secondary">{message}</p>
      <a href="/" className="text-sm text-blue-600 underline hover:text-blue-800">
        Return to home
      </a>
    </main>
  );
}
