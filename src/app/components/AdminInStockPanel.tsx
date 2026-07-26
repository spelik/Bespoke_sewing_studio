import { useEffect, useId, useRef, useState, type FormEvent } from "react";
import { Plus, Upload } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  archiveInStockItem,
  createInStockItem,
  deleteInStockImage,
  getAdminInStockImageBlob,
  getAdminInStockItems,
  reorderInStockImages,
  restoreInStockItem,
  updateInStockImage,
  updateInStockItem,
  uploadInStockImage,
} from "../../api/inStockApi";
import type {
  AdminInStockImage,
  AdminInStockItem,
  InStockItemStatus,
  SaveInStockItemRequest,
} from "../types";
import { runSequentialUploads } from "../uploads/runSequentialUploads";
import {
  createEmptyUploadQueue,
  isUploadBusy,
  type UploadQueueState,
} from "../uploads/uploadProgressMachine";
import { AdminConfirmDialog } from "./AdminUi";
import {
  buildInStockUploadEntryId,
  buildPendingPreviewAlt,
  completeInStockUploadFollowUp,
  createEmptyInStockForm,
  createPendingFilePreview,
  filterAdminInStockItems,
  formatInStockFileSize,
  formatInStockPriceGbp,
  getPrimaryInStockImage,
  getRestoreInStockOwnerMessage,
  IN_STOCK_LABEL_LOADING,
  IN_STOCK_LABEL_SAVING,
  IN_STOCK_META_SEPARATOR,
  itemToInStockForm,
  mergeFieldErrors,
  parseInStockPriceInput,
  planOrderedImageIdsAfterMove,
  removeSnapshotFilesFromPending,
  revokeAllPendingFilePreviews,
  revokePendingFilePreview,
  selectActiveUploadItem,
  shouldShowAdminInStockEmptyState,
  sortInStockImages,
  toInStockPriceString,
  validateInStockForm,
  type PendingFilePreview,
} from "./adminInStockHelpers";
import { UploadProgressControl } from "./UploadProgressControl";

interface Props {
  onUnauthorized(): void;
}

const input =
  "w-full border border-border bg-background px-3 py-2.5 text-[11px] focus:outline-none focus:border-accent";

type ConfirmState =
  | { type: "archive"; item: AdminInStockItem }
  | { type: "restore"; item: AdminInStockItem }
  | { type: "deleteImage"; itemId: string; image: AdminInStockImage }
  | null;

export function AdminInStockPanel({ onUnauthorized }: Props) {
  const [items, setItems] = useState<AdminInStockItem[]>([]);
  const [showArchived, setShowArchived] = useState(false);
  const [form, setForm] = useState<SaveInStockItemRequest | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingItem, setEditingItem] = useState<AdminInStockItem | null>(null);
  const [priceText, setPriceText] = useState("0.00");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [imageFieldErrors, setImageFieldErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [hasLoadedSuccessfully, setHasLoadedSuccessfully] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [confirm, setConfirm] = useState<ConfirmState>(null);
  const [confirmBusy, setConfirmBusy] = useState(false);
  const [uploadQueue, setUploadQueue] = useState<UploadQueueState>(createEmptyUploadQueue());
  const [queueStatus, setQueueStatus] = useState<string | null>(null);
  const [pendingPreviews, setPendingPreviews] = useState<PendingFilePreview[]>([]);
  const [failedFiles, setFailedFiles] = useState<File[]>([]);
  const [reorderBusy, setReorderBusy] = useState(false);
  const [imageActionBusyId, setImageActionBusyId] = useState<string | null>(null);
  const uploadAbortRef = useRef<AbortController | null>(null);
  const altSaveGenerationRef = useRef<Map<string, number>>(new Map());
  const pendingPreviewsRef = useRef<PendingFilePreview[]>([]);

  useEffect(() => {
    pendingPreviewsRef.current = pendingPreviews;
  }, [pendingPreviews]);

  useEffect(() => {
    void load();
    return () => {
      uploadAbortRef.current?.abort();
      revokeAllPendingFilePreviews(pendingPreviewsRef.current);
    };
  }, []);

  function clearPendingPreviews() {
    revokeAllPendingFilePreviews(pendingPreviewsRef.current);
    pendingPreviewsRef.current = [];
    setPendingPreviews([]);
  }

  async function load() {
    setLoading(true);
    setError(null);
    setHasLoadedSuccessfully(false);
    try {
      const next = await getAdminInStockItems();
      setItems(next);
      setHasLoadedSuccessfully(true);
    } catch (reason) {
      handleError(reason);
    } finally {
      setLoading(false);
    }
  }

  function handleError(reason: unknown) {
    if (reason instanceof ApiError && (reason.status === 401 || reason.status === 403)) {
      onUnauthorized();
      return;
    }
    if (reason instanceof ApiError) {
      if (reason.errors) {
        setFieldErrors((current) => mergeFieldErrors(current, reason.errors));
      }
      setError(
        reason.errors
          ? Object.values(reason.errors).flat().find(Boolean) ?? reason.message
          : reason.message,
      );
      return;
    }
    if (import.meta.env.DEV) {
      console.error("Admin IN STOCK request failed.", reason);
    }
    setError("The IN STOCK request could not be completed. Please try again.");
  }

  function openNew() {
    if (saving || isUploadBusy(selectActiveUploadItem(uploadQueue.items))) {
      return;
    }
    uploadAbortRef.current?.abort();
    setUploadQueue(createEmptyUploadQueue());
    setQueueStatus(null);
    clearPendingPreviews();
    setFailedFiles([]);
    setEditingId(null);
    setEditingItem(null);
    setFieldErrors({});
    setImageFieldErrors({});
    setError(null);
    setMessage(null);
    const nextOrder = items.reduce((max, item) => Math.max(max, item.displayOrder + 1), 0);
    setForm(createEmptyInStockForm(nextOrder));
    setPriceText("0.00");
  }

  function openEdit(item: AdminInStockItem) {
    if (saving || isUploadBusy(selectActiveUploadItem(uploadQueue.items))) {
      return;
    }
    uploadAbortRef.current?.abort();
    setUploadQueue(createEmptyUploadQueue());
    setQueueStatus(null);
    clearPendingPreviews();
    setFailedFiles([]);
    setEditingId(item.id);
    setEditingItem(item);
    setFieldErrors({});
    setImageFieldErrors({});
    setError(null);
    setMessage(null);
    setForm(itemToInStockForm(item));
    setPriceText(toInStockPriceString(item.price));
  }

  function closeForm() {
    if (saving) {
      return;
    }
    uploadAbortRef.current?.abort();
    setForm(null);
    setEditingId(null);
    setEditingItem(null);
    clearPendingPreviews();
    setFailedFiles([]);
    setUploadQueue(createEmptyUploadQueue());
    setQueueStatus(null);
    setImageFieldErrors({});
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    if (!form || saving) {
      return;
    }

    const price = parseInStockPriceInput(priceText);
    if (price == null) {
      setFieldErrors((current) => ({
        ...current,
        price: "Enter a valid price with up to 2 decimal places.",
      }));
      return;
    }

    const nextForm: SaveInStockItemRequest = {
      ...form,
      title: form.title.trim(),
      slug: form.slug?.trim() || null,
      shortDescription: form.shortDescription?.trim() || null,
      description: form.description?.trim() || null,
      sizes: form.sizes?.trim() || null,
      materials: form.materials?.trim() || null,
      currency: "GBP",
      price,
    };
    const localErrors = validateInStockForm(nextForm);
    if (Object.keys(localErrors).length > 0) {
      setFieldErrors(localErrors);
      return;
    }

    const wasEditing = Boolean(editingId);
    const pendingSnapshot = pendingPreviews.map((preview) => preview.file);

    setSaving(true);
    setError(null);
    setMessage(null);
    setFieldErrors({});
    try {
      const saved = editingId
        ? await updateInStockItem(editingId, nextForm)
        : await createInStockItem(nextForm);
      setItems((current) =>
        [...current.filter((item) => item.id !== saved.id), saved].sort(
          (left, right) =>
            left.displayOrder - right.displayOrder ||
            left.createdAt.localeCompare(right.createdAt),
        ),
      );
      setEditingId(saved.id);
      setEditingItem(saved);
      setForm(itemToInStockForm(saved));
      setPriceText(toInStockPriceString(saved.price));
      setMessage(wasEditing ? "IN STOCK item updated." : "IN STOCK item created.");

      if (!wasEditing && pendingSnapshot.length > 0) {
        await uploadSelectedFiles(saved.id, pendingSnapshot);
        setPendingPreviews((current) => {
          const keepFiles = new Set(
            removeSnapshotFilesFromPending(
              current.map((preview) => preview.file),
              pendingSnapshot,
            ),
          );
          const remaining: PendingFilePreview[] = [];
          for (const preview of current) {
            if (keepFiles.has(preview.file)) {
              remaining.push(preview);
            } else {
              revokePendingFilePreview(preview);
            }
          }
          return remaining;
        });
      }
    } catch (reason) {
      handleError(reason);
    } finally {
      setSaving(false);
    }
  }

  async function uploadSelectedFiles(itemId: string, files: File[]) {
    uploadAbortRef.current?.abort();
    const controller = new AbortController();
    uploadAbortRef.current = controller;
    setError(null);

    try {
      let outcome;
      try {
        outcome = await runSequentialUploads(
          files.map((file, index) => ({
            id: buildInStockUploadEntryId(file, index),
            file,
            upload: ({ onProgress, signal }) =>
              uploadInStockImage(itemId, file, { onProgress, signal }),
          })),
          {
            signal: controller.signal,
            onQueueChange: setUploadQueue,
            onQueueStatus: setQueueStatus,
          },
        );
      } catch (reason) {
        handleError(reason);
        return;
      }

      const followUp = await completeInStockUploadFollowUp({
        files,
        cancelled: outcome.cancelled,
        resultIds: outcome.results.map((entry) => entry.id),
        failureIds: outcome.failures.map((entry) => entry.id),
        refresh: getAdminInStockItems,
      });

      if (followUp.kind === "cancelled") {
        setMessage(null);
        return;
      }

      if (followUp.kind === "failedOnly") {
        setFailedFiles(followUp.failedFiles);
        setError(followUp.error);
        return;
      }

      setFailedFiles(followUp.failedFiles);

      if (followUp.kind === "uploadedRefreshFailed") {
        if (
          followUp.refreshError instanceof ApiError &&
          (followUp.refreshError.status === 401 || followUp.refreshError.status === 403)
        ) {
          handleError(followUp.refreshError);
          return;
        }
        setMessage(followUp.message);
        return;
      }

      setItems(followUp.items);
      setEditingItem(followUp.items.find((item) => item.id === itemId) ?? null);
      setMessage(followUp.message);
    } finally {
      if (uploadAbortRef.current === controller) {
        uploadAbortRef.current = null;
      }
    }
  }

  async function handleFilesSelected(files: File[]) {
    if (!form || saving) {
      return;
    }
    try {
      if (!editingId) {
        setPendingPreviews((current) => [
          ...current,
          ...files.map((file) => createPendingFilePreview(file)),
        ]);
        setMessage("Images selected. They will upload after you save the item.");
        return;
      }
      await uploadSelectedFiles(editingId, files);
    } catch (reason) {
      handleError(reason);
    }
  }

  async function retryFailed() {
    if (!editingId || failedFiles.length === 0 || saving) {
      return;
    }
    try {
      const retry = [...failedFiles];
      setFailedFiles([]);
      await uploadSelectedFiles(editingId, retry);
    } catch (reason) {
      handleError(reason);
    }
  }

  function removePendingPreview(key: string) {
    if (saving) {
      return;
    }
    setPendingPreviews((current) => {
      const target = current.find((preview) => preview.key === key);
      if (target) {
        revokePendingFilePreview(target);
      }
      return current.filter((preview) => preview.key !== key);
    });
  }

  async function runConfirm() {
    if (!confirm) {
      return;
    }
    setConfirmBusy(true);
    setError(null);
    setMessage(null);
    try {
      if (confirm.type === "archive") {
        const result = await archiveInStockItem(confirm.item.id);
        await load();
        if (editingId === confirm.item.id) {
          closeForm();
        }
        setMessage(result.message);
      } else if (confirm.type === "restore") {
        const result = await restoreInStockItem(confirm.item.id);
        await load();
        setMessage(getRestoreInStockOwnerMessage(result));
      } else if (confirm.type === "deleteImage") {
        setImageActionBusyId(confirm.image.id);
        await deleteInStockImage(confirm.itemId, confirm.image.id);
        const refreshed = await getAdminInStockItems();
        setItems(refreshed);
        const current = refreshed.find((item) => item.id === confirm.itemId) ?? null;
        setEditingItem(current);
        setMessage("Image deleted. Physical cleanup was scheduled.");
      }
      setConfirm(null);
    } catch (reason) {
      handleError(reason);
    } finally {
      setImageActionBusyId(null);
      setConfirmBusy(false);
    }
  }

  async function moveImage(image: AdminInStockImage, direction: -1 | 1) {
    if (!editingItem || reorderBusy || saving) {
      return;
    }
    const orderedIds = planOrderedImageIdsAfterMove(
      editingItem.images,
      image.id,
      direction,
    );
    if (!orderedIds) {
      return;
    }

    setReorderBusy(true);
    setError(null);
    try {
      const images = await reorderInStockImages(editingItem.id, { imageIds: orderedIds });
      setEditingItem((current) => (current ? { ...current, images } : current));
      setItems((current) =>
        current.map((item) =>
          item.id === editingItem.id ? { ...item, images } : item,
        ),
      );
    } catch (reason) {
      handleError(reason);
    } finally {
      setReorderBusy(false);
    }
  }

  async function saveImageAlt(image: AdminInStockImage, altText: string) {
    if (!editingItem || saving) {
      return;
    }
    const generation = (altSaveGenerationRef.current.get(image.id) ?? 0) + 1;
    altSaveGenerationRef.current.set(image.id, generation);
    setImageActionBusyId(image.id);
    setImageFieldErrors((current) => {
      const next = { ...current };
      delete next[image.id];
      return next;
    });
    try {
      const updated = await updateInStockImage(editingItem.id, image.id, {
        altText: altText.trim() || null,
        displayOrder: image.displayOrder,
      });
      if (altSaveGenerationRef.current.get(image.id) !== generation) {
        return;
      }
      setEditingItem((current) =>
        current
          ? {
              ...current,
              images: current.images.map((candidate) =>
                candidate.id === updated.id ? updated : candidate,
              ),
            }
          : current,
      );
      setItems((current) =>
        current.map((item) =>
          item.id === editingItem.id
            ? {
                ...item,
                images: item.images.map((candidate) =>
                  candidate.id === updated.id ? updated : candidate,
                ),
              }
            : item,
        ),
      );
    } catch (reason) {
      if (altSaveGenerationRef.current.get(image.id) !== generation) {
        return;
      }
      if (reason instanceof ApiError && reason.errors?.altText?.[0]) {
        setImageFieldErrors((current) => ({
          ...current,
          [image.id]: reason.errors!.altText![0]!,
        }));
      }
      handleError(reason);
    } finally {
      if (altSaveGenerationRef.current.get(image.id) === generation) {
        setImageActionBusyId((current) => (current === image.id ? null : current));
      }
    }
  }

  const visibleItems = filterAdminInStockItems(items, showArchived);
  const activeUpload = selectActiveUploadItem(uploadQueue.items);
  const uploadInFlight = isUploadBusy(activeUpload);
  const formLocked = saving || uploadInFlight;

  if (loading) {
    return (
      <div className="bg-card border border-border p-6 text-[11px] text-muted-foreground">
        {IN_STOCK_LABEL_LOADING}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <label className="inline-flex items-center gap-2 text-[10px] text-muted-foreground font-sans">
          <input
            type="checkbox"
            checked={showArchived}
            onChange={(event) => setShowArchived(event.target.checked)}
          />
          Show archived
        </label>
        <button
          type="button"
          onClick={openNew}
          disabled={formLocked}
          className="inline-flex items-center gap-2 bg-foreground text-primary-foreground px-4 py-2.5 text-[10px] hover:bg-accent disabled:opacity-50"
        >
          <Plus size={12} /> Add item
        </button>
      </div>

      {error ? (
        <div className="space-y-2">
          <p role="alert" className="border border-destructive/30 bg-card px-4 py-3 text-[11px] text-destructive">
            {error}
          </p>
          <button
            type="button"
            onClick={() => void load()}
            className="border border-border px-3 py-2 text-[10px] hover:border-foreground"
          >
            Retry
          </button>
        </div>
      ) : null}
      {message ? (
        <p role="status" className="border border-emerald-300 bg-emerald-50 px-4 py-3 text-[11px] text-emerald-700">
          {message}
        </p>
      ) : null}

      {form ? (
        <form onSubmit={(event) => void save(event)} className="bg-card border border-border p-5 space-y-5">
          <div className="flex justify-between gap-3">
            <h2 className="font-serif text-xl font-light">
              {editingId ? "Edit IN STOCK item" : "New IN STOCK item"}
            </h2>
            <button
              type="button"
              onClick={closeForm}
              disabled={saving}
              className="text-[10px] text-muted-foreground disabled:opacity-50"
            >
              Close
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Field
              label="Title"
              required
              value={form.title}
              error={fieldErrors.title}
              disabled={saving}
              onChange={(title) => setForm({ ...form, title })}
            />
            <Field
              label="Slug (optional)"
              value={form.slug}
              error={fieldErrors.slug}
              disabled={saving}
              onChange={(slug) => setForm({ ...form, slug: slug || null })}
            />
            <Field
              label="Price (GBP)"
              value={priceText}
              error={fieldErrors.price}
              disabled={saving}
              onChange={setPriceText}
            />
            <label className="text-[10px] text-muted-foreground">
              <span className="block mb-1.5">Currency</span>
              <input className={input} value="GBP" readOnly aria-readonly="true" />
            </label>
            <label className="text-[10px] text-muted-foreground">
              <span className="block mb-1.5">Status</span>
              <select
                className={input}
                value={form.status}
                disabled={saving}
                aria-invalid={Boolean(fieldErrors.status)}
                aria-describedby={fieldErrors.status ? "instock-status-error" : undefined}
                onChange={(event) =>
                  setForm({ ...form, status: event.target.value as InStockItemStatus })
                }
              >
                <option value="Available">Available</option>
                <option value="Reserved">Reserved</option>
                <option value="Sold">Sold</option>
              </select>
              {fieldErrors.status ? (
                <span id="instock-status-error" className="mt-1 block text-destructive">
                  {fieldErrors.status}
                </span>
              ) : null}
            </label>
            <Field
              label="Display order"
              type="number"
              value={String(form.displayOrder)}
              error={fieldErrors.displayOrder}
              disabled={saving}
              onChange={(value) =>
                setForm({ ...form, displayOrder: Number(value) || 0 })
              }
            />
            <Area
              label="Short description"
              value={form.shortDescription}
              error={fieldErrors.shortDescription}
              disabled={saving}
              onChange={(shortDescription) =>
                setForm({ ...form, shortDescription: shortDescription || null })
              }
            />
            <Area
              label="Description"
              value={form.description}
              error={fieldErrors.description}
              disabled={saving}
              onChange={(description) =>
                setForm({ ...form, description: description || null })
              }
            />
            <Field
              label="Sizes"
              value={form.sizes}
              error={fieldErrors.sizes}
              disabled={saving}
              onChange={(sizes) => setForm({ ...form, sizes: sizes || null })}
            />
            <Field
              label="Materials"
              value={form.materials}
              error={fieldErrors.materials}
              disabled={saving}
              onChange={(materials) =>
                setForm({ ...form, materials: materials || null })
              }
            />
            <label className="text-[10px] text-foreground flex gap-2 items-center md:col-span-2">
              <input
                type="checkbox"
                checked={form.isPublished}
                disabled={saving}
                onChange={(event) =>
                  setForm({ ...form, isPublished: event.target.checked })
                }
              />
              Published
            </label>
          </div>

          <div className="border border-border bg-background p-4 space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h3 className="font-serif text-lg font-light">Photographs</h3>
              <UploadProgressControl
                idleLabel="Upload JPG, PNG or WebP"
                multiple
                disabled={saving}
                icon={<Upload size={12} />}
                item={activeUpload}
                queueStatus={queueStatus}
                onFilesSelected={(files) => void handleFilesSelected(files)}
                onRetry={
                  failedFiles.length > 0 && !saving
                    ? () => void retryFailed()
                    : undefined
                }
              />
            </div>
            {!editingId ? (
              <p className="text-[10px] text-muted-foreground font-sans">
                You can create an item without photos. Selected files (
                {pendingPreviews.length}) upload after the first save.
              </p>
            ) : null}
            {pendingPreviews.length > 0 ? (
              <ul className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {pendingPreviews.map((preview) => (
                  <li
                    key={preview.key}
                    className="border border-dashed border-border p-3 space-y-2 bg-card"
                  >
                    <img
                      src={preview.objectUrl}
                      alt={buildPendingPreviewAlt(preview.file.name)}
                      className="w-full aspect-[3/4] object-cover bg-muted"
                    />
                    <p className="text-[10px] text-muted-foreground truncate">
                      {preview.file.name}
                    </p>
                    <p className="text-[10px] text-muted-foreground font-sans">
                      {formatInStockFileSize(preview.file.size)}
                      {" "}
                      {IN_STOCK_META_SEPARATOR}
                      {" "}
                      Pending upload
                    </p>
                    <Action
                      text="Remove"
                      danger
                      disabled={saving}
                      onClick={() => removePendingPreview(preview.key)}
                    />
                  </li>
                ))}
              </ul>
            ) : null}
            {editingItem ? (
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {sortInStockImages(editingItem.images).map((image, index) => (
                  <InStockImageCard
                    key={image.id}
                    image={image}
                    isPrimary={index === 0}
                    altError={imageFieldErrors[image.id]}
                    busy={
                      reorderBusy ||
                      imageActionBusyId === image.id ||
                      saving
                    }
                    reorderDisabled={reorderBusy || saving}
                    onAltBlur={(altText) => void saveImageAlt(image, altText)}
                    onMoveLeft={() => void moveImage(image, -1)}
                    onMoveRight={() => void moveImage(image, 1)}
                    onDelete={() =>
                      setConfirm({
                        type: "deleteImage",
                        itemId: editingItem.id,
                        image,
                      })
                    }
                  />
                ))}
              </div>
            ) : null}
          </div>

          <button
            disabled={saving}
            className="bg-foreground text-primary-foreground px-6 py-2.5 text-[10px] disabled:opacity-50"
          >
            {saving ? IN_STOCK_LABEL_SAVING : "Save item"}
          </button>
        </form>
      ) : null}

      <div className="space-y-3">
        {shouldShowAdminInStockEmptyState({
          loading,
          hasLoadedSuccessfully,
          error,
          itemCount: visibleItems.length,
        }) ? (
          <div className="bg-card border border-border p-6 text-[11px] text-muted-foreground font-sans">
            No IN STOCK items yet. Add your first finished piece.
          </div>
        ) : null}

        {visibleItems.map((item) => {
          const primary = getPrimaryInStockImage(item.images);
          return (
            <article
              key={item.id}
              className={`bg-card border p-4 flex flex-col sm:flex-row gap-4 ${
                item.archivedAt ? "border-amber-300 opacity-75" : "border-border"
              }`}
            >
              {primary ? (
                <InStockThumb imageId={primary.id} alt={primary.altText || item.title} />
              ) : (
                <div className="w-24 aspect-[3/4] bg-muted flex items-center justify-center text-[9px] text-muted-foreground">
                  No photo
                </div>
              )}
              <div className="flex-1 space-y-2">
                <div className="flex flex-wrap gap-2 items-center">
                  <h3 className="font-serif text-lg font-light">{item.title}</h3>
                  <Badge text={item.status} />
                  <Badge text={item.isPublished ? "Published" : "Draft"} />
                  {item.archivedAt ? <Badge text="Archived" /> : null}
                </div>
                <p className="text-[11px] font-sans text-foreground">
                  {formatInStockPriceGbp(item.price, item.currency)}
                </p>
                <p className="text-[10px] text-muted-foreground font-sans">
                  Photos: {item.images.length}
                  {" "}
                  {IN_STOCK_META_SEPARATOR}
                  {" "}
                  Order: {item.displayOrder}
                  {" "}
                  {IN_STOCK_META_SEPARATOR}
                  {" "}
                  /{item.slug}
                </p>
              </div>
              <div className="flex flex-wrap gap-2 items-start">
                <Action
                  text="Edit"
                  disabled={formLocked}
                  onClick={() => openEdit(item)}
                />
                {item.archivedAt ? (
                  <Action
                    text="Restore"
                    disabled={formLocked}
                    onClick={() => setConfirm({ type: "restore", item })}
                  />
                ) : (
                  <Action
                    text="Archive"
                    danger
                    disabled={formLocked}
                    onClick={() => setConfirm({ type: "archive", item })}
                  />
                )}
              </div>
            </article>
          );
        })}
      </div>

      {confirm ? (
        <AdminConfirmDialog
          title={
            confirm.type === "archive"
              ? "Archive this IN STOCK item?"
              : confirm.type === "restore"
                ? "Restore this IN STOCK item?"
                : "Delete this photograph?"
          }
          description={
            confirm.type === "archive"
              ? "The item will be hidden from the public catalogue. Photographs are kept."
              : confirm.type === "restore"
                ? "The item will return as a draft. You must publish it again when ready."
                : "The image will be removed from this item. Physical cleanup is scheduled after confirmation."
          }
          confirmLabel={
            confirm.type === "archive"
              ? "Archive"
              : confirm.type === "restore"
                ? "Restore"
                : "Delete image"
          }
          isBusy={confirmBusy}
          onCancel={() => {
            if (!confirmBusy) {
              setConfirm(null);
            }
          }}
          onConfirm={() => void runConfirm()}
        />
      ) : null}
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  required,
  type = "text",
  error,
  disabled,
}: {
  label: string;
  value: string | null;
  onChange(value: string): void;
  required?: boolean;
  type?: "text" | "number";
  error?: string;
  disabled?: boolean;
}) {
  const errorId = useId();
  return (
    <label className="text-[10px] text-muted-foreground">
      <span className="block mb-1.5">{label}</span>
      <input
        className={input}
        type={type}
        min={type === "number" ? 0 : undefined}
        required={required}
        disabled={disabled}
        value={value ?? ""}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        onChange={(event) => onChange(event.target.value)}
      />
      {error ? (
        <span id={errorId} className="mt-1 block text-destructive">
          {error}
        </span>
      ) : null}
    </label>
  );
}

function Area({
  label,
  value,
  onChange,
  error,
  disabled,
}: {
  label: string;
  value: string | null;
  onChange(value: string): void;
  error?: string;
  disabled?: boolean;
}) {
  const errorId = useId();
  return (
    <label className="text-[10px] text-muted-foreground md:col-span-2">
      <span className="block mb-1.5">{label}</span>
      <textarea
        className={`${input} resize-y`}
        rows={3}
        disabled={disabled}
        value={value ?? ""}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        onChange={(event) => onChange(event.target.value)}
      />
      {error ? (
        <span id={errorId} className="mt-1 block text-destructive">
          {error}
        </span>
      ) : null}
    </label>
  );
}

function Action({
  text,
  onClick,
  danger,
  disabled,
}: {
  text: string;
  onClick(): void;
  danger?: boolean;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={`border px-3 py-2 text-[10px] disabled:opacity-50 ${
        danger
          ? "border-destructive/30 text-destructive"
          : "border-border hover:border-foreground"
      }`}
    >
      {text}
    </button>
  );
}

function Badge({ text }: { text: string }) {
  return (
    <span className="text-[9px] px-2 py-0.5 bg-secondary text-muted-foreground">
      {text}
    </span>
  );
}

function InStockThumb({
  imageId,
  alt,
  className = "w-24 aspect-[3/4] object-cover bg-muted",
}: {
  imageId: string;
  alt: string;
  className?: string;
}) {
  const [src, setSrc] = useState<string | null>(null);
  useEffect(() => {
    let objectUrl: string | null = null;
    let active = true;
    getAdminInStockImageBlob(imageId)
      .then((blob) => {
        if (!active) {
          return;
        }
        objectUrl = URL.createObjectURL(blob);
        setSrc(objectUrl);
      })
      .catch(() => undefined);
    return () => {
      active = false;
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [imageId]);
  return src ? (
    <img src={src} alt={alt} className={className} />
  ) : (
    <div className={className} />
  );
}

function InStockImageCard({
  image,
  isPrimary,
  altError,
  busy,
  reorderDisabled,
  onAltBlur,
  onMoveLeft,
  onMoveRight,
  onDelete,
}: {
  image: AdminInStockImage;
  isPrimary: boolean;
  altError?: string;
  busy: boolean;
  reorderDisabled: boolean;
  onAltBlur(altText: string): void;
  onMoveLeft(): void;
  onMoveRight(): void;
  onDelete(): void;
}) {
  const [altText, setAltText] = useState(image.altText ?? "");
  const altErrorId = useId();
  useEffect(() => {
    setAltText(image.altText ?? "");
  }, [image.altText]);

  return (
    <div className="border border-border p-3 space-y-2 bg-card">
      <div className="relative">
        <InStockThumb
          imageId={image.id}
          alt={image.altText || image.originalFileName}
          className="w-full aspect-[3/4] object-cover bg-muted"
        />
        {isPrimary ? (
          <span className="absolute left-2 top-2 bg-foreground text-primary-foreground text-[9px] px-2 py-0.5">
            Main photo
          </span>
        ) : null}
      </div>
      <p className="text-[10px] text-muted-foreground truncate">{image.originalFileName}</p>
      <label className="text-[10px] text-muted-foreground block">
        Alt text
        <input
          className={`${input} mt-1`}
          value={altText}
          disabled={busy}
          aria-invalid={Boolean(altError)}
          aria-describedby={altError ? altErrorId : undefined}
          onChange={(event) => setAltText(event.target.value)}
          onBlur={() => onAltBlur(altText)}
        />
        {altError ? (
          <span id={altErrorId} className="mt-1 block text-destructive">
            {altError}
          </span>
        ) : null}
      </label>
      <div className="flex flex-wrap gap-2">
        <Action text="Earlier" disabled={reorderDisabled || busy} onClick={onMoveLeft} />
        <Action text="Later" disabled={reorderDisabled || busy} onClick={onMoveRight} />
        <Action text="Delete" danger disabled={busy} onClick={onDelete} />
      </div>
    </div>
  );
}
