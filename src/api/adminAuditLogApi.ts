import { ApiError, apiClient } from "./apiClient";

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
  take?: number;
  search?: string;
  action?: string;
  entityType?: string;
  actorEmail?: string;
}

export function getAdminAuditLog(query: AdminAuditLogQuery = {}): Promise<AdminAuditLogEntry[]> {
  const parameters = new URLSearchParams();

  if (query.take) {
    parameters.set("take", String(query.take));
  }

  setOptionalParameter(parameters, "search", query.search);
  setOptionalParameter(parameters, "action", query.action);
  setOptionalParameter(parameters, "entityType", query.entityType);
  setOptionalParameter(parameters, "actorEmail", query.actorEmail);

  const queryString = parameters.toString();
  const suffix = queryString ? `?${queryString}` : "";
  return apiClient.get<AdminAuditLogEntry[]>(`/admin/audit-log${suffix}`);
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
