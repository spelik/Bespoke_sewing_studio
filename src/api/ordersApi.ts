import type { OrderRequest, OrderServiceType, PrototypeOrderResult } from "../app/types";
import { ORDER_SERVICE_TYPES } from "../data/servicesData";
import { apiClient } from "./apiClient";

export function parseOrderServiceType(value: FormDataEntryValue | null): OrderServiceType {
  const service = String(value ?? "");

  if (!ORDER_SERVICE_TYPES.includes(service as OrderServiceType)) {
    throw new Error("Unknown order service type.");
  }

  return service as OrderServiceType;
}

export async function createPrototypeOrder(
  order: OrderRequest,
): Promise<PrototypeOrderResult> {
  apiClient.resolve(order);
  console.info("[prototype] order request accepted", order);

  return {
    requestId: `prototype-${Date.now()}`,
    accepted: true,
    mode: "prototype",
  };
}
