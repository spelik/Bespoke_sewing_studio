import type { ImgHTMLAttributes } from "react";
import type { ResponsiveImageAsset } from "../types";

interface ResponsiveImageProps
  extends Omit<ImgHTMLAttributes<HTMLImageElement>, "src" | "srcSet" | "sizes" | "fetchPriority"> {
  asset: ResponsiveImageAsset;
  pictureClassName?: string;
  imgClassName?: string;
  fetchPriority?: "high" | "low" | "auto";
}

export function ResponsiveImage({
  asset,
  alt,
  pictureClassName,
  imgClassName,
  fetchPriority,
  ...imgProps
}: ResponsiveImageProps) {
  return (
    <picture className={pictureClassName}>
      {asset.webpSrcSet && (
        <source
          type="image/webp"
          srcSet={asset.webpSrcSet}
          sizes={asset.sizes}
        />
      )}
      {asset.webpSrc && !asset.webpSrcSet && (
        <source
          type="image/webp"
          srcSet={asset.webpSrc}
          sizes={asset.sizes}
        />
      )}
      <img
        {...imgProps}
        {...(fetchPriority ? { fetchpriority: fetchPriority } : {})}
        src={asset.src}
        srcSet={asset.srcSet}
        sizes={asset.sizes}
        alt={alt}
        className={imgClassName}
      />
    </picture>
  );
}
