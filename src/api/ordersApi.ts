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

export function getOrderSubmissionErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const validationMessage = error.errors
      ? Object.values(error.errors).flat().find(Boolean)
      : undefined;

    return validationMessage ?? error.message;
  }

  return "We could not submit your request. Please check your connection and try again.";
}
