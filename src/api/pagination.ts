export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export const ADMIN_PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;
export type AdminPageSize = (typeof ADMIN_PAGE_SIZE_OPTIONS)[number];
