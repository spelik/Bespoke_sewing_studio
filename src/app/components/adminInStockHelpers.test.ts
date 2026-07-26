import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../../api/apiClient";
import type { AdminInStockItem } from "../types";
import type { UploadItemState } from "../uploads/uploadProgressMachine";
import { createUploadItem } from "../uploads/uploadProgressMachine";
import {
  buildPendingPreviewAlt,
  completeInStockUploadFollowUp,
  createPendingFilePreview,
  filterAdminInStockItems,
  formatInStockPriceGbp,
  getPrimaryInStockImage,
  getRestoreInStockOwnerMessage,
  IN_STOCK_LABEL_LOADING,
  IN_STOCK_LABEL_SAVING,
  IN_STOCK_META_SEPARATOR,
  IN_STOCK_UPLOAD_REFRESH_FAILED_MESSAGE,
  parseInStockPriceInput,
  planOrderedImageIdsAfterMove,
  removeSnapshotFilesFromPending,
  revokeAllPendingFilePreviews,
  revokePendingFilePreview,
  selectActiveUploadItem,
  shouldShowAdminInStockEmptyState,
  sortInStockImages,
} from "./adminInStockHelpers";

function item(overrides: Partial<AdminInStockItem> = {}): AdminInStockItem {
  return {
    id: "1",
    slug: "coat",
    title: "Coat",
    shortDescription: null,
    description: null,
    price: 120,
    currency: "GBP",
    status: "Available",
    isPublished: true,
    displayOrder: 0,
    sizes: null,
    materials: null,
    createdAt: "2026-07-01T00:00:00Z",
    updatedAt: "2026-07-01T00:00:00Z",
    archivedAt: null,
    images: [],
    ...overrides,
  };
}

function uploadItem(
  id: string,
  phase: UploadItemState["phase"],
): UploadItemState {
  return {
    ...createUploadItem(id, `${id}.jpg`),
    phase,
    percent: phase === "uploading" ? 40 : 100,
    errorMessage: phase === "error" ? "failed" : null,
  };
}

describe("adminInStockHelpers", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("does not show empty state on error", () => {
    expect(
      shouldShowAdminInStockEmptyState({
        loading: false,
        hasLoadedSuccessfully: false,
        error: "failed",
        itemCount: 0,
      }),
    ).toBe(false);
  });

  it("shows empty state after successful empty load", () => {
    expect(
      shouldShowAdminInStockEmptyState({
        loading: false,
        hasLoadedSuccessfully: true,
        error: null,
        itemCount: 0,
      }),
    ).toBe(true);
  });

  it("filters archived items", () => {
    const items = [
      item({ id: "a" }),
      item({ id: "b", archivedAt: "2026-07-02T00:00:00Z" }),
    ];
    expect(filterAdminInStockItems(items, false)).toHaveLength(1);
    expect(filterAdminInStockItems(items, true)).toHaveLength(2);
  });

  it("formats GBP price", () => {
    expect(formatInStockPriceGbp(12.5)).toContain("12.50");
  });

  it("selects primary image by displayOrder", () => {
    const images = sortInStockImages([
      {
        id: "b",
        uploadedFileId: "f2",
        imageUrl: "/api/in-stock/images/b",
        altText: null,
        displayOrder: 2,
        originalFileName: "b.jpg",
        contentType: "image/jpeg",
        fileSizeBytes: 1,
        createdAt: "2026-07-01T00:00:00Z",
      },
      {
        id: "a",
        uploadedFileId: "f1",
        imageUrl: "/api/in-stock/images/a",
        altText: null,
        displayOrder: 0,
        originalFileName: "a.jpg",
        contentType: "image/jpeg",
        fileSizeBytes: 1,
        createdAt: "2026-07-01T00:00:00Z",
      },
    ]);
    expect(getPrimaryInStockImage(images)?.id).toBe("a");
  });

  it("parses decimal price strings safely", () => {
    expect(parseInStockPriceInput("12.50")).toBe(12.5);
    expect(parseInStockPriceInput("12.555")).toBeNull();
    expect(parseInStockPriceInput("abc")).toBeNull();
  });

  it("restore message keeps draft guidance", () => {
    expect(
      getRestoreInStockOwnerMessage({
        id: "1",
        archived: false,
        restored: true,
        message: "IN STOCK item restored as unpublished. Republish when ready.",
      }),
    ).toContain("unpublished");
  });

  it("selectActiveUploadItem prefers uploading over earlier error", () => {
    const queue = [uploadItem("1", "error"), uploadItem("2", "uploading")];
    expect(selectActiveUploadItem(queue)?.id).toBe("2");
    expect(queue.map((entry) => entry.id)).toEqual(["1", "2"]);
  });

  it("selectActiveUploadItem prefers scanning over older error", () => {
    const queue = [uploadItem("1", "error"), uploadItem("2", "scanning")];
    expect(selectActiveUploadItem(queue)?.id).toBe("2");
  });

  it("selectActiveUploadItem falls back to latest error for Retry", () => {
    const queue = [
      uploadItem("1", "error"),
      uploadItem("2", "success"),
      uploadItem("3", "error"),
    ];
    expect(selectActiveUploadItem(queue)?.id).toBe("3");
  });

  it("removeSnapshotFilesFromPending keeps files added after snapshot", () => {
    const a = new File(["a"], "a.jpg", { type: "image/jpeg" });
    const b = new File(["b"], "b.jpg", { type: "image/jpeg" });
    const c = new File(["c"], "c.jpg", { type: "image/jpeg" });
    const snapshot = [a, b];
    expect(removeSnapshotFilesFromPending([a, b, c], snapshot)).toEqual([c]);
  });

  it("planOrderedImageIdsAfterMove swaps neighbors without mutating source", () => {
    const images = [
      {
        id: "a",
        uploadedFileId: "f1",
        imageUrl: "/a",
        altText: null,
        displayOrder: 0,
        originalFileName: "a.jpg",
        contentType: "image/jpeg",
        fileSizeBytes: 1,
        createdAt: "2026-07-01T00:00:00Z",
      },
      {
        id: "b",
        uploadedFileId: "f2",
        imageUrl: "/b",
        altText: null,
        displayOrder: 1,
        originalFileName: "b.jpg",
        contentType: "image/jpeg",
        fileSizeBytes: 1,
        createdAt: "2026-07-01T00:00:00Z",
      },
    ];
    expect(planOrderedImageIdsAfterMove(images, "a", 1)).toEqual(["b", "a"]);
    expect(images[0]?.id).toBe("a");
  });

  it("user-facing labels have no mojibake", () => {
    const labels = [
      IN_STOCK_LABEL_LOADING,
      IN_STOCK_LABEL_SAVING,
      IN_STOCK_META_SEPARATOR,
      IN_STOCK_UPLOAD_REFRESH_FAILED_MESSAGE,
      buildPendingPreviewAlt("front.jpg"),
    ];
    for (const label of labels) {
      expect(label).not.toMatch(/вЂ|â€|\uFFFD|В·|Р‚|Сљ/);
    }
    expect(IN_STOCK_LABEL_LOADING).toBe("Loading IN STOCK\u2026");
    expect(IN_STOCK_LABEL_SAVING).toBe("Saving\u2026");
    expect(IN_STOCK_META_SEPARATOR).toBe("\u00b7");
    expect(buildPendingPreviewAlt("front.jpg")).toBe("Preview of front.jpg");
  });

  it("creates and revokes pending preview object URLs", () => {
    const createSpy = vi
      .spyOn(URL, "createObjectURL")
      .mockReturnValue("blob:preview-1");
    const revokeSpy = vi.spyOn(URL, "revokeObjectURL").mockImplementation(() => undefined);
    const file = new File(["x"], "x.jpg", { type: "image/jpeg" });
    const preview = createPendingFilePreview(file, "key-1");
    expect(preview.objectUrl).toBe("blob:preview-1");
    expect(createSpy).toHaveBeenCalledWith(file);
    revokePendingFilePreview(preview);
    expect(revokeSpy).toHaveBeenCalledWith("blob:preview-1");
    revokeAllPendingFilePreviews([
      { key: "a", file, objectUrl: "blob:a" },
      { key: "b", file, objectUrl: "blob:b" },
    ]);
    expect(revokeSpy).toHaveBeenCalledWith("blob:a");
    expect(revokeSpy).toHaveBeenCalledWith("blob:b");
  });

  it("completeInStockUploadFollowUp handles cancellation without failure", async () => {
    const followUp = await completeInStockUploadFollowUp({
      files: [new File(["a"], "a.jpg")],
      cancelled: true,
      resultIds: [],
      failureIds: [],
      refresh: async () => [],
    });
    expect(followUp).toEqual({ kind: "cancelled" });
  });

  it("completeInStockUploadFollowUp maps failed-only uploads", async () => {
    const file = new File(["a"], "a.jpg", { type: "image/jpeg" });
    const followUp = await completeInStockUploadFollowUp({
      files: [file],
      cancelled: false,
      resultIds: [],
      failureIds: [`${file.name}-${file.size}-${file.lastModified}-0`],
      refresh: async () => [],
    });
    expect(followUp.kind).toBe("failedOnly");
    if (followUp.kind === "failedOnly") {
      expect(followUp.failedFiles).toEqual([file]);
    }
  });

  it("completeInStockUploadFollowUp keeps successes when refresh fails", async () => {
    const file = new File(["a"], "a.jpg", { type: "image/jpeg" });
    const followUp = await completeInStockUploadFollowUp({
      files: [file],
      cancelled: false,
      resultIds: [`${file.name}-${file.size}-${file.lastModified}-0`],
      failureIds: [],
      refresh: async () => {
        throw new Error("network");
      },
    });
    expect(followUp).toMatchObject({
      kind: "uploadedRefreshFailed",
      message: IN_STOCK_UPLOAD_REFRESH_FAILED_MESSAGE,
      failedFiles: [],
    });
  });

  it("completeInStockUploadFollowUp preserves auth and network refresh errors for the panel", async () => {
    const file = new File(["a"], "a.jpg", { type: "image/jpeg" });
    const unauthorized = await completeInStockUploadFollowUp({
      files: [file],
      cancelled: false,
      resultIds: [`${file.name}-${file.size}-${file.lastModified}-0`],
      failureIds: [],
      refresh: async () => {
        throw new ApiError("Unauthorized", 401);
      },
    });
    expect(unauthorized.kind).toBe("uploadedRefreshFailed");
    if (unauthorized.kind === "uploadedRefreshFailed") {
      expect(unauthorized.refreshError).toBeInstanceOf(ApiError);
      expect((unauthorized.refreshError as ApiError).status).toBe(401);
      expect(unauthorized.failedFiles).toEqual([]);
    }

    const network = await completeInStockUploadFollowUp({
      files: [file],
      cancelled: false,
      resultIds: [`${file.name}-${file.size}-${file.lastModified}-0`],
      failureIds: [],
      refresh: async () => {
        throw new ApiError("The server could not be reached.", 0);
      },
    });
    expect(network.kind).toBe("uploadedRefreshFailed");
    if (network.kind === "uploadedRefreshFailed") {
      expect((network.refreshError as ApiError).status).toBe(0);
      expect(network.failedFiles).toEqual([]);
    }
  });

  it("completeInStockUploadFollowUp refreshes after successful uploads", async () => {
    const file = new File(["a"], "a.jpg", { type: "image/jpeg" });
    const refreshed = [item({ id: "item-1" })];
    const followUp = await completeInStockUploadFollowUp({
      files: [file],
      cancelled: false,
      resultIds: [`${file.name}-${file.size}-${file.lastModified}-0`],
      failureIds: [],
      refresh: async () => refreshed,
    });
    expect(followUp).toEqual({
      kind: "uploaded",
      failedFiles: [],
      message: "Images uploaded.",
      items: refreshed,
    });
  });
});
