import { beforeEach, describe, expect, it, vi } from "vitest";
import type { AdminInStockItem } from "../app/types";

vi.mock("../config/appConfig", () => ({
  appConfig: {
    apiBaseUrl: "/api",
    publicSiteUrl: null,
  },
}));

vi.mock("./apiClient", () => ({
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
}));

vi.mock("./uploadTransport", () => ({
  uploadWithProgress: vi.fn(),
}));

import { apiClient } from "./apiClient";
import {
  createInStockItem,
  getAdminInStockItems,
  reorderInStockImages,
  uploadInStockImage,
} from "./inStockApi";
import { uploadWithProgress } from "./uploadTransport";

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
