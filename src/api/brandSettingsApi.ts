import type { AdminBrandSettings, PublicBrandSettings, UpdateBrandSettingsRequest, UploadedBrandImage } from "../app/types";
import { apiClient } from "./apiClient";

const assetUrl=(url:string|null)=>url?new URL(url,`${apiClient.baseUrl.replace(/\/api\/?$/,"")}/`).toString():null;
const normalize=<T extends PublicBrandSettings>(value:T):T=>({...value,logoUrl:assetUrl(value.logoUrl),faviconUrl:assetUrl(value.faviconUrl),defaultOgImageUrl:assetUrl(value.defaultOgImageUrl)});
export async function getPublicBrandSettings(){return normalize(await apiClient.get<PublicBrandSettings>("brand-settings/public"));}
export async function getAdminBrandSettings(){return normalize(await apiClient.get<AdminBrandSettings>("admin/brand-settings"));}
export async function updateAdminBrandSettings(request:UpdateBrandSettingsRequest){return normalize(await apiClient.patch<UpdateBrandSettingsRequest,AdminBrandSettings>("admin/brand-settings",request));}
export function uploadBrandImage(file:File){const body=new FormData();body.append("file",file);return apiClient.postForm<UploadedBrandImage>("admin/brand/uploads",body);}
export const getAdminBrandImage=(id:string)=>apiClient.getBlob(`admin/brand/images/${id}`);
