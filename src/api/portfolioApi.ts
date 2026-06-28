import type {
  AdminPortfolioCategory,
  AdminPortfolioItem,
  DeletePortfolioResult,
  PortfolioItem,
  PublicPortfolioCategory,
  SavePortfolioCategoryRequest,
  SavePortfolioItemRequest,
  UploadedPortfolioImage,
} from "../app/types";
import { apiClient } from "./apiClient";

const resolveImageUrl = (url: string | null): string | null =>
  url ? new URL(url, `${apiClient.baseUrl.replace(/\/api\/?$/, "")}/`).toString() : null;

export async function getPublicPortfolioItems(): Promise<PortfolioItem[]> {
  const items = await apiClient.get<PortfolioItem[]>("portfolio");
  return items.map((item) => ({ ...item, imageUrl: resolveImageUrl(item.imageUrl) ?? "" }));
}
export const getPublicPortfolioCategories = () => apiClient.get<PublicPortfolioCategory[]>("portfolio/categories");
export async function getAdminPortfolioItems(): Promise<AdminPortfolioItem[]> {
  const items = await apiClient.get<AdminPortfolioItem[]>("admin/portfolio/items");
  return items.map((item) => ({ ...item, imageUrl: resolveImageUrl(item.imageUrl) }));
}
export const getAdminPortfolioCategories = () => apiClient.get<AdminPortfolioCategory[]>("admin/portfolio/categories");
export async function createPortfolioItem(request: SavePortfolioItemRequest): Promise<AdminPortfolioItem> {
  return normalizeAdminItem(await apiClient.post<SavePortfolioItemRequest, AdminPortfolioItem>("admin/portfolio/items", request));
}
export async function updatePortfolioItem(id: string, request: SavePortfolioItemRequest): Promise<AdminPortfolioItem> {
  return normalizeAdminItem(await apiClient.patch<SavePortfolioItemRequest, AdminPortfolioItem>(`admin/portfolio/items/${id}`, request));
}
export const deletePortfolioItem = (id: string) =>
  apiClient.delete<DeletePortfolioResult>(`admin/portfolio/items/${id}`);
export const createPortfolioCategory = (request: SavePortfolioCategoryRequest) =>
  apiClient.post<SavePortfolioCategoryRequest, AdminPortfolioCategory>("admin/portfolio/categories", request);
export const updatePortfolioCategory = (id: string, request: SavePortfolioCategoryRequest) =>
  apiClient.patch<SavePortfolioCategoryRequest, AdminPortfolioCategory>(`admin/portfolio/categories/${id}`, request);
export const deletePortfolioCategory = (id: string) =>
  apiClient.delete<DeletePortfolioResult>(`admin/portfolio/categories/${id}`);
export function uploadPortfolioImage(file: File): Promise<UploadedPortfolioImage> {
  const form = new FormData();
  form.append("file", file);
  return apiClient.postForm<UploadedPortfolioImage>("admin/portfolio/uploads", form);
}
export const getAdminPortfolioImage = (id: string): Promise<Blob> =>
  apiClient.getBlob(`admin/portfolio/images/${id}`);

const normalizeAdminItem = (item: AdminPortfolioItem): AdminPortfolioItem => ({
  ...item,
  imageUrl: resolveImageUrl(item.imageUrl),
});
