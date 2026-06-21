import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { SearchResultsList } from '../SearchResultsList';
import type { GlobalSearchResponse } from '../../types';

const emptyFacets = { objectTypes: [], owners: [], statuses: [], regions: [], linesOfBusiness: [] };

function response(overrides: Partial<GlobalSearchResponse> = {}): GlobalSearchResponse {
  return {
    data: [
      {
        objectType: 'Account', objectId: 'a1', title: 'Acme Corp', subtitle: 'Acme Holdings',
        status: 'Active', ownerUserId: 'u1', ownerDisplayName: 'Dana', lineOfBusiness: 'Property',
        region: 'West', matchedFields: ['title'], snippet: 'Acme Holdings', targetUrl: '/accounts/a1',
        score: 1, lastUpdatedAt: '2026-06-19T00:00:00Z', indexedAt: '2026-06-19T00:00:00Z',
      },
    ],
    facets: emptyFacets, page: 1, pageSize: 20, totalCount: 1, totalPages: 1,
    queryEcho: 'acme', generatedAt: '2026-06-19T00:00:00Z',
    ...overrides,
  };
}

function renderList(ui: React.ReactNode) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

describe('SearchResultsList', () => {
  it('prompts when the query is too short', () => {
    renderList(<SearchResultsList query="a" isLoading={false} isError={false} onRetry={vi.fn()} onPage={vi.fn()} />);
    expect(screen.getByText(/at least 2 characters/i)).toBeInTheDocument();
  });

  it('renders authorized results with a title and authorized-count copy', () => {
    renderList(<SearchResultsList query="acme" data={response()} isLoading={false} isError={false} onRetry={vi.fn()} onPage={vi.fn()} />);
    expect(screen.getByText('Acme Corp')).toBeInTheDocument();
    expect(screen.getByText(/1 authorized result/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Acme Corp/ })).toHaveAttribute('href', '/accounts/a1');
  });

  it('shows an access-scoped empty state', () => {
    renderList(<SearchResultsList query="zzz" data={response({ data: [], totalCount: 0 })} isLoading={false} isError={false} onRetry={vi.fn()} onPage={vi.fn()} />);
    expect(screen.getByText(/No results match/i)).toBeInTheDocument();
    expect(screen.getByText(/records you can access/i)).toBeInTheDocument();
  });
});
