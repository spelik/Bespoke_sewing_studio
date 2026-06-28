import type { AdminPageContent,DeletePageContentResult,PublicPageContent,SavePageContentRequest,UploadedContentImage } from "../app/types";
import { apiClient } from "./apiClient";
const asset=(url:string|null)=>url?new URL(url,`${apiClient.baseUrl.replace(/\/api\/?$/,"")}/`).toString():null;
export async function getPublicPageContent(pageKey:string){const x=await apiClient.get<PublicPageContent>(`content/pages/${pageKey}`);return {...x,sections:x.sections.map(s=>({...s,imageUrl:asset(s.imageUrl)}))};}
export async function getAdminContent(){const x=await apiClient.get<AdminPageContent[]>("admin/content");return x.map(norm);}
const norm=(x:AdminPageContent)=>({...x,title:x.title?.replace(/\n/g," | ")??null,imageUrl:asset(x.imageUrl)});
export async function createContent(r:SavePageContentRequest){return norm(await apiClient.post<SavePageContentRequest,AdminPageContent>("admin/content",r));}
export async function updateContent(id:string,r:SavePageContentRequest){return norm(await apiClient.patch<SavePageContentRequest,AdminPageContent>(`admin/content/${id}`,r));}
export const deleteContent=(id:string)=>apiClient.delete<DeletePageContentResult>(`admin/content/${id}`);
export function uploadContentImage(file:File){const form=new FormData();form.append("file",file);return apiClient.postForm<UploadedContentImage>("admin/content/uploads",form);}
export const getAdminContentImage=(id:string)=>apiClient.getBlob(`admin/content/images/${id}`);
