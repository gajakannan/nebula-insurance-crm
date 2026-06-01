import { describe, expect, it } from 'vitest'
import { renderHook } from '@testing-library/react'
import { deepEqual, diffPaths, useControlledDirtyTracker } from '../useControlledDirtyTracker'

describe('deepEqual equality matrix (F0036-S0007)', () => {
  it('scalar typed-and-cleared back to initial is equal', () => {
    expect(deepEqual('', '')).toBe(true)
    expect(deepEqual('x', '')).toBe(false)
  })

  it('treats undefined / null / absent key as equal', () => {
    expect(deepEqual(undefined, null)).toBe(true)
    expect(deepEqual({ a: 1 }, { a: 1, b: undefined })).toBe(true)
  })

  it('compares numbers and Dates by value', () => {
    expect(deepEqual(5, 5)).toBe(true)
    expect(deepEqual(Number.NaN, Number.NaN)).toBe(true)
    expect(deepEqual(new Date('2026-05-29'), new Date('2026-05-29'))).toBe(true)
    expect(deepEqual(new Date('2026-05-29'), new Date('2026-05-30'))).toBe(false)
  })

  it('nested-object replacement with structurally-equal contents is equal (not reference)', () => {
    expect(deepEqual({ address: { line1: 'A' } }, { address: { line1: 'A' } })).toBe(true)
  })

  it('array reorder of identical contents is not dirty by default; order-sensitive flags it', () => {
    expect(deepEqual(['a', 'b'], ['b', 'a'])).toBe(true)
    expect(deepEqual(['a', 'b'], ['b', 'a'], true)).toBe(false)
    expect(deepEqual(['a', 'b'], ['a', 'c'])).toBe(false)
  })
})

describe('diffPaths (F0036-S0007)', () => {
  it('reports flattened JSON paths for differing leaves', () => {
    const paths = diffPaths(
      { notes: 'new', address: { line1: 'B', line2: 'x' } },
      { notes: 'old', address: { line1: 'A', line2: 'x' } },
    )
    expect(paths.sort()).toEqual(['address.line1', 'notes'])
  })

  it('reports the array path when array contents differ', () => {
    expect(diffPaths({ phones: ['1'] }, { phones: ['2'] })).toEqual(['phones'])
  })

  it('reports indexed paths when order-sensitive', () => {
    expect(diffPaths({ phones: ['1', '2'] }, { phones: ['1', '3'] }, true)).toEqual(['phones.1'])
  })
})

describe('useControlledDirtyTracker (F0036-S0007)', () => {
  it('returns the F0035 triple with deep-equality dirty semantics', () => {
    const initial = { notes: 'a', tags: ['x'] }
    const { result, rerender } = renderHook(
      ({ values }: { values: typeof initial }) => useControlledDirtyTracker(values, initial),
      { initialProps: { values: { notes: 'a', tags: ['x'] } } },
    )
    // Structurally equal to initial -> not dirty.
    expect(result.current.isDirty()).toBe(false)
    expect(result.current.getDirtyFieldPaths()).toEqual([])

    // Edit -> dirty with the changed path.
    rerender({ values: { notes: 'b', tags: ['x'] } })
    expect(result.current.isDirty()).toBe(true)
    expect(result.current.getDirtyFieldPaths()).toEqual(['notes'])

    // Edited-then-reset back to initial -> not dirty again.
    rerender({ values: { notes: 'a', tags: ['x'] } })
    expect(result.current.isDirty()).toBe(false)
  })

  it('excludes sensitiveFieldPaths from getValues and getDirtyFieldPaths', () => {
    const initial = { ssn: '', notes: '' }
    const { result } = renderHook(() =>
      useControlledDirtyTracker({ ssn: '123', notes: 'hello' }, initial, { sensitiveFieldPaths: ['ssn'] }),
    )
    expect(result.current.isDirty()).toBe(true)
    expect(result.current.getValues()).toEqual({ notes: 'hello' })
    expect(result.current.getDirtyFieldPaths()).toEqual(['notes'])
  })

  it('does not mark a form dirty when only sensitiveFieldPaths changed', () => {
    const initial = { ssn: '', notes: '' }
    const { result } = renderHook(() =>
      useControlledDirtyTracker({ ssn: '123', notes: '' }, initial, { sensitiveFieldPaths: ['ssn'] }),
    )
    expect(result.current.isDirty()).toBe(false)
    expect(result.current.getValues()).toEqual({ notes: '' })
    expect(result.current.getDirtyFieldPaths()).toEqual([])
  })
})
