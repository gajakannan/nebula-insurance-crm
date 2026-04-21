import type {
  BrokerCreateDto,
  BrokerUpdateDto,
  ContactCreateDto,
  ContactUpdateDto,
} from '../types';

type FieldErrors = Record<string, string>;

const EMAIL_REGEX = /.+@.+\..+/;
const PHONE_REGEX = /^\+?[1-9]\d{7,14}$/;
const STATE_REGEX = /^[A-Z]{2}$/;

export function validateBrokerCreate(dto: Partial<BrokerCreateDto>): FieldErrors {
  const errors: FieldErrors = {};

  if (!dto.legalName?.trim()) {
    errors.legalName = 'Legal name is required.';
  } else if (dto.legalName.length > 255) {
    errors.legalName = 'Legal name must be 255 characters or less.';
  }

  if (!dto.licenseNumber?.trim()) {
    errors.licenseNumber = 'License number is required.';
  } else if (dto.licenseNumber.length > 50) {
    errors.licenseNumber = 'License number must be 50 characters or less.';
  }

  if (!dto.state?.trim()) {
    errors.state = 'State is required.';
  } else if (!STATE_REGEX.test(dto.state)) {
    errors.state = 'State must be a 2-letter code (e.g., CA).';
  }

  if (dto.email && !EMAIL_REGEX.test(dto.email)) {
    errors.email = 'Enter a valid email address.';
  }

  if (dto.phone && !PHONE_REGEX.test(dto.phone)) {
    errors.phone = 'Enter a valid phone number (e.g., +12025551234).';
  }

  return errors;
}

export function validateBrokerUpdate(dto: Partial<BrokerUpdateDto>): FieldErrors {
  const errors: FieldErrors = {};

  if (!dto.legalName?.trim()) {
    errors.legalName = 'Legal name is required.';
  } else if (dto.legalName.length > 255) {
    errors.legalName = 'Legal name must be 255 characters or less.';
  }

  if (!dto.state?.trim()) {
    errors.state = 'State is required.';
  } else if (!STATE_REGEX.test(dto.state)) {
    errors.state = 'State must be a 2-letter code (e.g., CA).';
  }

  if (!dto.status) {
    errors.status = 'Status is required.';
  }

  if (dto.email && !EMAIL_REGEX.test(dto.email)) {
    errors.email = 'Enter a valid email address.';
  }

  if (dto.phone && !PHONE_REGEX.test(dto.phone)) {
    errors.phone = 'Enter a valid phone number (e.g., +12025551234).';
  }

  return errors;
}

export function validateContact(dto: Partial<ContactCreateDto | ContactUpdateDto>): FieldErrors {
  const errors: FieldErrors = {};

  if (!dto.fullName?.trim()) {
    errors.fullName = 'Full name is required.';
  } else if (dto.fullName.length > 200) {
    errors.fullName = 'Full name must be 200 characters or less.';
  }

  if (!dto.email?.trim()) {
    errors.email = 'Email is required.';
  } else if (!EMAIL_REGEX.test(dto.email)) {
    errors.email = 'Enter a valid email address.';
  }

  if (!dto.phone?.trim()) {
    errors.phone = 'Phone is required.';
  } else if (!PHONE_REGEX.test(dto.phone)) {
    errors.phone = 'Enter a valid phone number (e.g., +12025551234).';
  }

  return errors;
}
