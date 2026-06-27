import type {
  CreateOrderApiRequest,
  OrderApiServiceType,
  OrderRequest,
  OrderServiceType,
  OrderSubmissionResponse,
} from "../app/types";
import { ORDER_SERVICE_TYPES } from "../data/servicesData";
import { ApiError, apiClient } from "./apiClient";

const API_SERVICE_TYPES: Readonly<Record<OrderServiceType, OrderApiServiceType>> = {
  Tailoring: "Tailoring",
  Dressmaking: "Dressmaking",
  Alterations: "Alterations",
  "Memory Bears": "MemoryBear",
};

export const ORDER_STATUSES = [
  "New",
  "Contacted",
  "WaitingForDetails",
  "Quoted",
  "Accepted",
  "InProgress",
  "ReadyForCollection",
  "Completed",
  "Cancelled",
] as const;

export type AdminOrderStatus = (typeof ORDER_STATUSES)[number];

export interface AdminOrderListItem {
  id: string;
  clientId: string;
  clientName: string;
  clientEmail: string | null;
  clientPhone: string | null;
  serviceType: OrderApiServiceType;
  status: AdminOrderStatus;
  description: string;
  preferredDate: string | null;
  createdAt: string;
}

export interface AdminClient {
  id: string;
  fullName: string;
  email: string | null;
  phone: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface AdminOrderNote {
  id: string;
  text: string;
  createdAt: string;
}

export interface AdminOrderAttachment {
  id: string;
  uploadedFileId: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  caption: string | null;
  displayOrder: number;
}

export interface AdminOrderDetail {
  id: string;
  clientId: string;
  client: AdminClient;
  serviceType: OrderApiServiceType;
  status: AdminOrderStatus;
  description: string;
  preferredDate: string | null;
  consentGiven: boolean;
  consentRecordedAt: string | null;
  quotedAmount: number | null;
  currency: string;
  createdAt: string;
  updatedAt: string;
  attachments: AdminOrderAttachment[];
  notes: AdminOrderNote[];
}

export function parseOrderServiceType(value: FormDataEntryValue | null): OrderServiceType {
  const service = String(value ?? "");

  if (!ORDER_SERVICE_TYPES.includes(service as OrderServiceType)) {
    throw new Error("Unknown order service type.");
  }

  return service as OrderServiceType;
}

export async function createOrder(
  order: OrderRequest,
): Promise<OrderSubmissionResponse> {
  const request: CreateOrderApiRequest = {
    fullName: order.fullName.trim(),
    email: order.email.trim() || null,
    phone: order.phone?.trim() || null,
    serviceType: API_SERVICE_TYPES[order.service],
    description: order.description.trim(),
    preferredDate: order.preferredDate || null,
    consent: order.consent,
    attachmentIds: null,
  };

  return apiClient.post<CreateOrderApiRequest, OrderSubmissionResponse>("/orders", request);
}

export function getAdminOrders(): Promise<AdminOrderListItem[]> {
  return apiClient.get<AdminOrderListItem[]>("/orders");
}

export function getAdminOrder(id: string): Promise<AdminOrderDetail> {
  return apiClient.get<AdminOrderDetail>(`/orders/${id}`);
}

export function updateAdminOrderStatus(
  id: string,
  status: AdminOrderStatus,
): Promise<AdminOrderDetail> {
  return apiClient.patch<{ status: AdminOrderStatus }, AdminOrderDetail>(
    `/orders/${id}/status`,
    { status },
  );
}

export function addAdminOrderNote(id: string, text: string): Promise<AdminOrderDetail> {
  return apiClient.post<{ text: string }, AdminOrderDetail>(`/orders/${id}/notes`, {
    text: text.trim(),
  });
}

export function getAdminApiErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const validationMessage = error.errors
      ? Object.values(error.errors).flat().find(Boolean)
      : undefined;

    return validationMessage ?? error.message;
  }

  return "The admin request could not be completed. Please try again.";
}

export function getOrderSubmissionErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const validationMessage = error.errors
      ? Object.values(error.errors).flat().find(Boolean)
      : undefined;

    return validationMessage ?? error.message;
  }

  return "We could not submit your request. Please check your connection and try again.";
}
