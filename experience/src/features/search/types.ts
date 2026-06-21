// F0023 — Global search + saved views client contracts (camelCase, matching the API DTOs).

export interface FacetBucket {
  key: string;
  label: string | null;
  count: number;
}

export interface GlobalSearchFacets {
  objectTypes: FacetBucket[];
  owners: FacetBucket[];
  statuses: FacetBucket[];
  regions: FacetBucket[];
  linesOfBusiness: FacetBucket[];
}

export interface GlobalSearchResult {
  objectType: string;
  objectId: string;
  title: string;
  subtitle: string | null;
  status: string | null;
  ownerUserId: string | null;
  ownerDisplayName: string | null;
  lineOfBusiness: string | null;
  region: string | null;
  matchedFields: string[];
  snippet: string | null;
  targetUrl: string;
  score: number;
  lastUpdatedAt: string;
  indexedAt: string;
}

export interface GlobalSearchResponse {
  data: GlobalSearchResult[];
  facets: GlobalSearchFacets;
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  queryEcho: string | null;
  generatedAt: string;
}

export interface GlobalSearchParams {
  q: string;
  objectTypes?: string[];
  status?: string;
  ownerUserId?: string;
  region?: string;
  lineOfBusiness?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export type SavedViewType = 'Search' | 'WorkloadReport' | 'WorkflowAgingReport';
export type SavedViewVisibility = 'Personal' | 'Team';

export interface SavedView {
  id: string;
  name: string;
  description: string | null;
  viewType: SavedViewType;
  visibility: SavedViewVisibility;
  teamScopeType: string | null;
  teamScopeKey: string | null;
  criteria: unknown;
  sort: unknown;
  ownerUserId: string;
  ownerDisplayName: string | null;
  lastEditedByUserId: string | null;
  lastEditedByDisplayName: string | null;
  isDefault: boolean;
  archivedAt: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}

export interface SavedViewUpsertRequest {
  name: string;
  description?: string | null;
  viewType: SavedViewType;
  visibility: SavedViewVisibility;
  teamScopeType?: string | null;
  teamScopeKey?: string | null;
  criteria: unknown;
  sort?: unknown;
  isDefault: boolean;
}

export interface SavedViewListParams {
  viewType?: SavedViewType;
  visibility?: SavedViewVisibility;
  includeArchived?: boolean;
  page?: number;
  pageSize?: number;
}

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
