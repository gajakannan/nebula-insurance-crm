export type BrokerStatus = 'Active' | 'Inactive' | 'Pending';

export interface BrokerDto {
  id: string;
  legalName: string;
  licenseNumber: string;
  state: string;
  status: BrokerStatus;
  email: string | null;
  phone: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: number;
  /** True when the broker is soft-deleted (deactivated via DELETE endpoint, F0002-S0005).
   * Only Admin and DistributionManager receive a broker with isDeactivated=true. */
  isDeactivated: boolean;
}

export interface BrokerCreateDto {
  legalName: string;
  licenseNumber: string;
  state: string;
  email?: string;
  phone?: string;
}

export interface BrokerUpdateDto {
  legalName: string;
  state: string;
  status: BrokerStatus;
  email?: string;
  phone?: string;
}

export interface ContactDto {
  id: string;
  brokerId: string | null;
  accountId: string | null;
  fullName: string;
  email: string | null;
  phone: string | null;
  role: string;
  rowVersion: number;
}

export interface ContactCreateDto {
  brokerId: string;
  fullName: string;
  email: string;
  phone: string;
  role?: string;
}

export interface ContactUpdateDto {
  fullName: string;
  email: string;
  phone: string;
  role?: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
