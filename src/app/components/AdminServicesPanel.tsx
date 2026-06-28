import { useEffect, useState, type FormEvent } from "react";
import { Plus, Trash2 } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  createAdminService,
  deleteAdminService,
  getAdminServices,
  updateAdminService,
} from "../../api/servicesApi";
import { useServices } from "../services/ServicesContext";
import type {
  AdminServiceOffering,
  SaveServiceOfferingRequest,
  ServicePriceOptionRequest,
} from "../types";

interface AdminServicesPanelProps {
  onUnauthorized(): void;
}

interface EditablePriceOption extends ServicePriceOptionRequest {
  key: string;
}

type EditorForm = Omit<SaveServiceOfferingRequest, "priceOptions"> & {
  priceOptions: EditablePriceOption[];
};

const inputClassName =
  "w-full border border-border bg-background px-3 py-2.5 text-[11px] text-foreground focus:outline-none focus:border-accent transition-colors font-sans";

export function AdminServicesPanel({ onUnauthorized }: AdminServicesPanelProps) {
  const { refresh: refreshPublicServices } = useServices();
  const [services, setServices] = useState<AdminServiceOffering[]>([]);
  const [form, setForm] = useState<EditorForm | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    void loadServices();
  }, []);

  async function loadServices() {
    setIsLoading(true);
    setError(null);
    try {
      setServices(await getAdminServices());
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setIsLoading(false);
    }
  }

  function handleRequestError(reason: unknown) {
    if (reason instanceof ApiError && reason.status === 401) {
      onUnauthorized();
      return;
    }

    setError(getErrorMessage(reason));
  }

  function startCreate() {
    const nextOrder = services.reduce(
      (highest, service) => Math.max(highest, service.displayOrder + 1),
      0,
    );
    setEditingId(null);
    setForm({
      slug: null,
      name: "",
      shortDescription: "",
      description: null,
      category: null,
      isActive: true,
      isFeatured: false,
      displayOrder: nextOrder,
      priceOptions: [],
      imageUrl: null,
    });
    setError(null);
    setMessage(null);
  }

  function startEdit(service: AdminServiceOffering) {
    setEditingId(service.id);
    setForm({
      slug: service.slug,
      name: service.name,
      shortDescription: service.shortDescription,
      description: service.description,
      category: service.category,
      isActive: service.isActive,
      isFeatured: service.isFeatured,
      displayOrder: service.displayOrder,
      priceOptions: service.priceOptions.map((option) => ({
        key: option.id ?? crypto.randomUUID(),
        label: option.label,
        description: option.description,
        priceText: option.priceText,
        displayOrder: option.displayOrder,
        isActive: option.isActive,
      })),
      imageUrl: service.imageUrl,
    });
    setError(null);
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
    const request = toSaveRequest(form);

    try {
      const saved = editingId
        ? await updateAdminService(editingId, request)
        : await createAdminService(request);
      setServices((current) =>
        [...current.filter((service) => service.id !== saved.id), saved].sort(compareServices),
      );
      setEditingId(saved.id);
      setForm(toEditorForm(saved));
      await refreshPublicServices().catch(() => undefined);
      setMessage(editingId ? "Service updated." : "Service created.");
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setIsSaving(false);
    }
  }

  async function toggleService(
    service: AdminServiceOffering,
    field: "isActive" | "isFeatured",
  ) {
    setError(null);
    setMessage(null);
    try {
      const saved = await updateAdminService(service.id, {
        ...toSaveRequest(toEditorForm(service)),
        [field]: !service[field],
      });
      setServices((current) =>
        current.map((item) => (item.id === saved.id ? saved : item)).sort(compareServices),
      );
      await refreshPublicServices().catch(() => undefined);
      setMessage(`${saved.name} updated.`);
    } catch (reason: unknown) {
      handleRequestError(reason);
    }
  }

  async function removeService(service: AdminServiceOffering) {
    if (!window.confirm(`Delete "${service.name}"? Used services will be archived instead.`)) {
      return;
    }

    setError(null);
    setMessage(null);
    try {
      const result = await deleteAdminService(service.id);
      if (result.deleted) {
        setServices((current) => current.filter((item) => item.id !== service.id));
      } else {
        await loadServices();
      }
      if (editingId === service.id) {
        setEditingId(null);
        setForm(null);
      }
      await refreshPublicServices().catch(() => undefined);
      setMessage(result.message);
    } catch (reason: unknown) {
      handleRequestError(reason);
    }
  }

  function updatePriceOption(
    key: string,
    updates: Partial<EditablePriceOption>,
  ) {
    setForm((current) =>
      current
        ? {
            ...current,
            priceOptions: current.priceOptions.map((option) =>
              option.key === key ? { ...option, ...updates } : option,
            ),
          }
        : current,
    );
  }

  if (isLoading) {
    return <div className="bg-card border border-border p-6 text-[11px] text-muted-foreground">Loading services...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-[11px] text-muted-foreground font-sans">
          Active services appear on the public site and in the enquiry form.
        </p>
        <button
          type="button"
          onClick={startCreate}
          className="inline-flex items-center gap-2 bg-foreground text-primary-foreground px-4 py-2.5 text-[10px] tracking-wide hover:bg-accent transition-colors"
        >
          <Plus size={12} /> Add service
        </button>
      </div>

      {error ? <p role="alert" className="border border-destructive/30 bg-card px-4 py-3 text-[11px] text-destructive">{error}</p> : null}
      {message ? <p role="status" className="border border-emerald-300 bg-emerald-50 px-4 py-3 text-[11px] text-emerald-700">{message}</p> : null}

      {form ? (
        <form onSubmit={handleSubmit} className="bg-card border border-border p-5 lg:p-6 space-y-5">
          <div className="flex items-center justify-between gap-3">
            <h2 className="font-serif text-[1.25rem] font-light">{editingId ? "Edit service" : "New service"}</h2>
            <button type="button" onClick={() => setForm(null)} className="text-[10px] text-muted-foreground hover:text-foreground">Close</button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Field label="Name" required value={form.name} onChange={(value) => setForm({ ...form, name: value })} />
            <Field label="Slug (optional for new services)" value={form.slug} onChange={(value) => setForm({ ...form, slug: value || null })} />
            <Field label="Category" value={form.category} onChange={(value) => setForm({ ...form, category: value || null })} />
            <Field label="Display order" type="number" value={String(form.displayOrder)} onChange={(value) => setForm({ ...form, displayOrder: Number(value) || 0 })} />
            <TextArea label="Short description" required value={form.shortDescription} onChange={(value) => setForm({ ...form, shortDescription: value })} />
            <TextArea label="Description" value={form.description} onChange={(value) => setForm({ ...form, description: value || null })} />
            <label className="flex items-center gap-3 text-[11px] text-foreground">
              <input type="checkbox" checked={form.isActive} onChange={(event) => setForm({ ...form, isActive: event.target.checked })} className="accent-foreground" /> Active
            </label>
            <label className="flex items-center gap-3 text-[11px] text-foreground">
              <input type="checkbox" checked={form.isFeatured} onChange={(event) => setForm({ ...form, isFeatured: event.target.checked })} className="accent-foreground" /> Featured on Home
            </label>
          </div>

          <div className="border-t border-border pt-5 space-y-4">
            <div className="flex items-center justify-between gap-3">
              <h3 className="text-[11px] font-medium tracking-wide">Price options</h3>
              <button
                type="button"
                onClick={() => setForm({
                  ...form,
                  priceOptions: [...form.priceOptions, {
                    key: crypto.randomUUID(),
                    label: "",
                    description: null,
                    priceText: "",
                    displayOrder: form.priceOptions.length,
                    isActive: true,
                  }],
                })}
                className="inline-flex items-center gap-1.5 text-[10px] text-accent hover:text-foreground"
              >
                <Plus size={11} /> Add price
              </button>
            </div>

            {form.priceOptions.length === 0 ? (
              <p className="text-[10px] text-muted-foreground">No price options. This service can still be saved.</p>
            ) : null}
            {form.priceOptions.map((option) => (
              <div key={option.key} className="grid grid-cols-1 md:grid-cols-[1fr_1fr_90px_auto] gap-3 border border-border bg-background p-3">
                <Field label="Label" required value={option.label} onChange={(value) => updatePriceOption(option.key, { label: value })} />
                <Field label="Price text" required value={option.priceText} onChange={(value) => updatePriceOption(option.key, { priceText: value })} />
                <Field label="Order" type="number" value={String(option.displayOrder)} onChange={(value) => updatePriceOption(option.key, { displayOrder: Number(value) || 0 })} />
                <div className="flex items-end gap-2 pb-2">
                  <label className="text-[10px] text-muted-foreground flex items-center gap-1.5">
                    <input type="checkbox" checked={option.isActive} onChange={(event) => updatePriceOption(option.key, { isActive: event.target.checked })} /> Active
                  </label>
                  <button type="button" onClick={() => setForm({ ...form, priceOptions: form.priceOptions.filter((item) => item.key !== option.key) })} className="text-destructive" aria-label={`Remove price ${option.label || "option"}`}>
                    <Trash2 size={12} />
                  </button>
                </div>
                <div className="md:col-span-4">
                  <Field label="Price description (optional)" value={option.description} onChange={(value) => updatePriceOption(option.key, { description: value || null })} />
                </div>
              </div>
            ))}
          </div>

          <button type="submit" disabled={isSaving} className="bg-foreground text-primary-foreground px-6 py-2.5 text-[10px] tracking-wide hover:bg-accent disabled:opacity-50">
            {isSaving ? "Saving..." : "Save service"}
          </button>
        </form>
      ) : null}

      <div className="space-y-3">
        {services.map((service) => (
          <article key={service.id} className={`bg-card border p-5 ${service.archivedAt ? "border-amber-300 opacity-75" : "border-border"}`}>
            <div className="flex flex-col lg:flex-row lg:items-start justify-between gap-4">
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2 mb-2">
                  <h3 className="font-serif text-[1.15rem] font-light">{service.name}</h3>
                  <span className="text-[9px] px-2 py-0.5 bg-secondary text-muted-foreground">/{service.slug}</span>
                  {service.archivedAt ? <span className="text-[9px] px-2 py-0.5 bg-amber-100 text-amber-700">Archived</span> : null}
                  {!service.isActive ? <span className="text-[9px] px-2 py-0.5 bg-slate-100 text-slate-600">Hidden</span> : null}
                </div>
                <p className="text-[11px] text-muted-foreground max-w-2xl">{service.shortDescription}</p>
                <div className="mt-3 flex flex-wrap gap-3 text-[9px] text-muted-foreground">
                  <span>Order: {service.displayOrder}</span>
                  <span>Prices: {service.priceOptions.length}</span>
                  <span>Orders: {service.usageCount}</span>
                </div>
              </div>
              <div className="flex flex-wrap gap-2">
                <button type="button" onClick={() => startEdit(service)} className="border border-border px-3 py-2 text-[10px] hover:border-foreground">Edit</button>
                {!service.archivedAt ? (
                  <>
                    <button type="button" onClick={() => void toggleService(service, "isActive")} className="border border-border px-3 py-2 text-[10px] hover:border-foreground">
                      {service.isActive ? "Hide" : "Activate"}
                    </button>
                    <button type="button" onClick={() => void toggleService(service, "isFeatured")} className="border border-border px-3 py-2 text-[10px] hover:border-foreground">
                      {service.isFeatured ? "Unfeature" : "Feature"}
                    </button>
                  </>
                ) : null}
                <button type="button" onClick={() => void removeService(service)} className="border border-destructive/30 px-3 py-2 text-[10px] text-destructive hover:border-destructive">
                  {service.canDelete ? "Delete" : "Archive"}
                </button>
              </div>
            </div>
          </article>
        ))}
      </div>
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  required = false,
  type = "text",
}: {
  label: string;
  value: string | null;
  onChange(value: string): void;
  required?: boolean;
  type?: "text" | "number";
}) {
  return (
    <label className="text-[10px] text-muted-foreground font-sans">
      <span className="block mb-1.5">{label}</span>
      <input type={type} min={type === "number" ? 0 : undefined} required={required} value={value ?? ""} onChange={(event) => onChange(event.target.value)} className={inputClassName} />
    </label>
  );
}

function TextArea({ label, value, onChange, required = false }: { label: string; value: string | null; onChange(value: string): void; required?: boolean }) {
  return (
    <label className="text-[10px] text-muted-foreground font-sans md:col-span-2">
      <span className="block mb-1.5">{label}</span>
      <textarea rows={3} required={required} value={value ?? ""} onChange={(event) => onChange(event.target.value)} className={`${inputClassName} resize-y`} />
    </label>
  );
}

function toEditorForm(service: AdminServiceOffering): EditorForm {
  return {
    slug: service.slug,
    name: service.name,
    shortDescription: service.shortDescription,
    description: service.description,
    category: service.category,
    isActive: service.isActive,
    isFeatured: service.isFeatured,
    displayOrder: service.displayOrder,
    priceOptions: service.priceOptions.map((option) => ({
      key: option.id ?? crypto.randomUUID(),
      label: option.label,
      description: option.description,
      priceText: option.priceText,
      displayOrder: option.displayOrder,
      isActive: option.isActive,
    })),
    imageUrl: service.imageUrl,
  };
}

function toSaveRequest(form: EditorForm): SaveServiceOfferingRequest {
  return {
    ...form,
    slug: form.slug?.trim() || null,
    name: form.name.trim(),
    shortDescription: form.shortDescription.trim(),
    description: form.description?.trim() || null,
    category: form.category?.trim() || null,
    imageUrl: form.imageUrl?.trim() || null,
    priceOptions: form.priceOptions.map(({ key: _key, ...option }) => ({
      ...option,
      label: option.label.trim(),
      description: option.description?.trim() || null,
      priceText: option.priceText.trim(),
    })),
  };
}

function compareServices(left: AdminServiceOffering, right: AdminServiceOffering): number {
  if (Boolean(left.archivedAt) !== Boolean(right.archivedAt)) {
    return left.archivedAt ? 1 : -1;
  }
  return left.displayOrder - right.displayOrder || left.name.localeCompare(right.name);
}

function getErrorMessage(reason: unknown): string {
  if (reason instanceof ApiError) {
    const validation = reason.errors ? Object.values(reason.errors).flat().find(Boolean) : undefined;
    return validation ?? reason.message;
  }
  return "The service request could not be completed. Please try again.";
}
