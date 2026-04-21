import { describe, expect, it } from 'vitest'
import {
  validateBrokerCreate,
  validateBrokerUpdate,
  validateContact,
} from '../lib/validation'

describe('broker validation helpers', () => {
  it('validates broker creation payloads', () => {
    expect(validateBrokerCreate({})).toEqual({
      legalName: 'Legal name is required.',
      licenseNumber: 'License number is required.',
      state: 'State is required.',
    })

    expect(
      validateBrokerCreate({
        legalName: '  Harbor Agency  ',
        licenseNumber: 'LIC-1',
        state: 'CA',
        email: 'invalid',
        phone: '12345',
      }),
    ).toEqual({
      email: 'Enter a valid email address.',
      phone: 'Enter a valid phone number (e.g., +12025551234).',
    })
  })

  it('validates broker update payloads', () => {
    expect(
      validateBrokerUpdate({
        legalName: '',
        state: 'California',
        status: undefined as never,
        email: 'bad-email',
        phone: 'bad-phone',
      }),
    ).toEqual({
      legalName: 'Legal name is required.',
      state: 'State must be a 2-letter code (e.g., CA).',
      status: 'Status is required.',
      email: 'Enter a valid email address.',
      phone: 'Enter a valid phone number (e.g., +12025551234).',
    })
  })

  it('validates contact payloads', () => {
    expect(validateContact({})).toEqual({
      fullName: 'Full name is required.',
      email: 'Email is required.',
      phone: 'Phone is required.',
    })

    expect(
      validateContact({
        fullName: 'Nadia Brooks',
        email: 'bad-email',
        phone: 'bad-phone',
        role: 'Underwriter',
      }),
    ).toEqual({
      email: 'Enter a valid email address.',
      phone: 'Enter a valid phone number (e.g., +12025551234).',
    })
  })
})
