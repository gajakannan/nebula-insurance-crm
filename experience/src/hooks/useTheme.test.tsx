import type React from 'react'
import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { ThemeContext, useTheme, useThemeProvider } from './useTheme'

describe('useThemeProvider', () => {
  function Wrapper({ children }: { children: React.ReactNode }) {
    const value = useThemeProvider()
    return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  }

  beforeEach(() => {
    localStorage.clear()
    document.documentElement.removeAttribute('data-theme')
  })

  it('defaults to dark theme and toggles to light theme', () => {
    const { result } = renderHook(() => useThemeProvider())

    expect(result.current.theme).toBe('dark')
    expect(localStorage.getItem('nebula-theme')).toBe('dark')

    act(() => {
      result.current.toggleTheme()
    })

    expect(result.current.theme).toBe('light')
    expect(document.documentElement).toHaveAttribute('data-theme', 'light')
    expect(localStorage.getItem('nebula-theme')).toBe('light')
  })

  it('reads the initial theme from local storage and exposes it through useTheme', () => {
    localStorage.setItem('nebula-theme', 'light')

    const { result } = renderHook(() => useTheme(), { wrapper: Wrapper })

    expect(result.current.theme).toBe('light')
  })
})
