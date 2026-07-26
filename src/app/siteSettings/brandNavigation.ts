import { NAV_LINKS } from "../appContent";
import type { BrandNavigationSettings, NavigationItem, Page } from "../types";

export function getBrandNavigation(n: BrandNavigationSettings): NavigationItem[] {
  const values: Partial<Record<Page, { show: boolean; label: string }>> = {
    services: { show: n.showServicesLink, label: n.servicesLabel },
    inStock: { show: n.showInStockLink, label: n.inStockLabel },
    portfolio: { show: n.showPortfolioLink, label: n.portfolioLabel },
    order: { show: n.showOrderLink, label: n.orderLabel },
    about: { show: n.showAboutLink, label: n.aboutLabel },
    contact: { show: n.showContactLink, label: n.contactLabel },
  };

  return NAV_LINKS.flatMap((link) => {
    if (link.page === "home") {
      return [link];
    }
    const value = values[link.page];
    if (!value || !value.show) {
      return [];
    }
    return [{ ...link, label: value.label }];
  });
}

/** Test helper: Services → IN STOCK → Portfolio adjacency in the visible nav. */
export function getMainNavPageOrder(items: readonly NavigationItem[]): Page[] {
  return items.map((item) => item.page);
}
