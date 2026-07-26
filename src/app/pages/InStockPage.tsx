import { useCallback, useEffect, useId, useState } from "react";
import { getPublicInStockItems } from "../../api/inStockApi";
import { ApiError } from "../../api/apiClient";
import { appConfig } from "../../config/appConfig";
import { InStockCard } from "../components/InStockCard";
import { SectionLabel } from "../components/SectionLabel";
import {
  buildInStockItemListJsonLd,
  IN_STOCK_CATALOGUE_INTRO,
  shouldShowInStockEmptyState,
} from "../inStock/publicInStockHelpers";
import { useSeoOverride } from "../seo/SeoOverrideContext";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";
import type { PublicInStockItem } from "../types";

export function InStockPage() {
  const { brand } = useSiteSettings();
  const { setOverride } = useSeoOverride();
  const statusId = useId();
  const [items, setItems] = useState<PublicInStockItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [hasLoadedSuccessfully, setHasLoadedSuccessfully] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);

  const retry = useCallback(() => {
    setReloadToken((value) => value + 1);
  }, []);

  useEffect(() => {
    let active = true;

    async function load() {
      setLoading(true);
      setError(null);
      try {
        const next = await getPublicInStockItems();
        if (!active) {
          return;
        }
        setItems(next);
        setHasLoadedSuccessfully(true);
      } catch (reason) {
        if (!active) {
          return;
        }
        setHasLoadedSuccessfully(false);
        setError(
          reason instanceof ApiError
            ? reason.message
            : "The IN STOCK catalogue could not be loaded. Please try again.",
        );
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    void load();
    return () => {
      active = false;
    };
  }, [reloadToken]);

  useEffect(() => {
    const origin = (appConfig.publicSiteUrl ?? "").replace(/\/+$/, "");
    const catalogueUrl = origin ? `${origin}/in-stock` : "/in-stock";
    setOverride({
      structuredData: buildInStockItemListJsonLd(items, catalogueUrl),
    });
    return () => setOverride(null);
  }, [items, setOverride]);

  const showEmpty = shouldShowInStockEmptyState({
    loading,
    hasLoadedSuccessfully,
    error,
    itemCount: items.length,
  });

  return (
    <div className="pt-[72px]">
      <div className="bg-secondary py-20 px-6 lg:px-10">
        <div className="max-w-7xl mx-auto max-w-3xl">
          <SectionLabel text="Ready to buy" />
          <h1 className="font-serif text-[3rem] lg:text-[5rem] font-light text-foreground mt-4 leading-tight">
            {brand.navigation.inStockLabel || "IN STOCK"}
          </h1>
          <p className="mt-6 text-[14px] lg:text-[16px] text-muted-foreground font-sans leading-relaxed">
            {IN_STOCK_CATALOGUE_INTRO}
          </p>
        </div>
      </div>

      <div className="bg-background py-16 px-6 lg:px-10">
        <div className="max-w-7xl mx-auto space-y-10">
          <div id={statusId} className="sr-only" aria-live="polite">
            {loading
              ? "Loading IN STOCK catalogue"
              : error
                ? "IN STOCK catalogue failed to load"
                : showEmpty
                  ? "No ready-to-buy pieces available"
                  : `${items.length} pieces available`}
          </div>

          {loading ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6" aria-hidden="true">
              {Array.from({ length: 6 }).map((_, index) => (
                <div key={index} className="border border-border bg-card animate-pulse">
                  <div className="aspect-[3/4] bg-muted" />
                  <div className="p-4 space-y-3">
                    <div className="h-5 bg-muted w-2/3" />
                    <div className="h-3 bg-muted w-full" />
                    <div className="h-3 bg-muted w-1/3" />
                  </div>
                </div>
              ))}
            </div>
          ) : null}

          {error ? (
            <div className="space-y-3" role="alert">
              <p className="border border-destructive/30 bg-card px-4 py-3 text-[12px] text-destructive font-sans">
                {error}
              </p>
              <button
                type="button"
                className="border border-border px-4 py-2 text-[11px] font-sans hover:border-foreground focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-foreground"
                onClick={retry}
              >
                Retry
              </button>
            </div>
          ) : null}

          {showEmpty ? (
            <p className="text-center py-16 text-muted-foreground font-serif text-lg font-light">
              There are no ready-to-buy pieces available at the moment.
            </p>
          ) : null}

          {!loading && !error && items.length > 0 ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {items.map((item, index) => (
                <InStockCard key={item.id} item={item} priority={index < 3} />
              ))}
            </div>
          ) : null}
        </div>
      </div>
    </div>
  );
}
