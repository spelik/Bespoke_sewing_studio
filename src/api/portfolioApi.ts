import type { PortfolioCategory } from "../app/types";
import { PORTFOLIO_CATEGORIES, PORTFOLIO_ITEMS } from "../data/portfolioData";
import { apiClient } from "./apiClient";

export const getPortfolioItems = (category?: PortfolioCategory) => {
  const items = category
    ? PORTFOLIO_ITEMS.filter((item) => item.category === category)
    : PORTFOLIO_ITEMS;

  return apiClient.resolve(items);
};

export const getPortfolioCategories = () => apiClient.resolve(PORTFOLIO_CATEGORIES);
