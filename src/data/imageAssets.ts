import type { ResponsiveImageAsset } from "../app/types";
import aboutHero768Jpg from "../assets/images/optimized/about-hero-768.jpg";
import aboutHero1280Jpg from "../assets/images/optimized/about-hero-1280.jpg";
import aboutHero1920Jpg from "../assets/images/optimized/about-hero-1920.jpg";
import aboutHero768Webp from "../assets/images/optimized/about-hero-768.webp";
import aboutHero1280Webp from "../assets/images/optimized/about-hero-1280.webp";
import aboutHero1920Webp from "../assets/images/optimized/about-hero-1920.webp";
import homeHero768Jpg from "../assets/images/optimized/home-hero-768.jpg";
import homeHero1280Jpg from "../assets/images/optimized/home-hero-1280.jpg";
import homeHero1920Jpg from "../assets/images/optimized/home-hero-1920.jpg";
import homeHero768Webp from "../assets/images/optimized/home-hero-768.webp";
import homeHero1280Webp from "../assets/images/optimized/home-hero-1280.webp";
import homeHero1920Webp from "../assets/images/optimized/home-hero-1920.webp";
import portfolio1aJpg from "../assets/images/optimized/portfolio-1a-960.jpg";
import portfolio1aWebp from "../assets/images/optimized/portfolio-1a-960.webp";
import portfolio1bJpg from "../assets/images/optimized/portfolio-1b-957.jpg";
import portfolio1bWebp from "../assets/images/optimized/portfolio-1b-957.webp";
import portfolio2Jpg from "../assets/images/optimized/portfolio-2-960.jpg";
import portfolio2Webp from "../assets/images/optimized/portfolio-2-960.webp";
import portfolio4Jpg from "../assets/images/optimized/portfolio-4-960.jpg";
import portfolio4Webp from "../assets/images/optimized/portfolio-4-960.webp";
import portfolio5Jpg from "../assets/images/optimized/portfolio-5-960.jpg";
import portfolio5Webp from "../assets/images/optimized/portfolio-5-960.webp";
import portfolioA1Jpg from "../assets/images/optimized/portfolio-a1-960.jpg";
import portfolioA1Webp from "../assets/images/optimized/portfolio-a1-960.webp";
import portfolioA2Jpg from "../assets/images/optimized/portfolio-a2-576.jpg";
import portfolioA2Webp from "../assets/images/optimized/portfolio-a2-576.webp";
import portfolioA4Jpg from "../assets/images/optimized/portfolio-a4-922.jpg";
import portfolioA4Webp from "../assets/images/optimized/portfolio-a4-922.webp";
import headerLogo from "../imports/logo-1.png";

const buildSrcSet = (sources: ReadonlyArray<[string, number]>) =>
  sources.map(([src, width]) => `${src} ${width}w`).join(", ");

const heroSizes = "(min-width: 1024px) 42vw, 100vw";
const portfolioSizes = "(min-width: 1024px) 30vw, (min-width: 640px) 48vw, 100vw";

const createHeroAsset = (
  jpgSources: ReadonlyArray<[string, number]>,
  webpSources: ReadonlyArray<[string, number]>,
): ResponsiveImageAsset => {
  if (jpgSources.length === 0 || webpSources.length === 0) {
    throw new Error("Hero image sources must include at least one fallback.");
  }

  const jpgFallback = jpgSources[jpgSources.length - 1]!;
  const webpFallback = webpSources[webpSources.length - 1]!;

  return {
    src: jpgFallback[0],
    srcSet: buildSrcSet(jpgSources),
    webpSrc: webpFallback[0],
    webpSrcSet: buildSrcSet(webpSources),
    sizes: heroSizes,
  };
};

const createCardAsset = (
  jpgSrc: string,
  webpSrc: string,
  width: number,
): ResponsiveImageAsset => ({
  src: jpgSrc,
  srcSet: `${jpgSrc} ${width}w`,
  webpSrc,
  webpSrcSet: `${webpSrc} ${width}w`,
  sizes: portfolioSizes,
});

export const HEADER_LOGO = headerLogo;

export const HOME_HERO_IMAGE = createHeroAsset(
  [
    [homeHero768Jpg, 768],
    [homeHero1280Jpg, 1280],
    [homeHero1920Jpg, 1920],
  ],
  [
    [homeHero768Webp, 768],
    [homeHero1280Webp, 1280],
    [homeHero1920Webp, 1920],
  ],
);

export const ABOUT_HERO_IMAGE = createHeroAsset(
  [
    [aboutHero768Jpg, 768],
    [aboutHero1280Jpg, 1280],
    [aboutHero1920Jpg, 1920],
  ],
  [
    [aboutHero768Webp, 768],
    [aboutHero1280Webp, 1280],
    [aboutHero1920Webp, 1920],
  ],
);

export const PORTFOLIO_IMAGES = {
  img1a: createCardAsset(portfolio1aJpg, portfolio1aWebp, 960),
  img1b: createCardAsset(portfolio1bJpg, portfolio1bWebp, 957),
  img2: createCardAsset(portfolio2Jpg, portfolio2Webp, 960),
  img4: createCardAsset(portfolio4Jpg, portfolio4Webp, 960),
  img5: createCardAsset(portfolio5Jpg, portfolio5Webp, 960),
  imgA1: createCardAsset(portfolioA1Jpg, portfolioA1Webp, 960),
  imgA2: createCardAsset(portfolioA2Jpg, portfolioA2Webp, 576),
  imgA4: createCardAsset(portfolioA4Jpg, portfolioA4Webp, 922),
} as const;
