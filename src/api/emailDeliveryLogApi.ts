import { ApiError, apiClient } from "./apiClient";

export interface EmailDeliveryLogEntry {
  id: string;
  messageType: string;
  recipientEmail: string;
  subject: string;
  provider: string;
  status: "Sent" | "Failed" | string;
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
  take?: number;
  search?: string;
  messageType?: string;
  status?: string;
  recipientEmail?: string;
  provider?: string;
}

export function getEmailDeliveryLog(
  query: EmailDeliveryLogQuery = {},
): Promise<EmailDeliveryLogEntry[]> {
  const parameters = new URLSearchParams();

  if (query.take) {
    parameters.set("take", String(query.take));
  }

  setOptionalParameter(parameters, "search", query.search);
  setOptionalParameter(parameters, "messageType", query.messageType);
  setOptionalParameter(parameters, "status", query.status);
  setOptionalParameter(parameters, "recipientEmail", query.recipientEmail);
  setOptionalParameter(parameters, "provider", query.provider);

  const queryString = parameters.toString();
  const suffix = queryString ? `?${queryString}` : "";
  return apiClient.get<EmailDeliveryLogEntry[]>(`/admin/email-log${suffix}`);
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
