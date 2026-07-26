import { describe, expect, it } from "vitest";
import { NAV_LINKS } from "../../data/siteData";
import { getBrandNavigation } from "../siteSettings/brandNavigation";
import type { BrandNavigationSettings, PublicInStockItem } from "../types";
import {
  assertInStockNavOrder,
  buildInStockEnquiryMessage,
  buildInStockItemListJsonLd,
  buildInStockProductJsonLd,
  buildInStockSeoDescription,
  countNavPage,
  formatInStockPriceGbp,
  getInStockCanonicalPath,
  getInStockCtaHref,
  getInStockCtaLabel,
  getInStockEnquirySubject,
  getPrimaryInStockImageUrl,
  getPrimaryPublicInStockImage,
  IN_STOCK_CATALOGUE_INTRO,
  IN_STOCK_PUBLIC_META_SEPARATOR,
  mapStatusToSchemaAvailability,
  serializeJsonLd,
  shouldShowInStockEmptyState,
  sortPublicInStockImages,
} from "./publicInStockHelpers";

const MOJIBAKE_PATTERN = /вЂ|Рђ|Р‚|РЎ|â€|ï¿½|\uFFFD/;

function item(overrides: Partial<PublicInStockItem> = {}): PublicInStockItem {
  return {
    id: "item-1",
    slug: "silk-blouse",
    title: "Silk blouse",
    shortDescription: "A soft silk blouse.",
    description: "Full description of the silk blouse.",
    price: 85,
    currency: "GBP",
    status: "Available",
    sizes: "UK 10",
    materials: "Silk",
    images: [
      {
        id: "img-2",
        imageUrl: "/api/in-stock/images/img-2",
        altText: "Side",
        displayOrder: 1,
      },
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

const brandNav: BrandNavigationSettings = {
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

describe("publicInStockHelpers", () => {
  it("public IN STOCK user-facing strings have no mojibake", () => {
    const strings = [
      IN_STOCK_CATALOGUE_INTRO,
      IN_STOCK_PUBLIC_META_SEPARATOR,
      buildInStockSeoDescription(item({ shortDescription: null, description: null })),
      getInStockCtaLabel("Available"),
      getInStockCtaLabel("Reserved"),
      getInStockCtaLabel("Sold"),
      buildInStockEnquiryMessage("Silk blouse", "silk-blouse", "Available"),
      buildInStockEnquiryMessage("Silk blouse", "silk-blouse", "Reserved"),
      buildInStockEnquiryMessage("Silk blouse", "silk-blouse", "Sold"),
      getInStockEnquirySubject("Silk blouse", "Available"),
      getInStockEnquirySubject("Silk blouse", "Reserved"),
      getInStockEnquirySubject("Silk blouse", "Sold"),
      "Message received \u2014 thank you.",
    ];
    for (const value of strings) {
      expect(value).not.toMatch(MOJIBAKE_PATTERN);
    }
    expect(IN_STOCK_CATALOGUE_INTRO).toContain("\u2014");
    expect(IN_STOCK_CATALOGUE_INTRO).toBe(
      "Finished pieces available now. Quantities are limited. To buy a piece, contact the studio \u2014 there is no online checkout.",
    );
    expect(IN_STOCK_PUBLIC_META_SEPARATOR).toBe("\u00b7");
    expect(
      buildInStockSeoDescription(
        item({ shortDescription: null, description: null }),
      ),
    ).toBe("Silk blouse \u2014 ready-to-buy piece from Bespoke Sewing Studio.");
  });

  it("builds status-specific enquiry messages and subjects", () => {
    const available = buildInStockEnquiryMessage(
      "Silk blouse",
      "silk-blouse",
      "Available",
    );
    expect(available).toContain("viewing or purchase");
    expect(available).toContain('IN STOCK piece "Silk blouse"');
    expect(getInStockEnquirySubject("Silk blouse", "Available")).toBe(
      "Enquiry: Silk blouse",
    );

    const reserved = buildInStockEnquiryMessage(
      "Silk blouse",
      "silk-blouse",
      "Reserved",
    );
    expect(reserved).toContain("reserved IN STOCK piece");
    expect(reserved).toContain("whether it may become available");
    expect(getInStockEnquirySubject("Silk blouse", "Reserved")).toBe(
      "Availability: Silk blouse",
    );

    const sold = buildInStockEnquiryMessage("Silk blouse", "silk-blouse", "Sold");
    expect(sold).toContain("commissioning something similar");
    expect(sold.toLowerCase()).not.toContain("purchase");
    expect(sold.toLowerCase()).not.toContain("viewing");
    expect(getInStockEnquirySubject("Silk blouse", "Sold")).toBe(
      "Similar to IN STOCK: Silk blouse",
    );
  });

  it("formats GBP prices", () => {
    expect(formatInStockPriceGbp(85, "GBP")).toMatch(/£85\.00/);
  });

  it("orders images by displayOrder then id and picks primary", () => {
    const ordered = sortPublicInStockImages(item().images);
    expect(ordered.map((image) => image.id)).toEqual(["img-1", "img-2"]);
    expect(getPrimaryPublicInStockImage(item().images)?.id).toBe("img-1");
    expect(getPrimaryInStockImageUrl(item().images)).toBe(
      "/api/in-stock/images/img-1",
    );
  });

  it("handles empty or malformed image lists", () => {
    expect(getPrimaryPublicInStockImage([])).toBeNull();
    expect(getPrimaryInStockImageUrl([])).toBeNull();
  });

  it("shows empty state only after a successful empty response", () => {
    expect(
      shouldShowInStockEmptyState({
        loading: false,
        hasLoadedSuccessfully: true,
        error: null,
        itemCount: 0,
      }),
    ).toBe(true);
    expect(
      shouldShowInStockEmptyState({
        loading: false,
        hasLoadedSuccessfully: false,
        error: "failed",
        itemCount: 0,
      }),
    ).toBe(false);
    expect(
      shouldShowInStockEmptyState({
        loading: true,
        hasLoadedSuccessfully: false,
        error: null,
        itemCount: 0,
      }),
    ).toBe(false);
  });

  it("maps CTA labels by status", () => {
    expect(getInStockCtaLabel("Available")).toBe("Enquire about this piece");
    expect(getInStockCtaLabel("Reserved")).toBe("Ask about availability");
    expect(getInStockCtaLabel("Sold")).toBe("Request something similar");
  });

  it("builds contact enquiry href with URL-encoded subject/message query params", () => {
    const title = "Coat & Dress";
    const href = getInStockCtaHref(
      item({ title, slug: "coat-dress", status: "Sold" }),
    );
    expect(href.startsWith("/contact?")).toBe(true);

    const query = href.slice("/contact?".length);
    // URLSearchParams application/x-www-form-urlencoded encoding.
    expect(query).toContain("Coat+%26+Dress");
    expect(query).toMatch(/message=.+/);

    const params = new URLSearchParams(query);
    expect(params.get("subject")).toBe("Similar to IN STOCK: Coat & Dress");
    expect(params.get("message")).toContain("commissioning something similar");
    expect(params.get("message")).toContain("coat-dress");
    expect(params.get("message")?.toLowerCase()).not.toContain("purchase");
    expect(params.get("message")?.toLowerCase()).not.toContain("viewing");
  });

  it("builds SEO description with short/long fallbacks", () => {
    expect(buildInStockSeoDescription(item())).toBe("A soft silk blouse.");
    expect(
      buildInStockSeoDescription(
        item({ shortDescription: null, description: "Longer text" }),
      ),
    ).toBe("Longer text");
    expect(
      buildInStockSeoDescription(
        item({ shortDescription: null, description: null }),
      ),
    ).toContain("Silk blouse");
  });

  it("builds canonical path and Product JSON-LD availability", () => {
    expect(getInStockCanonicalPath("silk-blouse")).toBe("/in-stock/silk-blouse");
    expect(mapStatusToSchemaAvailability("Available")).toContain("InStock");
    expect(mapStatusToSchemaAvailability("Reserved")).toContain(
      "LimitedAvailability",
    );
    expect(mapStatusToSchemaAvailability("Sold")).toContain("SoldOut");

    const product = buildInStockProductJsonLd({
      item: item({ status: "Sold" }),
      pageUrl: "https://oksanalogosha.com/in-stock/silk-blouse",
      imageUrl: "https://oksanalogosha.com/api/in-stock/images/img-1",
    });
    expect(product["@type"]).toBe("Product");
    expect((product.offers as { availability: string }).availability).toContain(
      "SoldOut",
    );
    expect(product).not.toHaveProperty("aggregateRating");
  });

  it("serializes JSON-LD safely and builds ItemList", () => {
    const list = buildInStockItemListJsonLd(
      [item()],
      "https://oksanalogosha.com/in-stock",
    );
    expect(list["@type"]).toBe("ItemList");
    const serialized = serializeJsonLd({
      "@type": "Product",
      name: "</script><script>alert(1)</script>",
    });
    expect(serialized).not.toContain("</script>");
    expect(serialized).toContain("\\u003c/script>");
  });

  it("keeps Services → IN STOCK → Portfolio order without duplicates", () => {
    expect(assertInStockNavOrder(NAV_LINKS)).toBe(true);
    expect(countNavPage(NAV_LINKS, "inStock")).toBe(1);
    const visible = getBrandNavigation(brandNav);
    expect(assertInStockNavOrder(visible)).toBe(true);
    expect(countNavPage(visible, "inStock")).toBe(1);
    const pages = visible.map((link) => link.page);
    expect(pages.indexOf("services")).toBeLessThan(pages.indexOf("inStock"));
    expect(pages.indexOf("inStock")).toBeLessThan(pages.indexOf("portfolio"));
  });

});
