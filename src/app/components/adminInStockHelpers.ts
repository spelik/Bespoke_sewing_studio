import type {
  AdminInStockImage,
  AdminInStockItem,
  ArchiveInStockItemResult,
  InStockItemStatus,
  SaveInStockItemRequest,
} from "../types";
import type { UploadItemState, UploadPhase } from "../uploads/uploadProgressMachine";

/** Unicode escapes keep labels valid UTF-8 across editors/locales. */
export const IN_STOCK_LABEL_LOADING = "Loading IN STOCK\u2026";
export const IN_STOCK_LABEL_SAVING = "Saving\u2026";
export const IN_STOCK_META_SEPARATOR = "\u00b7";
export const IN_STOCK_UPLOAD_REFRESH_FAILED_MESSAGE =
  "Images uploaded, but the item could not be refreshed. Reload the list.";

const ACTIVE_UPLOAD_PRIORITY: readonly UploadPhase[] = [
  "uploading",
  "scanning",
  "processing",
];

export interface PendingFilePreview {
  key: string;
  file: File;
  objectUrl: string;
}

/**
 * Prefer an in-flight upload over an earlier failed item so Retry UI does not
 * hide the current progress bar.
 */
export function selectActiveUploadItem(
  items: readonly UploadItemState[],
): UploadItemState | null {
  for (const phase of ACTIVE_UPLOAD_PRIORITY) {
    const match = items.find((item) => item.phase === phase);
    if (match) {
      return match;
    }
  }

  for (let index = items.length - 1; index >= 0; index -= 1) {
    if (items[index]?.phase === "error") {
      return items[index]!;
    }
  }

  for (let index = items.length - 1; index >= 0; index -= 1) {
    const phase = items[index]?.phase;
    if (phase === "success" || phase === "cancelled") {
      return items[index]!;
    }
  }

  return items[items.length - 1] ?? null;
}

export function buildInStockUploadEntryId(file: File, index: number): string {
  return `${file.name}-${file.size}-${file.lastModified}-${index}`;
}

export function mapUploadFailureIdsToFiles(
  files: readonly File[],
  failureIds: readonly string[],
): File[] {
  return failureIds
    .map((failureId) =>
      files.find(
        (file, index) => buildInStockUploadEntryId(file, index) === failureId,
      ),
    )
    .filter((file): file is File => Boolean(file));
}

/** Remove only the snapshot File references; later additions stay pending. */
export function removeSnapshotFilesFromPending(
  pending: readonly File[],
  snapshot: readonly File[],
): File[] {
  const snapshotSet = new Set(snapshot);
  return pending.filter((file) => !snapshotSet.has(file));
}

export function createPendingFilePreview(
  file: File,
  key = `${file.name}-${file.size}-${file.lastModified}-${Math.random().toString(36).slice(2)}`,
): PendingFilePreview {
  return {
    key,
    file,
    objectUrl: URL.createObjectURL(file),
  };
}

export function revokePendingFilePreview(preview: PendingFilePreview): void {
  URL.revokeObjectURL(preview.objectUrl);
}

export function revokeAllPendingFilePreviews(
  previews: readonly PendingFilePreview[],
): void {
  for (const preview of previews) {
    revokePendingFilePreview(preview);
  }
}

export function buildPendingPreviewAlt(fileName: string): string {
  return `Preview of ${fileName}`;
}

export function formatInStockFileSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0) {
    return "0 B";
  }
  if (bytes < 1024) {
    return `${bytes} B`;
  }
  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function planOrderedImageIdsAfterMove(
  images: readonly AdminInStockImage[],
  imageId: string,
  direction: -1 | 1,
): string[] | null {
  const sorted = sortInStockImages(images);
  const index = sorted.findIndex((image) => image.id === imageId);
  const swapWith = sorted[index + direction];
  if (index < 0 || !swapWith) {
    return null;
  }

  const next = [...sorted];
  const current = next[index]!;
  next[index] = swapWith;
  next[index + direction] = current;
  return next.map((image) => image.id);
}

export type InStockUploadFollowUp =
  | { kind: "cancelled" }
  | { kind: "failedOnly"; failedFiles: File[]; error: string }
  | {
      kind: "uploaded";
      failedFiles: File[];
      message: string;
      items: AdminInStockItem[];
    }
  | {
      kind: "uploadedRefreshFailed";
      failedFiles: File[];
      message: string;
      refreshError: unknown;
    };

export async function completeInStockUploadFollowUp(options: {
  files: readonly File[];
  cancelled: boolean;
  resultIds: readonly string[];
  failureIds: readonly string[];
  refresh(): Promise<AdminInStockItem[]>;
}): Promise<InStockUploadFollowUp> {
  if (options.cancelled) {
    return { kind: "cancelled" };
  }

  const failedFiles = mapUploadFailureIdsToFiles(options.files, options.failureIds);
  if (options.resultIds.length === 0) {
    if (failedFiles.length === 0) {
      return { kind: "cancelled" };
    }
    return {
      kind: "failedOnly",
      failedFiles,
      error: "Image upload failed. You can retry the failed files.",
    };
  }

  try {
    const items = await options.refresh();
    return {
      kind: "uploaded",
      failedFiles,
      message:
        failedFiles.length > 0
          ? "Some images uploaded. Retry the failed files."
          : "Images uploaded.",
      items,
    };
  } catch (refreshError) {
    return {
      kind: "uploadedRefreshFailed",
      failedFiles,
      message: IN_STOCK_UPLOAD_REFRESH_FAILED_MESSAGE,
      refreshError,
    };
  }
}

export function shouldShowAdminInStockEmptyState(options: {
  loading: boolean;
  hasLoadedSuccessfully: boolean;
  error: string | null;
  itemCount: number;
}): boolean {
  return (
    !options.loading &&
    options.hasLoadedSuccessfully &&
    options.error == null &&
    options.itemCount === 0
  );
}

export function filterAdminInStockItems(
  items: readonly AdminInStockItem[],
  showArchived: boolean,
): AdminInStockItem[] {
  return items.filter((item) => (showArchived ? true : item.archivedAt == null));
}

export function formatInStockPriceGbp(price: number, currency = "GBP"): string {
  try {
    return new Intl.NumberFormat("en-GB", {
      style: "currency",
      currency: currency || "GBP",
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(price);
  } catch {
    return `£${price.toFixed(2)}`;
  }
}

export function sortInStockImages(
  images: readonly AdminInStockImage[],
): AdminInStockImage[] {
  return [...images].sort((left, right) => {
    if (left.displayOrder !== right.displayOrder) {
      return left.displayOrder - right.displayOrder;
    }
    return left.createdAt.localeCompare(right.createdAt);
  });
}

export function getPrimaryInStockImage(
  images: readonly AdminInStockImage[],
): AdminInStockImage | null {
  const sorted = sortInStockImages(images);
  return sorted[0] ?? null;
}

export function parseInStockPriceInput(value: string): number | null {
  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }
  if (!/^\d+(\.\d{1,2})?$/.test(trimmed)) {
    return null;
  }
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : null;
}

export function toInStockPriceString(price: number): string {
  return price.toFixed(2);
}

export function getInStockStatusLabel(status: InStockItemStatus): string {
  return status;
}

export function getRestoreInStockOwnerMessage(
  result: ArchiveInStockItemResult,
): string {
  if (result.restored) {
    return (
      result.message ||
      "IN STOCK item restored as unpublished. Republish when ready."
    );
  }
  return result.message || "IN STOCK item is not archived.";
}

export function validateInStockForm(
  form: SaveInStockItemRequest,
): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!form.title.trim()) {
    errors.title = "Title is required.";
  } else if (form.title.trim().length > 200) {
    errors.title = "Title must be 200 characters or fewer.";
  }

  if (form.slug && !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(form.slug)) {
    errors.slug = "Enter a lowercase kebab-case slug.";
  } else if (form.slug && form.slug.length > 220) {
    errors.slug = "Slug must be 220 characters or fewer.";
  }

  if (form.shortDescription && form.shortDescription.length > 500) {
    errors.shortDescription = "Short description must be 500 characters or fewer.";
  }
  if (form.description && form.description.length > 4000) {
    errors.description = "Description must be 4000 characters or fewer.";
  }
  if (form.sizes && form.sizes.length > 500) {
    errors.sizes = "Sizes must be 500 characters or fewer.";
  }
  if (form.materials && form.materials.length > 1000) {
    errors.materials = "Materials must be 1000 characters or fewer.";
  }
  if (form.price < 0) {
    errors.price = "Price cannot be negative.";
  }
  if (form.displayOrder < 0) {
    errors.displayOrder = "Display order cannot be negative.";
  }
  if (form.currency && form.currency !== "GBP") {
    errors.currency = "Currency must be GBP.";
  }

  return errors;
}

export function createEmptyInStockForm(displayOrder: number): SaveInStockItemRequest {
  return {
    slug: null,
    title: "",
    shortDescription: null,
    description: null,
    price: 0,
    currency: "GBP",
    status: "Available",
    isPublished: false,
    displayOrder,
    sizes: null,
    materials: null,
  };
}

export function itemToInStockForm(item: AdminInStockItem): SaveInStockItemRequest {
  return {
    slug: item.slug,
    title: item.title,
    shortDescription: item.shortDescription,
    description: item.description,
    price: item.price,
    currency: "GBP",
    status: item.status,
    isPublished: item.isPublished,
    displayOrder: item.displayOrder,
    sizes: item.sizes,
    materials: item.materials,
  };
}

export function mergeFieldErrors(
  current: Record<string, string>,
  serverErrors?: Record<string, string[]>,
): Record<string, string> {
  if (!serverErrors) {
    return current;
  }

  const next = { ...current };
  for (const [key, values] of Object.entries(serverErrors)) {
    const message = values.find(Boolean);
    if (message) {
      next[key] = message;
    }
  }
  return next;
}
