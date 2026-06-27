import type { ImgHTMLAttributes } from "react";
import type { ResponsiveImageAsset } from "../types";

interface ResponsiveImageProps
  extends Omit<ImgHTMLAttributes<HTMLImageElement>, "src" | "srcSet" | "sizes"> {
  asset: ResponsiveImageAsset;
  pictureClassName?: string;
  imgClassName?: string;
}

export function ResponsiveImage({
  asset,
  alt,
  pictureClassName,
  imgClassName,
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
        src={asset.src}
        srcSet={asset.srcSet}
        sizes={asset.sizes}
        alt={alt}
        className={imgClassName}
      />
    </picture>
  );
}
