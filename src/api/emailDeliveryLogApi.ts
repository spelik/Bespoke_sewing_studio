import { ApiError, apiClient } from "./apiClient";
import type { PagedResponse } from "./pagination";

export interface EmailDeliveryLogEntry {
  id: string;
  messageType: string;
  recipientEmail: string;
  subject: string;
  provider: string;
  status: "Queued" | "Retrying" | "Sent" | "Failed" | string;
  sentExternally: boolean;
  resultMessage: string;
  errorMessage: string | null;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  relatedEntityLabel: string | null;
  createdAt: string;
  completedAt: string | null;
}

export interface EmailDeliveryLogQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  messageType?: string;
  status?: string;
  recipientEmail?: string;
  provider?: string;
}

export interface EmailDeliveryManualRetryResult {
  emailDeliveryLogEntryId: string;
  outboxMessageId: string;
  status: string;
  resultMessage: string;
  messageType: string;
  relatedEntityLabel: string | null;
  queuedAt: string;
}

export function getEmailDeliveryLog(
  query: EmailDeliveryLogQuery = {},
): Promise<PagedResponse<EmailDeliveryLogEntry>> {
  const parameters = new URLSearchParams();

  if (query.page) {
    parameters.set("page", String(query.page));
  }

  if (query.pageSize) {
    parameters.set("pageSize", String(query.pageSize));
  }

  setOptionalParameter(parameters, "search", query.search);
  setOptionalParameter(parameters, "messageType", query.messageType);
  setOptionalParameter(parameters, "status", query.status);
  setOptionalParameter(parameters, "recipientEmail", query.recipientEmail);
  setOptionalParameter(parameters, "provider", query.provider);

  const queryString = parameters.toString();
  const suffix = queryString ? `?${queryString}` : "";
  return apiClient.get<PagedResponse<EmailDeliveryLogEntry>>(`/admin/email-log${suffix}`);
}

export function retryEmailDeliveryLogEntry(
  id: string,
): Promise<EmailDeliveryManualRetryResult> {
  return apiClient.post<Record<string, never>, EmailDeliveryManualRetryResult>(
    `/admin/email-log/${id}/retry`,
    {},
  );
}

export function getEmailDeliveryLogErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const validationMessage = error.errors
      ? Object.values(error.errors).flat().find(Boolean)
      : undefined;

    return validationMessage ?? error.message;
  }

  return "The email log could not be loaded. Please try again.";
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
