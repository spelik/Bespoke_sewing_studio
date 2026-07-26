import { getPublicPortfolioCategories, getPublicPortfolioItems } from "../../api/portfolioApi";
import type { PortfolioItem, PublicPortfolioCategory } from "../types";

export interface PublicPortfolioData {
  items: PortfolioItem[];
  categories: PublicPortfolioCategory[];
}

/** Loads public CMS portfolio data. Does not touch React state. */
export async function loadPublicPortfolio(): Promise<PublicPortfolioData> {
  const [items, categories] = await Promise.all([
    getPublicPortfolioItems(),
    getPublicPortfolioCategories(),
  ]);
  return { items, categories };
}
