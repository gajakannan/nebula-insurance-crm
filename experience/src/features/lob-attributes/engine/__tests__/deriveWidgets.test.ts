import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'
import { deriveSections, UnmappableFieldError } from '../deriveWidgets'

function bundle(file: string) {
  return JSON.parse(
    readFileSync(resolve(process.cwd(), `../planning-mds/lob-schemas/cyber/1.0.0/${file}`), 'utf-8'),
  )
}

const dataSchema = bundle('data-schema.json')
const uiSchema = bundle('ui-schema.json')

describe('deriveSections (F0036-S0003)', () => {
  const sections = deriveSections(dataSchema, uiSchema)
  const byPath = new Map(sections.flatMap((s) => s.fields).map((f) => [f.fieldPath, f]))

  it('derives sections and order from ui-schema (no hardcoded Cyber list)', () => {
    expect(sections.map((s) => s.id)).toEqual(['exposure', 'controls', 'terms'])
    expect(sections.map((s) => s.title)).toEqual(['Exposure', 'Controls', 'Requested Terms'])
  })

  it('derives each widget from the data-schema type/enum', () => {
    expect(byPath.get('revenueBand')?.widget).toBe('select')
    expect(byPath.get('recordsHeld')?.widget).toBe('number')
    expect(byPath.get('controls.mfaEnabled')?.widget).toBe('checkbox')
    expect(byPath.get('controls.edrEnabled')?.widget).toBe('checkbox')
    expect(byPath.get('controls.backupEnabled')?.widget).toBe('checkbox')
    expect(byPath.get('controls.mfaMaturity')?.widget).toBe('select')
    expect(byPath.get('controls.trainingFrequency')?.widget).toBe('select')
    expect(byPath.get('requestedLimit')?.widget).toBe('money-minor')
    expect(byPath.get('requestedRetention')?.widget).toBe('money-minor')
  })

  it('binds money-minor widgets to the .amountMinor value path', () => {
    expect(byPath.get('requestedLimit')?.valuePath).toBe('requestedLimit.amountMinor')
    expect(byPath.get('requestedRetention')?.valuePath).toBe('requestedRetention.amountMinor')
  })

  it('derives required from the parent schema required[] and labels from ui-schema', () => {
    expect(byPath.get('revenueBand')?.required).toBe(true)
    expect(byPath.get('controls.mfaEnabled')?.required).toBe(true)
    // mfaMaturity is nullable / not in controls.required.
    expect(byPath.get('controls.mfaMaturity')?.required).toBe(false)
    expect(byPath.get('controls.mfaEnabled')?.label).toBe('MFA enabled')
  })

  it('derives select options from the data-schema enum', () => {
    expect(byPath.get('revenueBand')?.options?.map((o) => o.value)).toEqual(['0-10M', '10-50M', '50-250M', '250M+'])
  })

  it('fails closed on a field with no data-schema property', () => {
    const badUi = { sections: [{ id: 's', title: 'S', fields: ['doesNotExist'] }], fieldLabels: {} }
    expect(() => deriveSections(dataSchema, badUi)).toThrow(UnmappableFieldError)
  })
})
