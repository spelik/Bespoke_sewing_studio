import type { AdminPageContent,DeletePageContentResult,PublicPageContent,SavePageContentRequest,UploadedContentImage } from "../app/types";
import { apiClient } from "./apiClient";
import { resolveApiAssetUrl } from "./resolveApiAssetUrl";
import {
  uploadWithProgress,
  type UploadProgressEvent,
} from "./uploadTransport";
export async function getPublicPageContent(pageKey:string){const x=await apiClient.get<PublicPageContent>(`content/pages/${pageKey}`);return {...x,sections:x.sections.map(s=>({...s,imageUrl:resolveApiAssetUrl(s.imageUrl)}))};}
export async function getAdminContent(){const x=await apiClient.get<AdminPageContent[]>("admin/content");return x.map(norm);}
const norm=(x:AdminPageContent)=>({...x,title:x.title?.replace(/\n/g," | ")??null,imageUrl:resolveApiAssetUrl(x.imageUrl)});
export async function createContent(r:SavePageContentRequest){return norm(await apiClient.post<SavePageContentRequest,AdminPageContent>("admin/content",r));}
export async function updateContent(id:string,r:SavePageContentRequest){return norm(await apiClient.patch<SavePageContentRequest,AdminPageContent>(`admin/content/${id}`,r));}
export const deleteContent=(id:string)=>apiClient.delete<DeletePageContentResult>(`admin/content/${id}`);
export function uploadContentImage(
  file: File,
  options?: {
    onProgress?: (event: UploadProgressEvent) => void;
    signal?: AbortSignal;
  },
) {
  const form = new FormData();
  form.append("file", file);
  return uploadWithProgress<UploadedContentImage>({
    path: "admin/content/uploads",
    method: "POST",
    body: form,
    onProgress: options?.onProgress,
    signal: options?.signal,
  });
}
export const getAdminContentImage=(id:string)=>apiClient.getBlob(`admin/content/images/${id}`);
