import type React from 'react'
import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { SidebarContext, useSidebar, useSidebarProvider } from './useSidebar'

function setViewportWidth(width: number) {
  Object.defineProperty(window, 'innerWidth', {
    configurable: true,
    writable: true,
    value: width,
  })
  window.dispatchEvent(new Event('resize'))
}

describe('useSidebarProvider', () => {
  function Wrapper({ children }: { children: React.ReactNode }) {
    const value = useSidebarProvider()
    return <SidebarContext.Provider value={value}>{children}</SidebarContext.Provider>
  }

  beforeEach(() => {
    localStorage.clear()
    setViewportWidth(768)
  })

  it('reads collapsed state from local storage and persists toggles', () => {
    localStorage.setItem('nebula-sidebar-collapsed', 'true')

    const { result } = renderHook(() => useSidebarProvider())

    expect(result.current.collapsed).toBe(true)

    act(() => {
      result.current.toggleCollapsed()
    })

    expect(result.current.collapsed).toBe(false)
    expect(localStorage.getItem('nebula-sidebar-collapsed')).toBe('false')
  })

  it('opens and closes mobile navigation and resets it on desktop resize', () => {
    const { result } = renderHook(() => useSidebarProvider())

    act(() => {
      result.current.openMobile()
    })
    expect(result.current.mobileOpen).toBe(true)

    act(() => {
      result.current.closeMobile()
    })
    expect(result.current.mobileOpen).toBe(false)

    act(() => {
      result.current.openMobile()
    })
    expect(result.current.mobileOpen).toBe(true)

    act(() => {
      setViewportWidth(1280)
    })
    expect(result.current.mobileOpen).toBe(false)
  })

  it('exposes sidebar state through the context hook', () => {
    const { result } = renderHook(() => useSidebar(), { wrapper: Wrapper })

    expect(result.current.collapsed).toBe(false)
    expect(result.current.mobileOpen).toBe(false)
  })
})
