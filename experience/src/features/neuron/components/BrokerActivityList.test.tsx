import { screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { renderWithProviders } from '@/test-utils/render-app';
import { BrokerActivityList } from './BrokerActivityList';

const item = {
  id: '11111111-1111-1111-1111-111111111111',
  entityType: 'Broker' as const,
  entityId: '22222222-2222-2222-2222-222222222222',
  eventType: 'BrokerUpdated',
  eventDescription: 'Stored broker description',
  entityName: 'Atlas Brokerage',
  actorDisplayName: 'Unknown User',
  occurredAt: '2026-09-01T12:00:00Z',
};

describe('BrokerActivityList (F0040 registered component)', () => {
  it('renders the stored event details and Broker 360 link', () => {
    renderWithProviders(<BrokerActivityList props={{ items: [item] }} />);

    expect(screen.getByTestId('broker-activity-list')).toBeInTheDocument();
    expect(screen.getByText('Stored broker description')).toBeInTheDocument();
    expect(screen.getByText('Atlas Brokerage')).toHaveAttribute(
      'href',
      '/brokers/22222222-2222-2222-2222-222222222222',
    );
    expect(screen.getByText('Unknown User')).toBeInTheDocument();
    expect(screen.getByRole('list', { name: 'Recent Broker activity' })).toBeInTheDocument();
  });
});
