import { beforeEach, describe, expect, it, vi } from "vitest";
import type { AdminInStockItem, PublicInStockItem } from "../app/types";

vi.mock("../config/appConfig", () => ({
  appConfig: {
    apiBaseUrl: "/api",
    publicSiteUrl: null,
  },
}));

vi.mock("./apiClient", async (importOriginal) => {
  const actual = await importOriginal<typeof import("./apiClient")>();
  return {
    ...actual,
    apiClient: {
      baseUrl: "/api",
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
      postForm: vi.fn(),
      getBlob: vi.fn(),
    },
  };
});

vi.mock("./uploadTransport", () => ({
  uploadWithProgress: vi.fn(),
}));

import { ApiError, apiClient } from "./apiClient";
import {
  createInStockItem,
  getAdminInStockItems,
  getPublicInStockItemBySlug,
  getPublicInStockItems,
  reorderInStockImages,
  uploadInStockImage,
} from "./inStockApi";
import { uploadWithProgress } from "./uploadTransport";

function publicItem(overrides: Partial<PublicInStockItem> = {}): PublicInStockItem {
  return {
    id: "item-1",
    slug: "silk-blouse",
    title: "Silk blouse",
    shortDescription: null,
    description: null,
    price: 85,
    currency: "GBP",
    status: "Available",
    sizes: null,
    materials: null,
    images: [
      {
        id: "img-1",
        imageUrl: "/api/in-stock/images/img-1",
        altText: "Front",
        displayOrder: 0,
      },
    ],
    ...overrides,
  };
}

function adminItem(overrides: Partial<AdminInStockItem> = {}): AdminInStockItem {
  return {
    id: "item-1",
    slug: "silk-blouse",
    title: "Silk blouse",
    shortDescription: null,
    description: null,
    price: 85,
    currency: "GBP",
    status: "Available",
    isPublished: true,
    displayOrder: 0,
    sizes: null,
    materials: null,
    createdAt: "2026-07-01T00:00:00Z",
    updatedAt: "2026-07-01T00:00:00Z",
    archivedAt: null,
    images: [
      {
        id: "img-1",
        uploadedFileId: "file-1",
        imageUrl: "/api/in-stock/images/img-1",
        altText: "Front",
        displayOrder: 0,
        originalFileName: "front.jpg",
        contentType: "image/jpeg",
        fileSizeBytes: 10,
        createdAt: "2026-07-01T00:00:00Z",
      },
    ],
    ...overrides,
  };
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe("inStockApi public mapping", () => {
  it("maps public list items and root-relative images", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce([
      publicItem({ status: "Reserved" }),
      publicItem({ id: "2", status: "Sold", images: [] }),
    ]);
    const result = await getPublicInStockItems();
    expect(apiClient.get).toHaveBeenCalledWith("in-stock");
    expect(result.map((entry) => entry.status)).toEqual(["Reserved", "Sold"]);
    expect(result[0]?.images[0]?.imageUrl).toBe("/api/in-stock/images/img-1");
    expect(result[1]?.images).toEqual([]);
  });

  it("maps public detail and propagates 404", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(publicItem());
    const detail = await getPublicInStockItemBySlug("silk-blouse");
    expect(apiClient.get).toHaveBeenCalledWith("in-stock/silk-blouse");
    expect(detail.slug).toBe("silk-blouse");

    vi.mocked(apiClient.get).mockRejectedValueOnce(new ApiError("Not found", 404));
    await expect(getPublicInStockItemBySlug("missing")).rejects.toMatchObject({
      status: 404,
    });
  });

  it("tolerates malformed image collections on public detail", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(
      publicItem({ images: null as unknown as PublicInStockItem["images"] }),
    );
    const detail = await getPublicInStockItemBySlug("silk-blouse");
    expect(detail.images).toEqual([]);
  });
});

describe("inStockApi mapping", () => {
  it("keeps root-relative image URLs for admin items", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce([adminItem()]);
    const result = await getAdminInStockItems();
    expect(result[0]?.images[0]?.imageUrl).toBe("/api/in-stock/images/img-1");
    expect(result[0]?.status).toBe("Available");
  });

  it("maps Reserved and Sold statuses", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce([
      adminItem({ status: "Reserved" }),
      adminItem({ id: "2", status: "Sold" }),
    ]);
    const result = await getAdminInStockItems();
    expect(result.map((item) => item.status)).toEqual(["Reserved", "Sold"]);
  });

  it("creates items through admin DTO path", async () => {
    vi.mocked(apiClient.post).mockResolvedValueOnce(adminItem());
    const request = {
      slug: "silk-blouse",
      title: "Silk blouse",
      shortDescription: null,
      description: null,
      price: 85,
      currency: "GBP",
      status: "Available" as const,
      isPublished: false,
      displayOrder: 0,
      sizes: null,
      materials: null,
    };
    const result = await createInStockItem(request);
    expect(result.title).toBe("Silk blouse");
    expect(apiClient.post).toHaveBeenCalledWith("admin/in-stock/items", request);
  });

  it("uploadInStockImage uses progress transport and normalizes imageUrl", async () => {
    const onProgress = vi.fn();
    vi.mocked(uploadWithProgress).mockResolvedValueOnce(adminItem().images[0]!);
    const file = new File(["x"], "front.jpg", { type: "image/jpeg" });
    const result = await uploadInStockImage("item-1", file, { onProgress, altText: "Front" });
    expect(result.imageUrl).toBe("/api/in-stock/images/img-1");
    expect(uploadWithProgress).toHaveBeenCalledWith(
      expect.objectContaining({
        path: "admin/in-stock/items/item-1/images",
        onProgress,
      }),
    );
  });

  it("reorderInStockImages issues a single PUT with the ordered image ids", async () => {
    const images = [
      { ...adminItem().images[0]!, id: "img-2", displayOrder: 0 },
      { ...adminItem().images[0]!, id: "img-1", displayOrder: 1 },
    ];
    vi.mocked(apiClient.put).mockResolvedValueOnce(images);
    const result = await reorderInStockImages("item-1", {
      imageIds: ["img-2", "img-1"],
    });
    expect(apiClient.put).toHaveBeenCalledTimes(1);
    expect(apiClient.put).toHaveBeenCalledWith(
      "admin/in-stock/items/item-1/images/reorder",
      { imageIds: ["img-2", "img-1"] },
    );
    expect(result.map((image) => image.id)).toEqual(["img-2", "img-1"]);
  });
});
