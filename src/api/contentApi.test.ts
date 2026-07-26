import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
  AdminPageContent,
  PageContentSection,
  PublicPageContent,
  SavePageContentRequest,
} from "../app/types";

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
  createContent,
  getAdminContent,
  getPublicPageContent,
  updateContent,
} from "./contentApi";

function section(overrides: Partial<PageContentSection> = {}): PageContentSection {
  return {
    id: "section-1",
    sectionKey: "hero",
    title: "Hero",
    subtitle: null,
    body: null,
    ctaLabel: null,
    ctaUrl: null,
    imageUrl: "/api/content/images/id",
    imageAltText: "Hero image",
    displayOrder: 0,
    ...overrides,
  };
}

function adminContent(overrides: Partial<AdminPageContent> = {}): AdminPageContent {
  return {
    ...section(),
    pageKey: "home",
    imageFileId: "11111111-1111-1111-1111-111111111111",
    isActive: true,
    updatedAt: "2026-07-01T00:00:00Z",
    archivedAt: null,
    ...overrides,
  };
}

function publicContent(overrides: Partial<PublicPageContent> = {}): PublicPageContent {
  return {
    pageKey: "home",
    sections: [section()],
    ...overrides,
  };
}

const saveRequest: SavePageContentRequest = {
  pageKey: "home",
  sectionKey: "hero",
  title: "Hero",
  subtitle: null,
  body: null,
  ctaLabel: null,
  ctaUrl: null,
  imageFileId: "11111111-1111-1111-1111-111111111111",
  imageAltText: "Hero image",
  displayOrder: 0,
  isActive: true,
};

beforeEach(() => {
  vi.clearAllMocks();
});

describe("contentApi imageUrl mapping", () => {
  it("getPublicPageContent keeps root-relative imageUrl when apiBaseUrl is /api", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(publicContent());

    await expect(getPublicPageContent("home")).resolves.toEqual(
      publicContent({
        sections: [section({ imageUrl: "/api/content/images/id" })],
      }),
    );
    expect(apiClient.get).toHaveBeenCalledWith("content/pages/home");
  });

  it("getPublicPageContent keeps null imageUrl as null", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(
      publicContent({ sections: [section({ imageUrl: null })] }),
    );

    const result = await getPublicPageContent("home");

    expect(result.sections[0]?.imageUrl).toBeNull();
  });

  it("getPublicPageContent leaves absolute HTTPS imageUrl unchanged", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(
      publicContent({
        sections: [section({ imageUrl: "https://cdn.example.com/content.jpg" })],
      }),
    );

    const result = await getPublicPageContent("home");

    expect(result.sections[0]?.imageUrl).toBe("https://cdn.example.com/content.jpg");
  });

  it("getAdminContent returns empty array without error", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce([]);

    await expect(getAdminContent()).resolves.toEqual([]);
    expect(apiClient.get).toHaveBeenCalledWith("admin/content");
  });

  it("getAdminContent maps root-relative imageUrl without TypeError", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce([adminContent()]);

    const result = await getAdminContent();

    expect(result).toHaveLength(1);
    expect(result[0]?.imageUrl).toBe("/api/content/images/id");
  });

  it("createContent normalizes imageUrl through resolveApiAssetUrl", async () => {
    vi.mocked(apiClient.post).mockResolvedValueOnce(adminContent());

    const result = await createContent(saveRequest);

    expect(result.imageUrl).toBe("/api/content/images/id");
    expect(apiClient.post).toHaveBeenCalledWith("admin/content", saveRequest);
  });

  it("updateContent normalizes imageUrl through resolveApiAssetUrl", async () => {
    vi.mocked(apiClient.patch).mockResolvedValueOnce(adminContent());

    const result = await updateContent("section-1", saveRequest);

    expect(result.imageUrl).toBe("/api/content/images/id");
    expect(apiClient.patch).toHaveBeenCalledWith("admin/content/section-1", saveRequest);
  });

  it("createContent keeps null imageUrl as null after normalize", async () => {
    vi.mocked(apiClient.post).mockResolvedValueOnce(
      adminContent({ imageUrl: null, imageFileId: null }),
    );

    const result = await createContent(saveRequest);

    expect(result.imageUrl).toBeNull();
  });
});
