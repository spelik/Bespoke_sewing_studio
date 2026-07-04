import type {
  AdminContactMessageDetail,
  AdminContactMessageListItem,
  ContactMessageRequest,
  ContactMessageResponse,
  ContactMessageStatus,
  UpdateContactMessageStatusRequest,
} from "../app/types";
import { ApiError, apiClient } from "./apiClient";
import type { AdminPageSize, PagedResponse } from "./pagination";

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

export interface DeleteContactMessageResponse {
  id: string;
  referenceNumber: string;
  fullName: string;
  email: string;
}

export interface AdminContactMessageListQuery {
  page?: number;
  pageSize?: AdminPageSize;
  search?: string;
  status?: ContactMessageStatus;
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

export function getAdminContactMessages(
  query: AdminContactMessageListQuery = {},
): Promise<PagedResponse<AdminContactMessageListItem>> {
  const params = new URLSearchParams();
  params.set("page", String(query.page ?? 1));
  params.set("pageSize", String(query.pageSize ?? 25));

  if (query.search?.trim()) {
    params.set("search", query.search.trim());
  }

  if (query.status) {
    params.set("status", query.status);
  }

  return apiClient.get<PagedResponse<AdminContactMessageListItem>>(
    `/admin/contact-messages?${params.toString()}`,
  );
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

export function deleteAdminContactMessage(
  id: string,
): Promise<DeleteContactMessageResponse> {
  return apiClient.delete<DeleteContactMessageResponse>(`/admin/contact-messages/${id}`);
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
