import { useState } from "react";
import { Link } from "react-router-dom";
import type { PublicInStockItem } from "../types";
import {
  formatInStockPriceGbp,
  getInStockCtaHref,
  getInStockCtaLabel,
  getPrimaryPublicInStockImage,
  IN_STOCK_PUBLIC_META_SEPARATOR,
} from "../inStock/publicInStockHelpers";

export function InStockCard({
  item,
  priority = false,
}: {
  item: PublicInStockItem;
  priority?: boolean;
}) {
  const primary = getPrimaryPublicInStockImage(item.images);
  const [imageFailed, setImageFailed] = useState(false);
  const detailHref = `/in-stock/${item.slug}`;
  const isSold = item.status === "Sold";
  const isReserved = item.status === "Reserved";
  const showImage = Boolean(primary) && !imageFailed;

  return (
    <article
      className={`border border-border bg-card flex flex-col ${
        isSold ? "opacity-80" : ""
      }`}
    >
      <Link
        to={detailHref}
        className="block relative aspect-[3/4] bg-muted overflow-hidden focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-foreground"
        aria-label={`View details for ${item.title}`}
      >
        {showImage && primary ? (
          <img
            src={primary.imageUrl}
            alt={primary.altText || item.title}
            className="w-full h-full object-cover"
            loading={priority ? "eager" : "lazy"}
            decoding="async"
            onError={() => setImageFailed(true)}
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-[11px] text-muted-foreground font-sans">
            Photo coming soon
          </div>
        )}
        <span
          className={`absolute left-3 top-3 px-2 py-1 text-[9px] tracking-[0.16em] uppercase font-sans ${
            isSold
              ? "bg-foreground text-primary-foreground"
              : isReserved
                ? "bg-amber-700 text-white"
                : "bg-background/95 text-foreground border border-border"
          }`}
        >
          {item.status}
        </span>
      </Link>

      <div className="p-4 flex flex-col gap-2 flex-1">
        <h2 className="font-serif text-xl font-light leading-snug">
          <Link
            to={detailHref}
            className="hover:text-accent focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-foreground"
          >
            {item.title}
          </Link>
        </h2>
        {item.shortDescription ? (
          <p className="text-[12px] text-muted-foreground font-sans line-clamp-3">
            {item.shortDescription}
          </p>
        ) : null}
        <p className="text-[13px] font-sans text-foreground">
          {formatInStockPriceGbp(item.price, item.currency)}
        </p>
        {(item.sizes || item.materials) && (
          <p className="text-[11px] text-muted-foreground font-sans">
            {[item.sizes, item.materials]
              .filter(Boolean)
              .join(` ${IN_STOCK_PUBLIC_META_SEPARATOR} `)}
          </p>
        )}
        <div className="mt-auto pt-3 flex flex-wrap gap-3">
          <Link
            to={detailHref}
            className="text-[11px] tracking-[0.14em] uppercase font-sans border-b border-foreground pb-0.5 hover:text-accent hover:border-accent"
          >
            View details
          </Link>
          {!isSold ? (
            <Link
              to={getInStockCtaHref(item)}
              className="text-[11px] tracking-[0.14em] uppercase font-sans text-muted-foreground border-b border-border pb-0.5 hover:text-foreground hover:border-foreground"
            >
              {getInStockCtaLabel(item.status)}
            </Link>
          ) : null}
        </div>
      </div>
    </article>
  );
}
