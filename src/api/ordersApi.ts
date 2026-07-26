import type {
  CreateOrderApiRequest,
  OrderApiServiceType,
  OrderRequest,
  OrderServiceType,
  OrderSubmissionResponse,
} from "../app/types";
import { ApiError, apiClient } from "./apiClient";
import type { PagedResponse } from "./pagination";
import {
  uploadWithProgress,
  type UploadProgressEvent,
} from "./uploadTransport";

const API_SERVICE_TYPES: Readonly<Record<OrderServiceType, OrderApiServiceType>> = {
  Tailoring: "Tailoring",
  Dressmaking: "Dressmaking",
  Alterations: "Alterations",
  "Memory Bears": "MemoryBear",
};

export const MAX_ORDER_ATTACHMENT_SIZE_BYTES = 5 * 1024 * 1024;
export const MAX_ORDER_ATTACHMENTS = 5;
const ALLOWED_ORDER_ATTACHMENT_TYPES = new Set([
  "image/jpeg",
  "image/png",
  "image/webp",
  "application/pdf",
]);

export class OrderFileValidationError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "OrderFileValidationError";
  }
}

export type UploadScanStatus = "Pending" | "Clean" | "Skipped" | "Infected" | "Rejected" | "ScanFailed";

export interface UploadedOrderAttachment {
  id: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  purpose: "OrderAttachment";
  createdAt: string;
  scanStatus: UploadScanStatus;
  scanProvider: string | null;
  scannedAt: string | null;
}

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
  referenceNumber: string;
  clientId: string;
  clientName: string;
  clientEmail: string | null;
  clientPhone: string | null;
  serviceOfferingId: string | null;
  serviceName: string;
  serviceType: OrderApiServiceType;
  status: AdminOrderStatus;
  description: string;
  preferredDate: string | null;
  createdAt: string;
}

export interface AdminOrderListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: AdminOrderStatus;
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
  scanStatus: UploadScanStatus;
  scanProvider: string | null;
  scannedAt: string | null;
}

export interface DeleteOrderResponse {
  id: string;
  referenceNumber: string;
  clientName: string;
}

export interface AdminOrderDetail {
  id: string;
  referenceNumber: string;
  clientId: string;
  client: AdminClient;
  serviceOfferingId: string | null;
  serviceName: string;
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

export async function createOrder(
  order: OrderRequest,
  uploadOptions?: {
    onProgress?: (event: UploadProgressEvent) => void;
    onFileProgress?: (index: number, total: number, event: UploadProgressEvent) => void;
    signal?: AbortSignal;
  },
): Promise<OrderSubmissionResponse> {
  validateOrderAttachments(order.attachments);
  const uploadedFiles = order.attachments.length > 0
    ? await uploadOrderAttachments(order.attachments, uploadOptions)
    : [];
  const request: CreateOrderApiRequest = {
    fullName: order.fullName.trim(),
    email: order.email.trim() || null,
    phone: order.phone?.trim() || null,
    serviceType: order.legacyServiceType
      ? API_SERVICE_TYPES[order.legacyServiceType]
      : null,
    serviceOfferingId: order.serviceOfferingId ?? null,
    serviceSlug: order.serviceSlug ?? null,
    description: order.description.trim(),
    preferredDate: order.preferredDate || null,
    consent: order.consent,
    attachmentIds: uploadedFiles.length > 0
      ? uploadedFiles.map((file) => file.id)
      : null,
    websiteUrl: order.websiteUrl.trim() || null,
    formLoadedAt: order.formLoadedAt,
  };

  return apiClient.post<CreateOrderApiRequest, OrderSubmissionResponse>("/orders", request);
}

export function validateOrderAttachments(files: readonly File[]): void {
  if (files.length > MAX_ORDER_ATTACHMENTS) {
    throw new OrderFileValidationError(`You can attach up to ${MAX_ORDER_ATTACHMENTS} files.`);
  }

  for (const file of files) {
    if (file.size <= 0) {
      throw new OrderFileValidationError(`"${file.name}" is empty.`);
    }
    if (file.size > MAX_ORDER_ATTACHMENT_SIZE_BYTES) {
      throw new OrderFileValidationError(`"${file.name}" is larger than 5 MB.`);
    }
    if (!ALLOWED_ORDER_ATTACHMENT_TYPES.has(file.type)) {
      throw new OrderFileValidationError(`"${file.name}" is not a supported JPG, PNG, WebP or PDF file.`);
    }
  }
}

/**
 * Uploads order attachments sequentially (one multipart request per file) so
 * progress and ClamAV load stay predictable. Aggregate multi-file FormData is
 * still accepted by the backend, but sequential uploads give real per-file %.
 */
export async function uploadOrderAttachments(
  files: readonly File[],
  options?: {
    onProgress?: (event: UploadProgressEvent) => void;
    onFileProgress?: (index: number, total: number, event: UploadProgressEvent) => void;
    signal?: AbortSignal;
  },
): Promise<UploadedOrderAttachment[]> {
  const uploaded: UploadedOrderAttachment[] = [];

  for (let index = 0; index < files.length; index += 1) {
    const file = files[index]!;
    const formData = new FormData();
    formData.append("files", file, file.name);
    const batch = await uploadWithProgress<UploadedOrderAttachment[]>({
      path: "/uploads/order-attachments",
      method: "POST",
      body: formData,
      signal: options?.signal,
      onProgress: (event) => {
        options?.onProgress?.(event);
        options?.onFileProgress?.(index, files.length, event);
      },
    });
    uploaded.push(...batch);
  }

  return uploaded;
}

export function getAdminAttachmentFile(uploadedFileId: string): Promise<Blob> {
  return apiClient.getBlob(`/uploads/${uploadedFileId}`);
}

export function getAdminOrders(
  query: AdminOrderListQuery = {},
): Promise<PagedResponse<AdminOrderListItem>> {
  const parameters = new URLSearchParams();

  if (query.page) {
    parameters.set("page", String(query.page));
  }
  if (query.pageSize) {
    parameters.set("pageSize", String(query.pageSize));
  }

  const search = query.search?.trim();
  if (search) {
    parameters.set("search", search);
  }
  if (query.status) {
    parameters.set("status", query.status);
  }

  const queryString = parameters.toString();
  return apiClient.get<PagedResponse<AdminOrderListItem>>(
    `/orders${queryString ? `?${queryString}` : ""}`,
  );
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

export function deleteAdminOrder(
  orderId: string,
): Promise<DeleteOrderResponse> {
  return apiClient.delete<DeleteOrderResponse>(`/orders/${orderId}`);
}

export function deleteAdminOrderAttachment(
  orderId: string,
  attachmentId: string,
): Promise<AdminOrderDetail> {
  return apiClient.delete<AdminOrderDetail>(`/orders/${orderId}/attachments/${attachmentId}`);
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
  if (error instanceof OrderFileValidationError) {
    return error.message;
  }
  if (error instanceof ApiError) {
    const validationMessage = error.errors
      ? Object.values(error.errors).flat().find(Boolean)
      : undefined;

    return validationMessage ?? error.message;
  }

  return "We could not submit your request. Please check your connection and try again.";
}
