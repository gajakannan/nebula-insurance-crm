import { describe, expect, it } from 'vitest'
import { renderHook } from '@testing-library/react'
import { usePinnedBundle, type ResolvedBundle } from '../usePinnedBundle'

const V1: ResolvedBundle = { dataSchema: { title: 'V1' }, uiSchema: { sections: [] } }
const V2: ResolvedBundle = { dataSchema: { title: 'V2' }, uiSchema: { sections: [] } }

// Resolver standing in for the F0034 bundle query.
function makeResolver(table: Record<string, ResolvedBundle>) {
  return (productVersionId: string) => table[productVersionId] ?? null
}

describe('usePinnedBundle (F0036-S0004)', () => {
  it('binds the bundle at open from (productVersionId, stage)', () => {
    const resolve = makeResolver({ 'cyber/1.0.0': V1 })
    const { result } = renderHook(() => usePinnedBundle('cyber/1.0.0', 'Quote', resolve))
    expect(result.current.error).toBeNull()
    expect(result.current.pinned).toEqual({ productVersionId: 'cyber/1.0.0', stage: 'Quote' })
    expect(result.current.bundle?.productVersionId).toBe('cyber/1.0.0')
    expect((result.current.bundle?.dataSchema as { title: string }).title).toBe('V1')
  })

  it('does NOT rebind when a newer version activates while the form is open', () => {
    const resolve = makeResolver({ 'cyber/1.0.0': V1, 'cyber/2.0.0': V2 })
    const { result, rerender } = renderHook(
      ({ version }: { version: string }) => usePinnedBundle(version, 'Quote', resolve),
      { initialProps: { version: 'cyber/1.0.0' } },
    )
    expect(result.current.bundle?.productVersionId).toBe('cyber/1.0.0')

    // V2 activates elsewhere -> host re-renders with the new active version.
    rerender({ version: 'cyber/2.0.0' })

    // The open form stays pinned to V1 — no field/validation change under the user.
    expect(result.current.bundle?.productVersionId).toBe('cyber/1.0.0')
    expect((result.current.bundle?.dataSchema as { title: string }).title).toBe('V1')
  })

  it('a new form instance binds the now-active newer version', () => {
    const resolve = makeResolver({ 'cyber/1.0.0': V1, 'cyber/2.0.0': V2 })
    const { result } = renderHook(() => usePinnedBundle('cyber/2.0.0', 'Quote', resolve))
    expect(result.current.bundle?.productVersionId).toBe('cyber/2.0.0')
    expect((result.current.bundle?.dataSchema as { title: string }).title).toBe('V2')
  })

  it('resolves an activation race deterministically to the version captured at open', () => {
    const resolve = makeResolver({ 'cyber/1.0.0': V1, 'cyber/2.0.0': V2 })
    const { result, rerender } = renderHook(
      ({ version }: { version: string }) => usePinnedBundle(version, 'Quote', resolve),
      { initialProps: { version: 'cyber/1.0.0' } },
    )
    rerender({ version: 'cyber/2.0.0' })
    rerender({ version: 'cyber/1.0.0' })
    expect(result.current.bundle?.productVersionId).toBe('cyber/1.0.0')
  })

  it('surfaces a controlled error for an unresolvable pinned version (no silent fallback)', () => {
    const resolve = makeResolver({ 'cyber/1.0.0': V1 })
    const { result } = renderHook(() => usePinnedBundle('cyber/9.9.9', 'Quote', resolve))
    expect(result.current.bundle).toBeNull()
    expect(result.current.error).toMatch(/Product definition unavailable/)
  })
})
