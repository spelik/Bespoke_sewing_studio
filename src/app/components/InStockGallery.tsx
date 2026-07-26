import { useState } from "react";
import type { PublicInStockImage } from "../types";
import { sortPublicInStockImages } from "../inStock/publicInStockHelpers";

export function markInStockGalleryImageFailed(
  failedIds: ReadonlySet<string>,
  imageId: string,
): ReadonlySet<string> {
  if (failedIds.has(imageId)) {
    return failedIds;
  }
  const next = new Set(failedIds);
  next.add(imageId);
  return next;
}

export function isInStockGalleryImageFailed(
  failedIds: ReadonlySet<string>,
  imageId: string,
): boolean {
  return failedIds.has(imageId);
}

export function getInStockGalleryThumbnailAriaLabel(
  index: number,
  total: number,
  failed: boolean,
): string {
  const base = `Show photograph ${index + 1} of ${total}`;
  return failed ? `${base}, unavailable` : base;
}

export function InStockGallery({
  images,
  title,
}: {
  images: readonly PublicInStockImage[];
  title: string;
}) {
  const ordered = sortPublicInStockImages(images);
  const [selectedId, setSelectedId] = useState(ordered[0]?.id ?? null);
  const [failedIds, setFailedIds] = useState<ReadonlySet<string>>(new Set());
  const selected =
    ordered.find((image) => image.id === selectedId) ?? ordered[0] ?? null;

  if (!selected) {
    return (
      <div className="aspect-[3/4] bg-muted flex items-center justify-center text-[12px] text-muted-foreground font-sans">
        No photographs available
      </div>
    );
  }

  const mainFailed = isInStockGalleryImageFailed(failedIds, selected.id);

  return (
    <div className="space-y-3">
      <div className="aspect-[3/4] bg-muted overflow-hidden">
        {mainFailed ? (
          <div className="w-full h-full flex items-center justify-center text-[12px] text-muted-foreground font-sans">
            Photograph unavailable
          </div>
        ) : (
          <img
            src={selected.imageUrl}
            alt={selected.altText || title}
            className="w-full h-full object-cover"
            decoding="async"
            onError={() =>
              setFailedIds((current) =>
                markInStockGalleryImageFailed(current, selected.id),
              )
            }
          />
        )}
      </div>
      {ordered.length > 1 ? (
        <ul className="grid grid-cols-4 sm:grid-cols-5 gap-2" role="list">
          {ordered.map((image, index) => {
            const isActive = image.id === selected.id;
            const thumbFailed = isInStockGalleryImageFailed(failedIds, image.id);
            return (
              <li key={image.id}>
                <button
                  type="button"
                  onClick={() => setSelectedId(image.id)}
                  aria-label={getInStockGalleryThumbnailAriaLabel(
                    index,
                    ordered.length,
                    thumbFailed,
                  )}
                  aria-pressed={isActive}
                  className={`block w-full aspect-square overflow-hidden border focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-foreground ${
                    isActive ? "border-foreground" : "border-border"
                  }`}
                >
                  {thumbFailed ? (
                    <div
                      className="w-full h-full bg-muted flex items-center justify-center"
                      aria-hidden="true"
                    >
                      <span className="text-[9px] text-muted-foreground font-sans">
                        N/A
                      </span>
                    </div>
                  ) : (
                    <img
                      src={image.imageUrl}
                      alt=""
                      className="w-full h-full object-cover"
                      loading="lazy"
                      decoding="async"
                      onError={() =>
                        setFailedIds((current) =>
                          markInStockGalleryImageFailed(current, image.id),
                        )
                      }
                    />
                  )}
                </button>
              </li>
            );
          })}
        </ul>
      ) : null}
    </div>
  );
}
