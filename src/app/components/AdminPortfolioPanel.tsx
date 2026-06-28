import { useEffect, useState, type FormEvent } from "react";
import { Plus, Upload } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  createPortfolioCategory, createPortfolioItem, deletePortfolioCategory, deletePortfolioItem,
  getAdminPortfolioCategories, getAdminPortfolioImage, getAdminPortfolioItems, updatePortfolioCategory, updatePortfolioItem,
  uploadPortfolioImage,
} from "../../api/portfolioApi";
import { usePortfolio } from "../portfolio/PortfolioContext";
import type { AdminPortfolioCategory, AdminPortfolioItem, SavePortfolioCategoryRequest, SavePortfolioItemRequest } from "../types";

interface Props { onUnauthorized(): void; }
const input = "w-full border border-border bg-background px-3 py-2.5 text-[11px] focus:outline-none focus:border-accent";

export function AdminPortfolioPanel({ onUnauthorized }: Props) {
  const { refresh } = usePortfolio();
  const [tab, setTab] = useState<"items" | "categories">("items");
  const [items, setItems] = useState<AdminPortfolioItem[]>([]);
  const [categories, setCategories] = useState<AdminPortfolioCategory[]>([]);
  const [itemForm, setItemForm] = useState<SavePortfolioItemRequest | null>(null);
  const [categoryForm, setCategoryForm] = useState<SavePortfolioCategoryRequest | null>(null);
  const [editingItemId, setEditingItemId] = useState<string | null>(null);
  const [editingCategoryId, setEditingCategoryId] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => { void load(); }, []);

  async function load() {
    setLoading(true); setError(null);
    try {
      const [nextItems, nextCategories] = await Promise.all([getAdminPortfolioItems(), getAdminPortfolioCategories()]);
      setItems(nextItems); setCategories(nextCategories);
    } catch (reason) { handleError(reason); } finally { setLoading(false); }
  }

  function handleError(reason: unknown) {
    if (reason instanceof ApiError && (reason.status === 401 || reason.status === 403)) { onUnauthorized(); return; }
    if (reason instanceof ApiError) {
      setError(reason.errors ? Object.values(reason.errors).flat().find(Boolean) ?? reason.message : reason.message);
      return;
    }
    setError("The portfolio request could not be completed. Please try again.");
  }

  function newItem() {
    const categoryId = categories.find((category) => category.isActive && !category.archivedAt)?.id ?? "";
    setEditingItemId(null); setPreviewUrl(null); setError(null); setMessage(null);
    setItemForm({ categoryId, slug: null, title: "", shortDescription: null, description: null,
      imageFileId: null, altText: null, isActive: false, isFeatured: false,
      displayOrder: items.reduce((max, item) => Math.max(max, item.displayOrder + 1), 0) });
  }

  function editItem(item: AdminPortfolioItem) {
    setEditingItemId(item.id); setPreviewUrl(null); setError(null); setMessage(null);
    setItemForm({ categoryId: item.categoryId, slug: item.slug, title: item.title,
      shortDescription: item.shortDescription, description: item.description, imageFileId: item.imageFileId,
      altText: item.altText, isActive: item.isActive, isFeatured: item.isFeatured, displayOrder: item.displayOrder });
  }

  async function saveItem(event: FormEvent) {
    event.preventDefault(); if (!itemForm || saving) return;
    setSaving(true); setError(null); setMessage(null);
    try {
      const request = cleanItem(itemForm);
      const saved = editingItemId ? await updatePortfolioItem(editingItemId, request) : await createPortfolioItem(request);
      setItems((current) => sortItems([...current.filter((item) => item.id !== saved.id), saved]));
      setEditingItemId(saved.id); editItem(saved); await refresh().catch(() => undefined);
      setMessage(editingItemId ? "Portfolio item updated." : "Portfolio item created.");
    } catch (reason) { handleError(reason); } finally { setSaving(false); }
  }

  async function uploadImage(file: File | undefined) {
    if (!file || !itemForm) return;
    setUploading(true); setError(null); setMessage(null);
    try {
      const uploaded = await uploadPortfolioImage(file);
      setItemForm((current) => current ? { ...current, imageFileId: uploaded.id } : current);
      setPreviewUrl(URL.createObjectURL(file));
      setMessage("Image uploaded. Save the portfolio item to publish it.");
    } catch (reason) { handleError(reason); } finally { setUploading(false); }
  }

  async function toggleItem(item: AdminPortfolioItem, field: "isActive" | "isFeatured") {
    setError(null); setMessage(null);
    try {
      const saved = await updatePortfolioItem(item.id, { ...itemRequest(item), [field]: !item[field] });
      setItems((current) => sortItems(current.map((candidate) => candidate.id === saved.id ? saved : candidate)));
      await refresh().catch(() => undefined); setMessage(`${saved.title} updated.`);
    } catch (reason) { handleError(reason); }
  }

  async function removeItem(item: AdminPortfolioItem) {
    if (!window.confirm(`Archive "${item.title}"? The stored image will be retained.`)) return;
    try {
      const result = await deletePortfolioItem(item.id); await load(); await refresh().catch(() => undefined);
      if (editingItemId === item.id) { setItemForm(null); setEditingItemId(null); }
      setMessage(result.message);
    } catch (reason) { handleError(reason); }
  }

  function newCategory() {
    setEditingCategoryId(null); setError(null); setMessage(null);
    setCategoryForm({ slug: null, name: "", description: null, isActive: true,
      displayOrder: categories.reduce((max, category) => Math.max(max, category.displayOrder + 1), 0) });
  }

  function editCategory(category: AdminPortfolioCategory) {
    setEditingCategoryId(category.id); setError(null); setMessage(null);
    setCategoryForm({ slug: category.slug, name: category.name, description: category.description,
      isActive: category.isActive, displayOrder: category.displayOrder });
  }

  async function saveCategory(event: FormEvent) {
    event.preventDefault(); if (!categoryForm || saving) return;
    setSaving(true); setError(null); setMessage(null);
    try {
      const request = cleanCategory(categoryForm);
      const saved = editingCategoryId ? await updatePortfolioCategory(editingCategoryId, request) : await createPortfolioCategory(request);
      setCategories((current) => sortCategories([...current.filter((category) => category.id !== saved.id), saved]));
      setEditingCategoryId(saved.id); editCategory(saved); await refresh().catch(() => undefined);
      setMessage(editingCategoryId ? "Category updated." : "Category created.");
    } catch (reason) { handleError(reason); } finally { setSaving(false); }
  }

  async function toggleCategory(category: AdminPortfolioCategory) {
    try {
      const saved = await updatePortfolioCategory(category.id, { ...categoryRequest(category), isActive: !category.isActive });
      setCategories((current) => sortCategories(current.map((candidate) => candidate.id === saved.id ? saved : candidate)));
      await refresh().catch(() => undefined); setMessage(`${saved.name} updated.`);
    } catch (reason) { handleError(reason); }
  }

  async function removeCategory(category: AdminPortfolioCategory) {
    if (!window.confirm(`Delete "${category.name}"? Categories with items will be archived.`)) return;
    try {
      const result = await deletePortfolioCategory(category.id); await load(); await refresh().catch(() => undefined);
      if (editingCategoryId === category.id) { setCategoryForm(null); setEditingCategoryId(null); }
      setMessage(result.message);
    } catch (reason) { handleError(reason); }
  }

  if (loading) return <div className="bg-card border border-border p-6 text-[11px] text-muted-foreground">Loading portfolio...</div>;

  return <div className="space-y-6">
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div className="flex gap-2">
        {(["items", "categories"] as const).map((value) => <button key={value} type="button" onClick={() => setTab(value)} className={`px-4 py-2 text-[10px] uppercase tracking-wide ${tab === value ? "bg-foreground text-primary-foreground" : "border border-border bg-card"}`}>{value}</button>)}
      </div>
      <button type="button" onClick={tab === "items" ? newItem : newCategory} className="inline-flex items-center gap-2 bg-foreground text-primary-foreground px-4 py-2.5 text-[10px] hover:bg-accent"><Plus size={12}/> Add {tab === "items" ? "item" : "category"}</button>
    </div>
    {error ? <p role="alert" className="border border-destructive/30 bg-card px-4 py-3 text-[11px] text-destructive">{error}</p> : null}
    {message ? <p role="status" className="border border-emerald-300 bg-emerald-50 px-4 py-3 text-[11px] text-emerald-700">{message}</p> : null}

    {tab === "items" ? <>
      {itemForm ? <form onSubmit={saveItem} className="bg-card border border-border p-5 space-y-5">
        <div className="flex justify-between"><h2 className="font-serif text-xl font-light">{editingItemId ? "Edit portfolio item" : "New portfolio item"}</h2><button type="button" onClick={() => setItemForm(null)} className="text-[10px] text-muted-foreground">Close</button></div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Field label="Title" required value={itemForm.title} onChange={(title) => setItemForm({ ...itemForm, title })}/>
          <Field label="Slug (optional)" value={itemForm.slug} onChange={(slug) => setItemForm({ ...itemForm, slug: slug || null })}/>
          <label className="text-[10px] text-muted-foreground"><span className="block mb-1.5">Category</span><select required value={itemForm.categoryId} onChange={(event) => setItemForm({ ...itemForm, categoryId: event.target.value })} className={input}><option value="">Select category</option>{categories.filter((category) => !category.archivedAt).map((category) => <option key={category.id} value={category.id}>{category.name}{category.isActive ? "" : " (inactive)"}</option>)}</select></label>
          <Field label="Display order" type="number" value={String(itemForm.displayOrder)} onChange={(value) => setItemForm({ ...itemForm, displayOrder: Number(value) || 0 })}/>
          <Area label="Short description" value={itemForm.shortDescription} onChange={(value) => setItemForm({ ...itemForm, shortDescription: value || null })}/>
          <Area label="Description" value={itemForm.description} onChange={(value) => setItemForm({ ...itemForm, description: value || null })}/>
          <Field label="Alt text" value={itemForm.altText} onChange={(value) => setItemForm({ ...itemForm, altText: value || null })}/>
          <div className="flex gap-6 items-end pb-2"><Check label="Active" checked={itemForm.isActive} onChange={(isActive) => setItemForm({ ...itemForm, isActive })}/><Check label="Featured on Home" checked={itemForm.isFeatured} onChange={(isFeatured) => setItemForm({ ...itemForm, isFeatured })}/></div>
        </div>
        <div className="border border-border bg-background p-4 flex flex-col sm:flex-row gap-4 items-start">
          {previewUrl ? <img src={previewUrl} alt="Portfolio upload preview" className="w-28 aspect-[3/4] object-cover"/> : itemForm.imageFileId ? <AdminPortfolioImage imageFileId={itemForm.imageFileId} alt={itemForm.altText || itemForm.title} className="w-28 aspect-[3/4] object-cover"/> : <div className="w-28 aspect-[3/4] bg-muted flex items-center justify-center text-[9px] text-muted-foreground">No image</div>}
          <label className="inline-flex items-center gap-2 border border-border px-4 py-2.5 text-[10px] cursor-pointer hover:border-foreground"><Upload size={12}/>{uploading ? "Uploading..." : "Upload JPG, PNG or WebP"}<input type="file" accept="image/jpeg,image/png,image/webp" disabled={uploading} onChange={(event) => void uploadImage(event.target.files?.[0])} className="sr-only"/></label>
        </div>
        <button disabled={saving || uploading} className="bg-foreground text-primary-foreground px-6 py-2.5 text-[10px] disabled:opacity-50">{saving ? "Saving..." : "Save item"}</button>
      </form> : null}
      <div className="space-y-3">{items.map((item) => <article key={item.id} className={`bg-card border p-4 flex flex-col sm:flex-row gap-4 ${item.archivedAt ? "border-amber-300 opacity-75" : "border-border"}`}>
        {item.imageFileId ? <AdminPortfolioImage imageFileId={item.imageFileId} alt={item.altText} className="w-20 aspect-[3/4] object-cover bg-muted"/> : <div className="w-20 aspect-[3/4] bg-muted"/>}
        <div className="flex-1"><div className="flex flex-wrap gap-2 items-center"><h3 className="font-serif text-lg font-light">{item.title}</h3><Badge text={item.categoryName}/>{item.archivedAt ? <Badge text="Archived"/> : null}{!item.isActive ? <Badge text="Hidden"/> : null}{item.isFeatured ? <Badge text="Featured"/> : null}</div><p className="text-[10px] text-muted-foreground mt-2">Order: {item.displayOrder} · /{item.slug}</p></div>
        <div className="flex flex-wrap gap-2 items-start"><Action text="Edit" onClick={() => editItem(item)}/>{!item.archivedAt ? <><Action text={item.isActive ? "Hide" : "Activate"} onClick={() => void toggleItem(item, "isActive")}/><Action text={item.isFeatured ? "Unfeature" : "Feature"} onClick={() => void toggleItem(item, "isFeatured")}/></> : null}<Action text="Archive" danger onClick={() => void removeItem(item)}/></div>
      </article>)}</div>
    </> : <>
      {categoryForm ? <form onSubmit={saveCategory} className="bg-card border border-border p-5 space-y-4"><div className="flex justify-between"><h2 className="font-serif text-xl font-light">{editingCategoryId ? "Edit category" : "New category"}</h2><button type="button" onClick={() => setCategoryForm(null)} className="text-[10px] text-muted-foreground">Close</button></div><div className="grid grid-cols-1 md:grid-cols-2 gap-4"><Field label="Name" required value={categoryForm.name} onChange={(name) => setCategoryForm({ ...categoryForm, name })}/><Field label="Slug (optional)" value={categoryForm.slug} onChange={(slug) => setCategoryForm({ ...categoryForm, slug: slug || null })}/><Area label="Description" value={categoryForm.description} onChange={(description) => setCategoryForm({ ...categoryForm, description: description || null })}/><Field label="Display order" type="number" value={String(categoryForm.displayOrder)} onChange={(value) => setCategoryForm({ ...categoryForm, displayOrder: Number(value) || 0 })}/><Check label="Active" checked={categoryForm.isActive} onChange={(isActive) => setCategoryForm({ ...categoryForm, isActive })}/></div><button disabled={saving} className="bg-foreground text-primary-foreground px-6 py-2.5 text-[10px] disabled:opacity-50">{saving ? "Saving..." : "Save category"}</button></form> : null}
      <div className="space-y-3">{categories.map((category) => <article key={category.id} className={`bg-card border p-5 flex flex-col sm:flex-row justify-between gap-4 ${category.archivedAt ? "border-amber-300 opacity-75" : "border-border"}`}><div><div className="flex gap-2 items-center"><h3 className="font-serif text-lg font-light">{category.name}</h3>{category.archivedAt ? <Badge text="Archived"/> : null}{!category.isActive ? <Badge text="Hidden"/> : null}</div><p className="text-[10px] text-muted-foreground mt-2">/{category.slug} · Order: {category.displayOrder} · Items: {category.itemCount}</p></div><div className="flex gap-2 items-start"><Action text="Edit" onClick={() => editCategory(category)}/>{!category.archivedAt ? <Action text={category.isActive ? "Hide" : "Activate"} onClick={() => void toggleCategory(category)}/> : null}<Action text={category.itemCount ? "Archive" : "Delete"} danger onClick={() => void removeCategory(category)}/></div></article>)}</div>
    </>}
  </div>;
}

function Field({ label, value, onChange, required, type = "text" }: { label: string; value: string | null; onChange(value: string): void; required?: boolean; type?: "text" | "number" }) { return <label className="text-[10px] text-muted-foreground"><span className="block mb-1.5">{label}</span><input className={input} type={type} min={type === "number" ? 0 : undefined} required={required} value={value ?? ""} onChange={(event) => onChange(event.target.value)}/></label>; }
function Area({ label, value, onChange }: { label: string; value: string | null; onChange(value: string): void }) { return <label className="text-[10px] text-muted-foreground md:col-span-2"><span className="block mb-1.5">{label}</span><textarea className={`${input} resize-y`} rows={3} value={value ?? ""} onChange={(event) => onChange(event.target.value)}/></label>; }
function Check({ label, checked, onChange }: { label: string; checked: boolean; onChange(value: boolean): void }) { return <label className="text-[10px] text-foreground flex gap-2 items-center"><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)}/>{label}</label>; }
function Action({ text, onClick, danger }: { text: string; onClick(): void; danger?: boolean }) { return <button type="button" onClick={onClick} className={`border px-3 py-2 text-[10px] ${danger ? "border-destructive/30 text-destructive" : "border-border hover:border-foreground"}`}>{text}</button>; }
function Badge({ text }: { text: string }) { return <span className="text-[9px] px-2 py-0.5 bg-secondary text-muted-foreground">{text}</span>; }
function AdminPortfolioImage({ imageFileId, alt, className }: { imageFileId: string; alt: string; className: string }) {
  const [src, setSrc] = useState<string | null>(null);
  useEffect(() => {
    let objectUrl: string | null = null;
    let active = true;
    getAdminPortfolioImage(imageFileId).then((blob) => {
      if (!active) return;
      objectUrl = URL.createObjectURL(blob);
      setSrc(objectUrl);
    }).catch(() => undefined);
    return () => { active = false; if (objectUrl) URL.revokeObjectURL(objectUrl); };
  }, [imageFileId]);
  return src ? <img src={src} alt={alt} className={className}/> : <div className={`${className} bg-muted`}/>;
}
const cleanItem = (form: SavePortfolioItemRequest): SavePortfolioItemRequest => ({ ...form, slug: form.slug?.trim() || null, title: form.title.trim(), shortDescription: form.shortDescription?.trim() || null, description: form.description?.trim() || null, altText: form.altText?.trim() || null });
const cleanCategory = (form: SavePortfolioCategoryRequest): SavePortfolioCategoryRequest => ({ ...form, slug: form.slug?.trim() || null, name: form.name.trim(), description: form.description?.trim() || null });
const itemRequest = (item: AdminPortfolioItem): SavePortfolioItemRequest => ({ categoryId: item.categoryId, slug: item.slug, title: item.title, shortDescription: item.shortDescription, description: item.description, imageFileId: item.imageFileId, altText: item.altText, isActive: item.isActive, isFeatured: item.isFeatured, displayOrder: item.displayOrder });
const categoryRequest = (category: AdminPortfolioCategory): SavePortfolioCategoryRequest => ({ slug: category.slug, name: category.name, description: category.description, isActive: category.isActive, displayOrder: category.displayOrder });
const sortItems = (items: AdminPortfolioItem[]) => items.sort((a, b) => Number(Boolean(a.archivedAt)) - Number(Boolean(b.archivedAt)) || a.displayOrder - b.displayOrder || a.title.localeCompare(b.title));
const sortCategories = (items: AdminPortfolioCategory[]) => items.sort((a, b) => Number(Boolean(a.archivedAt)) - Number(Boolean(b.archivedAt)) || a.displayOrder - b.displayOrder || a.name.localeCompare(b.name));
