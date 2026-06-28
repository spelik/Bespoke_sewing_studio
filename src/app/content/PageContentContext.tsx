import {createContext,type ReactNode,useCallback,useContext,useEffect,useMemo,useState}from"react";
import {getPublicPageContent}from"../../api/contentApi";import{PAGE_CONTENT_FALLBACK}from"../../data/pageContentData";import type{PageContentSection,PublicPageContent}from"../types";
const keys=Object.keys(PAGE_CONTENT_FALLBACK);interface Value{pages:Record<string,PublicPageContent>;isFallback:boolean;refresh():Promise<void>;section(page:string,key:string):PageContentSection|undefined;}
const Context=createContext<Value|null>(null);
export function PageContentProvider({children}:{children:ReactNode}){const[pages,setPages]=useState({...PAGE_CONTENT_FALLBACK});const[isFallback,setFallback]=useState(true);
const refresh=useCallback(async()=>{const values=await Promise.all(keys.map(getPublicPageContent));setPages(Object.fromEntries(values.map(x=>[x.pageKey,x])));setFallback(false);},[]);
useEffect(()=>{let active=true;Promise.all(keys.map(getPublicPageContent)).then(values=>{if(active){setPages(Object.fromEntries(values.map(x=>[x.pageKey,x])));setFallback(false);}}).catch(()=>undefined);return()=>{active=false;};},[]);
const value=useMemo(()=>({pages,isFallback,refresh,section:(page:string,key:string)=>pages[page]?.sections.find(s=>s.sectionKey===key)}),[pages,isFallback,refresh]);return <Context.Provider value={value}>{children}</Context.Provider>}
export function usePageContent(page:string){const x=useContext(Context);if(!x)throw new Error("usePageContent must be used within PageContentProvider.");return{...x,section:(key:string)=>x.section(page,key)};}
