import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from "react";
import { Plus } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  createAdminRepeatableContent,
  deleteAdminRepeatableContent,
  getAdminRepeatableContent,
  updateAdminRepeatableContent,
} from "../../api/repeatableContentApi";
import { useRepeatableContent } from "../repeatableContent/RepeatableContentContext";
import type {
  AdminRepeatableContentItem,
  SaveRepeatableContentItemRequest,
} from "../types";

interface AdminRepeatableContentPanelProps {
  onUnauthorized(): void;
}

type TextFieldName =
  | "groupKey"
  | "itemKey"
  | "title"
  | "subtitle"
  | "body"
  | "label"
  | "value"
  | "iconKey"
  | "url"
  | "location"
  | "service";

const GROUPS = [
  {
    key: "process-steps",
    label: "Process Steps",
    hint: "Home process cards. Uses label as the visible step number.",
  },
  {
    key: "studio-values",
    label: "Studio Values",
    hint: "Home and About value cards. Uses iconKey: award, heart, shield or check.",
  },
  {
    key: "testimonials",
    label: "Testimonials",
    hint: "Client review cards. Uses title as client name, body as review text and rating/location/service fields.",
  },
  {
    key: "privacy-sections",
    label: "Privacy Sections",
    hint: "Detailed Privacy Policy subsections. Uses title and body.",
  },
] as const;

type GroupFilter = "all" | (typeof GROUPS)[number]["key"];

const inputClassName =
  "w-full border border-border bg-background px-3 py-2.5 text-[11px] text-foreground focus:outline-none focus:border-accent transition-colors font-sans";

export function AdminRepeatableContentPanel({ onUnauthorized }: AdminRepeatableContentPanelProps) {
  const { refresh: refreshPublicContent } = useRepeatableContent();
  const [items, setItems] = useState<AdminRepeatableContentItem[]>([]);
  const [filter, setFilter] = useState<GroupFilter>("all");
  const [form, setForm] = useState<SaveRepeatableContentItemRequest | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    void loadItems();
  }, []);

  const visibleItems = useMemo(
    () => (filter === "all" ? items : items.filter((item) => item.groupKey === filter)),
    [filter, items],
  );

  async function loadItems() {
    setIsLoading(true);
    setError(null);
    try {
      setItems((await getAdminRepeatableContent()).sort(compareItems));
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setIsLoading(false);
    }
  }

  function handleRequestError(reason: unknown) {
    if (reason instanceof ApiError && (reason.status === 401 || reason.status === 403)) {
      onUnauthorized();
      return;
    }

    setError(getErrorMessage(reason));
  }

  function startCreate() {
    const groupKey = filter === "all" ? GROUPS[0].key : filter;
    const nextOrder = items
      .filter((item) => item.groupKey === groupKey)
      .reduce((highest, item) => Math.max(highest, item.displayOrder + 1), 0);

    setEditingId(null);
    setForm({
      groupKey,
      itemKey: "",
      title: null,
      subtitle: null,
      body: null,
      label: null,
      value: null,
      iconKey: null,
      url: null,
      rating: null,
      location: null,
      service: null,
      displayOrder: nextOrder,
      isActive: true,
    });
    setError(null);
    setMessage(null);
  }

  function startEdit(item: AdminRepeatableContentItem) {
    setEditingId(item.id);
    setForm(toSaveRequest(item));
    setError(null);
    setMessage(null);
  }

  function updateTextField(field: TextFieldName, value: string) {
    setForm((current) =>
      current
        ? {
            ...current,
            [field]: field === "groupKey" || field === "itemKey" ? value : value || null,
          }
        : current,
    );
    setMessage(null);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!form || isSaving) {
      return;
    }

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      const request = cleanRequest(form);
      const saved = editingId
        ? await updateAdminRepeatableContent(editingId, request)
        : await createAdminRepeatableContent(request);
      setItems((current) =>
        [...current.filter((item) => item.id !== saved.id), saved].sort(compareItems),
      );
      setEditingId(saved.id);
      setForm(toSaveRequest(saved));
      await refreshPublicContent().catch(() => undefined);
      setMessage(editingId ? "Repeatable content item updated." : "Repeatable content item created.");
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setIsSaving(false);
    }
  }

  async function toggleItem(item: AdminRepeatableContentItem) {
    setError(null);
    setMessage(null);
    try {
      const saved = await updateAdminRepeatableContent(item.id, {
        ...toSaveRequest(item),
        isActive: !item.isActive,
      });
      setItems((current) =>
        current.map((candidate) => (candidate.id === saved.id ? saved : candidate)).sort(compareItems),
      );
      await refreshPublicContent().catch(() => undefined);
      setMessage(`${formatItemTitle(saved)} updated.`);
    } catch (reason: unknown) {
      handleRequestError(reason);
    }
  }

  async function archiveItem(item: AdminRepeatableContentItem) {
    if (!window.confirm(`Archive ${formatItemTitle(item)}?`)) {
      return;
    }

    setError(null);
    setMessage(null);
    try {
      const result = await deleteAdminRepeatableContent(item.id);
      await loadItems();
      if (editingId === item.id) {
        setEditingId(null);
        setForm(null);
      }
      await refreshPublicContent().catch(() => undefined);
      setMessage(result.message);
    } catch (reason: unknown) {
      handleRequestError(reason);
    }
  }

  if (isLoading) {
    return (
      <div className="bg-card border border-border p-6 text-[11px] text-muted-foreground">
        Loading repeatable content...
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-1">
          <p className="text-[11px] text-muted-foreground font-sans">
            Manage repeatable public blocks used by Home, About and Privacy pages.
          </p>
          <p className="text-[10px] text-muted-foreground/80 font-sans">
            Keep keys lowercase with letters, numbers and hyphens.
          </p>
        </div>
        <button
          type="button"
          onClick={startCreate}
          className="inline-flex items-center gap-2 bg-foreground text-primary-foreground px-4 py-2.5 text-[10px] tracking-wide hover:bg-accent transition-colors"
        >
          <Plus size={12} /> Add item
        </button>
      </div>

      <div className="flex flex-wrap gap-2">
        <FilterButton active={filter === "all"} onClick={() => setFilter("all")}>All groups</FilterButton>
        {GROUPS.map((group) => (
          <FilterButton key={group.key} active={filter === group.key} onClick={() => setFilter(group.key)}>
            {group.label}
          </FilterButton>
        ))}
      </div>

      {filter !== "all" ? (
        <p className="border border-border bg-card px-4 py-3 text-[10px] text-muted-foreground font-sans">
          {GROUPS.find((group) => group.key === filter)?.hint}
        </p>
      ) : null}

      {error ? <p role="alert" className="border border-destructive/30 bg-card px-4 py-3 text-[11px] text-destructive">{error}</p> : null}
      {message ? <p role="status" className="border border-emerald-300 bg-emerald-50 px-4 py-3 text-[11px] text-emerald-700">{message}</p> : null}

      {form ? (
        <form onSubmit={handleSubmit} className="bg-card border border-border p-5 lg:p-6 space-y-5">
          <div className="flex items-center justify-between gap-3">
            <div>
              <h2 className="font-serif text-[1.25rem] font-light">{editingId ? "Edit repeatable item" : "New repeatable item"}</h2>
              <p className="text-[10px] text-muted-foreground mt-1 font-sans">Fields that are not used by a selected group can stay empty.</p>
            </div>
            <button type="button" onClick={() => setForm(null)} className="text-[10px] text-muted-foreground hover:text-foreground">Close</button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <label className="text-[11px] text-muted-foreground font-sans">
              <span className="block mb-1.5">Group</span>
              <select
                value={form.groupKey}
                onChange={(event) => updateTextField("groupKey", event.target.value)}
                className={inputClassName}
              >
                {GROUPS.map((group) => (
                  <option key={group.key} value={group.key}>{group.label}</option>
                ))}
              </select>
            </label>
            <Field label="Item key" required value={form.itemKey} onChange={(value) => updateTextField("itemKey", value)} />
            <Field label="Title" value={form.title} onChange={(value) => updateTextField("title", value)} />
            <Field label="Subtitle" value={form.subtitle} onChange={(value) => updateTextField("subtitle", value)} />
            <TextArea label="Body" value={form.body} onChange={(value) => updateTextField("body", value)} />
            <Field label="Label / step number" value={form.label} onChange={(value) => updateTextField("label", value)} />
            <Field label="Value" value={form.value} onChange={(value) => updateTextField("value", value)} />
            <Field label="Icon key" value={form.iconKey} onChange={(value) => updateTextField("iconKey", value)} />
            <Field label="URL" type="url" value={form.url} onChange={(value) => updateTextField("url", value)} />
            <Field
              label="Rating"
              type="number"
              min={1}
              max={5}
              value={form.rating === null ? "" : String(form.rating)}
              onChange={(value) => setForm({ ...form, rating: toNullableNumber(value) })}
            />
            <Field label="Location" value={form.location} onChange={(value) => updateTextField("location", value)} />
            <Field label="Service" value={form.service} onChange={(value) => updateTextField("service", value)} />
            <Field
              label="Display order"
              type="number"
              min={0}
              value={String(form.displayOrder)}
              onChange={(value) => setForm({ ...form, displayOrder: Number(value) || 0 })}
            />
            <label className="flex items-center gap-3 border border-border bg-background px-3 py-3 text-[11px] text-foreground font-sans">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(event) => setForm({ ...form, isActive: event.target.checked })}
                className="accent-foreground"
              />
              Active on public site
            </label>
          </div>

          <button
            type="submit"
            disabled={isSaving}
            className="bg-foreground text-primary-foreground px-6 py-2.5 text-[11px] tracking-wide hover:bg-accent disabled:opacity-50 transition-colors font-sans"
          >
            {isSaving ? "Saving..." : "Save item"}
          </button>
        </form>
      ) : null}

      <div className="space-y-3">
        {visibleItems.length === 0 ? (
          <div className="bg-card border border-border p-6 text-[11px] text-muted-foreground font-sans">
            No repeatable content items found.
          </div>
        ) : null}
        {visibleItems.map((item) => (
          <article
            key={item.id}
            className={`bg-card border p-5 lg:p-6 grid grid-cols-1 lg:grid-cols-[minmax(0,1fr)_auto] gap-4 lg:gap-6 ${item.archivedAt ? "border-amber-300 opacity-70" : "border-border"}`}
          >
            <div className="min-w-0">
              <div className="flex flex-wrap gap-2 items-center">
                <h3 className="font-serif text-lg font-light">{formatItemTitle(item)}</h3>
                <Badge>{getGroupLabel(item.groupKey)}</Badge>
                {!item.isActive ? <Badge>Hidden</Badge> : null}
                {item.archivedAt ? <Badge>Archived</Badge> : null}
              </div>
              <p className="text-[11px] text-muted-foreground mt-2 leading-relaxed line-clamp-2">{item.body ?? item.subtitle ?? item.value ?? "No body text"}</p>
              <p className="text-[9px] text-muted-foreground mt-2 font-sans">
                {item.groupKey}/{item.itemKey} · Order: {item.displayOrder}
                {item.label ? ` · Label: ${item.label}` : ""}
                {item.iconKey ? ` · Icon: ${item.iconKey}` : ""}
                {item.rating ? ` · Rating: ${item.rating}` : ""}
              </p>
            </div>
            <div className="flex flex-row flex-nowrap gap-2 items-start justify-start lg:justify-end shrink-0">
              <ActionButton onClick={() => startEdit(item)}>Edit</ActionButton>
              {!item.archivedAt ? (
                <ActionButton onClick={() => void toggleItem(item)}>{item.isActive ? "Hide" : "Show"}</ActionButton>
              ) : null}
              <ActionButton danger onClick={() => void archiveItem(item)}>Archive</ActionButton>
            </div>
          </article>
        ))}
      </div>
    </div>
  );
}

function FilterButton({ active, onClick, children }: { active: boolean; onClick(): void; children: ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`border px-3 py-2 text-[10px] font-sans transition-colors ${active ? "border-foreground bg-foreground text-primary-foreground" : "border-border bg-card text-muted-foreground hover:text-foreground"}`}
    >
      {children}
    </button>
  );
}

function Field({
  label,
  value,
  onChange,
  required = false,
  type = "text",
  min,
  max,
}: {
  label: string;
  value: string | null;
  onChange(value: string): void;
  required?: boolean;
  type?: "text" | "number" | "url";
  min?: number;
  max?: number;
}) {
  return (
    <label className="text-[11px] text-muted-foreground font-sans">
      <span className="block mb-1.5">{label}</span>
      <input
        type={type}
        required={required}
        min={min}
        max={max}
        value={value ?? ""}
        onChange={(event) => onChange(event.target.value)}
        className={inputClassName}
      />
    </label>
  );
}

function TextArea({ label, value, onChange }: { label: string; value: string | null; onChange(value: string): void }) {
  return (
    <label className="text-[11px] text-muted-foreground font-sans md:col-span-2">
      <span className="block mb-1.5">{label}</span>
      <textarea
        rows={4}
        value={value ?? ""}
        onChange={(event) => onChange(event.target.value)}
        className={`${inputClassName} resize-y`}
      />
    </label>
  );
}

function ActionButton({ children, onClick, danger = false }: { children: ReactNode; onClick(): void; danger?: boolean }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`border px-3 py-2 min-w-[64px] text-center text-[10px] font-sans transition-colors ${danger ? "text-destructive border-destructive/30 hover:border-destructive" : "border-border hover:border-foreground"}`}
    >
      {children}
    </button>
  );
}

function Badge({ children }: { children: ReactNode }) {
  return <span className="text-[9px] bg-secondary px-2 py-0.5 text-muted-foreground font-sans">{children}</span>;
}

function cleanRequest(request: SaveRepeatableContentItemRequest): SaveRepeatableContentItemRequest {
  return {
    groupKey: request.groupKey.trim(),
    itemKey: request.itemKey.trim(),
    title: cleanOptional(request.title),
    subtitle: cleanOptional(request.subtitle),
    body: cleanOptional(request.body),
    label: cleanOptional(request.label),
    value: cleanOptional(request.value),
    iconKey: cleanOptional(request.iconKey),
    url: cleanOptional(request.url),
    rating: request.rating,
    location: cleanOptional(request.location),
    service: cleanOptional(request.service),
    displayOrder: request.displayOrder,
    isActive: request.isActive,
  };
}

function cleanOptional(value: string | null): string | null {
  return value?.trim() || null;
}

function toSaveRequest(item: AdminRepeatableContentItem): SaveRepeatableContentItemRequest {
  return {
    groupKey: item.groupKey,
    itemKey: item.itemKey,
    title: item.title,
    subtitle: item.subtitle,
    body: item.body,
    label: item.label,
    value: item.value,
    iconKey: item.iconKey,
    url: item.url,
    rating: item.rating,
    location: item.location,
    service: item.service,
    displayOrder: item.displayOrder,
    isActive: item.isActive,
  };
}

function compareItems(a: AdminRepeatableContentItem, b: AdminRepeatableContentItem): number {
  return a.groupKey.localeCompare(b.groupKey) || a.displayOrder - b.displayOrder || a.itemKey.localeCompare(b.itemKey);
}

function formatItemTitle(item: AdminRepeatableContentItem): string {
  return item.title ?? item.itemKey;
}

function getGroupLabel(groupKey: string): string {
  return GROUPS.find((group) => group.key === groupKey)?.label ?? groupKey;
}

function toNullableNumber(value: string): number | null {
  if (!value.trim()) {
    return null;
  }

  const numberValue = Number(value);
  return Number.isFinite(numberValue) ? numberValue : null;
}

function getErrorMessage(reason: unknown): string {
  if (reason instanceof ApiError) {
    const validationMessages = reason.errors ? Object.values(reason.errors).flat() : [];
    if (validationMessages.length > 0) {
      return validationMessages.join(" ");
    }

    if (reason.status === 403) {
      return "Administrator authorization is required to edit repeatable content.";
    }

    return reason.message;
  }

  return "The repeatable content request could not be completed.";
}
