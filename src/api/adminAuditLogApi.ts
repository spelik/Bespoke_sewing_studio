import { ApiError, apiClient } from "./apiClient";
import type { PagedResponse } from "./pagination";

export interface AdminAuditLogEntry {
  id: string;
  actorUserId: string | null;
  actorEmail: string;
  action: string;
  entityType: string;
  entityId: string | null;
  entityLabel: string | null;
  summary: string;
  metadataJson: string | null;
  createdAt: string;
}

export interface AdminAuditLogQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  action?: string;
  entityType?: string;
  actorEmail?: string;
}

export function getAdminAuditLog(
  query: AdminAuditLogQuery = {},
): Promise<PagedResponse<AdminAuditLogEntry>> {
  const parameters = new URLSearchParams();

  if (query.page) {
    parameters.set("page", String(query.page));
  }

  if (query.pageSize) {
    parameters.set("pageSize", String(query.pageSize));
  }

  setOptionalParameter(parameters, "search", query.search);
  setOptionalParameter(parameters, "action", query.action);
  setOptionalParameter(parameters, "entityType", query.entityType);
  setOptionalParameter(parameters, "actorEmail", query.actorEmail);

  const queryString = parameters.toString();
  const suffix = queryString ? `?${queryString}` : "";
  return apiClient.get<PagedResponse<AdminAuditLogEntry>>(`/admin/audit-log${suffix}`);
}

export function getAdminAuditLogErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const validationMessage = error.errors
      ? Object.values(error.errors).flat().find(Boolean)
      : undefined;

    return validationMessage ?? error.message;
  }

  return "The audit log could not be loaded. Please try again.";
}

function setOptionalParameter(
  parameters: URLSearchParams,
  name: string,
  value: string | undefined,
) {
  const trimmed = value?.trim();
  if (trimmed) {
    parameters.set(name, trimmed);
  }
}
