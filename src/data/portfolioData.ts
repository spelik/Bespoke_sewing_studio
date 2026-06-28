import type { PortfolioItem, PublicPortfolioCategory, ResponsiveImageAsset } from "../app/types";
import { PORTFOLIO_IMAGES } from "./imageAssets";

export const PORTFOLIO_CATEGORIES: ReadonlyArray<PublicPortfolioCategory> = [
  { id: "fallback-tailoring", slug: "tailoring", name: "Tailoring", description: null, displayOrder: 0 },
  { id: "fallback-dressmaking", slug: "dressmaking", name: "Dressmaking", description: null, displayOrder: 1 },
  { id: "fallback-alterations", slug: "alterations", name: "Alterations", description: null, displayOrder: 2 },
  { id: "fallback-memory-bears", slug: "memory-bears", name: "Memory Bears", description: null, displayOrder: 3 },
];

const category = (name: string) => PORTFOLIO_CATEGORIES.find((item) => item.name === name)!;

const portfolioEntries: ReadonlyArray<readonly [string, string, string, ResponsiveImageAsset]> = [
  ["1", "Custom Dressmaking", "Dressmaking", PORTFOLIO_IMAGES.img1a],
  ["2", "Memory Bear Keepsake", "Memory Bears", PORTFOLIO_IMAGES.imgA1],
  ["3", "Tailored Outfit", "Tailoring", PORTFOLIO_IMAGES.img1b],
  ["4", "Bespoke Memory Bear", "Memory Bears", PORTFOLIO_IMAGES.imgA2],
  ["5", "Bridal Alterations", "Alterations", PORTFOLIO_IMAGES.img2],
  ["6", "Handcrafted Bear", "Memory Bears", PORTFOLIO_IMAGES.imgA4],
  ["7", "Evening Wear", "Dressmaking", PORTFOLIO_IMAGES.img4],
  ["8", "Elegant Dress", "Dressmaking", PORTFOLIO_IMAGES.img5],
];

export const PORTFOLIO_ITEMS: ReadonlyArray<PortfolioItem> = portfolioEntries.map(([id, title, categoryName, imageAsset], index) => ({
  id: `fallback-${id}`,
  slug: null,
  title,
  shortDescription: null,
  description: null,
  category: category(categoryName),
  imageUrl: "",
  imageAsset,
  altText: title,
  isFeatured: index < 3,
  displayOrder: index,
}));
