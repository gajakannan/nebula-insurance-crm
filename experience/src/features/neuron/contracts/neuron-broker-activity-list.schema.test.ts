import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

describe('Broker activity contract drift', () => {
  it('matches the authoritative planning schema', () => {
    const local = JSON.parse(
      readFileSync(
        resolve(process.cwd(), 'src/features/neuron/contracts/neuron-broker-activity-list.schema.json'),
        'utf8',
      ),
    );
    const planning = JSON.parse(
      readFileSync(
        resolve(process.cwd(), '../planning-mds/schemas/neuron-broker-activity-list.schema.json'),
        'utf8',
      ),
    );
    expect(local).toEqual(planning);
  });
});
