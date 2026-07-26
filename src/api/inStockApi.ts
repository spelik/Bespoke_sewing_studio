import type {
  AdminInStockImage,
  AdminInStockItem,
  ArchiveInStockItemResult,
  ReorderInStockImagesRequest,
  SaveInStockItemRequest,
  UpdateInStockImageRequest,
} from "../app/types";
import { apiClient } from "./apiClient";
import { resolveApiAssetUrl } from "./resolveApiAssetUrl";
import {
  uploadWithProgress,
  type UploadProgressEvent,
} from "./uploadTransport";

function normalizeImage(image: AdminInStockImage): AdminInStockImage {
  return {
    ...image,
    imageUrl: resolveApiAssetUrl(image.imageUrl) ?? image.imageUrl,
  };
}

function normalizeItem(item: AdminInStockItem): AdminInStockItem {
  return {
    ...item,
    images: (item.images ?? []).map(normalizeImage),
  };
}

export async function getAdminInStockItems(): Promise<AdminInStockItem[]> {
  const items = await apiClient.get<AdminInStockItem[]>("admin/in-stock/items");
  return items.map(normalizeItem);
}

export async function getAdminInStockItem(id: string): Promise<AdminInStockItem> {
  return normalizeItem(await apiClient.get<AdminInStockItem>(`admin/in-stock/items/${id}`));
}

export async function createInStockItem(
  request: SaveInStockItemRequest,
): Promise<AdminInStockItem> {
  return normalizeItem(
    await apiClient.post<SaveInStockItemRequest, AdminInStockItem>(
      "admin/in-stock/items",
      request,
    ),
  );
}

export async function updateInStockItem(
  id: string,
  request: SaveInStockItemRequest,
): Promise<AdminInStockItem> {
  return normalizeItem(
    await apiClient.put<SaveInStockItemRequest, AdminInStockItem>(
      `admin/in-stock/items/${id}`,
      request,
    ),
  );
}

export function archiveInStockItem(id: string): Promise<ArchiveInStockItemResult> {
  return apiClient.post<Record<string, never>, ArchiveInStockItemResult>(
    `admin/in-stock/items/${id}/archive`,
    {},
  );
}

export function restoreInStockItem(id: string): Promise<ArchiveInStockItemResult> {
  return apiClient.post<Record<string, never>, ArchiveInStockItemResult>(
    `admin/in-stock/items/${id}/restore`,
    {},
  );
}

export async function uploadInStockImage(
  itemId: string,
  file: File,
  options?: {
    altText?: string | null;
    displayOrder?: number | null;
    onProgress?: (event: UploadProgressEvent) => void;
    signal?: AbortSignal;
  },
): Promise<AdminInStockImage> {
  const form = new FormData();
  form.append("file", file, file.name);
  if (options?.altText) {
    form.append("altText", options.altText);
  }
  if (options?.displayOrder != null) {
    form.append("displayOrder", String(options.displayOrder));
  }

  const uploaded = await uploadWithProgress<AdminInStockImage>({
    path: `admin/in-stock/items/${itemId}/images`,
    method: "POST",
    body: form,
    onProgress: options?.onProgress,
    signal: options?.signal,
  });
  return normalizeImage(uploaded);
}

export async function updateInStockImage(
  itemId: string,
  imageId: string,
  request: UpdateInStockImageRequest,
): Promise<AdminInStockImage> {
  return normalizeImage(
    await apiClient.patch<UpdateInStockImageRequest, AdminInStockImage>(
      `admin/in-stock/items/${itemId}/images/${imageId}`,
      request,
    ),
  );
}

export async function reorderInStockImages(
  itemId: string,
  request: ReorderInStockImagesRequest,
): Promise<AdminInStockImage[]> {
  const images = await apiClient.put<ReorderInStockImagesRequest, AdminInStockImage[]>(
    `admin/in-stock/items/${itemId}/images/reorder`,
    request,
  );
  return images.map(normalizeImage);
}

export function deleteInStockImage(itemId: string, imageId: string): Promise<void> {
  return apiClient.delete<void>(`admin/in-stock/items/${itemId}/images/${imageId}`);
}

export function getAdminInStockImageBlob(imageId: string): Promise<Blob> {
  return apiClient.getBlob(`admin/in-stock/images/${imageId}`);
}
