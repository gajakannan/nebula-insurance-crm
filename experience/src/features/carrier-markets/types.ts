export type CarrierMarketStatus = 'Active' | 'Inactive' | 'Prospect'
export type CarrierMarketType = 'Admitted' | 'NonAdmitted' | 'MGA' | 'Wholesaler' | 'Other'
export type AppetiteLevel = 'Preferred' | 'Open' | 'Selective' | 'Restricted' | 'Closed'
export type AppointmentStatus = 'NotAppointed' | 'InProgress' | 'Appointed' | 'Suspended' | 'Terminated'
export type RelatedEntityType = 'Submission' | 'Policy'
export type RelationshipKind = 'Marketed' | 'Quoted' | 'Bound' | 'Declined' | 'AppointedContext' | 'GeneralReference'

export interface PaginatedResponse<T> {
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface CarrierMarketDto {
  id: string
  code: string
  name: string
  naicCode: string | null
  amBestRating: string | null
  status: CarrierMarketStatus
  marketType: CarrierMarketType
  relationshipOwnerUserId: string | null
  websiteUrl: string | null
  generalEmail: string | null
  mainPhone: string | null
  notes: string | null
  createdAt: string
  createdByUserId: string
  updatedAt: string
  updatedByUserId: string
  rowVersion: number
}

export interface CarrierMarketDetailDto extends CarrierMarketDto {
  contacts: CarrierMarketContactDto[]
  appetiteNotes: CarrierAppetiteNoteDto[]
  appointments: CarrierAppointmentDto[]
  activityLinks: CarrierMarketActivityLinkDto[]
}

export type CarrierMarketCreateDto = Omit<CarrierMarketDto, 'id' | 'createdAt' | 'createdByUserId' | 'updatedAt' | 'updatedByUserId' | 'rowVersion'>
export type CarrierMarketUpdateDto = Omit<CarrierMarketCreateDto, 'code'>

export interface CarrierMarketContactDto {
  id: string
  carrierMarketId: string
  fullName: string
  title: string | null
  email: string | null
  phone: string | null
  roles: string[]
  isPrimary: boolean
  notes: string | null
  createdAt: string
  createdByUserId: string
  updatedAt: string
  updatedByUserId: string
  rowVersion: number
}

export interface CarrierMarketContactUpsertDto {
  fullName: string
  title: string | null
  email: string | null
  phone: string | null
  roles: string[]
  isPrimary: boolean
  notes: string | null
}

export interface CarrierAppetiteNoteDto {
  id: string
  carrierMarketId: string
  lineOfBusiness: string | null
  region: string | null
  appetiteLevel: AppetiteLevel
  summary: string
  detail: string | null
  effectiveFrom: string | null
  effectiveTo: string | null
  source: string | null
  createdAt: string
  createdByUserId: string
  updatedAt: string
  updatedByUserId: string
  rowVersion: number
}

export interface CarrierAppetiteNoteUpsertDto {
  lineOfBusiness: string | null
  region: string | null
  appetiteLevel: AppetiteLevel
  summary: string
  detail: string | null
  effectiveFrom: string | null
  effectiveTo: string | null
  source: string | null
}

export interface CarrierAppointmentDto {
  id: string
  carrierMarketId: string
  appointmentStatus: AppointmentStatus
  states: string[]
  lineOfBusiness: string | null
  appointmentNumber: string | null
  effectiveDate: string | null
  expirationDate: string | null
  notes: string | null
  createdAt: string
  createdByUserId: string
  updatedAt: string
  updatedByUserId: string
  rowVersion: number
}

export interface CarrierAppointmentUpsertDto {
  appointmentStatus: AppointmentStatus
  states: string[]
  lineOfBusiness: string | null
  appointmentNumber: string | null
  effectiveDate: string | null
  expirationDate: string | null
  notes: string | null
}

export interface CarrierMarketActivityLinkDto {
  id: string
  carrierMarketId: string
  relatedEntityType: RelatedEntityType
  relatedEntityId: string
  relationshipKind: RelationshipKind
  note: string | null
  createdAt: string
  createdByUserId: string
}

export interface CarrierMarketActivityLinkCreateDto {
  relatedEntityType: RelatedEntityType
  relatedEntityId: string
  relationshipKind: RelationshipKind
  note: string | null
}
