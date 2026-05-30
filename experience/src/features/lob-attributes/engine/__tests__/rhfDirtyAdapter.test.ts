import { describe, expect, it, vi } from 'vitest'
import type { UseFormReturn } from 'react-hook-form'
import { flattenDirtyFields, rhfDirtyAdapter } from '../rhfDirtyAdapter'

describe('flattenDirtyFields (F0036-S0006)', () => {
  it('flattens nested RHF dirtyFields into dotted string paths', () => {
    const dirty = {
      revenueBand: true,
      controls: { mfaEnabled: true, edrEnabled: false },
      requestedLimit: { amountMinor: true, currency: false },
    }
    expect(flattenDirtyFields(dirty).sort()).toEqual(
      ['controls.mfaEnabled', 'requestedLimit.amountMinor', 'revenueBand'].sort(),
    )
  })

  it('handles array dirty markers', () => {
    expect(flattenDirtyFields({ tags: [true, false, true] }).sort()).toEqual(['tags.0', 'tags.2'])
  })

  it('returns no paths when nothing is dirty', () => {
    expect(flattenDirtyFields({ a: false, b: { c: false } })).toEqual([])
  })
})

describe('rhfDirtyAdapter (F0036-S0006)', () => {
  it('builds a DirtyFormRegistration source from an RHF form', () => {
    const values = { revenueBand: '10-50M', controls: { mfaEnabled: true } }
    const form = {
      getValues: vi.fn(() => values),
      formState: { isDirty: true, dirtyFields: { revenueBand: true, controls: { mfaEnabled: true } } },
    } as unknown as UseFormReturn<typeof values>

    const reg = rhfDirtyAdapter(form, { formKey: 'cyber-attributes:/x', route: '/x' })
    expect(reg.formKey).toBe('cyber-attributes:/x')
    expect(reg.route).toBe('/x')
    expect(reg.isDirty()).toBe(true)
    expect(reg.getValues()).toBe(values)
    expect(reg.getDirtyFieldPaths().sort()).toEqual(['controls.mfaEnabled', 'revenueBand'])
  })
})
