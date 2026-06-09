import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { HierarchyPanel } from '../components/HierarchyPanel';
import type { DistributionNodeDto } from '../types';

const mga: DistributionNodeDto = {
  id: 'n1', nodeType: 'MGA', displayName: 'Acme MGA', parentId: null,
  ancestryPath: [], depth: 0, childCount: 1, isActive: true, rowVersion: '3',
};
const broker: DistributionNodeDto = {
  id: 'n2', nodeType: 'Broker', displayName: 'NE Brokers', parentId: 'n1',
  ancestryPath: ['n1'], depth: 1, childCount: 1, isActive: true, rowVersion: '5',
};
const producer: DistributionNodeDto = {
  id: 'n3', nodeType: 'Producer', displayName: 'J. Lee', parentId: 'n2',
  ancestryPath: ['n1', 'n2'], depth: 2, childCount: 0, isActive: true, rowVersion: '7',
};

vi.mock('../hooks/useDistributionHierarchy', () => ({
  useDistributionAncestors: () => ({
    isLoading: false,
    isError: false,
    data: { node: broker, ancestors: [mga] },
    refetch: vi.fn(),
  }),
  useDistributionDescendants: () => ({
    isLoading: false,
    isError: false,
    data: { data: [producer], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 },
    refetch: vi.fn(),
  }),
  useSetDistributionParent: () => ({ mutate: vi.fn(), isPending: false, isError: false }),
}));

describe('HierarchyPanel', () => {
  it('renders the root-to-node breadcrumb and the children list', () => {
    render(<HierarchyPanel nodeId="n2" />);

    expect(screen.getByText('Acme MGA')).toBeTruthy();
    expect(screen.getByText('NE Brokers')).toBeTruthy();
    expect(screen.getByText('J. Lee')).toBeTruthy();
    expect(screen.getByText('Producer')).toBeTruthy();
  });

  it('exposes a set-parent control', () => {
    render(<HierarchyPanel nodeId="n2" />);
    expect(screen.getByRole('button', { name: /set parent/i })).toBeTruthy();
  });
});
