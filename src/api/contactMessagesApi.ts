import type {
  AdminContactMessageDetail,
  AdminContactMessageListItem,
  ContactMessageRequest,
  ContactMessageResponse,
  ContactMessageStatus,
  UpdateContactMessageStatusRequest,
} from "../app/types";
import { ApiError, apiClient } from "./apiClient";

interface CreateContactMessageApiRequest {
  fullName: string;
  email: string;
  phone: string | null;
  subject: string | null;
  message: string;
  consent: boolean;
  websiteUrl: string | null;
  formLoadedAt: string;
}

export const CONTACT_MESSAGE_STATUSES: readonly ContactMessageStatus[] = [
  "New",
  "Read",
  "Replied",
  "Archived",
];

export function createContactMessage(
  request: ContactMessageRequest,
): Promise<ContactMessageResponse> {
  const apiRequest: CreateContactMessageApiRequest = {
    fullName: request.fullName.trim(),
    email: request.email.trim(),
    phone: request.phone?.trim() || null,
    subject: request.subject?.trim() || null,
    message: request.message.trim(),
    consent: request.consent,
    websiteUrl: request.websiteUrl.trim() || null,
    formLoadedAt: request.formLoadedAt,
  };

  return apiClient.post<CreateContactMessageApiRequest, ContactMessageResponse>(
    "/contact-messages",
    apiRequest,
  );
}

export function getAdminContactMessages(take = 100): Promise<AdminContactMessageListItem[]> {
  return apiClient.get<AdminContactMessageListItem[]>(`/admin/contact-messages?take=${take}`);
}

export function getAdminContactMessage(id: string): Promise<AdminContactMessageDetail> {
  return apiClient.get<AdminContactMessageDetail>(`/admin/contact-messages/${id}`);
}

export function updateAdminContactMessageStatus(
  id: string,
  status: ContactMessageStatus,
): Promise<AdminContactMessageDetail> {
  const request: UpdateContactMessageStatusRequest = { status };
  return apiClient.patch<UpdateContactMessageStatusRequest, AdminContactMessageDetail>(
    `/admin/contact-messages/${id}/status`,
    request,
  );
}

export function getContactMessageSubmissionErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const validationMessage = error.errors
      ? Object.values(error.errors).flat().find(Boolean)
      : undefined;

    return validationMessage ?? error.message;
  }

  return "We could not send your message. Please check your connection and try again.";
}
