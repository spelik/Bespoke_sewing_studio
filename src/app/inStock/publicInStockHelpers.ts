import { formatInStockPriceGbp } from "../components/adminInStockHelpers";
import type {
  InStockItemStatus,
  NavigationItem,
  Page,
  PublicInStockImage,
  PublicInStockItem,
} from "../types";

export { formatInStockPriceGbp };

/** Catalogue hero copy; em dash via escape to avoid source mojibake. */
export const IN_STOCK_CATALOGUE_INTRO =
  "Finished pieces available now. Quantities are limited. To buy a piece, contact the studio \u2014 there is no online checkout.";

export const IN_STOCK_PUBLIC_META_SEPARATOR = "\u00b7";

export function sortPublicInStockImages(
  images: readonly PublicInStockImage[],
): PublicInStockImage[] {
  return [...images].sort((left, right) => {
    if (left.displayOrder !== right.displayOrder) {
      return left.displayOrder - right.displayOrder;
    }
    return left.id.localeCompare(right.id);
  });
}

export function getPrimaryPublicInStockImage(
  images: readonly PublicInStockImage[],
): PublicInStockImage | null {
  return sortPublicInStockImages(images)[0] ?? null;
}

export function shouldShowInStockEmptyState(options: {
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

export function getInStockCtaLabel(status: InStockItemStatus): string {
  switch (status) {
    case "Available":
      return "Enquire about this piece";
    case "Reserved":
      return "Ask about availability";
    case "Sold":
      return "Request something similar";
    default:
      return "Enquire about this piece";
  }
}

export function buildInStockEnquiryMessage(
  title: string,
  slug: string,
  status: InStockItemStatus,
): string {
  switch (status) {
    case "Reserved":
      return `Hello,\n\nI am interested in the reserved IN STOCK piece "${title}" (reference: ${slug}).\n\nPlease let me know whether it may become available.\n\nThank you.`;
    case "Sold":
      return `Hello,\n\nI like the sold IN STOCK piece "${title}" (reference: ${slug}).\n\nI would like to enquire about commissioning something similar.\n\nThank you.`;
    case "Available":
    default:
      return `Hello,\n\nI am interested in the IN STOCK piece "${title}" (reference: ${slug}).\n\nPlease let me know how I can arrange viewing or purchase.\n\nThank you.`;
  }
}

export function getInStockEnquirySubject(
  title: string,
  status: InStockItemStatus,
): string {
  switch (status) {
    case "Sold":
      return `Similar to IN STOCK: ${title}`;
    case "Reserved":
      return `Availability: ${title}`;
    case "Available":
    default:
      return `Enquiry: ${title}`;
  }
}

export function getInStockCtaHref(
  item: Pick<PublicInStockItem, "title" | "slug" | "status">,
): string {
  const params = new URLSearchParams({
    subject: getInStockEnquirySubject(item.title, item.status),
    message: buildInStockEnquiryMessage(item.title, item.slug, item.status),
  });
  return `/contact?${params.toString()}`;
}

export function buildInStockSeoDescription(item: PublicInStockItem): string {
  const fromShort = item.shortDescription?.trim();
  if (fromShort) {
    return fromShort.slice(0, 300);
  }
  const fromLong = item.description?.trim();
  if (fromLong) {
    return fromLong.slice(0, 300);
  }
  return `${item.title} \u2014 ready-to-buy piece from Bespoke Sewing Studio.`;
}

export function getInStockCanonicalPath(slug: string): string {
  return `/in-stock/${slug}`;
}

export function getPrimaryInStockImageUrl(
  images: readonly PublicInStockImage[],
): string | null {
  return getPrimaryPublicInStockImage(images)?.imageUrl ?? null;
}

export function mapStatusToSchemaAvailability(status: InStockItemStatus): string {
  switch (status) {
    case "Available":
      return "https://schema.org/InStock";
    case "Reserved":
      return "https://schema.org/LimitedAvailability";
    case "Sold":
      return "https://schema.org/SoldOut";
    default:
      return "https://schema.org/InStock";
  }
}

export function buildInStockItemListJsonLd(
  items: readonly PublicInStockItem[],
  catalogueUrl: string,
): Record<string, unknown> {
  return {
    "@context": "https://schema.org",
    "@type": "ItemList",
    url: catalogueUrl,
    numberOfItems: items.length,
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      url: `${catalogueUrl.replace(/\/$/, "")}/${item.slug}`,
      name: item.title,
    })),
  };
}

export function buildInStockProductJsonLd(options: {
  item: PublicInStockItem;
  pageUrl: string;
  imageUrl: string | null;
}): Record<string, unknown> {
  const { item, pageUrl, imageUrl } = options;
  return {
    "@context": "https://schema.org",
    "@type": "Product",
    name: item.title,
    description: buildInStockSeoDescription(item),
    url: pageUrl,
    image: imageUrl ? [imageUrl] : undefined,
    offers: {
      "@type": "Offer",
      price: item.price.toFixed(2),
      priceCurrency: item.currency || "GBP",
      availability: mapStatusToSchemaAvailability(item.status),
      url: pageUrl,
    },
  };
}

/** JSON-LD text for script tags; stringify escapes unsafe characters. */
export function serializeJsonLd(value: Record<string, unknown>): string {
  return JSON.stringify(value).replace(/</g, "\\u003c");
}

export function assertInStockNavOrder(items: readonly NavigationItem[]): boolean {
  const pages = items.map((item) => item.page);
  const services = pages.indexOf("services");
  const inStock = pages.indexOf("inStock");
  const portfolio = pages.indexOf("portfolio");
  if (services < 0 || inStock < 0 || portfolio < 0) {
    return false;
  }
  return services < inStock && inStock < portfolio;
}

export function countNavPage(items: readonly NavigationItem[], page: Page): number {
  return items.filter((item) => item.page === page).length;
}
