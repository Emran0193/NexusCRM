export interface CustomerDto {
  id: string;
  tenantId: string;
  type: string;
  displayName: string;
  email?: string | null;
  phone?: string | null;
  status: string;
  tags: string[];
  createdAtUtc: string;
}

export interface CustomerDetailDto extends CustomerDto {
  contacts: Array<{
    id: string;
    name: string;
    email?: string | null;
    phone?: string | null;
    isPrimary: boolean;
  }>;
  notes: Array<{
    id: string;
    body: string;
    authorUserId?: string | null;
    createdAtUtc: string;
  }>;
  timeline: Array<{
    id: string;
    eventType: string;
    summary: string;
    actorUserId?: string | null;
    occurredAtUtc: string;
  }>;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CreateCustomerRequest {
  type: 'Individual' | 'Organization';
  displayName: string;
  email?: string | null;
  phone?: string | null;
}
