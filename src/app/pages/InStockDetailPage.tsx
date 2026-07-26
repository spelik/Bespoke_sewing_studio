import { useEffect, useId, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ApiError } from "../../api/apiClient";
import { getPublicInStockItemBySlug } from "../../api/inStockApi";
import { appConfig } from "../../config/appConfig";
import { InStockGallery } from "../components/InStockGallery";
import {
  buildInStockProductJsonLd,
  buildInStockSeoDescription,
  formatInStockPriceGbp,
  getInStockCanonicalPath,
  getInStockCtaHref,
  getInStockCtaLabel,
  getPrimaryInStockImageUrl,
} from "../inStock/publicInStockHelpers";
import { useSeoOverride } from "../seo/SeoOverrideContext";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";
import type { PublicInStockItem } from "../types";
import { NotFoundPage } from "./NotFoundPage";

export function InStockDetailPage() {
  const { slug = "" } = useParams<{ slug: string }>();
  const { brand } = useSiteSettings();
  const { setOverride } = useSeoOverride();
  const statusId = useId();
  const [item, setItem] = useState<PublicInStockItem | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    async function load() {
      setLoading(true);
      setError(null);
      setNotFound(false);
      setItem(null);
      try {
        const next = await getPublicInStockItemBySlug(slug);
        if (!active) {
          return;
        }
        setItem(next);
      } catch (reason) {
        if (!active) {
          return;
        }
        if (reason instanceof ApiError && reason.status === 404) {
          setNotFound(true);
          return;
        }
        setError(
          reason instanceof ApiError
            ? reason.message
            : "This piece could not be loaded. Please try again.",
        );
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    if (!slug) {
      setNotFound(true);
      setLoading(false);
      return;
    }

    void load();
    return () => {
      active = false;
    };
  }, [slug]);

  useEffect(() => {
    if (notFound) {
      setOverride({
        title: "Page not found | Bespoke Sewing Studio",
        description: "The requested IN STOCK piece could not be found.",
        robots: "noindex, nofollow",
        canonicalPath: getInStockCanonicalPath(slug || "not-found"),
        structuredData: null,
        ogImageUrl: null,
      });
      return () => setOverride(null);
    }

    if (!item) {
      setOverride(null);
      return;
    }

    const origin = (appConfig.publicSiteUrl ?? "").replace(/\/+$/, "");
    const path = getInStockCanonicalPath(item.slug);
    const pageUrl = origin ? `${origin}${path}` : path;
    const imagePath = getPrimaryInStockImageUrl(item.images);
    const imageUrl = imagePath
      ? (() => {
          try {
            return new URL(imagePath, origin || window.location.origin).toString();
          } catch {
            return imagePath;
          }
        })()
      : null;

    setOverride({
      title: `${item.title} | IN STOCK | Bespoke Sewing Studio`,
      description: buildInStockSeoDescription(item),
      canonicalPath: path,
      ogImageUrl: imageUrl,
      robots: "index, follow",
      structuredData: buildInStockProductJsonLd({
        item,
        pageUrl,
        imageUrl,
      }),
    });

    return () => setOverride(null);
  }, [item, notFound, setOverride, slug]);

  if (notFound) {
    return <NotFoundPage />;
  }

  if (loading) {
    return (
      <div className="pt-[72px] px-6 lg:px-10 py-16 max-w-7xl mx-auto">
        <div className="sr-only" aria-live="polite">
          Loading piece details
        </div>
        <div className="grid lg:grid-cols-2 gap-10 animate-pulse">
          <div className="aspect-[3/4] bg-muted" />
          <div className="space-y-4">
            <div className="h-8 bg-muted w-2/3" />
            <div className="h-4 bg-muted w-1/3" />
            <div className="h-24 bg-muted w-full" />
          </div>
        </div>
      </div>
    );
  }

  if (error || !item) {
    return (
      <div className="pt-[72px] px-6 lg:px-10 py-16 max-w-3xl mx-auto space-y-4">
        <p role="alert" className="border border-destructive/30 px-4 py-3 text-[12px] text-destructive">
          {error ?? "This piece could not be loaded."}
        </p>
        <Link to="/in-stock" className="text-[12px] font-sans border-b border-foreground pb-0.5">
          Back to IN STOCK
        </Link>
      </div>
    );
  }

  const ctaHref = getInStockCtaHref(item);
  const ctaLabel = getInStockCtaLabel(item.status);

  return (
    <div className="pt-[72px]">
      <div className="max-w-7xl mx-auto px-6 lg:px-10 py-10 lg:py-16">
        <nav aria-label="Breadcrumb" className="text-[11px] font-sans text-muted-foreground mb-8">
          <ol className="flex flex-wrap gap-2 items-center">
            <li>
              <Link to="/" className="hover:text-foreground">
                Home
              </Link>
            </li>
            <li aria-hidden="true">/</li>
            <li>
              <Link to="/in-stock" className="hover:text-foreground">
                {brand.navigation.inStockLabel || "IN STOCK"}
              </Link>
            </li>
            <li aria-hidden="true">/</li>
            <li className="text-foreground" aria-current="page">
              {item.title}
            </li>
          </ol>
        </nav>

        <div id={statusId} className="sr-only" aria-live="polite">
          {item.title}, {item.status}, {formatInStockPriceGbp(item.price, item.currency)}
        </div>

        <div className="grid lg:grid-cols-2 gap-10 lg:gap-16">
          <InStockGallery images={item.images} title={item.title} />

          <div className="space-y-6">
            <div className="space-y-3">
              <p className="text-[10px] tracking-[0.2em] uppercase font-sans text-muted-foreground">
                {item.status}
              </p>
              <h1 className="font-serif text-[2.5rem] lg:text-[3.5rem] font-light leading-tight">
                {item.title}
              </h1>
              <p className="text-[16px] font-sans">
                {formatInStockPriceGbp(item.price, item.currency)}
              </p>
            </div>

            {item.shortDescription ? (
              <p className="text-[14px] text-muted-foreground font-sans leading-relaxed">
                {item.shortDescription}
              </p>
            ) : null}

            {item.description ? (
              <div className="text-[14px] font-sans leading-relaxed whitespace-pre-wrap">
                {item.description}
              </div>
            ) : null}

            <dl className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-[12px] font-sans">
              {item.sizes ? (
                <div>
                  <dt className="text-muted-foreground uppercase tracking-[0.14em] text-[10px] mb-1">
                    Sizes
                  </dt>
                  <dd>{item.sizes}</dd>
                </div>
              ) : null}
              {item.materials ? (
                <div>
                  <dt className="text-muted-foreground uppercase tracking-[0.14em] text-[10px] mb-1">
                    Materials
                  </dt>
                  <dd>{item.materials}</dd>
                </div>
              ) : null}
            </dl>

            <div className="flex flex-wrap gap-4 pt-2">
              <Link
                to={ctaHref}
                className="inline-flex bg-foreground text-primary-foreground px-6 py-3 text-[11px] tracking-[0.14em] uppercase font-sans hover:bg-accent"
              >
                {ctaLabel}
              </Link>
              <Link
                to="/in-stock"
                className="inline-flex items-center text-[11px] tracking-[0.14em] uppercase font-sans border-b border-border pb-0.5 hover:border-foreground"
              >
                Back to IN STOCK
              </Link>
            </div>

            <p className="text-[12px] text-muted-foreground font-sans">
              Purchase is arranged directly with the studio. Sending an enquiry does not
              reserve this piece.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
