import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
  AdminBrandSettings,
  PublicBrandSettings,
  UpdateBrandSettingsRequest,
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
  getAdminBrandSettings,
  getPublicBrandSettings,
  updateAdminBrandSettings,
} from "./brandSettingsApi";

const navigation = {
  showServicesLink: true,
  servicesLabel: "Services",
  showInStockLink: true,
  inStockLabel: "IN STOCK",
  showPortfolioLink: true,
  portfolioLabel: "Portfolio",
  showOrderLink: true,
  orderLabel: "Order",
  showAboutLink: true,
  aboutLabel: "About",
  showContactLink: true,
  contactLabel: "Contact",
};

function publicSettings(overrides: Partial<PublicBrandSettings> = {}): PublicBrandSettings {
  return {
    brandDisplayName: "Bespoke Sewing Studio",
    logoUrl: "/api/brand/images/logo-id",
    logoAltText: "Studio logo",
    faviconUrl: "/api/brand/images/favicon-id",
    headerCtaLabel: "Order",
    headerCtaUrl: "/order",
    defaultMetaTitle: "Bespoke Sewing Studio",
    defaultMetaDescription: "Premium sewing studio",
    defaultOgTitle: "Bespoke Sewing Studio",
    defaultOgDescription: "Premium sewing studio",
    defaultOgImageUrl: "/api/brand/images/og-id",
    navigation,
    ...overrides,
  };
}

function adminSettings(overrides: Partial<AdminBrandSettings> = {}): AdminBrandSettings {
  return {
    ...publicSettings(),
    logoFileId: "11111111-1111-1111-1111-111111111111",
    faviconFileId: "22222222-2222-2222-2222-222222222222",
    defaultOgImageFileId: "33333333-3333-3333-3333-333333333333",
    updatedAt: "2026-07-01T00:00:00Z",
    ...overrides,
  };
}

const updateRequest: UpdateBrandSettingsRequest = {
  brandDisplayName: "Bespoke Sewing Studio",
  logoFileId: "11111111-1111-1111-1111-111111111111",
  logoAltText: "Studio logo",
  faviconFileId: "22222222-2222-2222-2222-222222222222",
  headerCtaLabel: "Order",
  headerCtaUrl: "/order",
  defaultMetaTitle: "Bespoke Sewing Studio",
  defaultMetaDescription: "Premium sewing studio",
  defaultOgTitle: "Bespoke Sewing Studio",
  defaultOgDescription: "Premium sewing studio",
  defaultOgImageFileId: "33333333-3333-3333-3333-333333333333",
  showServicesLink: true,
  servicesLabel: "Services",
  showInStockLink: true,
  inStockLabel: "IN STOCK",
  showPortfolioLink: true,
  portfolioLabel: "Portfolio",
  showOrderLink: true,
  orderLabel: "Order",
  showAboutLink: true,
  aboutLabel: "About",
  showContactLink: true,
  contactLabel: "Contact",
};

beforeEach(() => {
  vi.clearAllMocks();
});

describe("brandSettingsApi asset URL mapping", () => {
  it("getPublicBrandSettings keeps root-relative asset URLs when apiBaseUrl is /api", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(publicSettings());

    const result = await getPublicBrandSettings();

    expect(result.logoUrl).toBe("/api/brand/images/logo-id");
    expect(result.faviconUrl).toBe("/api/brand/images/favicon-id");
    expect(result.defaultOgImageUrl).toBe("/api/brand/images/og-id");
    expect(apiClient.get).toHaveBeenCalledWith("brand-settings/public");
  });

  it("getPublicBrandSettings keeps nullable asset URLs as null", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(
      publicSettings({
        logoUrl: null,
        faviconUrl: null,
        defaultOgImageUrl: null,
      }),
    );

    const result = await getPublicBrandSettings();

    expect(result.logoUrl).toBeNull();
    expect(result.faviconUrl).toBeNull();
    expect(result.defaultOgImageUrl).toBeNull();
  });

  it("getPublicBrandSettings leaves absolute HTTPS asset URLs unchanged", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(
      publicSettings({
        logoUrl: "https://cdn.example.com/logo.png",
        faviconUrl: "https://cdn.example.com/favicon.ico",
        defaultOgImageUrl: "https://cdn.example.com/og.jpg",
      }),
    );

    const result = await getPublicBrandSettings();

    expect(result.logoUrl).toBe("https://cdn.example.com/logo.png");
    expect(result.faviconUrl).toBe("https://cdn.example.com/favicon.ico");
    expect(result.defaultOgImageUrl).toBe("https://cdn.example.com/og.jpg");
  });

  it("getAdminBrandSettings normalizes asset URLs", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(adminSettings());

    const result = await getAdminBrandSettings();

    expect(result.logoUrl).toBe("/api/brand/images/logo-id");
    expect(result.faviconUrl).toBe("/api/brand/images/favicon-id");
    expect(result.defaultOgImageUrl).toBe("/api/brand/images/og-id");
    expect(apiClient.get).toHaveBeenCalledWith("admin/brand-settings");
  });

  it("updateAdminBrandSettings normalizes returned asset URLs", async () => {
    vi.mocked(apiClient.patch).mockResolvedValueOnce(adminSettings());

    const result = await updateAdminBrandSettings(updateRequest);

    expect(result.logoUrl).toBe("/api/brand/images/logo-id");
    expect(result.faviconUrl).toBe("/api/brand/images/favicon-id");
    expect(result.defaultOgImageUrl).toBe("/api/brand/images/og-id");
    expect(apiClient.patch).toHaveBeenCalledWith("admin/brand-settings", updateRequest);
  });

  it("does not alter non-asset Brand Settings fields", async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce(publicSettings());

    const result = await getPublicBrandSettings();

    expect(result.brandDisplayName).toBe("Bespoke Sewing Studio");
    expect(result.logoAltText).toBe("Studio logo");
    expect(result.headerCtaLabel).toBe("Order");
    expect(result.headerCtaUrl).toBe("/order");
    expect(result.defaultMetaTitle).toBe("Bespoke Sewing Studio");
    expect(result.defaultMetaDescription).toBe("Premium sewing studio");
    expect(result.defaultOgTitle).toBe("Bespoke Sewing Studio");
    expect(result.defaultOgDescription).toBe("Premium sewing studio");
    expect(result.navigation).toEqual(navigation);
  });
});
