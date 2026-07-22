import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { SidebarContext } from '@/hooks/useSidebar';
import { Sidebar } from './Sidebar';

function renderSidebar(route = '/operational-reports') {
  return render(
    <SidebarContext.Provider
      value={{
        collapsed: false,
        toggleCollapsed: vi.fn(),
        mobileOpen: false,
        openMobile: vi.fn(),
        closeMobile: vi.fn(),
      }}
    >
      <MemoryRouter initialEntries={[route]}>
        <Sidebar />
      </MemoryRouter>
    </SidebarContext.Provider>,
  );
}

describe('Sidebar', () => {
  it('links Operational Reports directly to the F0037 rollups tab', () => {
    renderSidebar();

    const link = screen.getByRole('link', { name: /operational reports/i });

    expect(link).toHaveAttribute('href', '/operational-reports?report=rollups');
    expect(link).toHaveAttribute('aria-current', 'page');
  });

  it('exposes billing and marks nested billing routes active', () => {
    renderSidebar('/billing/reconciliation');

    const link = screen.getByRole('link', { name: /^billing$/i });

    expect(link).toHaveAttribute('href', '/billing');
    expect(link).toHaveAttribute('aria-current', 'page');
  });
});
