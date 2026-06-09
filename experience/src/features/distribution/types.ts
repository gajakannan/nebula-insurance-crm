// F0017 distribution hierarchy, producer ownership, and territory types.
// Mirrors planning-mds/schemas/{distribution-node,producer-ownership,territory,territory-assignment}*.schema.json.
// rowVersion is the xmin concurrency token serialized as a string (used as the If-Match value).

export type NodeType = 'MGA' | 'Broker' | 'SubBroker' | 'Producer';

export interface DistributionNodeDto {
  id: string;
  nodeType: NodeType;
  displayName: string;
  parentId: string | null;
  ancestryPath: string[];
  depth: number;
  childCount: number;
  isActive: boolean;
  rowVersion: string;
}

export interface DistributionNodeParentRequest {
  parentId: string | null;
  note?: string | null;
}

export interface DistributionNodeAncestorsResponse {
  node: DistributionNodeDto;
  ancestors: DistributionNodeDto[];
}

export type ScopeType = 'Account' | 'BrokerRelationship';

export interface ProducerOwnershipDto {
  id: string;
  scopeType: ScopeType;
  scopeId: string;
  producerNodeId: string;
  producerDisplayName: string | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  assignmentReason: string | null;
  rowVersion: string;
  changedBy: string | null;
  changedAt: string | null;
}

export interface ProducerOwnershipAssignmentRequest {
  scopeType: ScopeType;
  scopeId: string;
  producerNodeId: string;
  effectiveFrom: string;
  assignmentReason?: string | null;
}

export interface ProducerOwnershipLookupResponse {
  scopeType: ScopeType;
  scopeId: string;
  asOf: string;
  ownership: ProducerOwnershipDto | null;
}

export type MemberType = 'Broker' | 'Producer';

export interface TerritoryDto {
  id: string;
  name: string;
  description: string | null;
  criteria: Record<string, string>;
  isActive: boolean;
  rowVersion: string;
  changedBy: string | null;
  changedAt: string | null;
}

export interface TerritoryCreateRequest {
  name: string;
  description?: string | null;
  criteria: Record<string, string>;
}

export interface TerritoryAssignmentDto {
  id: string;
  territoryId: string;
  territoryName: string | null;
  memberType: MemberType;
  memberId: string;
  memberDisplayName: string | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  assignmentReason: string | null;
  rowVersion: string;
  changedBy: string | null;
  changedAt: string | null;
}

export interface TerritoryMemberAssignmentRequest {
  memberType: MemberType;
  memberId: string;
  effectiveFrom: string;
  assignmentReason?: string | null;
}

export interface TerritoryAssignmentLookupResponse {
  memberType: MemberType;
  memberId: string;
  asOf: string;
  assignment: TerritoryAssignmentDto | null;
}

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
