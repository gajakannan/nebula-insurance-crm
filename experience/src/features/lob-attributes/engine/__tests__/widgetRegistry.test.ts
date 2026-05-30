import { describe, expect, it } from 'vitest'
import {
  assertOptionAllowed,
  createWidgetRegistry,
  UnknownWidgetError,
  UnknownWidgetOptionError,
} from '../widgetRegistry'
// A trivial stub widget — S0001 only exercises the registry contract, not rendering.
function StubWidget() {
  return null
}

describe('widgetRegistry (F0036-S0001)', () => {
  it('resolves a registered widget to its entry', () => {
    const registry = createWidgetRegistry()
    registry.register('text', { component: StubWidget })

    expect(registry.has('text')).toBe(true)
    expect(registry.resolve('text').component).toBe(StubWidget)
    expect(registry.registeredNames()).toEqual(['text'])
  })

  it('fails closed: resolving an unregistered widget throws (does not render a fallback)', () => {
    const registry = createWidgetRegistry()

    expect(registry.has('does-not-exist')).toBe(false)
    expect(() => registry.resolve('does-not-exist')).toThrow(UnknownWidgetError)
    // The error names the offending widget so the failure is developer-visible.
    expect(() => registry.resolve('does-not-exist')).toThrow(/does-not-exist/)
  })

  it('fails closed on an unknown option for a widget with an option schema', () => {
    const registry = createWidgetRegistry()
    registry.register('select', {
      component: StubWidget,
      isAllowedOption: (option) => option === 'allowed',
    })

    expect(() => assertOptionAllowed(registry, 'select', 'allowed')).not.toThrow()
    expect(() => assertOptionAllowed(registry, 'select', 'rogue')).toThrow(UnknownWidgetOptionError)
  })

  it('allows all options when a widget declares no option schema', () => {
    const registry = createWidgetRegistry()
    registry.register('text', { component: StubWidget })

    expect(() => assertOptionAllowed(registry, 'text', 'anything')).not.toThrow()
  })
})
