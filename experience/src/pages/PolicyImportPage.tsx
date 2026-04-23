import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Upload } from 'lucide-react';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import {
  describePolicyApiError,
  useImportPolicies,
  type PolicyCreateRequestDto,
  type PolicyImportResultDto,
} from '@/features/policies';

const SAMPLE_IMPORT = JSON.stringify({
  policies: [
    {
      accountId: '00000000-0000-0000-0000-000000000001',
      brokerOfRecordId: '00000000-0000-0000-0000-000000000101',
      carrierId: '17000000-0000-0000-0000-000000000001',
      lineOfBusiness: 'GeneralLiability',
      effectiveDate: '2026-05-01',
      expirationDate: '2027-05-01',
      totalPremium: 25000,
      premiumCurrency: 'USD',
      externalPolicyReference: 'IMPORT-001',
      coverages: [
        {
          coverageCode: 'GeneralLiability',
          coverageName: 'General Liability',
          limit: 1000000,
          premium: 25000,
        },
      ],
    },
  ],
}, null, 2);

export default function PolicyImportPage() {
  const importPolicies = useImportPolicies();
  const [payload, setPayload] = useState(SAMPLE_IMPORT);
  const [error, setError] = useState('');
  const [result, setResult] = useState<PolicyImportResultDto | null>(null);

  async function runImport() {
    setError('');
    setResult(null);

    let parsed: { policies?: PolicyCreateRequestDto[] };
    try {
      parsed = JSON.parse(payload) as { policies?: PolicyCreateRequestDto[] };
    } catch {
      setError('Import payload must be valid JSON.');
      return;
    }

    if (!Array.isArray(parsed.policies) || parsed.policies.length === 0) {
      setError('Import payload must include at least one policy row.');
      return;
    }

    try {
      setResult(await importPolicies.mutateAsync({ policies: parsed.policies }));
    } catch (requestError) {
      setError(describePolicyApiError(requestError));
    }
  }

  return (
    <DashboardLayout title="Import Policies">
      <div className="space-y-6">
        <Link to="/policies" className="text-xs text-text-muted hover:text-text-secondary">
          Policies
        </Link>

        <Card>
          <CardHeader>
            <CardTitle>Import payload</CardTitle>
          </CardHeader>
          <textarea
            value={payload}
            onChange={(event) => setPayload(event.target.value)}
            spellCheck={false}
            className="min-h-[360px] w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 font-mono text-xs text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
          />
          {error && <p className="mt-3 text-sm text-status-error">{error}</p>}
          <div className="mt-4 flex justify-end">
            <button
              type="button"
              onClick={runImport}
              disabled={importPolicies.isPending}
              className="inline-flex items-center gap-1.5 rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-60"
            >
              <Upload size={16} />
              Import
            </button>
          </div>
        </Card>

        {result && (
          <Card>
            <CardHeader>
              <CardTitle>Import result</CardTitle>
            </CardHeader>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="rounded-lg border border-status-success/30 bg-status-success/10 p-4">
                <p className="text-sm font-medium text-text-primary">{result.accepted.length} accepted</p>
                <div className="mt-2 space-y-1">
                  {result.accepted.map((policy) => (
                    <Link key={policy.id} to={`/policies/${policy.id}`} className="block text-sm text-nebula-violet hover:underline">
                      {policy.policyNumber}
                    </Link>
                  ))}
                </div>
              </div>
              <div className="rounded-lg border border-status-error/30 bg-status-error/10 p-4">
                <p className="text-sm font-medium text-text-primary">{result.rejected.length} rejected</p>
                <div className="mt-2 space-y-1">
                  {result.rejected.map((row) => (
                    <p key={`${row.index}-${row.code}`} className="text-sm text-text-secondary">
                      Row {row.index}: {row.message}
                    </p>
                  ))}
                </div>
              </div>
            </div>
          </Card>
        )}
      </div>
    </DashboardLayout>
  );
}
