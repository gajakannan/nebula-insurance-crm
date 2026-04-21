import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, type RenderOptions } from '@testing-library/react'
import type { ReactElement, ReactNode } from 'react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { vi } from 'vitest'
import { ThemeContext, type Theme } from '@/hooks/useTheme'

export function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  })
}

interface ProviderOptions extends Omit<RenderOptions, 'wrapper'> {
  route?: string
  theme?: Theme
}

function buildProviders(
  children: ReactNode,
  queryClient: QueryClient,
  route: string,
  theme: Theme,
) {
  return (
    <ThemeContext.Provider value={{ theme, toggleTheme: vi.fn() }}>
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={[route]}>
          {children}
        </MemoryRouter>
      </QueryClientProvider>
    </ThemeContext.Provider>
  )
}

export function renderWithProviders(
  ui: ReactElement,
  { route = '/', theme = 'dark', ...renderOptions }: ProviderOptions = {},
) {
  const queryClient = createTestQueryClient()

  return {
    queryClient,
    ...render(buildProviders(ui, queryClient, route, theme), renderOptions),
  }
}

interface RouteRenderOptions extends ProviderOptions {
  path?: string
  additionalRoutes?: ReactElement[]
}

export function renderRouteWithProviders(
  ui: ReactElement,
  {
    route = '/',
    path = '/',
    theme = 'dark',
    additionalRoutes = [],
    ...renderOptions
  }: RouteRenderOptions = {},
) {
  const queryClient = createTestQueryClient()

  return {
    queryClient,
    ...render(
      buildProviders(
        <Routes>
          <Route path={path} element={ui} />
          {additionalRoutes}
        </Routes>,
        queryClient,
        route,
        theme,
      ),
      renderOptions,
    ),
  }
}
