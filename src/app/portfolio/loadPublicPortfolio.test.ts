import { beforeEach, describe, expect, it, vi } from "vitest";
import type { PortfolioItem, PublicPortfolioCategory } from "../types";
import { PORTFOLIO_CATEGORIES, PORTFOLIO_ITEMS } from "../../data/portfolioData";

vi.mock("../../api/portfolioApi", () => ({
  getPublicPortfolioItems: vi.fn(),
  getPublicPortfolioCategories: vi.fn(),
}));

import { getPublicPortfolioCategories, getPublicPortfolioItems } from "../../api/portfolioApi";
import { loadPublicPortfolio } from "./loadPublicPortfolio";

const category: PublicPortfolioCategory = {
  id: "cat-1",
  slug: "tailoring",
  name: "Tailoring",
  description: null,
  displayOrder: 0,
};

function cmsItem(overrides: Partial<PortfolioItem> = {}): PortfolioItem {
  return {
    id: "cms-item-1",
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

beforeEach(() => {
  vi.clearAllMocks();
});

describe("loadPublicPortfolio", () => {
  it("returns real CMS items and categories on success", async () => {
    const items = [cmsItem()];
    const categories = [category];
    vi.mocked(getPublicPortfolioItems).mockResolvedValueOnce(items);
    vi.mocked(getPublicPortfolioCategories).mockResolvedValueOnce(categories);

    await expect(loadPublicPortfolio()).resolves.toEqual({ items, categories });
  });

  it("rejects when items API fails", async () => {
    const failure = new Error("items failed");
    vi.mocked(getPublicPortfolioItems).mockRejectedValueOnce(failure);
    vi.mocked(getPublicPortfolioCategories).mockResolvedValueOnce([category]);

    await expect(loadPublicPortfolio()).rejects.toBe(failure);
  });

  it("rejects when categories API fails", async () => {
    const failure = new Error("categories failed");
    vi.mocked(getPublicPortfolioItems).mockResolvedValueOnce([cmsItem()]);
    vi.mocked(getPublicPortfolioCategories).mockRejectedValueOnce(failure);

    await expect(loadPublicPortfolio()).rejects.toBe(failure);
  });

  it("accepts a production root-relative imageUrl without throwing", async () => {
    vi.mocked(getPublicPortfolioItems).mockResolvedValueOnce([
      cmsItem({ imageUrl: "/api/portfolio/images/id" }),
    ]);
    vi.mocked(getPublicPortfolioCategories).mockResolvedValueOnce([category]);

    const result = await loadPublicPortfolio();

    expect(result.items[0]?.imageUrl).toBe("/api/portfolio/images/id");
  });

  it("treats empty successful arrays as a valid CMS response", async () => {
    vi.mocked(getPublicPortfolioItems).mockResolvedValueOnce([]);
    vi.mocked(getPublicPortfolioCategories).mockResolvedValueOnce([]);

    await expect(loadPublicPortfolio()).resolves.toEqual({ items: [], categories: [] });
  });

  it("preserves refresh rejection semantics by rejecting the shared loader promise", async () => {
    const failure = new Error("network down");
    vi.mocked(getPublicPortfolioItems).mockRejectedValueOnce(failure);
    vi.mocked(getPublicPortfolioCategories).mockResolvedValueOnce([category]);

    // refresh() awaits loadPublicPortfolio() before setState; callers use refresh().catch(...)
    await expect(
      (async () => {
        const data = await loadPublicPortfolio();
        return data;
      })(),
    ).rejects.toBe(failure);
  });
});

describe("typed portfolio fallback data", () => {
  it("keeps fallback portfolio content unchanged", () => {
    expect(PORTFOLIO_ITEMS).toHaveLength(8);
    expect(PORTFOLIO_ITEMS.map((item) => item.id)).toEqual([
      "fallback-1",
      "fallback-2",
      "fallback-3",
      "fallback-4",
      "fallback-5",
      "fallback-6",
      "fallback-7",
      "fallback-8",
    ]);
    expect(PORTFOLIO_ITEMS[0]?.title).toBe("Custom Dressmaking");
    expect(PORTFOLIO_CATEGORIES.map((item) => item.id)).toEqual([
      "fallback-tailoring",
      "fallback-dressmaking",
      "fallback-alterations",
      "fallback-memory-bears",
    ]);
  });
});
