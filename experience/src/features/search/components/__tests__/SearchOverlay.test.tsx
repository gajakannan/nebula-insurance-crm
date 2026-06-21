import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { SearchOverlay } from '../SearchOverlay';

function renderOverlay() {
  return render(
    <MemoryRouter initialEntries={['/']}>
      <SearchOverlay />
      <Routes>
        <Route path="/" element={<div>home</div>} />
        <Route path="/search" element={<div>search workspace</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('SearchOverlay', () => {
  it('navigates to the search workspace on submit', async () => {
    renderOverlay();
    const input = screen.getByLabelText(/search crm records/i);
    await userEvent.type(input, 'acme{Enter}');
    expect(await screen.findByText('search workspace')).toBeInTheDocument();
  });

  it('does not navigate for queries under 2 characters', async () => {
    renderOverlay();
    const input = screen.getByLabelText(/search crm records/i);
    await userEvent.type(input, 'a{Enter}');
    expect(screen.getByText('home')).toBeInTheDocument();
  });
});
