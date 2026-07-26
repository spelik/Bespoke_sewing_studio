import type { AdminBrandSettings, PublicBrandSettings, UpdateBrandSettingsRequest, UploadedBrandImage } from "../app/types";
import { apiClient } from "./apiClient";
import { resolveApiAssetUrl } from "./resolveApiAssetUrl";
import {
  uploadWithProgress,
  type UploadProgressEvent,
} from "./uploadTransport";

const normalize=<T extends PublicBrandSettings>(value:T):T=>({
  ...value,
  logoUrl:resolveApiAssetUrl(value.logoUrl),
  faviconUrl:resolveApiAssetUrl(value.faviconUrl),
  defaultOgImageUrl:resolveApiAssetUrl(value.defaultOgImageUrl),
});
export async function getPublicBrandSettings(){return normalize(await apiClient.get<PublicBrandSettings>("brand-settings/public"));}
export async function getAdminBrandSettings(){return normalize(await apiClient.get<AdminBrandSettings>("admin/brand-settings"));}
export async function updateAdminBrandSettings(request:UpdateBrandSettingsRequest){return normalize(await apiClient.patch<UpdateBrandSettingsRequest,AdminBrandSettings>("admin/brand-settings",request));}
export function uploadBrandImage(
  file: File,
  options?: {
    onProgress?: (event: UploadProgressEvent) => void;
    signal?: AbortSignal;
  },
) {
  const body = new FormData();
  body.append("file", file);
  return uploadWithProgress<UploadedBrandImage>({
    path: "admin/brand/uploads",
    method: "POST",
    body,
    onProgress: options?.onProgress,
    signal: options?.signal,
  });
}
export const getAdminBrandImage=(id:string)=>apiClient.getBlob(`admin/brand/images/${id}`);
