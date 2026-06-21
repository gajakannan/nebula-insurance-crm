import { TextInput } from '@/components/ui/TextInput';
import type { OperationalReportParams } from '../types';

interface ReportControlsProps {
  params: OperationalReportParams;
  onChange: (key: keyof OperationalReportParams, value: string) => void;
}

export function ReportControls({ params, onChange }: ReportControlsProps) {
  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
      <TextInput
        label="Region"
        value={params.region ?? ''}
        placeholder="Any region"
        onChange={(e) => onChange('region', e.target.value)}
      />
      <TextInput
        label="Line of business"
        value={params.lineOfBusiness ?? ''}
        placeholder="Any LOB"
        onChange={(e) => onChange('lineOfBusiness', e.target.value)}
      />
      <TextInput
        label="Workflow type"
        value={params.workflowType ?? ''}
        placeholder="Submission / Renewal / Task"
        onChange={(e) => onChange('workflowType', e.target.value)}
      />
      <TextInput
        label="As of"
        type="date"
        value={params.asOf ?? ''}
        onChange={(e) => onChange('asOf', e.target.value)}
      />
    </div>
  );
}
