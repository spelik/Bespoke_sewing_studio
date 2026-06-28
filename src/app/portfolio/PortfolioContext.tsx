import { createContext, type ReactNode, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { getPublicPortfolioCategories, getPublicPortfolioItems } from "../../api/portfolioApi";
import { PORTFOLIO_CATEGORIES, PORTFOLIO_ITEMS } from "../../data/portfolioData";
import type { PortfolioItem, PublicPortfolioCategory } from "../types";

interface PortfolioContextValue {
  items: PortfolioItem[];
  categories: PublicPortfolioCategory[];
  isFallback: boolean;
  refresh(): Promise<void>;
}

const PortfolioContext = createContext<PortfolioContextValue | null>(null);

export function PortfolioProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<PortfolioItem[]>([...PORTFOLIO_ITEMS]);
  const [categories, setCategories] = useState<PublicPortfolioCategory[]>([...PORTFOLIO_CATEGORIES]);
  const [isFallback, setIsFallback] = useState(true);

  const refresh = useCallback(async () => {
    const [nextItems, nextCategories] = await Promise.all([getPublicPortfolioItems(), getPublicPortfolioCategories()]);
    setItems(nextItems);
    setCategories(nextCategories);
    setIsFallback(false);
  }, []);

  useEffect(() => {
    let active = true;
    Promise.all([getPublicPortfolioItems(), getPublicPortfolioCategories()])
      .then(([nextItems, nextCategories]) => {
        if (!active) return;
        setItems(nextItems);
        setCategories(nextCategories);
        setIsFallback(false);
      })
      .catch(() => undefined);
    return () => { active = false; };
  }, []);

  const value = useMemo(() => ({ items, categories, isFallback, refresh }), [items, categories, isFallback, refresh]);
  return <PortfolioContext.Provider value={value}>{children}</PortfolioContext.Provider>;
}

export function usePortfolio(): PortfolioContextValue {
  const value = useContext(PortfolioContext);
  if (!value) throw new Error("usePortfolio must be used within PortfolioProvider.");
  return value;
}
