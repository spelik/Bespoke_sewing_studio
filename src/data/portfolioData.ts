import type { PortfolioCategory, PortfolioItem } from "../app/types";
import { PORTFOLIO_IMAGES } from "./imageAssets";

export const PORTFOLIO_CATEGORIES: ReadonlyArray<PortfolioCategory> = [
  "Tailoring",
  "Dressmaking",
  "Alterations",
  "Memory Bears",
];

export const PORTFOLIO_ITEMS: ReadonlyArray<PortfolioItem> = [
  { id: 1, title: "Custom Dressmaking", category: "Dressmaking", image: PORTFOLIO_IMAGES.img1a, size: "tall" },
  { id: 2, title: "Memory Bear Keepsake", category: "Memory Bears", image: PORTFOLIO_IMAGES.imgA1, size: "normal" },
  { id: 3, title: "Tailored Outfit", category: "Tailoring", image: PORTFOLIO_IMAGES.img1b, size: "normal" },
  { id: 4, title: "Bespoke Memory Bear", category: "Memory Bears", image: PORTFOLIO_IMAGES.imgA2, size: "tall" },
  { id: 5, title: "Bridal Alterations", category: "Alterations", image: PORTFOLIO_IMAGES.img2, size: "normal" },
  { id: 6, title: "Handcrafted Bear", category: "Memory Bears", image: PORTFOLIO_IMAGES.imgA4, size: "normal" },
  { id: 7, title: "Evening Wear", category: "Dressmaking", image: PORTFOLIO_IMAGES.img4, size: "tall" },
  { id: 8, title: "Elegant Dress", category: "Dressmaking", image: PORTFOLIO_IMAGES.img5, size: "normal" },
];
