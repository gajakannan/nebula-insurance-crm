export type CommunicationType = 'Note' | 'Call' | 'Meeting' | 'EmailReference';
export type CommunicationDirection = 'Inbound' | 'Outbound' | 'Internal' | null;
export type CommunicationLinkEntityType = 'Broker' | 'Account' | 'Submission' | 'Policy' | 'Renewal' | 'Task';

export interface CommunicationLinkDto {
  entityType: CommunicationLinkEntityType;
  entityId: string;
  isPrimary: boolean;
  label?: string | null;
}

export interface CommunicationParticipantDto {
  displayName: string;
  email?: string | null;
  participantType: 'InternalUser' | 'BrokerContact' | 'ExternalContact' | 'Other';
  role?: string | null;
  linkedEntityType?: 'User' | 'Broker' | 'Contact' | null;
  linkedEntityId?: string | null;
}

export interface CommunicationEmailReferenceDto {
  provider?: string | null;
  messageId?: string | null;
  subject?: string | null;
  sentAt?: string | null;
}

export interface CommunicationEventDto {
  id: string;
  type: CommunicationType;
  direction?: CommunicationDirection;
  summary: string;
  body?: string | null;
  occurredAt: string;
  visibility: 'InternalOnly';
  emailReference?: CommunicationEmailReferenceDto | null;
  participants: CommunicationParticipantDto[];
  links: CommunicationLinkDto[];
  followUpTaskIds: string[];
  isRedacted: boolean;
  redactedAt?: string | null;
  createdByUserId: string;
  createdAt: string;
  updatedAt?: string | null;
  rowVersion: number;
}

export interface CommunicationHistoryResponseDto {
  data: CommunicationEventDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CommunicationEventFollowUpRequestDto {
  title: string;
  description?: string | null;
  priority?: 'Low' | 'Normal' | 'High' | null;
  dueDate?: string | null;
  assignedToUserId: string;
  linkedEntityType: CommunicationLinkEntityType;
  linkedEntityId: string;
}

export interface CommunicationEventCreateRequestDto {
  type: CommunicationType;
  direction?: CommunicationDirection;
  summary: string;
  body?: string | null;
  occurredAt: string;
  emailReference?: CommunicationEmailReferenceDto | null;
  participants: CommunicationParticipantDto[];
  links: CommunicationLinkDto[];
  followUp?: CommunicationEventFollowUpRequestDto | null;
}

export interface CommunicationEventCorrectionRequestDto {
  action: 'Correct' | 'Redact';
  reason: string;
  summary?: string | null;
  body?: string | null;
  links?: CommunicationLinkDto[] | null;
  participants?: CommunicationParticipantDto[] | null;
}
