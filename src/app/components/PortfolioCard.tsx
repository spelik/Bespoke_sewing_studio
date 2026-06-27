import { ResponsiveImage } from "./ResponsiveImage";
import type { PortfolioItem } from "../types";

interface PortfolioCardProps {
  item: PortfolioItem;
  variant?: "preview" | "gallery";
}

export function PortfolioCard({ item, variant = "preview" }: PortfolioCardProps) {
  const isPreview = variant === "preview";

  return (
    <div className={`group relative overflow-hidden bg-muted ${isPreview ? "aspect-[3/4]" : ""}`}>
      {isPreview ? (
        <ResponsiveImage
          asset={item.image}
          alt={item.title}
          pictureClassName="block w-full h-full"
          imgClassName="w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
          loading="lazy"
          decoding="async"
        />
      ) : (
        <div className="aspect-[3/4]">
          <ResponsiveImage
            asset={item.image}
            alt={item.title}
            pictureClassName="block w-full h-full"
            imgClassName="w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
            loading="lazy"
            decoding="async"
          />
        </div>
      )}
      <div
        className={`absolute inset-0 bg-foreground/0 transition-colors duration-300 flex items-end ${
          isPreview ? "group-hover:bg-foreground/45 p-5" : "group-hover:bg-foreground/50 p-6"
        }`}
      >
        <div className="translate-y-3 group-hover:translate-y-0 opacity-0 group-hover:opacity-100 transition-all duration-300">
          <p className={`text-primary-foreground font-serif font-light ${isPreview ? "text-sm" : "text-[1rem]"}`}>
            {item.title}
          </p>
          <p className={`text-[11px] mt-0.5 capitalize font-sans ${isPreview ? "text-primary-foreground/60" : "text-primary-foreground/65"}`}>
            {item.category}
          </p>
        </div>
      </div>
    </div>
  );
}
