import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'
import { createDataSchemaValidator, parityKeySet } from '../ajvValidator'
import {
  CROSS_FIELD_CODES,
  CYBER_PARITY_EXAMPLES,
  DATA_SCHEMA_CODES,
} from '../parity/cyber-examples.fixture'

// The bundle lives in the product planning tree, outside experience/. The
// engine consumes it from the F0034 API at runtime; the parity harness reads
// the published bundle directly.
const dataSchema = JSON.parse(
  readFileSync(
    resolve(process.cwd(), '../planning-mds/lob-schemas/cyber/1.0.0/data-schema.json'),
    'utf-8',
  ),
)

const dataSchemaCodes = new Set<string>(DATA_SCHEMA_CODES)
const crossFieldCodes = new Set<string>(CROSS_FIELD_CODES)

function backendDataSchemaKeys(example: (typeof CYBER_PARITY_EXAMPLES)[number]): string[] {
  return example.backendErrors
    .filter((e) => dataSchemaCodes.has(e.code))
    .map((e) => `${e.code}@${e.pointer}`)
    .sort()
}

describe('Cyber client/backend parity matrix (F0036-S0003, ADR-022)', () => {
  const validator = createDataSchemaValidator(dataSchema)

  it.each(CYBER_PARITY_EXAMPLES)(
    'data-schema parity: 0 disagreements for "$id"',
    (example) => {
      const clientKeys = parityKeySet(validator.validate(example.attributes))
      const backendKeys = backendDataSchemaKeys(example)
      // ADR-022 multiset equality on (code, pointer) over the data-schema layer.
      expect(clientKeys).toEqual(backendKeys)
    },
  )

  it.each(CYBER_PARITY_EXAMPLES)(
    'client AJV never duplicates backend-authoritative cross-field rules for "$id"',
    (example) => {
      const clientErrors = validator.validate(example.attributes)
      const clientCodes = clientErrors.map((e) => e.code)
      for (const code of clientCodes) {
        expect(crossFieldCodes.has(code)).toBe(false)
      }
    },
  )

  it('records at least one parity row per example with no aggregate disagreement', () => {
    expect(CYBER_PARITY_EXAMPLES.length).toBeGreaterThanOrEqual(1)
    const disagreements = CYBER_PARITY_EXAMPLES.filter((example) => {
      const clientKeys = parityKeySet(validator.validate(example.attributes))
      return JSON.stringify(clientKeys) !== JSON.stringify(backendDataSchemaKeys(example))
    })
    expect(disagreements).toEqual([])
  })
})
