import { apiClient } from "./apiClient";
import type {
  AdminRepeatableContentItem,
  DeleteRepeatableContentItemResult,
  PublicRepeatableContentGroup,
  SaveRepeatableContentItemRequest,
} from "../app/types";

export const getPublicRepeatableContentGroups = () =>
  apiClient.get<PublicRepeatableContentGroup[]>("/repeatable-content");

export const getPublicRepeatableContentGroup = (groupKey: string) =>
  apiClient.get<PublicRepeatableContentGroup>(
    `/repeatable-content/groups/${encodeURIComponent(groupKey)}`,
  );

export const getAdminRepeatableContent = () =>
  apiClient.get<AdminRepeatableContentItem[]>("admin/repeatable-content");

export const createAdminRepeatableContent = (request: SaveRepeatableContentItemRequest) =>
  apiClient.post<SaveRepeatableContentItemRequest, AdminRepeatableContentItem>(
    "admin/repeatable-content",
    request,
  );

export const updateAdminRepeatableContent = (
  id: string,
  request: SaveRepeatableContentItemRequest,
) =>
  apiClient.patch<SaveRepeatableContentItemRequest, AdminRepeatableContentItem>(
    `admin/repeatable-content/${id}`,
    request,
  );

export const deleteAdminRepeatableContent = (id: string) =>
  apiClient.delete<DeleteRepeatableContentItemResult>(`admin/repeatable-content/${id}`);
