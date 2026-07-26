import { beforeEach, describe, expect, it, vi } from "vitest";
import type { AdminPortfolioItem, PortfolioItem, SavePortfolioItemRequest } from "../app/types";

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
    patch: vi.fn(),
    delete: vi.fn(),
    postForm: vi.fn(),
    getBlob: vi.fn(),
  },
}));

import { apiClient } from "./apiClient";
import {
  createPortfolioItem,
  getAdminPortfolioItems,
  getPublicPortfolioItems,
  updatePortfolioItem,
} from "./portfolioApi";

const category = {
  id: "cat-1",
  slug: "tailoring",
  name: "Tailoring",
  description: null,
  displayOrder: 0,
};

function publicItem(overrides: Partial<PortfolioItem> = {}): PortfolioItem {
  return {
    id: "item-1",
    slug: "test",
    title: "Test",
    shortDescription: null,
    description: null,
    category,
    imageUrl: "/api/portfolio/images/id",
    altText: "Test",
    isFeatured: false,
    displayOrder: 0,
    ...overrides,
  };
}

function adminItem(overrides: Partial<AdminPortfolioItem> = {}): AdminPortfolioItem {
  return {
    id: "item-1",
    slug: "test",
    title: "Test",
    shortDescription: null,
    description: null,
    categoryId: "cat-1",
    categoryName: "Tailoring",
    imageFileId: "11111111-1111-1111-1111-111111111111",
    imageUrl: "/api/portfolio/images/id",
    altText: "Test",
    isFeatured: false,
    isActive: true,
    displayOrder: 0,
    createdAt: "2026-07-01T00:00:00Z",
    updatedAt: "2026-07-01T00:00:00Z",
    archivedAt: null,
    ...overrides,
  };
}

const saveRequest: SavePortfolioItemRequest = {
  categoryId: "cat-1",
  slug: "test",
  title: "Test",
  shortDescription: null,
  description: null,
  imageFileId: "11111111-1111-1111-1111-111111111111",
  altText: "Test",
  isActive: true,
  isFeatured: false,
  displayOrder: 0,
};

beforeEach(() => {
  vi.clearAllMocks();
});

describe("portfolioApi imageUrl mapping", () => {
  it("getPublicPortfolioItems keeps root-relative imageUrl when apiBaseUrl is /api", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce([publicItem()]);

    await expect(getPublicPortfolioItems()).resolves.toEqual([
      publicItem({ imageUrl: "/api/portfolio/images/id" }),
    ]);
    expect(apiClient.get).toHaveBeenCalledWith("portfolio");
  });

  it("getAdminPortfolioItems maps one item with root-relative imageUrl and archivedAt null", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce([adminItem({ archivedAt: null })]);

    const result = await getAdminPortfolioItems();

    expect(result).toHaveLength(1);
    expect(result[0]?.imageUrl).toBe("/api/portfolio/images/id");
    expect(result[0]?.archivedAt).toBeNull();
    expect(apiClient.get).toHaveBeenCalledWith("admin/portfolio/items");
  });

  it("getAdminPortfolioItems returns empty array without error", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce([]);

    await expect(getAdminPortfolioItems()).resolves.toEqual([]);
  });

  it("getPublicPortfolioItems leaves absolute HTTPS imageUrl unchanged", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce([
      publicItem({ imageUrl: "https://cdn.example.com/portfolio.jpg" }),
    ]);

    const result = await getPublicPortfolioItems();

    expect(result[0]?.imageUrl).toBe("https://cdn.example.com/portfolio.jpg");
  });

  it("getAdminPortfolioItems keeps null imageUrl as null", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce([adminItem({ imageUrl: null, imageFileId: null })]);

    const result = await getAdminPortfolioItems();

    expect(result[0]?.imageUrl).toBeNull();
  });

  it("createPortfolioItem normalizes imageUrl through resolveApiAssetUrl", async () => {
    vi.mocked(apiClient.post).mockResolvedValueOnce(adminItem());

    const result = await createPortfolioItem(saveRequest);

    expect(result.imageUrl).toBe("/api/portfolio/images/id");
    expect(apiClient.post).toHaveBeenCalledWith("admin/portfolio/items", saveRequest);
  });

  it("updatePortfolioItem normalizes imageUrl through resolveApiAssetUrl", async () => {
    vi.mocked(apiClient.patch).mockResolvedValueOnce(adminItem());

    const result = await updatePortfolioItem("item-1", saveRequest);

    expect(result.imageUrl).toBe("/api/portfolio/images/id");
    expect(apiClient.patch).toHaveBeenCalledWith("admin/portfolio/items/item-1", saveRequest);
  });
});
