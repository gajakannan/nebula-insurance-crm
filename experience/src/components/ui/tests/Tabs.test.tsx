import { useState } from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { Tabs } from '../Tabs'

function TabsHarness() {
  const [activeTab, setActiveTab] = useState('Profile')

  return (
    <Tabs tabs={['Profile', 'Contacts', 'Timeline']} activeTab={activeTab} onTabChange={setActiveTab}>
      <div>{activeTab} content</div>
    </Tabs>
  )
}

describe('Tabs', () => {
  it('changes tabs on click and keyboard navigation', async () => {
    const user = userEvent.setup()

    render(<TabsHarness />)

    const profileTab = screen.getByRole('tab', { name: 'Profile' })
    const contactsTab = screen.getByRole('tab', { name: 'Contacts' })
    const timelineTab = screen.getByRole('tab', { name: 'Timeline' })

    expect(profileTab).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tabpanel')).toHaveTextContent('Profile content')

    await user.click(contactsTab)
    expect(contactsTab).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tabpanel')).toHaveTextContent('Contacts content')

    contactsTab.focus()
    await user.keyboard('{ArrowRight}')
    expect(timelineTab).toHaveFocus()
    expect(timelineTab).toHaveAttribute('aria-selected', 'true')

    await user.keyboard('{Home}')
    expect(profileTab).toHaveFocus()
    expect(profileTab).toHaveAttribute('aria-selected', 'true')

    await user.keyboard('{End}')
    expect(timelineTab).toHaveFocus()
    expect(screen.getByRole('tabpanel')).toHaveTextContent('Timeline content')
  })
})
