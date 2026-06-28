import type {
  AdminServiceOffering,
  DeleteServiceOfferingResult,
  PublicServiceOffering,
  SaveServiceOfferingRequest,
} from "../app/types";
import { apiClient } from "./apiClient";

export function getPublicServices(): Promise<PublicServiceOffering[]> {
  return apiClient.get<PublicServiceOffering[]>("services");
}

export function getAdminServices(): Promise<AdminServiceOffering[]> {
  return apiClient.get<AdminServiceOffering[]>("admin/services");
}

export function createAdminService(
  request: SaveServiceOfferingRequest,
): Promise<AdminServiceOffering> {
  return apiClient.post<SaveServiceOfferingRequest, AdminServiceOffering>(
    "admin/services",
    request,
  );
}

export function updateAdminService(
  id: string,
  request: SaveServiceOfferingRequest,
): Promise<AdminServiceOffering> {
  return apiClient.patch<SaveServiceOfferingRequest, AdminServiceOffering>(
    `admin/services/${id}`,
    request,
  );
}

export function deleteAdminService(id: string): Promise<DeleteServiceOfferingResult> {
  return apiClient.delete<DeleteServiceOfferingResult>(`admin/services/${id}`);
}
